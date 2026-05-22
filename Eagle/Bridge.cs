using System.Text;
using Core.Logging;

namespace Expr.Scripting;

public static class Bridge {
	// Edge counter: on each rising edge of input a, increment a persisted
	// counter and publish on b. Exercises the bare-pin Tcl surface and the
	// `=` shorthand for arithmetic.
	private const string EdgeCounterScript = @"
persist  n number 0  prev number 0
trigger  a

if {$a > 0 && $prev == 0} { = n $n + 1 }
set prev $a
set b    $n
";

	public static void Initialize(ILogger log) {
		ScriptedGateFactory.Create = (l, script) => new Gate(l, script);
		log?.Info?.Log("[Expr] ScriptedGateFactory registered");
	}

	public static void RunSpike(ILogger log) {
		try {
			using var gate = new Gate(log, EdgeCounterScript);

			int[] triggerSeq = { 0, 1, 1, 0, 1, 0, 0, 1, 1, 1 };
			var rendered = new StringBuilder();
			rendered.Append("[Expr] spike trace:");

			foreach (var t in triggerSeq) {
				gate.SetInput("a", t.ToString());
				if (!gate.Tick()) {
					log.Error?.Log("[Expr] spike aborted on eval failure");
					return;
				}
				var c = gate.GetOutput("b");
				rendered.Append($" a={t} -> b={c ?? "?"};");
			}

			log.Info?.Log(
				$"[Expr] {gate.PersistTypes.Count} persist; trigger={string.Join(",", gate.TriggerOn)}");
			log.Info?.Log(rendered.ToString());
			log.Info?.Log("[Expr] Eagle Enterprise Runtime Copyright (c) 2007-2012 by Joseph Mistachkin. Used with permission.");
		} catch (System.Exception ex) {
			log.Exception?.LogException(ex);
		}
	}
}

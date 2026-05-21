using System.Text;
using Core.Logging;

namespace Expr.Scripting;

public static class Bridge {
	private const string EdgeCounterScript = @"
inputs   trig number
outputs  count number
persist  n number 0  prev number 0
trigger  trig

if {$trig > 0 && $prev == 0} { incr n }
set prev  $trig
set count $n
";

	public static void RunSpike(ILogger log) {
		try {
			using var gate = new Gate(log, EdgeCounterScript);

			int[] triggerSeq = { 0, 1, 1, 0, 1, 0, 0, 1, 1, 1 };
			var rendered = new StringBuilder();
			rendered.Append("[Expr] spike trace:");

			foreach (var t in triggerSeq) {
				gate.InputValues["trig"] = t.ToString();
				if (!gate.Tick()) {
					log.Error?.Log("[Expr] spike aborted on eval failure");
					return;
				}
				gate.OutputValues.TryGetValue("count", out var c);
				rendered.Append($" trig={t} -> count={c ?? "?"};");
			}

			log.Info?.Log(
				$"[Expr] discovered {gate.Inputs.Count} input(s), {gate.Outputs.Count} output(s), " +
				$"{gate.PersistTypes.Count} persist; trigger={string.Join(",", gate.TriggerOn)}");
			log.Info?.Log(rendered.ToString());
			log.Info?.Log("[Expr] Eagle Enterprise Runtime Copyright (c) 2007-2012 by Joseph Mistachkin. Used with permission.");
		} catch (System.Exception ex) {
			log.Exception?.LogException(ex);
		}
	}
}

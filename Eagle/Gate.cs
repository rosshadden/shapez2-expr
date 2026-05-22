using System;
using System.Collections.Generic;
using Core.Logging;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Expr.Scripting;

public class Gate : IScriptedGate {
	public sealed class PortInfo {
		public string Name;
		public string TypeName;
	}

	public readonly Dictionary<string, string> PersistTypes = new();
	public readonly List<string> TriggerOn = new();
	public readonly Dictionary<string, string> PersistValues = new();

	private readonly ILogger _log;
	private Interpreter _interp;
	private string _script;
	private readonly Dictionary<string, string> _inputs = new();
	private readonly Dictionary<string, string> _prevTriggerInputs = new();

	public Gate(ILogger log, string script) {
		_log = log;
		LoadScript(script);
	}

	public void LoadScript(string script) {
		_script = script ?? "";
		_interp?.Dispose();

		Result createResult = null;
		// NoLibraryUse skips both the standard library (init.eagle) and shell
		// library — neither is shipped with our bundled Eagle.dll, and core Tcl
		// commands (set, if, expr, proc) are baked into the interpreter, so we
		// don't need them.
		_interp = Interpreter.Create(
			args: null,
			createFlags: CreateFlags.Default | CreateFlags.NoLibraryUse,
			hostCreateFlags: HostCreateFlags.Default,
			ref createResult);
		if (_interp == null)
			throw new InvalidOperationException("Eagle Interpreter.Create failed: " + createResult);

		// Reset declarations — script may have changed which pins it wants to trigger on.
		PersistTypes.Clear();
		PersistValues.Clear();
		TriggerOn.Clear();
		_prevTriggerInputs.Clear();

		long token = 0;
		Result addResult = null;
		_interp.AddIExecute("persist", new PersistCmd(this), null, ref token, ref addResult);
		_interp.AddIExecute("trigger", new TriggerCmd(this), null, ref token, ref addResult);

		// `=` shorthand: `= b $a + 1` ≡ `set b [expr {$a + 1}]`. Treat args as a
		// numeric expression. For string assignment, use `set` as normal.
		Result preludeResult = null;
		_interp.EvaluateScript(
			"proc = {name args} { upvar 1 $name v; set v [uplevel 1 [list expr [join $args]]] }",
			ref preludeResult);
	}

	public void SetInput(string label, string value) {
		_inputs[label] = value ?? "";
	}

	// Pulled lazily from the interpreter — the simulation only asks for the
	// labels it physically has wired up. Pins are bare Tcl variables now,
	// not array elements: `set b 42` from script → we read $b here.
	public string GetOutput(string label) {
		if (_interp == null) return null;
		Result v = null, err = null;
		return _interp.GetVariableValue(label, ref v, ref err) == ReturnCode.Ok
			? v?.ToString()
			: null;
	}

	public bool Tick() {
		if (ShouldSkip()) return true;

		Result setErr = null;
		foreach (var kv in _inputs)
			_interp.SetVariableValue(kv.Key, kv.Value, ref setErr);
		foreach (var kv in PersistValues)
			_interp.SetVariableValue(kv.Key, kv.Value, ref setErr);

		Result evalResult = null;
		var rc = _interp.EvaluateScript(_script, ref evalResult);
		if (rc != ReturnCode.Ok) {
			var preview = _script.Length > 120 ? _script.Substring(0, 120) + "..." : _script;
			_log?.Error?.Log($"[Expr] Eagle eval failed: {evalResult} | script: {preview}");
			return false;
		}

		Result v = null, err = null;
		foreach (var kv in PersistTypes) {
			v = null; err = null;
			if (_interp.GetVariableValue(kv.Key, ref v, ref err) == ReturnCode.Ok)
				PersistValues[kv.Key] = v?.ToString();
		}

		if (TriggerOn.Count > 0) {
			_prevTriggerInputs.Clear();
			foreach (var label in TriggerOn) {
				_inputs.TryGetValue(label, out var cur);
				_prevTriggerInputs[label] = cur ?? "";
			}
		}
		return true;
	}

	private bool ShouldSkip() {
		if (TriggerOn.Count == 0) return false;
		// First eval always runs (snapshot below populates _prevTriggerInputs).
		if (_prevTriggerInputs.Count == 0) return false;
		foreach (var label in TriggerOn) {
			_inputs.TryGetValue(label, out var cur);
			_prevTriggerInputs.TryGetValue(label, out var prev);
			if ((cur ?? "") != (prev ?? "")) return false;
		}
		return true;
	}

	public void Dispose() => _interp?.Dispose();

	private sealed class PersistCmd : IExecute {
		private readonly Gate _g;
		public PersistCmd(Gate g) => _g = g;

		public ReturnCode Execute(Interpreter interpreter, IClientData clientData, ArgumentList arguments, ref Result result) {
			if (arguments.Count < 4 || ((arguments.Count - 1) % 3) != 0) {
				result = "wrong # args: should be \"persist name type init ?name type init ...?\"";
				return ReturnCode.Error;
			}
			for (int i = 1; i < arguments.Count; i += 3) {
				var name = arguments[i].ToString();
				var type = arguments[i + 1].ToString();
				var init = arguments[i + 2].ToString();
				if (_g.PersistTypes.ContainsKey(name)) continue;
				_g.PersistTypes[name] = type;
				_g.PersistValues[name] = init;
			}
			return ReturnCode.Ok;
		}
	}

	private sealed class TriggerCmd : IExecute {
		private readonly Gate _g;
		public TriggerCmd(Gate g) => _g = g;

		public ReturnCode Execute(Interpreter interpreter, IClientData clientData, ArgumentList arguments, ref Result result) {
			_g.TriggerOn.Clear();
			for (int i = 1; i < arguments.Count; i++)
				_g.TriggerOn.Add(arguments[i].ToString());
			return ReturnCode.Ok;
		}
	}
}

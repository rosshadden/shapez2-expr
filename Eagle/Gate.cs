using System;
using System.Collections.Generic;
using Core.Logging;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Expr.Scripting;

public class Gate : IDisposable {
	public sealed class PortInfo {
		public string Name;
		public string TypeName;
	}

	public readonly List<PortInfo> Inputs = new();
	public readonly List<PortInfo> Outputs = new();
	public readonly Dictionary<string, string> PersistTypes = new();
	public readonly List<string> TriggerOn = new();

	public readonly Dictionary<string, string> InputValues = new();
	public readonly Dictionary<string, string> OutputValues = new();
	public readonly Dictionary<string, string> PersistValues = new();

	private readonly ILogger _log;
	private readonly Interpreter _interp;
	private readonly string _script;

	public Gate(ILogger log, string script) {
		_log = log;
		_script = script;

		Result createResult = null;
		_interp = Interpreter.Create(
			args: null,
			createFlags: CreateFlags.Default,
			hostCreateFlags: HostCreateFlags.Default,
			ref createResult);
		if (_interp == null)
			throw new InvalidOperationException("Eagle Interpreter.Create failed: " + createResult);

		long token = 0;
		Result addResult = null;
		_interp.AddIExecute("inputs",  new PortCmd(Inputs),  null, ref token, ref addResult);
		_interp.AddIExecute("outputs", new PortCmd(Outputs), null, ref token, ref addResult);
		_interp.AddIExecute("persist", new PersistCmd(this), null, ref token, ref addResult);
		_interp.AddIExecute("trigger", new TriggerCmd(this), null, ref token, ref addResult);
	}

	public bool Tick() {
		Result setErr = null;
		foreach (var p in Inputs) {
			InputValues.TryGetValue(p.Name, out var v);
			_interp.SetVariableValue(p.Name, v ?? "0", ref setErr);
		}
		foreach (var kv in PersistTypes) {
			PersistValues.TryGetValue(kv.Key, out var v);
			_interp.SetVariableValue(kv.Key, v ?? "0", ref setErr);
		}

		Result evalResult = null;
		var rc = _interp.EvaluateScript(_script, ref evalResult);
		if (rc != ReturnCode.Ok) {
			_log.Error?.Log($"[Expr] Eagle eval failed: {evalResult}");
			return false;
		}

		OutputValues.Clear();
		foreach (var p in Outputs) {
			Result v = null, err = null;
			if (_interp.GetVariableValue(p.Name, ref v, ref err) == ReturnCode.Ok)
				OutputValues[p.Name] = v?.ToString();
		}
		foreach (var kv in PersistTypes) {
			Result v = null, err = null;
			if (_interp.GetVariableValue(kv.Key, ref v, ref err) == ReturnCode.Ok)
				PersistValues[kv.Key] = v?.ToString();
		}
		return true;
	}

	public void Dispose() => _interp?.Dispose();

	private sealed class PortCmd : IExecute {
		private readonly List<PortInfo> _list;
		public PortCmd(List<PortInfo> list) => _list = list;

		public ReturnCode Execute(Interpreter interpreter, IClientData clientData, ArgumentList arguments, ref Result result) {
			if (arguments.Count < 3 || (arguments.Count % 2) == 0) {
				result = $"wrong # args: should be \"{arguments[0]} name type ?name type ...?\"";
				return ReturnCode.Error;
			}
			for (int i = 1; i < arguments.Count; i += 2) {
				var name = arguments[i].ToString();
				var type = arguments[i + 1].ToString();
				if (_list.Exists(p => p.Name == name)) continue;
				_list.Add(new PortInfo { Name = name, TypeName = type });
			}
			return ReturnCode.Ok;
		}
	}

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

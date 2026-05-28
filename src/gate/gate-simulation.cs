using System;
using NCalc;
using NCalc.Handlers;
using Game.Content.Features.Signals;
using Game.Content.Features.Signals.Conductor;
using Game.Content.Features.Signals.Connections;
using Game.Content.Features.Signals.Simulation;
using Game.Content.Features.Signals.Tick;
using Game.Core.Simulation;
using ILogger = Core.Logging.ILogger;

namespace Expr;

public class GateSimulation : Simulation<GateSimulationState>, ISignalSimulation, IUpdatableSimulation {
	public static ILogger Log;

	private static readonly string[] InputLabels = { "a", "b", "c" };

	private readonly SignalConductorInput[] _inputs;
	private readonly SignalConductorOutput _out = new();
	private readonly SignalCodec _codec;
	private Expression _expr;
	private string _loadedScript;
	private bool _loggedFirstEval;

	public GateSimulation(GateSimulationState state, SignalCodec codec) : base(state) {
		_codec = codec;
		_inputs = new[] {
			new SignalConductorInput(state.In0),
			new SignalConductorInput(state.In1),
			new SignalConductorInput(state.In2),
		};
	}

	public int NumSignalProviders => 1;
	public int NumSignalReceivers => 3;
	public ISignalProvider GetSignalProvider(int i) => _out;
	public ISignalReceiver GetSignalReceiver(int i) => _inputs[i];

	public void Update(Ticks startTicks, Ticks deltaTicks) {
		int n = SignalSimulation.GetAmountOfSignalsThisUpdate(startTicks, deltaTicks);
		if (n <= 0) return;

		EnsureExpressionLoaded();

		var baseTick = SignalTicks.FromTicks(startTicks);
		for (int s = 0; s < n; s++) {
			var tick = baseTick + new SignalTicks(s);

			ISignal[] sigs = new ISignal[3];
			for (int i = 0; i < 3; i++)
				_inputs[i].TryPopSignal(startTicks, tick, out sigs[i]);

			ISignal output;
			if (_expr != null) {
				_expr.Parameters["tick"] = State.Tick++;
				for (int i = 0; i < InputLabels.Length; i++)
					_expr.Parameters[InputLabels[i]] = _codec.ToObject(sigs[i]);
				try {
					var result = _expr.Evaluate();
					output = _codec.FromObject(result);
					if (!_loggedFirstEval) {
						_loggedFirstEval = true;
						Log?.Info?.Log($"[Expr] first eval ok -> {result}");
					}
				} catch (Exception ex) {
					if (!_loggedFirstEval) {
						_loggedFirstEval = true;
						Log?.Error?.Log($"[Expr] eval error ({ex.GetType().Name}): {ex.Message}");
					}
					output = NullSignal.Instance;
				}
			} else {
				output = sigs[0] ?? NullSignal.Instance;
			}
			_out.PushSignal(output, startTicks, tick);
		}
	}

	private void EnsureExpressionLoaded() {
		var script = State.Script ?? "";
		if (script == _loadedScript) return;
		_loadedScript = script;
		_loggedFirstEval = false;

		if (string.IsNullOrWhiteSpace(script)) {
			_expr = null;
			return;
		}

		var expr = script.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();
		try {
			var e = new Expression(expr);
			e.EvaluateFunction += EvalFunction;
			_expr = e;
		} catch (Exception ex) {
			Log?.Error?.Log($"[Expr] parse error: {ex.Message}");
			_expr = null;
		}
	}

	private void EvalFunction(string name, FunctionArgs args) {
		switch (name.ToLower()) {
			// util
			case "isnull":
				if (args.Parameters.Length == 1)
					args.Result = args.Parameters[0].Evaluate() == null;
				break;
			case "isshape":
				if (args.Parameters.Length == 1)
					args.Result = _codec.Decode(args.Parameters[0].Evaluate()?.ToString() ?? "") is BeltItemSignal;
				break;
			case "iscolor":
				if (args.Parameters.Length == 1)
					args.Result = _codec.Decode(args.Parameters[0].Evaluate()?.ToString() ?? "") is FluidSignal;
				break;

			// shape operations
			case "rotate":
				if (args.Parameters.Length >= 1 && args.Parameters[0].Evaluate() is string rot)
					args.Result = ShapeRotate(rot);
				break;
			case "paint":
				if (args.Parameters.Length >= 2
					&& args.Parameters[0].Evaluate() is string paintShape
					&& args.Parameters[1].Evaluate() is string paintColor)
					args.Result = ShapePaint(paintShape, paintColor);
				break;
			case "stack":
				if (args.Parameters.Length >= 2
					&& args.Parameters[0].Evaluate() is string stackBot
					&& args.Parameters[1].Evaluate() is string stackTop)
					args.Result = stackBot + ":" + stackTop;
				break;
			case "layer":
				if (args.Parameters.Length >= 2
					&& args.Parameters[0].Evaluate() is string layHash)
					args.Result = ShapeLayer(layHash, Convert.ToInt32(args.Parameters[1].Evaluate()));
				break;
			case "quadrant":
				if (args.Parameters.Length >= 2
					&& args.Parameters[0].Evaluate() is string quadHash)
					args.Result = ShapeQuadrant(quadHash, Convert.ToInt32(args.Parameters[1].Evaluate()));
				break;

			// string operations
			case "len":
				if (args.Parameters.Length == 1)
					args.Result = (args.Parameters[0].Evaluate()?.ToString() ?? "").Length;
				break;
			case "substr":
				if (args.Parameters.Length >= 3) {
					var s = args.Parameters[0].Evaluate()?.ToString() ?? "";
					var start = Convert.ToInt32(args.Parameters[1].Evaluate());
					var n = Convert.ToInt32(args.Parameters[2].Evaluate());
					args.Result = start >= 0 && start < s.Length
						? s.Substring(start, Math.Min(n, s.Length - start))
						: "";
				}
				break;
			case "concat": {
				var parts = new string[args.Parameters.Length];
				for (int i = 0; i < args.Parameters.Length; i++)
					parts[i] = args.Parameters[i].Evaluate()?.ToString() ?? "";
				args.Result = string.Concat(parts);
				break;
			}

			// persistent memory (per-gate, resets on game load)
			case "get":
				if (args.Parameters.Length == 1) {
					var key = args.Parameters[0].Evaluate()?.ToString() ?? "";
					State.Vars.TryGetValue(key, out var val);
					args.Result = val;
				}
				break;
			case "set":
				if (args.Parameters.Length == 2) {
					var key = args.Parameters[0].Evaluate()?.ToString() ?? "";
					var val = args.Parameters[1].Evaluate();
					State.Vars[key] = val;
					args.Result = val;
				}
				break;
		}
	}

	// Rotate shape CW: quadrant order [0,1,2,3] -> [3,0,1,2]
	private static string ShapeRotate(string hash) {
		var layers = hash.Split(':');
		for (int l = 0; l < layers.Length; l++) {
			var lay = layers[l];
			if (lay.Length == 8)
				layers[l] = lay.Substring(6, 2) + lay.Substring(0, 6);
		}
		return string.Join(":", layers);
	}

	// Paint all non-empty quadrants with the given single-char color code
	private static string ShapePaint(string hash, string colorCode) {
		if (colorCode == null || colorCode.Length != 1) return hash;
		char color = colorCode[0];
		var layers = hash.Split(':');
		for (int l = 0; l < layers.Length; l++) {
			var chars = layers[l].ToCharArray();
			for (int q = 0; q < 4 && q * 2 + 1 < chars.Length; q++) {
				if (chars[q * 2] != '-')
					chars[q * 2 + 1] = color;
			}
			layers[l] = new string(chars);
		}
		return string.Join(":", layers);
	}

	private static string ShapeLayer(string hash, int n) {
		var layers = hash.Split(':');
		return n >= 0 && n < layers.Length ? layers[n] : "";
	}

	// Returns the 2-char quadrant string (e.g. "Cu") for quadrant n of layer 0
	private static string ShapeQuadrant(string hash, int n) {
		var layer = hash.Split(':')[0];
		int start = n * 2;
		return start + 1 < layer.Length ? layer.Substring(start, 2) : "--";
	}
}

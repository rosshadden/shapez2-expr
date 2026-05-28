using System;
using NCalc;
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
			_expr = new Expression(expr);
		} catch (Exception ex) {
			Log?.Error?.Log($"[Expr] parse error: {ex.Message}");
			_expr = null;
		}
	}
}

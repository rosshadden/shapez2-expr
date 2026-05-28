using Game.Content.Features.Signals;
using Game.Content.Features.Signals.Conductor;
using Game.Content.Features.Signals.Connections;
using Game.Content.Features.Signals.Simulation;
using Game.Content.Features.Signals.Tick;
using Game.Core.Simulation;

namespace Expr;

// Reads one wire input and stores the encoded signal text in state so the
// renderer can display it without accessing the simulation on every draw.
public class MonitorSimulation : Simulation<MonitorSimulationState>, ISignalSimulation, IUpdatableSimulation {
	private readonly SignalConductorInput _input;
	private readonly SignalCodec _codec;

	public string DisplayText => State.DisplayText;

	public MonitorSimulation(MonitorSimulationState state, SignalCodec codec) : base(state) {
		_codec = codec;
		_input = new SignalConductorInput(state.In0);
	}

	public int NumSignalProviders => 0;
	public int NumSignalReceivers => 1;
	public ISignalProvider GetSignalProvider(int i) => throw new System.InvalidOperationException();
	public ISignalReceiver GetSignalReceiver(int i) => _input;

	public void Update(Ticks startTicks, Ticks deltaTicks) {
		int n = SignalSimulation.GetAmountOfSignalsThisUpdate(startTicks, deltaTicks);
		if (n <= 0) return;

		var baseTick = SignalTicks.FromTicks(startTicks);
		for (int s = 0; s < n; s++) {
			var tick = baseTick + new SignalTicks(s);
			_input.TryPopSignal(startTicks, tick, out var sig);
			State.DisplayText = _codec.Encode(sig);
		}
	}
}

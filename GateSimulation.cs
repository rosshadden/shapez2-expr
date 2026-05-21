using Game.Content.Features.Signals;
using Game.Content.Features.Signals.Conductor;
using Game.Content.Features.Signals.Connections;
using Game.Content.Features.Signals.Simulation;
using Game.Content.Features.Signals.Tick;
using Game.Core.Simulation;

namespace Expr;

// Stub simulation: 1 wire input (west), 1 wire output (east).
// Passes whatever signal is received straight through.
// Eagle integration wires in later.
public class GateSimulation : Simulation<GateSimulationState>, ISignalSimulation, IUpdatableSimulation {
	private readonly SignalConductorInput _in;
	private readonly SignalConductorOutput _out = new();

	public GateSimulation(GateSimulationState state) : base(state) {
		_in = new SignalConductorInput(state.In0);
	}

	public int NumSignalProviders => 1;
	public int NumSignalReceivers => 1;
	public ISignalProvider GetSignalProvider(int i) => _out;
	public ISignalReceiver GetSignalReceiver(int i) => _in;

	public void Update(Ticks startTicks, Ticks deltaTicks) {
		int n = SignalSimulation.GetAmountOfSignalsThisUpdate(startTicks, deltaTicks);
		if (n <= 0) return;
		var baseTick = SignalTicks.FromTicks(startTicks);
		for (int s = 0; s < n; s++) {
			var tick = baseTick + new SignalTicks(s);
			_in.TryPopSignal(startTicks, tick, out var sig);
			_out.PushSignal(sig ?? NullSignal.Instance, startTicks, tick);
		}
	}
}

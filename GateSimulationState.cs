using Game.Content.Features.Signals.Conductor;
using Game.Core.Serialization;
using Game.Core.Simulation;

namespace Expr;

[SyncableIdentifier("ExprGateState")]
public class GateSimulationState : ISimulationState {
	public readonly SignalConductorInputState In0 = new();
	public string Script = "";

	public void Sync(ISerializationVisitor visitor) {
		In0.Sync(visitor);
		visitor.SyncString_4(ref Script);
	}
}

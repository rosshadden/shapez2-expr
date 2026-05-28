using Game.Content.Features.Signals.Conductor;
using Game.Core.Serialization;
using Game.Core.Simulation;

namespace Expr;

[SyncableIdentifier("ExprMonitorState")]
public class MonitorSimulationState : ISimulationState {
	public readonly SignalConductorInputState In0 = new();
	public string DisplayText = "";

	public void Sync(ISerializationVisitor visitor) {
		In0.Sync(visitor);
		visitor.SyncString_4(ref DisplayText);
	}
}

namespace Expr;

// No dynamic rendering — gate uses the static copied mesh from the NOT gate.
// OnDrawDynamic is virtual with a no-op default in the base class.
public class GateRenderer : StatelessBuildingSimulationRenderer<GateSimulation, GateDrawData> {
	public GateRenderer(IMapModel map) : base(map) { }
}

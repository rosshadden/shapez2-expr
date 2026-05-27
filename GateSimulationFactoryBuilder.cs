using Core.Factory;
using ShapezShifter.Flow.Atomic;
using ShapezShifter.Hijack;

namespace Expr;

public class GateSimulationFactoryBuilder
	: IBuildingSimulationFactoryBuilder<GateSimulation, GateSimulationState, EmptyCustomSimulationConfiguration> {
	private readonly int _inputCount;

	public GateSimulationFactoryBuilder(int inputCount) {
		_inputCount = inputCount;
	}

	public IFactory<GateSimulationState, GateSimulation> BuildFactory(
		SimulationSystemsDependencies deps,
		out EmptyCustomSimulationConfiguration config) {
		config = new EmptyCustomSimulationConfiguration();
		var codec = new SignalCodec(deps.ShapeRegistry, deps.ShapeIdManager, deps.Mode.ShapeColorScheme);
		return new LambdaFactory<GateSimulationState, GateSimulation>(s => new GateSimulation(s, codec, _inputCount));
	}
}

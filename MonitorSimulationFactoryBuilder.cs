using Core.Factory;
using ShapezShifter.Flow.Atomic;
using ShapezShifter.Hijack;

namespace Expr;

public class MonitorSimulationFactoryBuilder
	: IBuildingSimulationFactoryBuilder<MonitorSimulation, MonitorSimulationState, EmptyCustomSimulationConfiguration> {
	public IFactory<MonitorSimulationState, MonitorSimulation> BuildFactory(
		SimulationSystemsDependencies deps,
		out EmptyCustomSimulationConfiguration config) {
		config = new EmptyCustomSimulationConfiguration();

		// Capture the Display building's text-rendering draw data once so the
		// MonitorRenderer can borrow its material and font without storing them
		// per-building. We go through the group to avoid guessing the definition ID.
		if (MonitorTextResources.DisplayDrawData == null
				&& deps.Mode.Buildings.TryGetDefinitionGroup(
					new BuildingDefinitionGroupId("DisplayDefaultVariant"), out var grp)) {
			var concrete = grp as BuildingDefinitionGroup;
			if (concrete?.Definitions?.Count > 0)
				MonitorTextResources.DisplayDrawData =
					concrete.Definitions[0].CustomDrawDataAs<DisplayMetaBuildingDefinition.DrawData>();
		}

		var codec = new SignalCodec(deps.ShapeRegistry, deps.ShapeIdManager, deps.Mode.ShapeColorScheme);
		return new LambdaFactory<MonitorSimulationState, MonitorSimulation>(s => new MonitorSimulation(s, codec));
	}
}

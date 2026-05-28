using System;
using System.Collections.Generic;

namespace Expr;

public class MonitorBuildingModules : IBuildingModules {
	public IEnumerable<IHUDSidePanelModuleData> GetInfoModules(IMapModel map, BuildingModel building) =>
		Array.Empty<IHUDSidePanelModuleData>();

	public IEnumerable<IHUDSidePanelModuleData> GetInfoModules(IBuildingDefinition definition) =>
		Array.Empty<IHUDSidePanelModuleData>();
}

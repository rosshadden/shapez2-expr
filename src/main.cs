using System;
using System.IO;
using Core.Localization;
using Game.Core.Coordinates;
using ShapezShifter.Flow;
using ShapezShifter.Flow.Atomic;
using ShapezShifter.Flow.ConnectorData;
using ShapezShifter.Flow.Research;
using ShapezShifter.Flow.Toolbar;
using ShapezShifter.Hijack;
using ShapezShifter.Kit;
using ShapezShifter.Textures;
using UnityEngine;
using ILogger = Core.Logging.ILogger;

namespace Expr;

public class Main : IMod {
	internal static readonly ModFolderLocator Res = ModDirectoryLocator.CreateLocator<Main>().SubLocator("Resources");

	public Main(ILogger logger) {
		try {
			DialogStackHolder.Install();
			ConfirmKeySuppressor.Install();
			SignalSerializerPatch.Install();
		} catch (Exception ex) {
			logger.Exception?.LogException(ex);
		}

		GateBuildingModules.Log = logger;
		GateSimulation.Log = logger;
		RegisterBuildings(logger);
	}

	private static void RegisterBuildings(ILogger logger) {
		var gateIcon = LoadIcon("ExprGateIcon.png", MakeGateIcon);
		var monitorIcon = LoadIcon("ExprMonitorIcon.png", MakeMonitorIcon);
		var toolbar = new ExprToolbarGroup(gateIcon);

		var gateDefId = new BuildingDefinitionId("ExprGate");
		var gateGroupId = new BuildingDefinitionGroupId("ExprGateGroup");

		// West = a (receiver 0), North = b (receiver 1), South = c (receiver 2).
		// AddWireInput order determines GetSignalReceiver index order.
		var gateConnectors = BuildingConnectors.SingleTile()
			.AddWireOutput(WireConnectorConfig.DefaultOutput())
			.AddWireInput(WireConnectorConfig.CustomInput(TileDirection.West))
			.AddWireInput(WireConnectorConfig.CustomInput(TileDirection.North))
			.AddWireInput(WireConnectorConfig.CustomInput(TileDirection.South))
			.Build();

		var gateBuilding = Building.Create(gateDefId)
			.WithConnectorData(gateConnectors)
			.DynamicallyRendering<GateRenderer, GateSimulation, GateDrawData>(new GateDrawData())
			.WithCopiedStaticDrawData(new BuildingDefinitionId("LogicGateOrInternalVariant"))
			.WithoutSound()
			.WithoutSimulationConfiguration()
			.WithoutEfficiencyData();

		var gateGroup = BuildingGroup.Create(gateGroupId)
			.WithTitle(new Core.Localization.RawText("Expr Gate"))
			.WithDescription(new Core.Localization.RawText("Evaluates an NCalc expression. Inputs: West=a, North=b, South=c."))
			.WithIcon(gateIcon)
			.AsNonTransportableBuilding()
			.WithPreferredPlacement(DefaultPreferredPlacementMode.Single)
			.WithDefaultStructureOverview();

		AtomicBuildings.Extend()
			.AllScenarios()
			.WithBuilding(gateBuilding, gateGroup)
			.UnlockedAtMilestone(new ByIndexMilestoneSelector(0))
			.WithDefaultPlacement()
			.InToolbar(toolbar)
			.WithSimulation(new GateSimulationFactoryBuilder(), logger)
			.WithCustomModules(new GateBuildingModules())
			.WithoutPrediction()
			.Build();

		logger.Info?.Log("[Expr] ExprGate registered");

		var monitorDefId = new BuildingDefinitionId("ExprMonitor");
		var monitorGroupId = new BuildingDefinitionGroupId("ExprMonitorGroup");

		var monitorConnectors = BuildingConnectors.SingleTile()
			.AddWireInput(WireConnectorConfig.CustomInput(TileDirection.West))
			.Build();

		var monitorBuilding = Building.Create(monitorDefId)
			.WithConnectorData(monitorConnectors)
			.DynamicallyRendering<MonitorRenderer, MonitorSimulation, MonitorDrawData>(new MonitorDrawData())
			.WithCopiedStaticDrawData(new BuildingDefinitionId("DisplayDefaultInternalVariant"))
			.WithoutSound()
			.WithoutSimulationConfiguration()
			.WithoutEfficiencyData();

		var monitorGroup = BuildingGroup.Create(monitorGroupId)
			.WithTitle(new Core.Localization.RawText("Expr Monitor"))
			.WithDescription(new Core.Localization.RawText("Displays the wire signal as text. Input: West."))
			.WithIcon(monitorIcon)
			.AsNonTransportableBuilding()
			.WithPreferredPlacement(DefaultPreferredPlacementMode.Single)
			.WithDefaultStructureOverview();

		AtomicBuildings.Extend()
			.AllScenarios()
			.WithBuilding(monitorBuilding, monitorGroup)
			.UnlockedAtMilestone(new ByIndexMilestoneSelector(0))
			.WithDefaultPlacement()
			.InToolbar(toolbar)
			.WithSimulation(new MonitorSimulationFactoryBuilder(), logger)
			.WithCustomModules(new MonitorBuildingModules())
			.WithoutPrediction()
			.Build();

		logger.Info?.Log("[Expr] ExprMonitor registered");
	}

	private static Sprite LoadIcon(string fileName, Func<Sprite> fallback) {
		try {
			var path = Res.SubPath(fileName);
			if (File.Exists(path))
				return FileTextureLoader.LoadTextureAsSprite(path, out _);
		} catch {}
		return fallback();
	}

	// Simple "fx" icon: white background with a dark "f(x)"
	// TODO: not working right
	private static Sprite MakeGateIcon() {
		var tex = new Texture2D(8, 8);
		var px = new Color[64];
		var bg = new Color(0.15f, 0.15f, 0.2f);
		var fg = Color.white;
		Array.Fill(px, bg);
		// top-left cluster (inputs)
		px[7 * 8 + 1] = fg; px[7 * 8 + 2] = fg;
		px[6 * 8 + 1] = fg;
		// bottom-right (output)
		px[0 * 8 + 6] = fg; px[0 * 8 + 7] = fg;
		px[1 * 8 + 7] = fg;
		tex.SetPixels(px);
		tex.Apply();
		return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f));
	}

	// Simple "screen" icon: bright border with dark center.
	private static Sprite MakeMonitorIcon() {
		var tex = new Texture2D(8, 8);
		var px = new Color[64];
		var border = new Color(0.8f, 0.8f, 0.85f);
		var inner = new Color(0.1f, 0.1f, 0.15f);
		// fill inner
		for (int y = 0; y < 8; y++)
			for (int x = 0; x < 8; x++)
				px[y * 8 + x] = (x == 0 || x == 7 || y == 0 || y == 7) ? border : inner;
		// blinking cursor dot in center
		px[3 * 8 + 3] = border;
		tex.SetPixels(px);
		tex.Apply();
		return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f));
	}

	public void Dispose() {}
}

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
		} catch (Exception ex) {
			logger.Exception?.LogException(ex);
		}

		GateBuildingModules.Log = logger;
		GateSimulation.Log = logger;
		RegisterBuildings(logger);
	}

	private static void RegisterBuildings(ILogger logger) {
		var icon = LoadIcon();

		// Insert the Expr submenu group first so the building's ToolbarRewirer can
		// navigate into it. The group lands at Root→wires(2)→last→after, which
		// makes it the new last child of the wires tab (index ^1 from then on).
		GameRewirers.AddRewirer(new ExprToolbarGroup(icon));

		var defId = new BuildingDefinitionId("ExprGate");
		var groupId = new BuildingDefinitionGroupId("ExprGateGroup");

		// West = a (receiver 0), North = b (receiver 1), South = c (receiver 2).
		// AddWireInput order determines GetSignalReceiver index order.
		var connectors = BuildingConnectors.SingleTile()
			.AddWireOutput(WireConnectorConfig.DefaultOutput())
			.AddWireInput(WireConnectorConfig.CustomInput(TileDirection.West))
			.AddWireInput(WireConnectorConfig.CustomInput(TileDirection.North))
			.AddWireInput(WireConnectorConfig.CustomInput(TileDirection.South))
			.Build();

		var building = Building.Create(defId)
			.WithConnectorData(connectors)
			.DynamicallyRendering<GateRenderer, GateSimulation, GateDrawData>(new GateDrawData())
			.WithCopiedStaticDrawData(new BuildingDefinitionId("LogicGateOrInternalVariant"))
			.WithoutSound()
			.WithoutSimulationConfiguration()
			.WithoutEfficiencyData();

		var group = BuildingGroup.Create(groupId)
			.WithTitle(new Core.Localization.RawText("Expression Gate"))
			.WithDescription(new Core.Localization.RawText("Evaluates an NCalc expression. Inputs: West=a, North=b, South=c."))
			.WithIcon(icon)
			.AsNonTransportableBuilding()
			.WithPreferredPlacement(DefaultPreferredPlacementMode.Single)
			.WithDefaultStructureOverview();

		// Place inside the Expr group: Root→wires(2)→last(^1, now the Expr group)→first slot.
		AtomicBuildings.Extend()
			.AllScenarios()
			.WithBuilding(building, group)
			.UnlockedAtMilestone(new ByIndexMilestoneSelector(0))
			.WithDefaultPlacement()
			.InToolbar(ToolbarElementLocator.Root().ChildAt(2).ChildAt(^1).ChildAt(0).InsertBefore())
			.WithSimulation(new GateSimulationFactoryBuilder(), logger)
			.WithCustomModules(new GateBuildingModules())
			.WithoutPrediction()
			.Build();

		logger.Info?.Log("[Expr] ExprGate registered");
	}

	private static Sprite LoadIcon() {
		try {
			var path = Res.SubPath("ExprGateIcon.png");
			if (File.Exists(path))
				return FileTextureLoader.LoadTextureAsSprite(path, out _);
		} catch { }
		var tex = new Texture2D(8, 8);
		var pixels = new Color[64];
		Array.Fill(pixels, Color.white);
		tex.SetPixels(pixels);
		tex.Apply();
		return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f));
	}

	public void Dispose() { }
}

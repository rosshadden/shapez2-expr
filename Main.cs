using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Localization;
using Game.Content.Features.Signals.Connections;
using Game.Core.Coordinates;
using Game.Core.Map.Simulation;
using ShapezShifter.Flow;
using ShapezShifter.Flow.Atomic;
using ShapezShifter.Flow.ConnectorData;
using ShapezShifter.Flow.Research;
using ShapezShifter.Flow.Toolbar;
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

		for (int n = 1; n <= 3; n++) {
			var defId = new BuildingDefinitionId($"ExprGate{n}In");
			var groupId = new BuildingDefinitionGroupId($"ExprGateGroup{n}In");

			var connectors = BuildConnectors(n);

			var building = Building.Create(defId)
				.WithConnectorData(connectors)
				.DynamicallyRendering<GateRenderer, GateSimulation, GateDrawData>(new GateDrawData())
				.WithCopiedStaticDrawData(new BuildingDefinitionId("LogicGateNotInternalVariant"))
				.WithoutSound()
				.WithoutSimulationConfiguration()
				.WithoutEfficiencyData();

			var inputList = string.Join(", ", InputLabels(n));
			var group = BuildingGroup.Create(groupId)
				.WithTitle(new RawText($"Expression Gate ({n}in)"))
				.WithDescription(new RawText($"NCalc expression. Inputs: {inputList}. Output: expression result."))
				.WithIcon(icon)
				.AsNonTransportableBuilding()
				.WithPreferredPlacement(DefaultPreferredPlacementMode.Single)
				.WithDefaultStructureOverview();

			AtomicBuildings.Extend()
				.AllScenarios()
				.WithBuilding(building, group)
				.UnlockedAtMilestone(new ByIndexMilestoneSelector(0))
				.WithDefaultPlacement()
				.InToolbar(ToolbarElementLocator.Root().ChildAt(2).ChildAt(^1).InsertAfter())
				.WithSimulation(new GateSimulationFactoryBuilder(n), logger)
				.WithCustomModules(new GateBuildingModules())
				.WithoutPrediction()
				.Build();
		}

		logger.Info?.Log("[Expr] ExprGate 1/2/3-input variants registered");
	}

	// Builds connector data for an n-input, 1-output gate.
	// Each input tile stacks northward: tile (0,0) carries input "a" and the East output;
	// tile (0,1) carries input "b"; tile (0,2) carries input "c".
	// All inputs face West; the single output faces East at tile (0,0).
	private static IBuildingConnectorData BuildConnectors(int n) {
		var ios = new List<IBuildingIO>();

		for (int i = 0; i < n; i++) {
			ios.Add(new BuildingSignalInput {
				Position_L = new TileVector { x = 0, y = i },
				TileDirection = TileDirection.West,
				_IOType = BuildingSignalIOType.Wire,
			});
		}

		ios.Add(new BuildingSignalOutput {
			Position_L = TileVector.Zero,
			TileDirection = TileDirection.East,
			_IOType = BuildingSignalIOType.Wire,
		});

		var tiles = Enumerable.Range(0, n)
			.Select(i => new TileVector { x = 0, y = i })
			.ToArray();

		var bounds = LocalTileBounds.From(TileVector.Zero, new TileVector { y = n - 1 });
		var center = LocalVector.Lerp((LocalVector)bounds.Min, (LocalVector)bounds.Max, 0.5f);

		return new BuildingConnectorData(ios, tiles, bounds, center, bounds.Dimensions);
	}

	private static string[] InputLabels(int n) {
		var labels = new string[n];
		for (int i = 0; i < n; i++) labels[i] = ((char)('a' + i)).ToString();
		return labels;
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

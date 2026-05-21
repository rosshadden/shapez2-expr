using System;
using System.IO;
using System.Reflection;
using Core.Localization;
using Core.Logging;
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

	private static readonly string ModDir = Path.GetDirectoryName(typeof(Main).Assembly.Location);
	private static ILogger _resolverLog;

	public Main(ILogger logger) {
		_resolverLog = logger;
		AppDomain.CurrentDomain.AssemblyResolve += ResolveModFolder;

		try {
			// Load Expr.Eagle.dll and run the Eagle spike to verify Eagle is functional.
			var bridgeAsm = Assembly.LoadFrom(Path.Combine(ModDir, "Expr.Eagle.dll"));
			var bridge = bridgeAsm.GetType("Expr.Scripting.Bridge");
			var runSpike = bridge.GetMethod("RunSpike", BindingFlags.Public | BindingFlags.Static);
			runSpike.Invoke(null, new object[] { logger });
		} catch (Exception ex) {
			logger.Exception?.LogException(ex);
		}

		try {
			DialogStackHolder.Install();
		} catch (Exception ex) {
			logger.Exception?.LogException(ex);
		}

		RegisterBuildings(logger);
	}

	private static void RegisterBuildings(ILogger logger) {
		var groupId = new BuildingDefinitionGroupId("ExprGateGroup");
		var defId   = new BuildingDefinitionId("ExprGate1x1");

		var connectors = BuildingConnectors.SingleTile()
			.AddWireInput(WireConnectorConfig.DefaultInput())
			.AddWireOutput(WireConnectorConfig.DefaultOutput())
			.Build();

		var building = Building.Create(defId)
			.WithConnectorData(connectors)
			.DynamicallyRendering<GateRenderer, GateSimulation, GateDrawData>(new GateDrawData())
			.WithCopiedStaticDrawData(new BuildingDefinitionId("LogicGateNotInternalVariant"))
			.WithoutSound()
			.WithoutSimulationConfiguration()
			.WithoutEfficiencyData();

		var icon = LoadIcon();

		var group = BuildingGroup.Create(groupId)
			.WithTitle(new RawText("Expression Gate"))
			.WithDescription(new RawText("Runs an Eagle/Tcl script on wire signals"))
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
		// Fallback: 8x8 white square
		var tex = new Texture2D(8, 8);
		var pixels = new Color[64];
		Array.Fill(pixels, Color.white);
		tex.SetPixels(pixels);
		tex.Apply();
		return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f));
	}

	private static Assembly ResolveModFolder(object sender, ResolveEventArgs args) {
		var simpleName = new AssemblyName(args.Name).Name;
		var path = Path.Combine(ModDir, simpleName + ".dll");
		if (!File.Exists(path)) return null;
		try {
			var asm = Assembly.LoadFrom(path);
			_resolverLog?.Info?.Log($"[Expr] resolved {simpleName} from mod folder");
			return asm;
		} catch (Exception ex) {
			_resolverLog?.Error?.Log($"[Expr] failed to load {path}: {ex.Message}");
			return null;
		}
	}

	public void Dispose() { }
}

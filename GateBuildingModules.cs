using System;
using System.Collections.Generic;
using System.Reflection;
using Core.Localization;
using UnityEngine;

namespace Expr;

public class GateBuildingModules : IBuildingModules {
	private static readonly FieldInfo DialogInputFieldField =
		typeof(HUDDialogSimpleInput).GetField("UIInputField", BindingFlags.NonPublic | BindingFlags.Instance);
	private static readonly FieldInfo InputFieldTmpField =
		typeof(HUDInputField).GetField("UIInputField", BindingFlags.NonPublic | BindingFlags.Instance);

	public IEnumerable<IHUDSidePanelModuleData> GetInfoModules(IMapModel map, BuildingModel building) {
		if (!map.Simulator.TryFindTileSimulation(building.Tile_G, out var simulation)
				|| simulation.Simulation is not GateSimulation gate) {
			return Array.Empty<IHUDSidePanelModuleData>();
		}
		var state = gate.State;
		return new IHUDSidePanelModuleData[] {
			new HUDSidePanelModuleGenericButton.Data(
				new RawText("Configure"),
				() => ShowConfigureDialog(state)),
		};
	}

	public IEnumerable<IHUDSidePanelModuleData> GetInfoModules(IBuildingDefinition definition) =>
		Array.Empty<IHUDSidePanelModuleData>();

	private static void ShowConfigureDialog(GateSimulationState state) {
		var stack = DialogStackHolder.Instance;
		if (stack == null) return;
		var dlg = stack.Show(Globals.Resources.UIDialogSimpleInputPrefab);
		dlg.Init(
			new RawText("Expression Gate Script"),
			new RawText("Eagle/Tcl source. Enter inserts a newline; click Confirm to apply."),
			new RawText("Confirm"),
			new RawText(state.Script ?? ""));
		var restore = MakeMultiline(dlg);
		dlg.OnConfirmed.Register(text => { state.Script = text ?? ""; });
		if (restore != null) dlg.OnClosed.Register(restore);
	}

	// Flips the dialog's TMP_InputField to MultiLineNewline (and bumps the
	// height) for the duration of this open. Returns an action that puts it
	// back so the shared dialog prefab doesn't infect other Configure flows.
	private static Action MakeMultiline(HUDDialogSimpleInput dlg) {
		try {
			var hudInput = DialogInputFieldField?.GetValue(dlg);
			if (hudInput == null) return null;
			var tmp = InputFieldTmpField?.GetValue(hudInput);
			if (tmp == null) return null;

			var tmpType = tmp.GetType();
			var lineTypeProp = tmpType.GetProperty("lineType");
			object originalLineType = null;
			if (lineTypeProp != null) {
				originalLineType = lineTypeProp.GetValue(tmp);
				lineTypeProp.SetValue(tmp, Enum.Parse(lineTypeProp.PropertyType, "MultiLineNewline"));
			}

			RectTransform rt = null;
			Vector2 originalSize = default;
			if (tmp is Component comp) {
				rt = comp.GetComponent<RectTransform>();
				if (rt != null) {
					originalSize = rt.sizeDelta;
					if (originalSize.y < 240f) rt.sizeDelta = new Vector2(originalSize.x, 240f);
				}
			}

			return () => {
				try {
					if (lineTypeProp != null && originalLineType != null) lineTypeProp.SetValue(tmp, originalLineType);
					if (rt != null) rt.sizeDelta = originalSize;
				} catch { }
			};
		} catch {
			return null;
		}
	}
}

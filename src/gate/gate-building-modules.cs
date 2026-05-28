using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Localization;
using UnityEngine;
using UnityEngine.UI;
using ILogger = Core.Logging.ILogger;

namespace Expr;

public class GateBuildingModules : IBuildingModules {
	public static ILogger Log;

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
			new RawText("Expr Gate"),
			new RawText("NCalc. Inputs: a(W) b(N) c(S) tick\nShape: rotate(s) paint(s,c) stack(bot,top) layer(s,n) quadrant(s,n)\nStr: len(s) substr(s,i,n) concat(a,b,...)\nMem: get(k) set(k,v)  Pred: isNull isShape isColor"),
			new RawText("Confirm"),
			new RawText(state.Script ?? ""));
		var restore = MakeTextarea(dlg);
		ConfirmKeySuppressor.Log = Log;
		ConfirmKeySuppressor.TargetField = GetTmpField(dlg);
		ConfirmKeySuppressor.Active = true;
		dlg.OnConfirmed.Register(text => { state.Script = text ?? ""; });
		dlg.OnClosed.Register(() => {
			ConfirmKeySuppressor.Active = false;
			ConfirmKeySuppressor.TargetField = null;
			restore?.Invoke();
		});
	}

	private static object GetTmpField(HUDDialogSimpleInput dlg) {
		var hudInput = DialogInputFieldField?.GetValue(dlg);
		return hudInput != null ? InputFieldTmpField?.GetValue(hudInput) : null;
	}

	// Setup multi-line textarea.
	private static Action MakeTextarea(HUDDialogSimpleInput dlg) {
		try {
			var hudInput = DialogInputFieldField?.GetValue(dlg);
			if (hudInput == null) {
				Log?.Info?.Log("[Expr] MakeTextarea: HUDInputField not found on dialog");
				return null;
			}
			var tmp = InputFieldTmpField?.GetValue(hudInput);
			if (tmp == null) {
				Log?.Info?.Log("[Expr] MakeTextarea: TMP_InputField not found on HUDInputField");
				return null;
			}

			var tmpType = tmp.GetType();

			Action restoreLineType = null;
			var lineTypeProp = tmpType.GetProperty("lineType");
			var lineTypeField = tmpType.GetField("m_LineType", BindingFlags.NonPublic | BindingFlags.Instance);
			if (lineTypeProp != null) {
				var enumType = lineTypeProp.PropertyType;
				var original = lineTypeProp.GetValue(tmp);
				var multi = Enum.Parse(enumType, "MultiLineNewline");
				try {
					lineTypeProp.SetValue(tmp, multi);
					if (lineTypeField != null) lineTypeField.SetValue(tmp, multi);
				} catch (Exception ex) {
					Log?.Error?.Log($"[Expr] lineType set failed: {ex.Message}");
				}
				restoreLineType = () => {
					try {
						lineTypeProp.SetValue(tmp, original);
						if (lineTypeField != null) lineTypeField.SetValue(tmp, original);
					} catch {}
				};
			}

			Action restoreAlignment = null;
			var textComponentProp = tmpType.GetProperty("textComponent");
			object textComponent = textComponentProp?.GetValue(tmp);
			if (textComponent != null) {
				var alignmentProp = textComponent.GetType().GetProperty("alignment");
				if (alignmentProp != null) {
					var original = alignmentProp.GetValue(textComponent);
					var topLeft = Enum.Parse(alignmentProp.PropertyType, "TopLeft");
					alignmentProp.SetValue(textComponent, topLeft);
					restoreAlignment = () => { try { alignmentProp.SetValue(textComponent, original); } catch {} };
				}
			}

			Action restoreLayout = null;
			if (tmp is Component comp) {
				var go = comp.gameObject;
				var le = go.GetComponent<LayoutElement>();
				bool addedLe = false;
				if (le == null) {
					le = go.AddComponent<LayoutElement>();
					addedLe = true;
				}
				var origPreferred = le.preferredHeight;
				var origMin = le.minHeight;
				le.preferredHeight = 320f;
				le.minHeight = 320f;

				restoreLayout = () => {
					try {
						if (addedLe) UnityEngine.Object.Destroy(le);
						else {
							le.preferredHeight = origPreferred;
							le.minHeight = origMin;
						}
					} catch {}
				};
			}

			return () => {
				restoreLineType?.Invoke();
				restoreAlignment?.Invoke();
				restoreLayout?.Invoke();
			};
		} catch (Exception ex) {
			Log?.Exception?.LogException(ex);
			return null;
		}
	}
}

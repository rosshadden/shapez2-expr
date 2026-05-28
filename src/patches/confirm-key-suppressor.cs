using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using ILogger = Core.Logging.ILogger;

namespace Expr;

// Lets enter key insert a newline rather than submitting the dialog.
public static class ConfirmKeySuppressor {
	public static bool Active;
	public static ILogger Log;

	// The TMP_InputField currently being edited (set by GateBuildingModules
	// when our editor opens, cleared when it closes). Object-typed so this
	// file doesn't need a TMPro reference.
	public static object TargetField;

	private static Hook _hook;

	public static void Install() {
		if (_hook != null) return;
		var method = typeof(HUDDialog).GetMethod(nameof(HUDDialog.DoUpdate));
		_hook = new Hook(method, (Action<Action<HUDDialog, InputDownstreamContext>, HUDDialog, InputDownstreamContext>)Patch);
	}

	private static void Patch(Action<HUDDialog, InputDownstreamContext> orig, HUDDialog self, InputDownstreamContext ctx) {
		if (Active && self.Visible && self is HUDDialogSimpleInput) {
			if (ctx.ConsumeWasActivated("global.confirm")) {
				InsertNewlineAtCaret();
			}
		}
		orig(self, ctx);
	}

	private static void InsertNewlineAtCaret() {
		var tmp = TargetField;
		if (tmp == null) return;
		try {
			var t = tmp.GetType();
			var textProp = t.GetProperty("text");
			var caretProp = t.GetProperty("caretPosition");
			if (textProp == null || caretProp == null) return;

			var text = (string)textProp.GetValue(tmp) ?? "";
			int pos = (int)caretProp.GetValue(tmp);
			if (pos < 0 || pos > text.Length) pos = text.Length;

			var newText = text.Substring(0, pos) + "\n" + text.Substring(pos);
			textProp.SetValue(tmp, newText);

			// Move the caret past the inserted newline. selectionAnchor/Focus
			// keep the cursor a point (no selection) at the new position.
			var anchorProp = t.GetProperty("selectionAnchorPosition");
			var focusProp = t.GetProperty("selectionFocusPosition");
			anchorProp?.SetValue(tmp, pos + 1);
			focusProp?.SetValue(tmp, pos + 1);
			caretProp.SetValue(tmp, pos + 1);
		} catch (Exception ex) {
			Log?.Exception?.LogException(ex);
		}
	}
}

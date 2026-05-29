using System;
using System.Reflection;
using MonoMod.RuntimeDetour;
using ILogger = Core.Logging.ILogger;

namespace Expr;

// intercepts dialog confirm key to insert newline instead
public static class ConfirmKeySuppressor {
	public static bool Active;
	public static ILogger Log;
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

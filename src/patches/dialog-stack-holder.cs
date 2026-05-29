using System;
using MonoMod.RuntimeDetour;

namespace Expr;

// grabs the live HUDDialogStack from its Update tick; overwrites on scene change
public static class DialogStackHolder {
	public static HUDDialogStack Instance;
	private static Hook _hook;

	public static void Install() {
		if (_hook != null) return;
		var method = typeof(HUDDialogStack).GetMethod(nameof(HUDDialogStack.Update));
		_hook = new Hook(method, (Action<Action<HUDDialogStack, InputDownstreamContext>, HUDDialogStack, InputDownstreamContext>)Patch);
	}

	private static void Patch(Action<HUDDialogStack, InputDownstreamContext> orig, HUDDialogStack self, InputDownstreamContext ctx) {
		Instance = self;
		orig(self, ctx);
	}
}

using System;
using MonoMod.RuntimeDetour;

namespace Expr;

// Mods don't get DI, so we grab the live HUDDialogStack from its Update tick.
// Each scene (main menu vs game) has its own HUDDialogStack; when scenes change
// the old one stops ticking and the new one takes over, so we always overwrite
// to track whichever is currently active.
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

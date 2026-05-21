using System;
using MonoMod.RuntimeDetour;

namespace Expr;

// Mods don't get DI, so we grab the live HUDDialogStack the first time it ticks.
// HUDDialogStack.Update fires every frame once the HUD is up; we capture and detach.
public static class DialogStackHolder {
	public static IHUDDialogStack Instance;
	private static Hook _hook;

	public static void Install() {
		if (_hook != null) return;
		var method = typeof(HUDDialogStack).GetMethod(nameof(HUDDialogStack.Update));
		_hook = new Hook(method, (Action<Action<HUDDialogStack, InputDownstreamContext>, HUDDialogStack, InputDownstreamContext>)Patch);
	}

	private static void Patch(Action<HUDDialogStack, InputDownstreamContext> orig, HUDDialogStack self, InputDownstreamContext ctx) {
		if (Instance == null) Instance = self;
		orig(self, ctx);
	}
}

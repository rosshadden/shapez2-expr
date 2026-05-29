using System;
using System.Reflection;
using Game.Content.Features.Signals;
using Game.Content.Features.Signals.Serialization;
using Game.Core.Serialization;
using MonoMod.RuntimeDetour;

namespace Expr;

// substitutes NullSignal for ExprSignal on save; wire signals recompute on load
public static class SignalSerializerPatch {
	private static Hook _hook;

	public static void Install() {
		if (_hook != null) return;
		var method = typeof(SignalSerializer).GetMethod(
			"Serialize",
			BindingFlags.Public | BindingFlags.Instance,
			null,
			new[] { typeof(ISignal), typeof(ISerializationVisitor) },
			null);
		_hook = new Hook(method, (Action<Action<SignalSerializer, ISignal, ISerializationVisitor>, SignalSerializer, ISignal, ISerializationVisitor>)Patch);
	}

	private static void Patch(
		Action<SignalSerializer, ISignal, ISerializationVisitor> orig,
		SignalSerializer self, ISignal value, ISerializationVisitor visitor) {
		orig(self, value is ExprSignal ? NullSignal.Instance : value, visitor);
	}
}

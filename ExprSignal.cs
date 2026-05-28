using System;
using Game.Content.Features.Signals;

namespace Expr;

// Custom signal type that carries arbitrary values (strings, booleans, etc.)
// through the standard Shapez 2 wire network.
//
// Standard game buildings treat this as null (graceful fallback).
// ExprGate/ExprMonitor encode and decode it via SignalCodec.
// On save, SignalSerializerPatch converts any ExprSignal in the conductor
// buffer to NullSignal so the game serializer doesn't throw.
public sealed class ExprSignal : ISignal {
	public readonly object Value;

	public ExprSignal(object value) => Value = value;

	public bool IsTruthy() => Value switch {
		null => false,
		bool b => b,
		int i => i != 0,
		double d => d != 0,
		string s => s.Length > 0,
		_ => true,
	};

	public override bool Equals(object obj) =>
		obj is ExprSignal e && (Value?.Equals(e.Value) ?? e.Value == null);

	public override int GetHashCode() => HashCode.Combine(7919, Value);
	public override string ToString() => Value?.ToString() ?? "null";
}

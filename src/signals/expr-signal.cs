using System;
using Game.Content.Features.Signals;

namespace Expr;

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

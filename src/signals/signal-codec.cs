using System;
using System.Globalization;
using Game.Content.Features.Signals;

namespace Expr;

// Converts between ISignal and .NET objects / strings for the expression engine.
//
// ToObject/FromObject: typed round-trip for NCalc (int, string, null).
// Encode/Decode: string round-trip (kept for logging and future use).
// Shape (BeltItemSignal wrapping ShapeItem): hash string, e.g. "CuCuCuCu".
// Color fluid (FluidSignal wrapping ColorFluid): single char code, e.g. "r".
public class SignalCodec {
	private readonly IShapeRegistry _shapes;
	private readonly IShapeIdManager _shapeIds;
	private readonly IShapeColorScheme _colors;

	public SignalCodec(IShapeRegistry shapes, IShapeIdManager shapeIds, IShapeColorScheme colors) {
		_shapes = shapes;
		_shapeIds = shapeIds;
		_colors = colors;
	}

	public string Encode(ISignal sig) {
		if (sig == null || sig is NullSignal) return "0";
		if (sig is IntegerSignal i) return i.Value.ToString(CultureInfo.InvariantCulture);
		if (sig is BeltItemSignal b && b.Value is ShapeItem shape) return shape.Definition.Hash;
		if (sig is FluidSignal f && f.Value is ColorFluid cf) return cf.Color.Code.ToString();
		if (sig is ExprSignal e) return e.Value?.ToString() ?? "null";
		return "0";
	}

	public ISignal Decode(string s) {
		if (string.IsNullOrEmpty(s)) return NullSignal.Instance;
		if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
			return IntegerSignal.Get(n);
		if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
			return IntegerSignal.Get((int)Math.Round(d));

		// Single-char color code: "r", "g", "b", "y", "p", "w", "c"
		if (s.Length == 1 && _colors != null) {
			try {
				var color = _colors.GetColorByCode(s[0]);
				if (color != null)
					return FluidSignal.From(ColorFluid.ForColor(color));
			} catch { }
		}

		// Shape hash: e.g. "CuCuCuCu" or "CuCuCuCu:--Ru--Ru"
		if (_shapes != null && _shapeIds != null && IsShapeHash(s)) {
			try {
				var id = _shapeIds.Resolve(s);
				var item = _shapes.GetItem(id);
				if (item != null)
					return BeltItemSignal.From(item);
			} catch { }
		}

		return NullSignal.Instance;
	}

	public object ToObject(ISignal sig) {
		if (sig == null || sig is NullSignal) return null;
		if (sig is IntegerSignal i) return i.Value;
		if (sig is BeltItemSignal b && b.Value is ShapeItem shape) return shape.Definition.Hash;
		if (sig is FluidSignal f && f.Value is ColorFluid cf) return cf.Color.Code.ToString();
		if (sig is ExprSignal e) return e.Value;
		return null;
	}

	public ISignal FromObject(object value) {
		if (value == null) return NullSignal.Instance;
		if (value is bool bv) return bv ? IntegerSignal.Get(1) : NullSignal.Instance;
		if (value is int iv) return IntegerSignal.Get(iv);
		if (value is long lv) return IntegerSignal.Get((int)lv);
		if (value is double dv) return IntegerSignal.Get((int)Math.Round(dv));
		if (value is decimal mv) return IntegerSignal.Get((int)Math.Round(mv));
		if (value is string sv) {
			var decoded = Decode(sv);
			return decoded is NullSignal && sv.Length > 0 ? new ExprSignal(sv) : decoded;
		}
		return NullSignal.Instance;
	}

	private static bool IsShapeHash(string s) {
		if (s.Length < 2) return false;
		foreach (char c in s) {
			if (!char.IsLetterOrDigit(c) && c != '-' && c != ':') return false;
		}
		return true;
	}
}

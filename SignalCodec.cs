using System;
using System.Globalization;
using Game.Content.Features.Signals;

namespace Expr;

// Converts between ISignal and the Tcl string representation used inside Eagle.
//
// Null/integer: unchanged (null→"0", int→decimal string).
// Shape (BeltItemSignal wrapping ShapeItem): hash string, e.g. "CuCuCuCu".
// Color fluid (FluidSignal wrapping ColorFluid): single char code, e.g. "r".
// All other signal types: encode as "0", decode falls through to NullSignal.
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
		if (sig is BeltItemSignal b && b.Value is ShapeItem shape)
			return shape.Definition.Hash;
		if (sig is FluidSignal f && f.Value is ColorFluid cf)
			return cf.Color.Code.ToString();
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

	private static bool IsShapeHash(string s) {
		if (s.Length < 2) return false;
		foreach (char c in s) {
			if (!char.IsLetterOrDigit(c) && c != '-' && c != ':') return false;
		}
		return true;
	}
}

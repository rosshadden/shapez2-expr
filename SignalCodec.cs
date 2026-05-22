using System.Globalization;
using Game.Content.Features.Signals;

namespace Expr;

// v1 codec: round-trips null and integer cleanly so booleans/numbers behave
// naturally in Tcl. Disconnected/null inputs encode as "0" (E2 convention)
// so `$a + 1` on a disconnected pin gives 1, not an Eagle expr error.
// Floats round to nearest int on decode since Shapez 2 has no float signal.
// Shape/fluid signals encode to a bracketed form for visibility but can't
// decode yet — that needs IShapeRegistry / IShapeColorScheme injected.
public static class SignalCodec {
	public static string Encode(ISignal sig) {
		if (sig == null || sig is NullSignal) return "0";
		if (sig is IntegerSignal i) return i.Value.ToString(CultureInfo.InvariantCulture);
		return sig.ToString();
	}

	public static ISignal Decode(string s) {
		if (string.IsNullOrEmpty(s)) return NullSignal.Instance;
		if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
			return IntegerSignal.Get(n);
		if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
			return IntegerSignal.Get((int)System.Math.Round(d));
		return NullSignal.Instance;
	}
}

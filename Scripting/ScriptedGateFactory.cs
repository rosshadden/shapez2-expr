using System;
using Core.Logging;

namespace Expr.Scripting;

// The Eagle-aware assembly (Expr.Eagle.dll) sets `Create` once during its
// load. Expr.dll calls it whenever a building needs a fresh interpreter.
// Keeping the registration as a static delegate avoids any direct
// reference from Expr.dll into Eagle types — important because the game's
// ModLoader scans Expr.dll's types before we install AssemblyResolve.
public static class ScriptedGateFactory {
	public static Func<ILogger, string, IScriptedGate> Create;
}

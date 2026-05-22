using System;

namespace Expr.Scripting;

// Direction-neutral handle on a per-building script interpreter. Lives in
// Expr.dll so GateSimulation can hold one without compile-time references
// to Eagle types. The concrete impl (Expr.Eagle.Gate) is created via
// ScriptedGateFactory, which Expr.Eagle registers when its assembly loads.
public interface IScriptedGate : IDisposable {
	void SetInput(string label, string value);
	string GetOutput(string label);
	bool Tick();
	void LoadScript(string script);
}

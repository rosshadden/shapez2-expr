namespace Expr;

// Holds the Display building's text-rendering draw data, captured once during
// simulation factory construction (when GameBuildings is available) and then
// used by MonitorRenderer on every frame. Safe to read from any thread because
// it's written exactly once before any rendering can occur.
internal static class MonitorTextResources {
	internal static DisplayMetaBuildingDefinition.DrawData DisplayDrawData;
}

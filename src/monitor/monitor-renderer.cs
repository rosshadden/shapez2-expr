using System.Runtime.CompilerServices;
using Game.Core.Rendering.MeshGeneration;
using UnityEngine;
using Unity.Mathematics;

namespace Expr;

// Renders the monitor's current DisplayText using the text mesh system borrowed
// from the Display building. The DI container injects ITextMeshAccessor the same
// way it does for DisplaySimulationRenderer and ConstantSignalSimulationRenderer.
//
// Per-entity mesh cache lives in a ConditionalWeakTable so entries are dropped
// automatically when a building is garbage-collected (e.g. on demolish).
public class MonitorRenderer : StatelessBuildingSimulationRenderer<MonitorSimulation, MonitorDrawData> {
	private readonly ITextMeshAccessor _textAccessor;
	private readonly ConditionalWeakTable<MonitorSimulation, CachedEntry> _cache = new();

	private class CachedEntry {
		public string Text;
		public IMeshReference Mesh;
		public float Scale;
	}

	public MonitorRenderer(IMapModel map, ITextMeshAccessor textMeshAccessor) : base(map) {
		_textAccessor = textMeshAccessor;
	}

	protected override void OnDrawDynamic(in Entity entity, FrameDrawOptions options) {
		var dd = MonitorTextResources.DisplayDrawData;
		if (dd == null) return;
		if (!options.Viewport.IsTileVisible(entity.Transform.Position)) return;

		var text = entity.Simulation.DisplayText ?? "";
		var entry = _cache.GetOrCreateValue(entity.Simulation);

		if (entry.Mesh == null || entry.Text != text) {
			var mesh = _textAccessor.GetMeshReference(text, dd.Font);
			if (mesh != null) {
				entry.Mesh = mesh;
				entry.Text = text;
				entry.Scale = TextMeshUtils.CalculateScale(mesh, dd.BaseScale * 0.5f, dd.ValueRenderBounds);
			}
		}

		if (entry.Mesh == null) return;

		var worldPos = dd.ValueRenderOrigin * entity.Transform;
		options.Renderers.RegularNonInstanced.DrawMesh(
			entry.Mesh, dd.TextMaterial3D,
			Matrix4x4.TRS(
				worldPos,
				dd.ValueRenderRotation * Quaternion.Euler(90f, 0f, 0f),
				new float3(entry.Scale)),
			RenderCategory.BuildingsDynamic);
	}
}

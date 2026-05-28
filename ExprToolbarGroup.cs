using System;
using Core.Localization;
using Game.Orchestration;
using ShapezShifter.Flow.Toolbar;
using ShapezShifter.Hijack;
using UnityEngine;

namespace Expr;

// Inserts the "Expr" submenu group at the end of the wires tab (child[2] of root).
// Must be registered via GameRewirers.AddRewirer before any building ToolbarRewirers
// that want to place themselves inside this group.
public class ExprToolbarGroup : IToolbarDataRewirer {
	private readonly Sprite _icon;

	public ExprToolbarGroup(Sprite icon) {
		_icon = icon;
	}

	public ToolbarData ModifyToolbarData(ToolbarData toolbarData) {
		var group = new GroupToolbarElementData {
			Title = new LazyLocalizedText(new TranslationId("expr.toolbar-group.title")),
			Description = new LazyLocalizedText(new TranslationId("expr.toolbar-group.description")),
			Icon = _icon,
			RememberPreferredChild = false,
			Children = Array.Empty<IToolbarElementData>(),
		};
		ToolbarElementLocator.Root().ChildAt(2).ChildAt(^1).InsertAfter().AddEntry(toolbarData, (IToolbarElementData)group);
		return toolbarData;
	}
}

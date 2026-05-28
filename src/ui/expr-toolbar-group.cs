using System.Collections.Generic;
using Core.Localization;
using ShapezShifter.Flow.Toolbar;
using UnityEngine;

namespace Expr;

// Toolbar group.
public class ExprToolbarGroup : IToolbarEntryInsertLocation {
	private readonly Sprite _icon;
	private GroupToolbarElementData _group;
	private object _lastToolbarData;

	public ExprToolbarGroup(Sprite icon) => _icon = icon;

	public void AddEntry(ToolbarData toolbarData, IToolbarElementData entry) {
		if (!ReferenceEquals(_lastToolbarData, toolbarData)) {
			_group = null;
			_lastToolbarData = toolbarData;
		}
		if (_group == null) {
			_group = new GroupToolbarElementData {
				Title = new LazyLocalizedText(new TranslationId("expr.toolbar-group.title")),
				Description = new LazyLocalizedText(new TranslationId("expr.toolbar-group.description")),
				Icon = _icon,
				RememberPreferredChild = false,
				Children = new IToolbarElementData[] { entry },
			};
			ToolbarElementLocator.Root().ChildAt(2).ChildAt(^1).InsertAfter()
				.AddEntry(toolbarData, _group);
		} else {
			var list = new List<IToolbarElementData>(_group.Children) { entry };
			_group.Children = list.ToArray();
		}
	}
}

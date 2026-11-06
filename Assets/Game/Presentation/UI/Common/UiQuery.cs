using System;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public static class UiQuery
	{
		public static T Require<T>(VisualElement scope, string name) where T : VisualElement
		{
			if (scope == null)
			{
				throw new ArgumentNullException(nameof(scope));
			}

			return scope.Q<T>(name)
				?? throw new InvalidOperationException($"The UXML requires a {typeof(T).Name} named '{name}'.");
		}

		public static void SetVisible(VisualElement element, bool visible)
		{
			element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
		}
	}
}

using System;
using RD.Localization;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public static class LocalizedText
	{
		public const string ClassName = "nc-localized";

		public static void Apply(VisualElement root, ILocalizationService localization)
		{
			if (root == null)
			{
				throw new ArgumentNullException(nameof(root));
			}

			if (localization == null)
			{
				throw new ArgumentNullException(nameof(localization));
			}

			root.Query<TextElement>(className: ClassName).ForEach(element => Apply(element, localization));
		}

		public static void Apply(TextElement element, ILocalizationService localization)
		{
			string key = element.userData as string ?? element.text;

			element.userData = key;
			element.text = localization.Get(key);
		}

		public static void SetKey(TextElement element, ILocalizationService localization, string key)
		{
			element.userData = key;
			element.text = localization.Get(key);
		}
	}
}

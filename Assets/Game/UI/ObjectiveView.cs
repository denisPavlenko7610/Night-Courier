using System;
using RD.Localization;
using UnityEngine;

namespace NightCourier.UI
{
	public sealed class ObjectiveView : MonoBehaviour
	{
		[SerializeField] private LocalizedLabel _label;

		public void Bind(ILocalizationService localization)
		{
			if (localization == null)
			{
				throw new ArgumentNullException(nameof(localization));
			}

			_label.Bind(localization);
			_label.gameObject.SetActive(false);
		}

		public void Show(string key)
		{
			_label.gameObject.SetActive(key != null);

			if (key != null)
			{
				_label.SetKey(key);
			}
		}
	}
}

using System;
using RD.Attributes;
using RD.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	[RequireComponent(typeof(Text))]
	public sealed class LocalizedLabel : MonoBehaviour
	{
		[SerializeField] private string _key;
		[SerializeField, Assign] private Text _text;

		private ILocalizationService _localization;
		private object[] _arguments = Array.Empty<object>();

		public static void BindAll(GameObject root, ILocalizationService localization)
		{
			foreach (LocalizedLabel label in root.GetComponentsInChildren<LocalizedLabel>(true))
			{
				label.Bind(localization);
			}
		}

		public void Bind(ILocalizationService localization, params object[] arguments)
		{
			Unsubscribe();

			_localization = localization ?? throw new ArgumentNullException(nameof(localization));
			_arguments = arguments ?? Array.Empty<object>();
			_localization.LocaleChanged += OnLocaleChanged;

			Refresh();
		}

		public void SetKey(string key, params object[] arguments)
		{
			if (_localization == null)
			{
				throw new InvalidOperationException("The label must be bound before its key can change.");
			}

			_key = key;
			_arguments = arguments ?? Array.Empty<object>();

			Refresh();
		}

		private void Refresh()
		{
			_text.text = _localization.Get(_key, _arguments);
		}

		private void OnLocaleChanged(LocaleChanged change)
		{
			Refresh();
		}

		private void Unsubscribe()
		{
			if (_localization != null)
			{
				_localization.LocaleChanged -= OnLocaleChanged;
			}
		}

		private void OnDestroy()
		{
			Unsubscribe();
		}
	}
}

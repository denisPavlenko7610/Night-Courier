using System;
using RD.Localization;
using UnityEngine;

namespace NightCourier.UI
{
	public sealed class ObjectiveView : MonoBehaviour
	{
		private const string CaptionKey = "hud.objective";

		[SerializeField] private LocalizedLabel _label;

		private ILocalizationService _localization;
		private string _key;
		private bool _hidden;

		public void Bind(ILocalizationService localization)
		{
			_localization = localization ?? throw new ArgumentNullException(nameof(localization));

			_label.Bind(localization);
			_key = null;
			Apply();
		}

		public void Show(string key)
		{
			_key = key;

			Apply();
		}

		public void SetHidden(bool hidden)
		{
			if (_hidden == hidden)
			{
				return;
			}

			_hidden = hidden;

			Apply();
		}

		private void Apply()
		{
			bool visible = _key != null && !_hidden;

			_label.gameObject.SetActive(visible);

			if (visible)
			{
				_label.SetRichText(UiStyle.Heading(_localization.Get(CaptionKey)) + "\n" + _localization.Get(_key));
			}
		}
	}
}

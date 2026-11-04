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
		private FirstDeliveryTraining _training;

		public void Bind(ILocalizationService localization)
		{
			_localization = localization ?? throw new ArgumentNullException(nameof(localization));

			_label.Bind(localization);
			_key = null;
			Apply();
		}

		public void SetTraining(FirstDeliveryTraining training)
		{
			_training = training;
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
				bool training = _training != null && _training.StepNumber > 0;
				string caption = training
					? _localization.Get(FirstDeliveryTraining.CaptionKey, _training.StepNumber, _training.StepCount)
					: _localization.Get(CaptionKey);
				string key = training ? "training." + _key : _key;
				_label.SetRichText(UiStyle.Heading(caption) + "\n" + _localization.Get(key));
			}
		}
	}
}

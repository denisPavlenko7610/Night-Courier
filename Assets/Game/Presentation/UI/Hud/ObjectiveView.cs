using System;
using RD.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class ObjectiveView : MonoBehaviour
	{
		private const string PlateName = "objective-plate";
		private const string LabelName = "objective-label";

		[SerializeField] private UIDocument _document;

		private ILocalizationService _localization;
		private VisualElement _plate;
		private Label _label;
		private string _key;
		private bool _hidden;
		private FirstDeliveryTraining _training;

		public bool IsVisible => _plate != null && _plate.style.display != DisplayStyle.None;

		public string Text => _label?.text;

		public void Bind(ILocalizationService localization)
		{
			_localization = localization ?? throw new ArgumentNullException(nameof(localization));

			VisualElement root = _document.rootVisualElement;

			_plate = UiQuery.Require<VisualElement>(root, PlateName);
			_label = UiQuery.Require<Label>(root, LabelName);
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

			UiQuery.SetVisible(_plate, visible);

			if (!visible)
			{
				return;
			}

			bool training = _training != null && _training.StepNumber > 0;
			string key = training ? "training." + _key : _key;
			string text = _localization.Get(key);

			if (training)
			{
				string caption = _localization.Get(FirstDeliveryTraining.CaptionKey, _training.StepNumber, _training.StepCount);
				text = UiStyle.Heading(caption) + "\n" + text;
			}

			_label.text = text;
		}
	}
}

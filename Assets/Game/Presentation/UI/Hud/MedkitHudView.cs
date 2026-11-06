using System;
using NightCourier.Player;
using RD.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class MedkitHudView : MonoBehaviour
	{
		private const string MedkitsKey = "hud.medkits";
		private const string RowName = "medkit-row";
		private const string LabelName = "medkit-label";

		[SerializeField] private UIDocument _document;

		private ILocalizationService _localization;
		private PlayerMedkits _medkits;
		private VisualElement _row;
		private Label _label;

		public void Bind(ILocalizationService localization, PlayerMedkits medkits)
		{
			if (localization == null)
			{
				throw new ArgumentNullException(nameof(localization));
			}

			Unbind();

			VisualElement root = _document.rootVisualElement;

			_localization = localization;
			_medkits = medkits != null ? medkits : throw new ArgumentNullException(nameof(medkits));
			_row = UiQuery.Require<VisualElement>(root, RowName);
			_label = UiQuery.Require<Label>(root, LabelName);

			_localization.LocaleChanged += OnLocaleChanged;
			_medkits.Changed += Refresh;

			Refresh();
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_medkits == null)
			{
				return;
			}

			_localization.LocaleChanged -= OnLocaleChanged;
			_medkits.Changed -= Refresh;
			_medkits = null;
		}

		private void OnLocaleChanged(LocaleChanged change)
		{
			Refresh();
		}

		private void Refresh()
		{
			int count = _medkits.Count;

			UiQuery.SetVisible(_row, count > 0);

			if (count > 0)
			{
				_label.text = _localization.Get(MedkitsKey, count);
			}
		}
	}
}

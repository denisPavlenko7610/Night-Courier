using System;
using RD.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class DepotHudView : MonoBehaviour
	{
		private const string CreditsKey = "depot.credits";
		private const string GroupName = "hud-group";
		private const string CreditsName = "depot-credits";
		private const string RankName = "depot-rank";

		[SerializeField] private UIDocument _document;

		private ILocalizationService _localization;
		private VisualElement _group;
		private Label _credits;
		private Label _rank;

		public bool IsHidden { get; private set; }

		public string CreditsText => _credits?.text;

		public void Bind(ILocalizationService localization)
		{
			_localization = localization ?? throw new ArgumentNullException(nameof(localization));

			VisualElement root = _document.rootVisualElement;

			_group = UiQuery.Require<VisualElement>(root, GroupName);
			_credits = UiQuery.Require<Label>(root, CreditsName);
			_rank = UiQuery.Require<Label>(root, RankName);

			LocalizedText.Apply(root, localization);
			UiQuery.SetVisible(_group, !IsHidden);
		}

		public void SetCredits(long credits)
		{
			_credits.text = _localization.Get(CreditsKey, credits);
		}

		public void SetRank(string key, params object[] arguments)
		{
			_rank.text = _localization.Get(key, arguments);
		}

		public void SetHidden(bool hidden)
		{
			IsHidden = hidden;

			if (_group != null)
			{
				UiQuery.SetVisible(_group, !hidden);
			}
		}
	}
}

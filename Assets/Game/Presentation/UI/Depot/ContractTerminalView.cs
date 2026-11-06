using System;
using System.Collections.Generic;
using NightCourier.Contracts;
using RD.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class ContractTerminalView : MonoBehaviour
	{
		public const string PanelName = "contract-terminal";
		public const string AcceptButtonName = "contract-accept";
		public const string CloseButtonName = "contract-close";

		private const string SlotsName = "contract-slots";
		private const string EmptyName = "contract-empty";
		private const string DetailsName = "contract-details";
		private const string TitleName = "contract-title";
		private const string StoryName = "contract-story";
		private const string TermsName = "contract-terms";

		[SerializeField] private UIDocument _document;

		private readonly List<ContractSlot> _slots = new();

		private ILocalizationService _localization;
		private VisualElement _panel;
		private VisualElement _empty;
		private VisualElement _details;
		private Label _title;
		private Label _story;
		private Label _terms;
		private Button _accept;
		private Button _close;

		public event Action<ContractSpec> Selected;

		public event Action Accepted;

		public event Action CloseRequested;

		public VisualElement Panel => _panel;

		public bool IsOpen => _panel != null && _panel.style.display != DisplayStyle.None;

		public bool CanAccept => _accept != null && _accept.style.display != DisplayStyle.None;

		public int VisibleSlotCount
		{
			get
			{
				int count = 0;

				foreach (ContractSlot slot in _slots)
				{
					if (slot.IsVisible)
					{
						count++;
					}
				}

				return count;
			}
		}

		public void Bind(ILocalizationService localization)
		{
			_localization = localization ?? throw new ArgumentNullException(nameof(localization));

			VisualElement root = _document.rootVisualElement;

			_panel = UiQuery.Require<VisualElement>(root, PanelName);
			_empty = UiQuery.Require<VisualElement>(_panel, EmptyName);
			_details = UiQuery.Require<VisualElement>(_panel, DetailsName);
			_title = UiQuery.Require<Label>(_panel, TitleName);
			_story = UiQuery.Require<Label>(_panel, StoryName);
			_terms = UiQuery.Require<Label>(_panel, TermsName);
			_accept = UiQuery.Require<Button>(_panel, AcceptButtonName);
			_close = UiQuery.Require<Button>(_panel, CloseButtonName);

			foreach (Button button in UiQuery.Require<VisualElement>(_panel, SlotsName).Query<Button>(ContractSlot.ButtonName).ToList())
			{
				_slots.Add(new ContractSlot(button));
			}

			LocalizedText.Apply(_panel, localization);

			_accept.clicked += OnAcceptClicked;
			_close.clicked += OnCloseClicked;

			UiQuery.SetVisible(_panel, false);
		}

		public void Open()
		{
			UiQuery.SetVisible(_panel, true);
			_panel.schedule.Execute(FocusPrimary);
		}

		public void Close()
		{
			UiQuery.SetVisible(_panel, false);
		}

		public void ShowOffers(IReadOnlyList<ContractSpec> offers, ContractSpec selected)
		{
			for (int index = 0; index < _slots.Count; index++)
			{
				if (index < offers.Count)
				{
					ContractSpec offer = offers[index];
					bool isSelected = offer == selected;

					_slots[index].Show(offer, isSelected, ContractBriefing.SlotLabel(offer, _localization, isSelected), OnSlotSelected);
				}
				else
				{
					_slots[index].Hide();
				}
			}

			UiQuery.SetVisible(_empty, offers.Count == 0);
			ShowDetails(selected);
			UiQuery.SetVisible(_accept, selected != null);
		}

		public void ShowActive(ContractSpec active)
		{
			foreach (ContractSlot slot in _slots)
			{
				slot.Hide();
			}

			UiQuery.SetVisible(_empty, false);
			ShowDetails(active);
			UiQuery.SetVisible(_accept, false);
		}

		private void OnDestroy()
		{
			foreach (ContractSlot slot in _slots)
			{
				slot.Dispose();
			}

			if (_accept != null)
			{
				_accept.clicked -= OnAcceptClicked;
			}

			if (_close != null)
			{
				_close.clicked -= OnCloseClicked;
			}
		}

		private void ShowDetails(ContractSpec spec)
		{
			UiQuery.SetVisible(_details, spec != null);

			if (spec == null)
			{
				return;
			}

			_title.text = _localization.Get(spec.TitleKey);
			_story.text = ContractBriefing.Story(spec, _localization);
			_terms.text = ContractBriefing.Terms(spec, _localization);
		}

		private void FocusPrimary()
		{
			if (!IsOpen)
			{
				return;
			}

			(CanAccept ? _accept : _close).Focus();
		}

		private void OnSlotSelected(ContractSpec spec)
		{
			Selected?.Invoke(spec);
		}

		private void OnAcceptClicked()
		{
			Accepted?.Invoke();
		}

		private void OnCloseClicked()
		{
			CloseRequested?.Invoke();
		}
	}
}

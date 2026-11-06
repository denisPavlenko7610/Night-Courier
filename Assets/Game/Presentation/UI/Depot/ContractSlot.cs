using System;
using NightCourier.Contracts;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class ContractSlot : IDisposable
	{
		public const string ButtonName = "slot";

		private const string LabelName = "slot-label";
		private const string MarkerName = "slot-marker";

		private readonly Button _button;
		private readonly Label _label;
		private readonly VisualElement _marker;

		private ContractSpec _spec;
		private Action<ContractSpec> _onSelected;

		public ContractSlot(Button button)
		{
			_button = button ?? throw new ArgumentNullException(nameof(button));
			_label = UiQuery.Require<Label>(button, LabelName);
			_marker = UiQuery.Require<VisualElement>(button, MarkerName);

			_button.clicked += OnClicked;

			Hide();
		}

		public Button Button => _button;

		public bool IsVisible => _button.style.display != DisplayStyle.None;

		public ContractSpec Spec => _spec;

		public void Show(ContractSpec spec, bool isSelected, string label, Action<ContractSpec> onSelected)
		{
			_spec = spec;
			_onSelected = onSelected;

			UiQuery.SetVisible(_button, true);
			UiQuery.SetVisible(_marker, isSelected);

			_label.text = label;
		}

		public void Hide()
		{
			_spec = null;

			UiQuery.SetVisible(_button, false);
		}

		public void Dispose()
		{
			_button.clicked -= OnClicked;
		}

		private void OnClicked()
		{
			if (_spec != null)
			{
				_onSelected?.Invoke(_spec);
			}
		}
	}
}

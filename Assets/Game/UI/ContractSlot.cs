using System;
using NightCourier.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class ContractSlot : MonoBehaviour
	{
		[SerializeField] private Button _button;
		[SerializeField] private LocalizedLabel _title;

		private ContractSpec _spec;
		private Action<ContractSpec> _onSelected;

		public void Show(ContractSpec spec, bool isSelected, Action<ContractSpec> onSelected)
		{
			_spec = spec;
			_onSelected = onSelected;

			gameObject.SetActive(true);
			_title.SetKey(spec.TitleKey);
			_button.interactable = !isSelected;
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}

		private void Awake()
		{
			_button.onClick.AddListener(OnClicked);
		}

		private void OnDestroy()
		{
			_button.onClick.RemoveListener(OnClicked);
		}

		private void OnClicked()
		{
			_onSelected?.Invoke(_spec);
		}
	}
}

using System;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class LockerRow : MonoBehaviour
	{
		[SerializeField] private LocalizedLabel _title;
		[SerializeField] private LocalizedLabel _status;
		[SerializeField] private Button _button;
		[SerializeField] private LocalizedLabel _buttonLabel;

		private Action _onClick;

		public void Show(
			string titleKey,
			string statusKey,
			object[] statusArguments,
			string buttonKey,
			object[] buttonArguments,
			bool interactable,
			Action onClick)
		{
			_onClick = onClick;

			gameObject.SetActive(true);
			_title.SetKey(titleKey);
			_status.SetKey(statusKey, statusArguments);
			_buttonLabel.SetKey(buttonKey, buttonArguments);
			_button.interactable = interactable;
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
			_onClick?.Invoke();
		}
	}
}

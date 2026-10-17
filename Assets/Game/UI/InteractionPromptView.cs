using System;
using NightCourier.Controls;
using NightCourier.Player;
using RD.Localization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class InteractionPromptView : MonoBehaviour
	{
		private const string PressKey = "prompt.press";
		private const string HoldKey = "prompt.hold";

		[SerializeField] private Text _prompt;
		[SerializeField] private GameObject _holdBar;
		[SerializeField] private Image _holdFill;

		private PlayerInteractor _interactor;
		private ILocalizationService _localization;
		private PlayerInputActions _actions;

		public void Bind(PlayerInteractor interactor, ILocalizationService localization, PlayerInputActions actions)
		{
			Unbind();

			_interactor = interactor ?? throw new ArgumentNullException(nameof(interactor));
			_localization = localization ?? throw new ArgumentNullException(nameof(localization));
			_actions = actions ?? throw new ArgumentNullException(nameof(actions));

			_interactor.TargetChanged += Refresh;
			_localization.LocaleChanged += OnLocaleChanged;

			Refresh();
		}

		private void Update()
		{
			if (_interactor == null || !_holdBar.activeSelf)
			{
				return;
			}

			_holdFill.fillAmount = _interactor.HoldProgress;
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_interactor != null)
			{
				_interactor.TargetChanged -= Refresh;
			}

			if (_localization != null)
			{
				_localization.LocaleChanged -= OnLocaleChanged;
			}
		}

		private void OnLocaleChanged(LocaleChanged change)
		{
			Refresh();
		}

		private void Refresh()
		{
			Interactable target = _interactor.Target;

			if (target == null)
			{
				_prompt.text = string.Empty;
				_holdBar.SetActive(false);
				return;
			}

			string key = _actions.Interact.GetBindingDisplayString(0);
			string action = _localization.Get(target.PromptKey);

			_prompt.text = _localization.Get(target.RequiresHold ? HoldKey : PressKey, key, action);
			_holdFill.fillAmount = 0f;
			_holdBar.SetActive(target.RequiresHold);
		}
	}
}

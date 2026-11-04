using System;
using NightCourier.Controls;
using NightCourier.Player;
using RD.Localization;
using RD.Mechanics.Interactable;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class InteractionPromptView : MonoBehaviour
	{
		private const string PressKey = "prompt.press";
		private const string HoldKey = "prompt.hold";
		private const string PressGlyphKey = "prompt.press_glyph";
		private const string HoldGlyphKey = "prompt.hold_glyph";
		private const float GlyphGap = 14f;

		[SerializeField] private Text _prompt;
		[SerializeField] private GameObject _holdBar;
		[SerializeField] private Image _holdFill;
		[SerializeField] private Image _glyph;
		[SerializeField] private PromptGlyphs _glyphs;

		private InteractionFocus _interactor;
		private ILocalizationService _localization;
		private PlayerInputActions _actions;
		private Vector2 _promptHome;

		private void Awake()
		{
			_promptHome = _prompt.rectTransform.anchoredPosition;
		}

		public void Bind(InteractionFocus interactor, ILocalizationService localization, PlayerInputActions actions)
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
				Interactable blocked = _interactor.Blocked;

				_prompt.text = blocked != null ? _localization.Get(blocked.BlockedKey) : string.Empty;
				_holdBar.SetActive(false);
				ShowGlyph(blocked != null && _glyphs != null ? _glyphs.Lock : null);
				return;
			}

			string action = _localization.Get(target.PromptKey);
			Sprite keyGlyph = _glyphs != null ? _glyphs.FindKey(_actions.Interact.bindings[0].effectivePath) : null;

			if (keyGlyph != null)
			{
				_prompt.text = _localization.Get(target.RequiresHold ? HoldGlyphKey : PressGlyphKey, action);
			}
			else
			{
				string key = _actions.Interact.GetBindingDisplayString(0);

				_prompt.text = _localization.Get(target.RequiresHold ? HoldKey : PressKey, key, action);
			}

			_holdFill.fillAmount = 0f;
			_holdBar.SetActive(target.RequiresHold);
			ShowGlyph(keyGlyph);
		}

		private void ShowGlyph(Sprite sprite)
		{
			if (_glyph == null)
			{
				return;
			}

			RectTransform promptRect = _prompt.rectTransform;

			if (sprite == null)
			{
				_glyph.gameObject.SetActive(false);
				promptRect.anchoredPosition = _promptHome;
				return;
			}

			_glyph.sprite = sprite;
			_glyph.gameObject.SetActive(true);

			float glyphWidth = _glyph.rectTransform.rect.width;
			float textWidth = Mathf.Min(_prompt.preferredWidth, promptRect.rect.width);
			float shift = (glyphWidth + GlyphGap) / 2f;

			promptRect.anchoredPosition = _promptHome + Vector2.right * shift;
			_glyph.rectTransform.anchoredPosition = _promptHome + new Vector2(-textWidth / 2f - GlyphGap / 2f, 0f);
		}
	}
}

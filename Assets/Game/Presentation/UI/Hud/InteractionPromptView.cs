using System;
using NightCourier.Controls;
using RD.Localization;
using RD.Mechanics.Interactable;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class InteractionPromptView : MonoBehaviour
	{
		private const string PressKey = "prompt.press";
		private const string HoldKey = "prompt.hold";
		private const string PressGlyphKey = "prompt.press_glyph";
		private const string HoldGlyphKey = "prompt.hold_glyph";
		private const string PromptName = "prompt-label";
		private const string GlyphName = "prompt-glyph";
		private const string HoldBarName = "hold-bar";
		private const string HoldFillName = "hold-fill";

		[SerializeField] private UIDocument _document;
		[SerializeField] private PromptGlyphs _glyphs;

		private InteractionFocus _interactor;
		private ILocalizationService _localization;
		private PlayerInputActions _actions;
		private Label _prompt;
		private VisualElement _glyph;
		private VisualElement _holdBar;
		private VisualElement _holdFill;
		private bool _holdVisible;

		public string PromptText => _prompt?.text;

		public bool HoldBarVisible => _holdVisible;

		public void Bind(InteractionFocus interactor, ILocalizationService localization, PlayerInputActions actions)
		{
			Unbind();

			VisualElement root = _document.rootVisualElement;

			_interactor = interactor ?? throw new ArgumentNullException(nameof(interactor));
			_localization = localization ?? throw new ArgumentNullException(nameof(localization));
			_actions = actions ?? throw new ArgumentNullException(nameof(actions));
			_prompt = UiQuery.Require<Label>(root, PromptName);
			_glyph = UiQuery.Require<VisualElement>(root, GlyphName);
			_holdBar = UiQuery.Require<VisualElement>(root, HoldBarName);
			_holdFill = UiQuery.Require<VisualElement>(root, HoldFillName);

			_interactor.TargetChanged += Refresh;
			_localization.LocaleChanged += OnLocaleChanged;

			Refresh();
		}

		private void Update()
		{
			if (_interactor == null || !_holdVisible)
			{
				return;
			}

			_holdFill.style.width = new Length(_interactor.HoldProgress * 100f, LengthUnit.Percent);
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
				_interactor = null;
			}

			if (_localization != null)
			{
				_localization.LocaleChanged -= OnLocaleChanged;
				_localization = null;
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
				UiQuery.SetVisible(_prompt, blocked != null);
				SetHoldBar(false);
				ShowGlyph(blocked != null && _glyphs != null ? _glyphs.Lock : null);
				return;
			}

			UiQuery.SetVisible(_prompt, true);
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

			_holdFill.style.width = new Length(0f, LengthUnit.Percent);
			SetHoldBar(target.RequiresHold);
			ShowGlyph(keyGlyph);
		}

		private void SetHoldBar(bool visible)
		{
			_holdVisible = visible;

			UiQuery.SetVisible(_holdBar, visible);
		}

		private void ShowGlyph(Sprite sprite)
		{
			if (sprite == null)
			{
				UiQuery.SetVisible(_glyph, false);
				return;
			}

			_glyph.style.backgroundImage = new StyleBackground(sprite);

			UiQuery.SetVisible(_glyph, true);
		}
	}
}

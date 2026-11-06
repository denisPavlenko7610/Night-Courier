using System;
using System.Threading;
using RD.Menus;
using RD.ScreenNavigation;
using RD.Tween;
using UnityEngine;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class UiScreen : IScreen
	{
		private const float FadeSeconds = 0.15f;

		private readonly VisualElement _root;
		private readonly Action _show;
		private readonly Action _hide;
		private readonly Func<bool> _isVisible;
		private readonly CancellationToken _cancellation;

		private float _alpha = 1f;

		private UiScreen(
			string id,
			VisualElement root,
			Action show,
			Action hide,
			Func<bool> isVisible,
			CancellationToken cancellation)
		{
			Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("A screen id is required.", nameof(id)) : id;
			_root = root ?? throw new ArgumentNullException(nameof(root));
			_show = show;
			_hide = hide;
			_isVisible = isVisible;
			_cancellation = cancellation;
		}

		public string Id { get; }

		public bool IsVisible => _isVisible();

		public static UiScreen ForView(string id, MenuViewBase view, CancellationToken cancellation)
		{
			if (view == null)
			{
				throw new ArgumentNullException(nameof(view));
			}

			return new UiScreen(id, view.Root, view.Show, view.Hide, () => view.IsVisible, cancellation);
		}

		public static UiScreen ForElement(string id, VisualElement root, CancellationToken cancellation)
		{
			if (root == null)
			{
				throw new ArgumentNullException(nameof(root));
			}

			bool visible = false;

			root.style.display = DisplayStyle.None;

			return new UiScreen(
				id,
				root,
				() =>
				{
					visible = true;
					root.style.display = DisplayStyle.Flex;
				},
				() =>
				{
					visible = false;
					root.style.display = DisplayStyle.None;
				},
				() => visible,
				cancellation);
		}

		public void HideImmediately()
		{
			Tween.Kill(_root);

			_hide();
		}

		public async Awaitable ShowAsync(object arguments = null)
		{
			Tween.Kill(_root);

			SetAlpha(0f);
			_show();

			await FadeAsync(1f);
		}

		public async Awaitable HideAsync()
		{
			await FadeAsync(0f);

			_hide();
		}

		private async Awaitable FadeAsync(float alpha)
		{
			Tween.Kill(_root);

			PropertyTween<float> fade = Tween.To(_root, () => _alpha, SetAlpha, alpha, FadeSeconds);

			fade.SetUpdate(true);

			await fade.WaitForCompletionAsync(_cancellation);
		}

		private void SetAlpha(float value)
		{
			_alpha = value;
			_root.style.opacity = value;
		}
	}
}

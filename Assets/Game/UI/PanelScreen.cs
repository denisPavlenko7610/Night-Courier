using RD.ScreenNavigation;
using RD.Tween;
using UnityEngine;

namespace NightCourier.UI
{
	[RequireComponent(typeof(CanvasGroup))]
	public sealed class PanelScreen : MonoBehaviour, IScreen
	{
		private const float FadeSeconds = 0.15f;

		[SerializeField] private string _id;
		[SerializeField] private CanvasGroup _group;

		public string Id => _id;

		public bool IsVisible => gameObject.activeSelf;

		public void HideImmediately()
		{
			Tween.Kill(_group);
			_group.alpha = 0f;
			_group.interactable = false;
			gameObject.SetActive(false);
		}

		public async Awaitable ShowAsync(object arguments = null)
		{
			gameObject.SetActive(true);
			_group.interactable = false;

			await FadeAsync(1f);

			_group.interactable = true;
		}

		public async Awaitable HideAsync()
		{
			_group.interactable = false;

			await FadeAsync(0f);

			gameObject.SetActive(false);
		}

		private async Awaitable FadeAsync(float alpha)
		{
			Tween.Kill(_group);

			PropertyTween<float> fade = Tween.To(_group, () => _group.alpha, value => _group.alpha = value, alpha, FadeSeconds);

			fade.SetUpdate(true);

			await fade.WaitForCompletionAsync(destroyCancellationToken);
		}
	}
}

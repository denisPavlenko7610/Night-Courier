using System;
using RD.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace NightCourier.Audio
{
	public sealed class UiSounds : MonoBehaviour
	{
		private static readonly string[] BackNames =
		{
			"settings-back", "controls-back", "confirm-cancel", "entry-resume",
			"contract-close", "upgrade-close", "locker-close"
		};

		private static readonly string[] ConfirmNames =
		{
			"confirm-ok", "settings-apply", "entry-new-game", "entry-continue",
			"contract-accept", "upgrade-buy"
		};

		[SerializeField] private AudioCue _click;
		[SerializeField] private AudioCue _confirm;
		[SerializeField] private AudioCue _back;
		[SerializeField] private AudioCue _open;

		private AudioService _audio;
		private VisualElement _clickRoot;

		public AudioService Audio => _audio;

		public void Bind(AudioService audio, VisualElement root)
		{
			Unbind();

			_audio = audio ?? throw new ArgumentNullException(nameof(audio));
			_clickRoot = root ?? throw new ArgumentNullException(nameof(root));
			_clickRoot.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
		}

		public void PlayOpen()
		{
			_audio.Play(_open);
		}

		public void PlayBack()
		{
			_audio.Play(_back);
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_clickRoot != null)
			{
				_clickRoot.UnregisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
				_clickRoot = null;
			}

			_audio = null;
		}

		private void OnClick(ClickEvent evt)
		{
			VisualElement element = evt.target as VisualElement;

			while (element != null && element is not Button)
			{
				element = element.parent;
			}

			if (element != null && element.enabledInHierarchy)
			{
				_audio.Play(CueFor(element.name));
			}
		}

		private AudioCue CueFor(string buttonName)
		{
			if (Array.IndexOf(BackNames, buttonName) >= 0)
			{
				return _back;
			}

			return Array.IndexOf(ConfirmNames, buttonName) >= 0 ? _confirm : _click;
		}
	}
}

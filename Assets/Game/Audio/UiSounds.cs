using System;
using System.Collections.Generic;
using RD.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.Audio
{
	public sealed class UiSounds : MonoBehaviour
	{
		private static readonly string[] BackNames = { "BackButton", "CloseButton", "CancelButton", "ResumeButton" };
		private static readonly string[] ConfirmNames = { "ConfirmButton", "ActionButton", "BuyButton", "NewGameButton", "ContinueButton" };

		[SerializeField] private AudioCue _click;
		[SerializeField] private AudioCue _confirm;
		[SerializeField] private AudioCue _back;
		[SerializeField] private AudioCue _open;

		private readonly List<(Button Button, UnityEngine.Events.UnityAction Action)> _bound = new();

		private AudioService _audio;

		public AudioService Audio => _audio;

		public void Bind(AudioService audio)
		{
			Unbind();

			_audio = audio ?? throw new ArgumentNullException(nameof(audio));

			foreach (Button button in GetComponentsInChildren<Button>(true))
			{
				AudioCue cue = CueFor(button.name);

				void OnClick()
				{
					_audio.Play(cue);
				}

				button.onClick.AddListener(OnClick);
				_bound.Add((button, OnClick));
			}
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
			foreach ((Button button, UnityEngine.Events.UnityAction action) in _bound)
			{
				if (button != null)
				{
					button.onClick.RemoveListener(action);
				}
			}

			_bound.Clear();
			_audio = null;
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

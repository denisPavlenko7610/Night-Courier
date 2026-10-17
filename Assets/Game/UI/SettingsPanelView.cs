using System;
using NightCourier.App;
using RD.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class SettingsPanelView : MonoBehaviour
	{
		[SerializeField] private Slider _masterVolume;
		[SerializeField] private Slider _mouseSensitivity;

		private ISettingsService _settings;

		public void Bind(ISettingsService settings)
		{
			Unbind();

			_settings = settings ?? throw new ArgumentNullException(nameof(settings));

			_masterVolume.minValue = 0f;
			_masterVolume.maxValue = 1f;
			_mouseSensitivity.minValue = GameSettings.MinMouseSensitivity;
			_mouseSensitivity.maxValue = GameSettings.MaxMouseSensitivity;

			_masterVolume.SetValueWithoutNotify(_settings.Get(GameSettings.MasterVolume));
			_mouseSensitivity.SetValueWithoutNotify(_settings.Get(GameSettings.MouseSensitivity));

			_masterVolume.onValueChanged.AddListener(OnMasterVolumeChanged);
			_mouseSensitivity.onValueChanged.AddListener(OnMouseSensitivityChanged);
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			_masterVolume.onValueChanged.RemoveListener(OnMasterVolumeChanged);
			_mouseSensitivity.onValueChanged.RemoveListener(OnMouseSensitivityChanged);
			_settings = null;
		}

		private void OnMasterVolumeChanged(float value)
		{
			_settings.Set(GameSettings.MasterVolume, value);
		}

		private void OnMouseSensitivityChanged(float value)
		{
			_settings.Set(GameSettings.MouseSensitivity, value);
		}
	}
}

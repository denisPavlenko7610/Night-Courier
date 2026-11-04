using System;
using NightCourier.Player;
using RD.Localization;
using UnityEngine;

namespace NightCourier.UI
{
	public sealed class MedkitHudView : MonoBehaviour
	{
		private const string MedkitsKey = "hud.medkits";

		[SerializeField] private LocalizedLabel _label;

		private PlayerMedkits _medkits;

		public void Bind(ILocalizationService localization, PlayerMedkits medkits)
		{
			if (localization == null)
			{
				throw new ArgumentNullException(nameof(localization));
			}

			Unbind();

			_medkits = medkits != null ? medkits : throw new ArgumentNullException(nameof(medkits));

			_label.Bind(localization);
			_medkits.Changed += Refresh;

			Refresh();
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_medkits == null)
			{
				return;
			}

			_medkits.Changed -= Refresh;
			_medkits = null;
		}

		private void Refresh()
		{
			int count = _medkits.Count;

			_label.gameObject.SetActive(count > 0);

			if (count > 0)
			{
				_label.SetKey(MedkitsKey, count);
			}
		}
	}
}

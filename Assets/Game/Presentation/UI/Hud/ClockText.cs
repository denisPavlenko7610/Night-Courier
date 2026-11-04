using UnityEngine;

namespace NightCourier.UI
{
	public static class ClockText
	{
		public static string Format(double seconds)
		{
			int whole = Mathf.CeilToInt((float)seconds);
			return $"{whole / 60:00}:{whole % 60:00}";
		}
	}
}

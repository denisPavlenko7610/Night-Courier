using System;
using System.Collections.Generic;

namespace NightCourier.App
{
	[Serializable]
	public sealed class LocalizationTable
	{
		public string Locale;
		public List<LocalizationEntry> Entries = new();
	}

	[Serializable]
	public sealed class LocalizationEntry
	{
		public string Key;
		public string Value;
	}
}

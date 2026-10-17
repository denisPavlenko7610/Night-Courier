using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using RD.Localization;
using RD.Foundation;
using UnityEngine;

namespace NightCourier.App
{
	public sealed class JsonLocalizationProvider : ILocalizationProvider
	{
		private readonly Dictionary<string, string> _entries;

		public JsonLocalizationProvider(TextAsset tableAsset)
		{
			if (tableAsset == null)
			{
				throw new ArgumentNullException(nameof(tableAsset));
			}

			LocalizationTable table = JsonUtility.FromJson<LocalizationTable>(tableAsset.text);

			if (table == null || string.IsNullOrWhiteSpace(table.Locale))
			{
				throw new InvalidOperationException($"Localization table '{tableAsset.name}' has no locale.");
			}

			_entries = new Dictionary<string, string>(table.Entries.Count, StringComparer.Ordinal);

			foreach (LocalizationEntry entry in table.Entries)
			{
				if (string.IsNullOrEmpty(entry.Key))
				{
					throw new InvalidOperationException($"Localization table '{tableAsset.name}' has an empty key.");
				}

				if (!_entries.TryAdd(entry.Key, entry.Value ?? string.Empty))
				{
					throw new InvalidOperationException(
						$"Localization table '{tableAsset.name}' repeats key '{entry.Key}'.");
				}
			}

			CurrentLocale = table.Locale.Trim();
			AvailableLocales = new[] { CurrentLocale };
		}

		public string CurrentLocale { get; }

		public IReadOnlyCollection<string> AvailableLocales { get; }

		public event Action<LocaleChanged> LocaleChanged
		{
			add { }
			remove { }
		}

		public bool TryGet(string key, object[] arguments, out string text)
		{
			if (!_entries.TryGetValue(key, out string template))
			{
				text = null;
				return false;
			}

			text = arguments == null || arguments.Length == 0
				? template
				: string.Format(CultureInfo.InvariantCulture, template, arguments);

			return true;
		}

		public Awaitable SetLocaleAsync(string locale, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (!string.Equals(locale, CurrentLocale, StringComparison.OrdinalIgnoreCase))
			{
				throw new LocaleNotAvailableException(locale, AvailableLocales);
			}

			return AwaitableExtensions.Completed;
		}
	}
}

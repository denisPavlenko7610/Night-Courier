#if UNITY_INCLUDE_TESTS
using System;
using NightCourier.App;
using NUnit.Framework;
using RD.Localization;
using UnityEngine;

namespace NightCourier.Tests
{
	public sealed class JsonLocalizationProviderTests
	{
		private const string Table =
			"{\"Locale\":\"en\",\"Entries\":[{\"Key\":\"a\",\"Value\":\"Alpha\"},{\"Key\":\"b\",\"Value\":\"Pay: {0}\"}]}";

		[Test]
		public void TryGet_ReturnsTextAndFormatsArguments()
		{
			JsonLocalizationProvider provider = new(new TextAsset(Table));

			Assert.That(provider.TryGet("a", Array.Empty<object>(), out string plain), Is.True);
			Assert.That(plain, Is.EqualTo("Alpha"));
			Assert.That(provider.TryGet("b", new object[] { 150 }, out string formatted), Is.True);
			Assert.That(formatted, Is.EqualTo("Pay: 150"));
			Assert.That(provider.TryGet("missing", Array.Empty<object>(), out _), Is.False);
		}

		[Test]
		public void Constructor_RejectsDuplicateKeysAndMissingLocale()
		{
			string duplicate = "{\"Locale\":\"en\",\"Entries\":[{\"Key\":\"a\",\"Value\":\"1\"},{\"Key\":\"a\",\"Value\":\"2\"}]}";
			string noLocale = "{\"Entries\":[]}";

			Assert.Throws<InvalidOperationException>(() => new JsonLocalizationProvider(new TextAsset(duplicate)));
			Assert.Throws<InvalidOperationException>(() => new JsonLocalizationProvider(new TextAsset(noLocale)));
			Assert.Throws<ArgumentNullException>(() => new JsonLocalizationProvider(null));
		}

		[Test]
		public void Service_UsesProvider_AndRejectsUnknownLocales()
		{
			LocalizationService service = new(new JsonLocalizationProvider(new TextAsset(Table)));

			Assert.That(service.Get("b", 7), Is.EqualTo("Pay: 7"));
			Assert.That(service.Get("nope"), Is.EqualTo("[nope]"));
			Assert.That(service.CurrentLocale, Is.EqualTo("en"));
			Assert.ThrowsAsync<LocaleNotAvailableException>(async () => await service.SetLocaleAsync("de"));
		}
	}
}
#endif

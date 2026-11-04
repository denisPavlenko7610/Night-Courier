#if UNITY_INCLUDE_TESTS
using NightCourier.UI;
using NUnit.Framework;
using UnityEngine;

namespace NightCourier.Tests
{
	public sealed class PromptGlyphsTests
	{
		[TestCase("<Keyboard>/e", "keyboard_e")]
		[TestCase("<Keyboard>/1", "keyboard_1")]
		[TestCase("<Keyboard>/space", "keyboard_space")]
		[TestCase("<Keyboard>/leftShift", "keyboard_shift")]
		[TestCase("<Keyboard>/rightCtrl", "keyboard_ctrl")]
		[TestCase("<Keyboard>/upArrow", "keyboard_arrow_up")]
		[TestCase("<Mouse>/leftButton", "mouse_left")]
		[TestCase("<Mouse>/rightButton", "mouse_right")]
		public void GlyphName_MapsControlsToKenneySprites(string path, string expected)
		{
			Assert.That(PromptGlyphs.GlyphName(path), Is.EqualTo(expected));
		}

		[TestCase("")]
		[TestCase(null)]
		[TestCase("<Gamepad>/buttonSouth")]
		[TestCase("<Mouse>/delta")]
		public void GlyphName_ReturnsNullWhereThereIsNoGlyph(string path)
		{
			Assert.That(PromptGlyphs.GlyphName(path), Is.Null);
		}

		[Test]
		public void FindKey_ReturnsNullWithoutSprites()
		{
			PromptGlyphs glyphs = ScriptableObject.CreateInstance<PromptGlyphs>();

			try
			{
				Assert.That(glyphs.FindKey("<Keyboard>/e"), Is.Null);
				Assert.That(glyphs.Lock, Is.Null);
			}
			finally
			{
				Object.DestroyImmediate(glyphs);
			}
		}
	}
}
#endif

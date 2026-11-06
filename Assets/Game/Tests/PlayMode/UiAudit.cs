#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace NightCourier.PlayModeTests
{
	internal sealed class UiAudit
	{
		private const float OverlapTolerancePixels = 3f;
		private const float EdgeTolerancePixels = 1f;
		private const float ClippedTolerancePixels = 3f;
		private const float MinimumOpacity = 0.05f;
		private const int MaximumPathLength = 90;
		private const int MaximumSnippetLength = 40;

		private static readonly Regex RichTextTags = new("<[^>]*>", RegexOptions.Compiled);
		private static readonly Regex SpaceRuns = new("[ \\t]{2,}", RegexOptions.Compiled);

		private readonly string _folder;
		private readonly StringBuilder _report = new();

		public UiAudit(string folder)
		{
			_folder = folder;
			Directory.CreateDirectory(folder);
		}

		public int Problems { get; private set; }

		public string Report => _report.ToString();

		public IEnumerator Capture(string name)
		{
			yield return new WaitForEndOfFrame();

			Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();

			File.WriteAllBytes(Path.Combine(_folder, name + ".png"), shot.EncodeToPNG());
			UnityEngine.Object.Destroy(shot);

			_report.AppendLine($"== {name} ({Screen.width}x{Screen.height})");

			if (Analyse() == 0)
			{
				_report.AppendLine("  (no visible text)");
			}
		}

		public void Save()
		{
			File.WriteAllText(Path.Combine(_folder, "report.txt"), _report.ToString());
		}

		private int Analyse()
		{
			List<TextElement> elements = new();

			foreach (UIDocument document in UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude))
			{
				VisualElement root = document.rootVisualElement;

				if (root?.panel == null)
				{
					continue;
				}

				foreach (TextElement element in root.Query<TextElement>().ToList())
				{
					if (IsRendered(element))
					{
						elements.Add(element);
					}
				}
			}

			foreach (TextElement element in elements)
			{
				Rect panelBounds = element.panel.visualTree.layout;
				Rect box = element.worldBound;
				Rect content = element.contentRect;
				string whiteSpace = element.resolvedStyle.whiteSpace.ToString();
				string measured = RichTextTags.Replace(element.text, string.Empty);

				if (whiteSpace == "Normal")
				{
					measured = SpaceRuns.Replace(measured, " ");
				}

				bool wraps = whiteSpace is "Normal" or "PreWrap";
				Vector2 size = wraps
					? element.MeasureTextSize(measured, content.width + ClippedTolerancePixels, VisualElement.MeasureMode.AtMost, 0f, VisualElement.MeasureMode.Undefined)
					: element.MeasureTextSize(measured, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);

				if (box.xMin < panelBounds.xMin - EdgeTolerancePixels || box.yMin < panelBounds.yMin - EdgeTolerancePixels
					|| box.xMax > panelBounds.xMax + EdgeTolerancePixels || box.yMax > panelBounds.yMax + EdgeTolerancePixels)
				{
					Add($"OFFSCREEN  {PathOf(element)}  \"{Snippet(element.text)}\"  box={Fmt(box)} panel={Fmt(panelBounds)}");
				}

				if (size.x > content.width + ClippedTolerancePixels || size.y > content.height + ClippedTolerancePixels)
				{
					Add($"OVERFLOW   {PathOf(element)}  \"{Snippet(element.text)}\"  text={size.x:F0}x{size.y:F0} content={content.width:F0}x{content.height:F0}");
				}
			}

			for (int left = 0; left < elements.Count; left++)
			{
				for (int right = left + 1; right < elements.Count; right++)
				{
					TextElement a = elements[left];
					TextElement b = elements[right];

					if (a.Contains(b) || b.Contains(a))
					{
						continue;
					}

					Rect shared = Intersect(a.worldBound, b.worldBound);

					if (shared.width > OverlapTolerancePixels && shared.height > OverlapTolerancePixels)
					{
						Add($"OVERLAP    {PathOf(a)} \"{Snippet(a.text)}\"  x  {PathOf(b)} \"{Snippet(b.text)}\"  shared={Fmt(shared)}");
					}
				}
			}

			return elements.Count;
		}

		private static bool IsRendered(TextElement element)
		{
			if (string.IsNullOrWhiteSpace(element.text) || float.IsNaN(element.layout.width) || element.layout.width <= 0f || element.layout.height <= 0f)
			{
				return false;
			}

			for (VisualElement current = element; current != null; current = current.parent)
			{
				if (current.resolvedStyle.display == DisplayStyle.None
					|| current.resolvedStyle.opacity < MinimumOpacity
					|| current.resolvedStyle.visibility != Visibility.Visible)
				{
					return false;
				}
			}

			return true;
		}

		private static string PathOf(VisualElement element)
		{
			StringBuilder path = new(string.IsNullOrEmpty(element.name) ? element.GetType().Name : element.name);

			for (VisualElement parent = element.parent; parent != null && path.Length < MaximumPathLength; parent = parent.parent)
			{
				if (!string.IsNullOrEmpty(parent.name))
				{
					path.Insert(0, parent.name + "/");
				}
			}

			return path.ToString();
		}

		private static string Snippet(string text)
		{
			string value = text.Replace("\n", " | ");

			return value.Length > MaximumSnippetLength ? value.Substring(0, MaximumSnippetLength) + "..." : value;
		}

		private void Add(string line)
		{
			Problems++;
			_report.AppendLine("  " + line);
		}

		private static Rect Intersect(Rect a, Rect b)
		{
			float xMin = Mathf.Max(a.xMin, b.xMin);
			float yMin = Mathf.Max(a.yMin, b.yMin);
			float xMax = Mathf.Min(a.xMax, b.xMax);
			float yMax = Mathf.Min(a.yMax, b.yMax);

			return xMax > xMin && yMax > yMin ? Rect.MinMaxRect(xMin, yMin, xMax, yMax) : default;
		}

		private static string Fmt(Rect rect)
		{
			return $"[{rect.xMin:F0},{rect.yMin:F0} {rect.width:F0}x{rect.height:F0}]";
		}

		public static void SetGameViewSize(int width, int height)
		{
#if UNITY_EDITOR
			Assembly editor = typeof(UnityEditor.Editor).Assembly;
			Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
			Type singleton = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
			object instance = singleton.GetProperty("instance").GetValue(null);
			object group = sizesType.GetMethod("GetGroup").Invoke(instance, new object[] { (int)UnityEditor.GameViewSizeGroupType.Standalone });
			Type groupType = group.GetType();
			Type sizeType = editor.GetType("UnityEditor.GameViewSize");
			Type sizeKind = editor.GetType("UnityEditor.GameViewSizeType");
			string label = $"UiAudit {width}x{height}";

			int total = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
			int index = -1;

			for (int candidate = 0; candidate < total; candidate++)
			{
				object existing = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { candidate });
				string name = (string)sizeType.GetProperty("baseText").GetValue(existing);

				if (name == label)
				{
					index = candidate;
					break;
				}
			}

			if (index < 0)
			{
				object size = Activator.CreateInstance(sizeType, Enum.ToObject(sizeKind, 1), width, height, label);

				groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
				index = total;
			}

			Type viewType = editor.GetType("UnityEditor.GameView");
			UnityEditor.EditorWindow view = UnityEditor.EditorWindow.GetWindow(viewType);

			viewType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.Invoke(view, new object[] { index, null });
#endif
		}
	}
}
#endif

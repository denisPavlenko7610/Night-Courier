#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.PlayModeTests
{
	internal sealed class UiAudit
	{
		private const float OverlapTolerancePixels = 3f;
		private const float EdgeTolerancePixels = 1f;
		private const float ClippedTolerancePixels = 3f;

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

			Canvas.ForceUpdateCanvases();

			Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();

			File.WriteAllBytes(Path.Combine(_folder, name + ".png"), shot.EncodeToPNG());
			UnityEngine.Object.Destroy(shot);

			_report.AppendLine($"== {name} ({Screen.width}x{Screen.height})");
			Analyse();
		}

		public void Save()
		{
			File.WriteAllText(Path.Combine(_folder, "report.txt"), _report.ToString());
		}

		private struct Item
		{
			public Text Text;
			public Rect Glyphs;
			public Rect Box;
			public string Path;
		}

		private void Analyse()
		{
			List<Item> items = new();

			foreach (Text text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
			{
				if (!text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text) || text.color.a < 0.05f || !IsShown(text))
				{
					continue;
				}

				if (!TryGlyphs(text, out Rect glyphs))
				{
					continue;
				}

				items.Add(new Item { Text = text, Glyphs = glyphs, Box = WorldRect(text.rectTransform), Path = PathOf(text.transform) });
			}

			foreach (Item item in items)
			{
				if (item.Glyphs.xMin < -EdgeTolerancePixels || item.Glyphs.yMin < -EdgeTolerancePixels
					|| item.Glyphs.xMax > Screen.width + EdgeTolerancePixels || item.Glyphs.yMax > Screen.height + EdgeTolerancePixels)
				{
					Add($"OFFSCREEN  {item.Path}  \"{Snippet(item.Text)}\"  glyphs={Fmt(item.Glyphs)}");
				}

				if (item.Glyphs.height > item.Box.height + ClippedTolerancePixels || item.Glyphs.width > item.Box.width + ClippedTolerancePixels)
				{
					Add($"OVERFLOW   {item.Path}  \"{Snippet(item.Text)}\"  glyphs={Fmt(item.Glyphs)} box={Fmt(item.Box)}");
				}
			}

			for (int left = 0; left < items.Count; left++)
			{
				for (int right = left + 1; right < items.Count; right++)
				{
					Item a = items[left];
					Item b = items[right];

					if (IsRelated(a.Text.transform, b.Text.transform))
					{
						continue;
					}

					Rect shared = Intersect(a.Glyphs, b.Glyphs);

					if (shared.width > OverlapTolerancePixels && shared.height > OverlapTolerancePixels)
					{
						Add($"OVERLAP    {a.Path} \"{Snippet(a.Text)}\"  x  {b.Path} \"{Snippet(b.Text)}\"  shared={Fmt(shared)}");
					}
				}
			}

			if (items.Count == 0)
			{
				_report.AppendLine("  (no visible text)");
			}
		}

		private void Add(string line)
		{
			Problems++;
			_report.AppendLine("  " + line);
		}

		private static bool IsShown(Text text)
		{
			CanvasGroup[] groups = text.GetComponentsInParent<CanvasGroup>();

			foreach (CanvasGroup group in groups)
			{
				if (group.alpha < 0.05f)
				{
					return false;
				}

				if (group.ignoreParentGroups)
				{
					break;
				}
			}

			return true;
		}

		private static bool TryGlyphs(Text text, out Rect glyphs)
		{
			glyphs = default;

			TextGenerator generator = text.cachedTextGenerator;
			IList<UIVertex> verts = generator.verts;

			if (generator.vertexCount == 0 || verts.Count == 0)
			{
				return false;
			}

			Vector2 min = new(float.MaxValue, float.MaxValue);
			Vector2 max = new(float.MinValue, float.MinValue);
			float scale = text.pixelsPerUnit;

			for (int index = 0; index < generator.vertexCount; index++)
			{
				Vector3 world = text.transform.TransformPoint(verts[index].position / scale);

				min = Vector2.Min(min, world);
				max = Vector2.Max(max, world);
			}

			glyphs = Rect.MinMaxRect(min.x, min.y, max.x, max.y);

			return glyphs.width > 0f && glyphs.height > 0f;
		}

		private static Rect WorldRect(RectTransform rect)
		{
			Vector3[] corners = new Vector3[4];

			rect.GetWorldCorners(corners);

			return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
		}

		private static Rect Intersect(Rect a, Rect b)
		{
			float xMin = Mathf.Max(a.xMin, b.xMin);
			float yMin = Mathf.Max(a.yMin, b.yMin);
			float xMax = Mathf.Min(a.xMax, b.xMax);
			float yMax = Mathf.Min(a.yMax, b.yMax);

			return xMax > xMin && yMax > yMin ? Rect.MinMaxRect(xMin, yMin, xMax, yMax) : default;
		}

		private static bool IsRelated(Transform a, Transform b)
		{
			return a == b || a.IsChildOf(b) || b.IsChildOf(a);
		}

		private static string PathOf(Transform transform)
		{
			StringBuilder path = new(transform.name);

			for (Transform parent = transform.parent; parent != null && path.Length < 90; parent = parent.parent)
			{
				path.Insert(0, parent.name + "/");
			}

			return path.ToString();
		}

		private static string Snippet(Text text)
		{
			string value = text.text.Replace("\n", " | ");

			return value.Length > 40 ? value.Substring(0, 40) + "..." : value;
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

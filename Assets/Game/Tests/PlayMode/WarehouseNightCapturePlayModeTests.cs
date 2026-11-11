#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NightCourier.PlayModeTests
{
	[Category("Visual")]
	public sealed class WarehouseNightCapturePlayModeTests
	{
		private const int TestTimeoutMilliseconds = 180000;
		private const string LightsOut = "contract.lights-out";
		private const string CaptureFolder = "Temp/NightCaptures";
		private const string DarkState = "dark";
		private const string PoweredState = "powered";
		private const int Width = 1920;
		private const int Height = 1080;
		private const float SettleSeconds = 0.6f;
		private const float PowerSettleSeconds = 1.5f;
		private const int SampleStride = 8;
		private const float MinimumHallGain = 0.02f;

		private static readonly Vector3 HallFloor = new(0f, 0.1f, -2f);
		private static readonly Vector3 DockSideFloor = new(-13f, 0.1f, -9f);
		private static readonly Vector3 LampPoint = new(-15.6f, 2.6f, -13.8f);

		private readonly Dictionary<string, float> _luminance = new();
		private readonly StringBuilder _report = new();
		private GameDriver _driver;

		[SetUp]
		public void DeleteTestSave()
		{
			GameDriver.DeleteTestSaves();

			_driver = new GameDriver();
		}

		[OneTimeTearDown]
		public void DeleteTestSaveAfterTheRun()
		{
			GameDriver.DeleteTestSaves();
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator Warehouse_PoweredHall_IsClearlyBrighterThanTheBlackout()
		{
			UiAudit.SetGameViewSize(Width, Height);

			yield return null;
			yield return null;

			Directory.CreateDirectory(CaptureFolder);

			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();
			yield return _driver.CompleteInState(GameDriver.StandardDelivery);
			yield return _driver.CompleteInState(GameDriver.Fragile);
			yield return _driver.AcceptInTerminal(LightsOut);
			yield return _driver.CarryPackageToTheVanAndDepart();
			yield return new WaitForSeconds(SettleSeconds);

			Vector3 spawnPosition = _driver.Player.transform.position;
			Quaternion spawnRotation = _driver.Player.transform.rotation;

			yield return CaptureViews(DarkState, spawnPosition, spawnRotation);

			Place(spawnPosition, spawnRotation);

			yield return _driver.PowerUpTheBlackout();
			yield return new WaitForSeconds(PowerSettleSeconds);

			yield return CaptureViews(PoweredState, spawnPosition, spawnRotation);

			File.WriteAllText(Path.Combine(CaptureFolder, "report.txt"), _report.ToString());

			Assert.That(_luminance["powered_hall_north"], Is.GreaterThan(_luminance["dark_hall_north"] + MinimumHallGain));
			Assert.That(_luminance["powered_hall_south"], Is.GreaterThan(_luminance["dark_hall_south"] + MinimumHallGain));
		}

		private IEnumerator CaptureViews(string state, Vector3 spawnPosition, Quaternion spawnRotation)
		{
			Place(spawnPosition, spawnRotation);

			yield return Capture(state + "_dock_spawn");

			Vector3 towardLamp = LampPoint - DockSideFloor;
			towardLamp.y = 0f;

			Place(DockSideFloor, Quaternion.LookRotation(towardLamp));

			yield return Capture(state + "_dock_lamp");

			Place(HallFloor, Quaternion.Euler(0f, 0f, 0f));

			yield return Capture(state + "_hall_north");

			Place(HallFloor, Quaternion.Euler(0f, 180f, 0f));

			yield return Capture(state + "_hall_south");
		}

		private void Place(Vector3 position, Quaternion rotation)
		{
			GameDriver.Teleport(_driver.Player, position);
			_driver.Player.transform.rotation = rotation;
			Physics.SyncTransforms();
		}

		private IEnumerator Capture(string name)
		{
			yield return new WaitForSeconds(SettleSeconds);
			yield return new WaitForEndOfFrame();

			Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();

			File.WriteAllBytes(Path.Combine(CaptureFolder, name + ".png"), shot.EncodeToPNG());

			float luminance = MeanLuminance(shot);

			_luminance[name] = luminance;
			_report.AppendLine($"{name} {luminance:F4}");
			Object.Destroy(shot);
		}

		private static float MeanLuminance(Texture2D texture)
		{
			Color32[] pixels = texture.GetPixels32();
			double total = 0d;
			int count = 0;

			for (int index = 0; index < pixels.Length; index += SampleStride)
			{
				Color32 pixel = pixels[index];

				total += (0.2126 * pixel.r + 0.7152 * pixel.g + 0.0722 * pixel.b) / 255d;
				count++;
			}

			return count == 0 ? 0f : (float)(total / count);
		}
	}
}
#endif

using System;

namespace NightCourier.World
{
	public sealed class WarehouseFacts
	{
		public bool PowerOn { get; private set; }

		public bool CargoScanned { get; private set; }

		public bool BatteryRequired { get; private set; }

		public bool BatteryInserted { get; private set; }

		public bool LeverReady => !PowerOn && (!BatteryRequired || BatteryInserted);

		public event Action Changed;

		public void SetBatteryRequired(bool required)
		{
			if (BatteryRequired == required)
			{
				return;
			}

			BatteryRequired = required;
			Changed?.Invoke();
		}

		public void SetBatteryInserted(bool inserted)
		{
			if (BatteryInserted == inserted)
			{
				return;
			}

			BatteryInserted = inserted;
			Changed?.Invoke();
		}

		public void SetPower(bool on)
		{
			if (PowerOn == on)
			{
				return;
			}

			PowerOn = on;
			Changed?.Invoke();
		}

		public void SetCargoScanned(bool scanned)
		{
			if (CargoScanned == scanned)
			{
				return;
			}

			CargoScanned = scanned;
			Changed?.Invoke();
		}
	}
}

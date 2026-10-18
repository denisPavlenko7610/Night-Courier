namespace NightCourier.Weapons
{
	public enum WeaponKind
	{
		StunPistol = 0,
		Crowbar = 1
	}

	public static class WeaponRules
	{
		public static bool CanUse(WeaponKind kind, bool carryingPackage, bool carryingHeavy)
		{
			if (carryingHeavy)
			{
				return false;
			}

			return kind == WeaponKind.StunPistol || !carryingPackage;
		}

		public static string NameKey(WeaponKind kind)
		{
			return kind switch
			{
				WeaponKind.StunPistol => "weapon.stun_pistol",
				WeaponKind.Crowbar => "weapon.crowbar",
				_ => throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "Unknown weapon.")
			};
		}
	}
}

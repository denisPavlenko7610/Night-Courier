using RD.Equipment;
using RD.Inventory;

namespace NightCourier.Loadout
{
	public static class LoadoutIds
	{
		public static readonly EquipmentSlotId Primary = new("slot.primary");
		public static readonly EquipmentSlotId Tool = new("slot.tool");
		public static readonly EquipmentSlotId UtilityFirst = new("slot.utility.1");
		public static readonly EquipmentSlotId UtilitySecond = new("slot.utility.2");

		public static readonly EquipmentDefinitionId StunPistol = new("equipment.stun_pistol");
		public static readonly EquipmentDefinitionId Crowbar = new("equipment.crowbar");
		public static readonly EquipmentDefinitionId Medkit = new("equipment.medkit");

		public static readonly EquipmentInstanceId StunPistolInstance = new("instance.stun_pistol");
		public static readonly EquipmentInstanceId CrowbarInstance = new("instance.crowbar");

		public static readonly ItemId MedkitItem = new("consumable.medkit");

		public const string MedkitInstancePrefix = "instance.medkit.";
	}

	public static class LoadoutRules
	{
		public const long MedkitPrice = 30;
		public const int MedkitCapacity = 3;
		public const int InventorySlots = 4;
		public const double MedkitHealAmount = 40d;
	}
}

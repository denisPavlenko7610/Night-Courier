using System;
using System.Collections.Generic;
using NightCourier.Economy;
using NightCourier.Weapons;
using RD.Equipment;
using RD.Inventory;
using RD.Wallet;

namespace NightCourier.Loadout
{
	public enum MedkitPurchaseStatus
	{
		Purchased = 0,
		AtCapacity = 1,
		InsufficientFunds = 2
	}

	public sealed class CourierLoadout
	{
		private static readonly EquipmentSlotId[] UtilitySlots = { LoadoutIds.UtilityFirst, LoadoutIds.UtilitySecond };

		private readonly IWallet _wallet;
		private int _nextMedkitInstance = 1;

		public CourierLoadout(IWallet wallet)
		{
			_wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));

			Equipment = new EquipmentLoadout(
				new[] { LoadoutIds.Primary, LoadoutIds.Tool, LoadoutIds.UtilityFirst, LoadoutIds.UtilitySecond },
				new[]
				{
					new EquipmentDefinition(LoadoutIds.StunPistol, new[] { LoadoutIds.Primary }),
					new EquipmentDefinition(LoadoutIds.Crowbar, new[] { LoadoutIds.Tool }),
					new EquipmentDefinition(LoadoutIds.Medkit, UtilitySlots)
				});

			Inventory = new InventoryContainer(
				new ItemCatalog(new[] { new ItemDefinition(LoadoutIds.MedkitItem, LoadoutRules.MedkitCapacity) }),
				LoadoutRules.InventorySlots);

			EquipDefaults();

			Equipment.Changed += OnChanged;
			Inventory.Changed += OnChanged;
		}

		public EquipmentLoadout Equipment { get; }

		public InventoryContainer Inventory { get; }

		public event Action Changed;

		public int MedkitsInStock => (int)Inventory.GetQuantity(LoadoutIds.MedkitItem);

		public int MedkitsEquipped
		{
			get
			{
				int count = 0;

				foreach (EquipmentSlotId slot in UtilitySlots)
				{
					if (HasMedkit(slot))
					{
						count++;
					}
				}

				return count;
			}
		}

		public int MedkitsOwned => MedkitsInStock + MedkitsEquipped;

		public bool HasMedkit(EquipmentSlotId slot)
		{
			return Equipment.TryGet(slot, out EquipmentItem item) && item.DefinitionId == LoadoutIds.Medkit;
		}

		public bool IsWeaponEquipped(WeaponKind kind)
		{
			return Equipment.TryGet(SlotOf(kind), out EquipmentItem item) && item.DefinitionId == DefinitionOf(kind);
		}

		public void SetWeaponEquipped(WeaponKind kind, bool equipped)
		{
			EquipmentResult result = equipped
				? Equipment.Equip(new EquipmentItem(InstanceOf(kind), DefinitionOf(kind)), SlotOf(kind))
				: Equipment.Unequip(SlotOf(kind));

			Require(result);
		}

		public MedkitPurchaseStatus TryBuyMedkit()
		{
			if (MedkitsOwned >= LoadoutRules.MedkitCapacity)
			{
				return MedkitPurchaseStatus.AtCapacity;
			}

			WalletOperationResult payment = _wallet.Spend(GameCurrencies.Credits, LoadoutRules.MedkitPrice);

			if (!payment.IsSuccess)
			{
				return payment.Status == WalletOperationStatus.InsufficientFunds
					? MedkitPurchaseStatus.InsufficientFunds
					: throw new InvalidOperationException($"The medkit purchase failed: {payment.Status}. {payment.Message}");
			}

			Require(Inventory.Add(LoadoutIds.MedkitItem, 1, InventoryOperationMode.RequireFull));

			return MedkitPurchaseStatus.Purchased;
		}

		public bool TryEquipMedkit(EquipmentSlotId slot)
		{
			if (!IsUtilitySlot(slot) || HasMedkit(slot) || MedkitsInStock < 1)
			{
				return false;
			}

			Require(Inventory.Remove(LoadoutIds.MedkitItem, 1));

			EquipmentInstanceId instance = new(LoadoutIds.MedkitInstancePrefix + _nextMedkitInstance++);

			Require(Equipment.Equip(new EquipmentItem(instance, LoadoutIds.Medkit), slot, EquipPolicy.RequireEmpty));

			return true;
		}

		public bool TryUnequipMedkit(EquipmentSlotId slot)
		{
			if (!IsUtilitySlot(slot) || !HasMedkit(slot))
			{
				return false;
			}

			Require(Equipment.Unequip(slot));
			Require(Inventory.Add(LoadoutIds.MedkitItem, 1, InventoryOperationMode.RequireFull));

			return true;
		}

		public bool TryConsumeMedkit()
		{
			foreach (EquipmentSlotId slot in UtilitySlots)
			{
				if (HasMedkit(slot))
				{
					Require(Equipment.Unequip(slot));

					return true;
				}
			}

			return false;
		}

		public LoadoutSave Capture()
		{
			return new LoadoutSave
			{
				Equipment = Equipment.CaptureSnapshot(),
				Inventory = Inventory.CaptureSnapshot(),
				NextMedkitInstance = _nextMedkitInstance
			};
		}

		public void Restore(LoadoutSave save)
		{
			if (save == null || save.Equipment == null || save.Inventory == null)
			{
				ResetToDefaults();
				return;
			}

			EquipmentResult equipment = Equipment.Restore(save.Equipment);
			InventoryResult inventory = Inventory.Restore(save.Inventory);

			if (!equipment.IsSuccess || !inventory.IsSuccess)
			{
				ResetToDefaults();
				return;
			}

			_nextMedkitInstance = Math.Max(1, save.NextMedkitInstance);
			Changed?.Invoke();
		}

		private void ResetToDefaults()
		{
			foreach (EquipmentSlotId slot in new[] { LoadoutIds.Primary, LoadoutIds.Tool, LoadoutIds.UtilityFirst, LoadoutIds.UtilitySecond })
			{
				Require(Equipment.Unequip(slot));
			}

			int stock = MedkitsInStock;

			if (stock > 0)
			{
				Require(Inventory.Remove(LoadoutIds.MedkitItem, stock));
			}

			_nextMedkitInstance = 1;
			EquipDefaults();
			Changed?.Invoke();
		}

		private void EquipDefaults()
		{
			Require(Equipment.Equip(new EquipmentItem(LoadoutIds.StunPistolInstance, LoadoutIds.StunPistol), LoadoutIds.Primary));
			Require(Equipment.Equip(new EquipmentItem(LoadoutIds.CrowbarInstance, LoadoutIds.Crowbar), LoadoutIds.Tool));
		}

		private void OnChanged(IReadOnlyList<EquipmentChange> changes)
		{
			Changed?.Invoke();
		}

		private void OnChanged(InventoryChanged change)
		{
			Changed?.Invoke();
		}

		private static bool IsUtilitySlot(EquipmentSlotId slot)
		{
			return slot == LoadoutIds.UtilityFirst || slot == LoadoutIds.UtilitySecond;
		}

		private static EquipmentSlotId SlotOf(WeaponKind kind)
		{
			return kind == WeaponKind.StunPistol ? LoadoutIds.Primary : LoadoutIds.Tool;
		}

		private static EquipmentDefinitionId DefinitionOf(WeaponKind kind)
		{
			return kind == WeaponKind.StunPistol ? LoadoutIds.StunPistol : LoadoutIds.Crowbar;
		}

		private static EquipmentInstanceId InstanceOf(WeaponKind kind)
		{
			return kind == WeaponKind.StunPistol ? LoadoutIds.StunPistolInstance : LoadoutIds.CrowbarInstance;
		}

		private static void Require(EquipmentResult result)
		{
			if (!result.IsSuccess)
			{
				throw new InvalidOperationException($"The loadout operation failed: {result.Status}. {result.Message}");
			}
		}

		private static void Require(InventoryResult result)
		{
			if (!result.IsSuccess)
			{
				throw new InvalidOperationException($"The inventory operation failed: {result.Status}. {result.Message}");
			}
		}
	}

	[Serializable]
	public sealed class LoadoutSave
	{
		public EquipmentSnapshot Equipment;
		public InventorySnapshot Inventory;
		public int NextMedkitInstance = 1;
	}
}

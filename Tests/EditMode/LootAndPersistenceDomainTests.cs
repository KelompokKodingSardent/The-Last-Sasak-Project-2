using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TheLastSasak.Items.Equipment;
using TheLastSasak.Items.Loot;
using TheLastSasak.Items.Modifiers;
using TheLastSasak.Systems.Save;

namespace TheLastSasak.Tests.EditMode
{
    [TestFixture]
    public class LootAndPersistenceDomainTests
    {
        private ItemTestFixtureBuilder _builder;

        [SetUp]
        public void SetUp()
        {
            _builder = new ItemTestFixtureBuilder();
        }

        [TearDown]
        public void TearDown()
        {
            _builder.Dispose();
        }

        [Test]
        public void L01_ItemPickup_AddsItemToInventoryAndMarksCollected()
        {
            var rig = _builder.CreatePlayerRig();
            var klewang = _builder.CreateWeapon("weapon.klewang", "Klewang", "KlewangVisual");
            var lootDef = _builder.CreateLoot("loot.klewang", LootRewardType.Item, klewang, 1);

            var pickupObj = _builder.Track(new GameObject("Pickup_Klewang"));
            var pickup = pickupObj.AddComponent<LootPickup>();
            pickup.Initialize(lootDef, 1, null, null);

            bool success = pickup.TryCollectBy(rig.LootCollector);

            Assert.IsTrue(success);
            Assert.IsTrue(pickup.IsCollected);
            Assert.IsFalse(pickupObj.activeSelf);
            Assert.IsTrue(rig.Inventory.HasItem(klewang));
        }

        [Test]
        public void L02_FullInventoryPickup_PreservesWorldPickupWithoutLoss()
        {
            var rig = _builder.CreatePlayerRig();
            ItemTestFixtureBuilder.SetPrivateField(rig.Inventory, "capacity", 1);

            var matA = _builder.CreateMaterial("material.a", "Mat A", maxStack: 1);
            var matB = _builder.CreateMaterial("material.b", "Mat B", maxStack: 1);
            rig.Inventory.TryAddItem(matA, 1);

            var lootDefB = _builder.CreateLoot("loot.mat_b", LootRewardType.Item, matB, 1);
            var pickupObj = _builder.Track(new GameObject("Pickup_MatB"));
            var pickup = pickupObj.AddComponent<LootPickup>();
            pickup.Initialize(lootDefB, 1, null, null);

            bool collected = pickup.TryCollectBy(rig.LootCollector);

            Assert.IsFalse(collected);
            Assert.IsFalse(pickup.IsCollected);
            Assert.IsTrue(pickupObj.activeSelf);
            Assert.AreEqual(1, pickup.RemainingAmount);
        }

        [Test]
        public void L03_CurrencyPickup_AddsSoulsToWallet()
        {
            var rig = _builder.CreatePlayerRig();
            var soulsLoot = _builder.CreateLoot("loot.souls", LootRewardType.Currency, amount: 75);

            var pickupObj = _builder.Track(new GameObject("Pickup_Souls"));
            var pickup = pickupObj.AddComponent<LootPickup>();
            pickup.Initialize(soulsLoot, 75, null, null);

            Assert.IsTrue(pickup.TryCollectBy(rig.LootCollector));
            Assert.AreEqual(75, rig.WalletAndExp.Souls);
            Assert.AreEqual(0, rig.Inventory.Controller.Model.GetUsedSlotCount());
        }

        [Test]
        public void L04_ExpPickup_AddsExperienceToExperienceSystem()
        {
            var rig = _builder.CreatePlayerRig();
            var expLoot = _builder.CreateLoot("loot.exp", LootRewardType.Experience, amount: 120);

            var pickupObj = _builder.Track(new GameObject("Pickup_Exp"));
            var pickup = pickupObj.AddComponent<LootPickup>();
            pickup.Initialize(expLoot, 120, null, null);

            Assert.IsTrue(pickup.TryCollectBy(rig.LootCollector));
            Assert.AreEqual(120, rig.WalletAndExp.Experience);
            Assert.AreEqual(0, rig.Inventory.Controller.Model.GetUsedSlotCount());
        }

        [Test]
        public void L05_RepeatedPickupAndRewardCollector_PreventsDoubleRewards()
        {
            var rig = _builder.CreatePlayerRig();
            var soulsLoot = _builder.CreateLoot("loot.souls", LootRewardType.Currency, amount: 50);

            var pickupObj = _builder.Track(new GameObject("Pickup_Souls"));
            var pickup = pickupObj.AddComponent<LootPickup>();
            pickup.Initialize(soulsLoot, 50, null, null);

            Assert.IsTrue(pickup.TryCollectBy(rig.LootCollector));
            Assert.IsFalse(pickup.TryCollectBy(rig.LootCollector));
            Assert.AreEqual(50, rig.WalletAndExp.Souls);

            // Uji anti double-reward pada RewardCollector musuh
            var enemyObj = _builder.Track(new GameObject("Enemy"));
            var rewardCollector = enemyObj.AddComponent<RewardCollector>();
            ItemTestFixtureBuilder.SetPrivateField(rewardCollector, "directSoulsReward", 30);
            ItemTestFixtureBuilder.SetPrivateField(rewardCollector, "directExperienceReward", 40);

            rewardCollector.DistributeRewards(rig.Root);
            rewardCollector.DistributeRewards(rig.Root);

            Assert.AreEqual(80, rig.WalletAndExp.Souls);
            Assert.AreEqual(40, rig.WalletAndExp.Experience);
        }

        [Test]
        public void L06_PooledPickup_ResetsStateCleanlyOnReuse()
        {
            var rig = _builder.CreatePlayerRig();
            var shard = _builder.CreateMaterial("material.serpihan_roh", "Serpihan Roh", maxStack: 10);

            var prefabObj = _builder.Track(new GameObject("Pickup_Prefab"));
            prefabObj.SetActive(false);
            var prefabPickup = prefabObj.AddComponent<LootPickup>();

            var lootDef = _builder.CreateLoot("loot.shard", LootRewardType.Item, shard, 2, prefabPickup);

            var dropCtrlObj = _builder.Track(new GameObject("LootDropController"));
            var dropCtrl = dropCtrlObj.AddComponent<LootDropController>();

            // Siklus 1: Spawn 2, ambil semua, kembali ke pool
            LootPickup instance1 = dropCtrl.SpawnLoot(lootDef, Vector3.zero, 2);
            Assert.IsTrue(instance1.TryCollectBy(rig.LootCollector));
            Assert.IsFalse(instance1.gameObject.activeSelf);

            // Siklus 2: Spawn ulang 5 dari pool yang sama
            LootPickup instance2 = dropCtrl.SpawnLoot(lootDef, Vector3.one, 5);
            Assert.AreSame(instance1, instance2);
            Assert.IsFalse(instance2.IsCollected);
            Assert.AreEqual(5, instance2.RemainingAmount);
            Assert.IsTrue(instance2.TryCollectBy(rig.LootCollector));
            Assert.AreEqual(7, rig.Inventory.GetItemCount(shard));
        }

        [Test]
        public void P01_And_P02_SaveAndLoadInventory_RestoresAllItemsAndSlotIndices()
        {
            var rig = _builder.CreatePlayerRig();
            var klewang = _builder.CreateWeapon("weapon.klewang", "Klewang", "KlewangVisual");
            var shard = _builder.CreateMaterial("material.serpihan_roh", "Serpihan Roh", maxStack: 10);
            var catalog = _builder.CreateCatalog(klewang, shard);

            var saveSystem = CreateTestSaveSystem(rig, catalog, "tls_test_p01.json");

            rig.Inventory.TryAddItem(klewang, 1);
            rig.Inventory.TryAddItem(shard, 4);

            Assert.IsTrue(saveSystem.SaveGame());

            // Bersihkan inventory lalu muat ulang dari JSON
            rig.Inventory.Controller.ClearInventory();
            Assert.AreEqual(0, rig.Inventory.Controller.Model.GetUsedSlotCount());

            Assert.IsTrue(saveSystem.LoadGame());
            Assert.IsTrue(rig.Inventory.HasItem(klewang));
            Assert.AreEqual(4, rig.Inventory.GetItemCount(shard));
            Assert.AreEqual(klewang, rig.Inventory.GetSlots()[0].ItemDefinition);
            Assert.AreEqual(shard, rig.Inventory.GetSlots()[1].ItemDefinition);
        }

        [Test]
        public void P03_And_P04_SaveAndLoadEquipment_RestoresSlotsVisualsAndRecalculatedStats()
        {
            var baseStats = _builder.CreateBaseStats(maxHp: 100f, damage: 25f);
            var rig = _builder.CreatePlayerRig(baseStats);

            var klewang = _builder.CreateWeapon("weapon.klewang", "Klewang", "KlewangVisual");
            var pedang = _builder.CreateWeapon(
                "weapon.pedang_cepat", "Pedang Cepat", "PedangCepatVisual",
                modifiers: new StatModifier(StatType.AttackDamage, ModifierOperation.Additive, 15f));
            var armor = _builder.CreateArmor(
                "armor.besi_ambat", "Besi Ambat",
                modifiers: new StatModifier(StatType.MaxHealth, ModifierOperation.Additive, 20f));
            var artifact = _builder.CreateArtifact(
                "artifact.batu_ambang", "Batu Ambang",
                new StatModifier(StatType.MaxHealth, ModifierOperation.Additive, 25f));

            var catalog = _builder.CreateCatalog(klewang, pedang, armor, artifact);
            var saveSystem = CreateTestSaveSystem(rig, catalog, "tls_test_p03.json");

            rig.Inventory.TryAddItem(klewang);
            rig.Inventory.TryAddItem(pedang);
            rig.Inventory.TryAddItem(armor);
            rig.Inventory.TryAddItem(artifact);

            rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot1, klewang);
            rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot2, pedang);
            rig.Equipment.TrySwitchWeaponSlot(WeaponSlotType.Slot2);
            rig.Equipment.TryEquipArmor(armor);
            rig.Artifact.TryEquipArtifact(0, artifact);

            Assert.IsTrue(saveSystem.SaveGame());

            // Reset seluruh state runtime sebelum load
            rig.Equipment.RestoreEquipmentState(null, null, WeaponSlotType.None, null);
            rig.Artifact.RestoreArtifactState(null);
            rig.Inventory.Controller.ClearInventory();

            Assert.IsTrue(saveSystem.LoadGame());

            Assert.AreEqual(klewang, rig.Equipment.State.WeaponSlot1);
            Assert.AreEqual(pedang, rig.Equipment.State.WeaponSlot2);
            Assert.AreEqual(WeaponSlotType.Slot2, rig.Equipment.ActiveWeaponSlot);
            Assert.AreEqual(armor, rig.Equipment.EquippedArmor);
            Assert.AreEqual(artifact, rig.Artifact.GetArtifactInSlot(0));
            Assert.IsTrue(rig.WeaponSlot02Root.activeSelf);
            Assert.AreEqual(40f, rig.StatController.FinalAttackDamage, 0.001f);
            Assert.AreEqual(145f, rig.StatController.FinalMaxHealth, 0.001f);
        }

        [Test]
        public void P05_InvalidItemId_SafelyDiscardedWithWarningDuringLoad()
        {
            var rig = _builder.CreatePlayerRig();
            var klewang = _builder.CreateWeapon("weapon.klewang", "Klewang", "KlewangVisual");
            var catalog = _builder.CreateCatalog(klewang);
            var saveSystem = CreateTestSaveSystem(rig, catalog, "tls_test_p05.json");

            var corruptedData = new TLS_SaveData();
            corruptedData.InventorySaveData.Items.Add(new InventorySlotSaveEntry(0, "weapon.klewang", 1));
            corruptedData.InventorySaveData.Items.Add(new InventorySlotSaveEntry(1, "weapon.id_tidak_valid", 1));
            corruptedData.EquipmentSaveData.WeaponSlot1Id = "weapon.klewang";
            corruptedData.EquipmentSaveData.WeaponSlot2Id = "weapon.id_tidak_valid";
            corruptedData.EquipmentSaveData.ActiveWeaponSlot = 1;

            File.WriteAllText(saveSystem.SaveFilePath, JsonUtility.ToJson(corruptedData, true));

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("tidak ditemukan di catalog"));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("tidak valid atau tidak ada di catalog"));

            Assert.IsTrue(saveSystem.LoadGame());
            Assert.IsTrue(rig.Inventory.HasItem(klewang));
            Assert.AreEqual(1, rig.Inventory.Controller.Model.GetUsedSlotCount());
            Assert.IsNull(rig.Equipment.State.WeaponSlot2);
        }

        [Test]
        public void P06_VersionMigration_MigratesV0SaveAndNormalizesIds()
        {
            var rig = _builder.CreatePlayerRig();
            var klewang = _builder.CreateWeapon("weapon.klewang", "Klewang", "KlewangVisual");
            var catalog = _builder.CreateCatalog(klewang);
            var saveSystem = CreateTestSaveSystem(rig, catalog, "tls_test_p06.json");

            var legacyV0 = new TLS_SaveData
            {
                SaveVersion = 0
            };
            legacyV0.InventorySaveData.Items.Add(new InventorySlotSaveEntry(0, "  WEAPON.KLEWANG  ", 1));
            legacyV0.EquipmentSaveData.WeaponSlot1Id = "  WEAPON.KLEWANG ";
            legacyV0.EquipmentSaveData.ActiveWeaponSlot = 1;

            File.WriteAllText(saveSystem.SaveFilePath, JsonUtility.ToJson(legacyV0, true));

            Assert.IsTrue(saveSystem.LoadGame());
            Assert.AreEqual(TLS_SaveData.LatestSaveVersion, saveSystem.CurrentSaveData.SaveVersion);
            Assert.IsTrue(rig.Inventory.HasItem(klewang));
            Assert.AreEqual(klewang, rig.Equipment.ActiveWeapon);
        }

        private SaveSystem CreateTestSaveSystem(PlayerRig rig, TheLastSasak.Items.Catalog.ItemCatalogSO catalog, string fileName)
        {
            var coordinator = rig.Root.AddComponent<ItemPersistenceCoordinator>();
            ItemTestFixtureBuilder.SetPrivateField(coordinator, "itemCatalog", catalog);
            ItemTestFixtureBuilder.SetPrivateField(coordinator, "inventory", rig.Inventory);
            ItemTestFixtureBuilder.SetPrivateField(coordinator, "equipmentController", rig.Equipment);
            ItemTestFixtureBuilder.SetPrivateField(coordinator, "artifactController", rig.Artifact);
            ItemTestFixtureBuilder.SetPrivateField(coordinator, "playerStatController", rig.StatController);

            var saveObj = _builder.Track(new GameObject("TestSaveSystem"));
            var saveSystem = saveObj.AddComponent<SaveSystem>();
            ItemTestFixtureBuilder.SetPrivateField(saveSystem, "saveFileName", fileName);
            ItemTestFixtureBuilder.SetPrivateField(saveSystem, "itemPersistence", coordinator);

            _builder.TrackFile(saveSystem.SaveFilePath);
            return saveSystem;
        }
    }
}
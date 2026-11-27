using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TheLastSasak.Items.Inventory;
using TheLastSasak.Items.Loot;

namespace TheLastSasak.Tests.EditMode
{
    [TestFixture]
    public class InventoryDomainTests
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
        public void I01_AddItem_ValidItemAddedToFirstAvailableSlot()
        {
            var inv = new InventoryController(20);
            var klewang = _builder.CreateWeapon("weapon.klewang", "Klewang", "KlewangVisual");

            bool result = inv.TryAddItem(klewang, 1);

            Assert.IsTrue(result);
            Assert.IsTrue(inv.HasItem(klewang));
            Assert.AreEqual(1, inv.GetItemCount(klewang));
            Assert.AreEqual(1, inv.Model.GetUsedSlotCount());
        }

        [Test]
        public void I02_StackItem_IncreasesQuantityInExistingSlot()
        {
            var inv = new InventoryController(20);
            var shard = _builder.CreateMaterial("material.serpihan_roh", "Serpihan Roh", maxStack: 10);

            inv.TryAddItem(shard, 3);
            inv.TryAddItem(shard, 4);

            Assert.AreEqual(7, inv.GetItemCount(shard));
            Assert.AreEqual(1, inv.Model.GetUsedSlotCount());
        }

        [Test]
        public void I03_MaxStack_OverflowsIntoNewSlot()
        {
            var inv = new InventoryController(20);
            var shard = _builder.CreateMaterial("material.serpihan_roh", "Serpihan Roh", maxStack: 5);

            bool added = inv.TryAddItem(shard, 8, out int amountAdded);

            Assert.IsTrue(added);
            Assert.AreEqual(8, amountAdded);
            Assert.AreEqual(2, inv.Model.GetUsedSlotCount());
            Assert.AreEqual(5, inv.GetSlots()[0].Quantity);
            Assert.AreEqual(3, inv.GetSlots()[1].Quantity);
        }

        [Test]
        public void I04_InventoryFull_SafelyRejectsOverflowWithoutDataLoss()
        {
            var inv = new InventoryController(capacity: 2);
            var matA = _builder.CreateMaterial("material.a", "Mat A", maxStack: 2);
            var matB = _builder.CreateMaterial("material.b", "Mat B", maxStack: 2);

            inv.TryAddItem(matA, 2);
            inv.TryAddItem(matB, 2);

            bool rejected = inv.TryAddItem(matA, 1, out int addedWhenFull);

            Assert.IsFalse(rejected);
            Assert.AreEqual(0, addedWhenFull);
            Assert.AreEqual(2, inv.GetItemCount(matA));
            Assert.AreEqual(2, inv.GetItemCount(matB));
        }

        [Test]
        public void I05_RemoveItem_ReducesQuantityAndRejectsInsufficientStockAtomically()
        {
            var inv = new InventoryController(20);
            var shard = _builder.CreateMaterial("material.serpihan_roh", "Serpihan Roh", maxStack: 10);
            inv.TryAddItem(shard, 5);

            // Permintaan melebihi stok harus ditolak tanpa mengubah jumlah
            bool overRemove = inv.TryRemoveItem(shard, 6);
            Assert.IsFalse(overRemove);
            Assert.AreEqual(5, inv.GetItemCount(shard));

            // Penghapusan sebagian yang valid
            bool partialRemove = inv.TryRemoveItem(shard, 3);
            Assert.IsTrue(partialRemove);
            Assert.AreEqual(2, inv.GetItemCount(shard));

            // Penghapusan seluruh sisa mengosongkan slot
            bool fullRemove = inv.TryRemoveItem(shard, 2);
            Assert.IsTrue(fullRemove);
            Assert.IsFalse(inv.HasItem(shard));
            Assert.AreEqual(0, inv.Model.GetUsedSlotCount());
        }

        [Test]
        public void I06_InvalidItem_NullOrZeroQuantityRejected()
        {
            var inv = new InventoryController(20);
            var shard = _builder.CreateMaterial("material.serpihan_roh", "Serpihan Roh", maxStack: 5);

            Assert.IsFalse(inv.TryAddItem(null, 1));
            Assert.IsFalse(inv.TryAddItem(shard, 0));
            Assert.IsFalse(inv.TryAddItem(shard, -3));
            Assert.AreEqual(0, inv.Model.GetUsedSlotCount());
        }

        [Test]
        public void I07_DuplicateId_CatalogDetectsAndLogsError()
        {
            var w1 = _builder.CreateWeapon("weapon.klewang", "Klewang A", "KlewangVisual");
            var w2 = _builder.CreateWeapon("weapon.klewang", "Klewang Duplikat", "KlewangVisual");
            var catalog = _builder.CreateCatalog(w1, w2);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("DUPLIKAT ItemId"));
            catalog.ValidateCatalog();
        }

        [Test]
        public void I08_CurrencySeparation_SoulsRoutedToWalletNotInventory()
        {
            var rig = _builder.CreatePlayerRig();
            var soulsLoot = _builder.CreateLoot("loot.souls_50", LootRewardType.Currency, amount: 50);

            bool collected = rig.LootCollector.TryCollectLoot(soulsLoot, 50, out int consumed);

            Assert.IsTrue(collected);
            Assert.AreEqual(50, consumed);
            Assert.AreEqual(50, rig.WalletAndExp.Souls);
            Assert.AreEqual(0, rig.Inventory.Controller.Model.GetUsedSlotCount());
        }

        [Test]
        public void I09_ExpSeparation_ExpRoutedToExperienceSystemNotInventory()
        {
            var rig = _builder.CreatePlayerRig();
            var expLoot = _builder.CreateLoot("loot.exp_100", LootRewardType.Experience, amount: 100);

            bool collected = rig.LootCollector.TryCollectLoot(expLoot, 100, out int consumed);

            Assert.IsTrue(collected);
            Assert.AreEqual(100, consumed);
            Assert.AreEqual(100, rig.WalletAndExp.Experience);
            Assert.AreEqual(0, rig.Inventory.Controller.Model.GetUsedSlotCount());
        }
    }
}
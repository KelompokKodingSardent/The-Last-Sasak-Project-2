using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TheLastSasak.Items.Equipment;

namespace TheLastSasak.Tests.EditMode
{
    [TestFixture]
    public class EquipmentDomainTests
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
        public void E01_EquipWeapon1_ActivatesSlot1WhenOwned()
        {
            var rig = _builder.CreatePlayerRig();
            var klewang = _builder.CreateWeapon("weapon.klewang", "Klewang", "KlewangVisual");
            rig.Inventory.TryAddItem(klewang);

            bool equipped = rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot1, klewang);

            Assert.IsTrue(equipped);
            Assert.AreEqual(klewang, rig.Equipment.State.WeaponSlot1);
            Assert.AreEqual(WeaponSlotType.Slot1, rig.Equipment.ActiveWeaponSlot);
            Assert.AreEqual(klewang, rig.Equipment.ActiveWeapon);
        }

        [Test]
        public void E02_EquipWeapon2_StoresInSlot2Independently()
        {
            var rig = _builder.CreatePlayerRig();
            var klewang = _builder.CreateWeapon("weapon.klewang", "Klewang", "KlewangVisual");
            var pedang = _builder.CreateWeapon("weapon.pedang_cepat", "Pedang Cepat", "PedangCepatVisual");
            rig.Inventory.TryAddItem(klewang);
            rig.Inventory.TryAddItem(pedang);

            rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot1, klewang);
            bool equipped2 = rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot2, pedang);

            Assert.IsTrue(equipped2);
            Assert.AreEqual(pedang, rig.Equipment.State.WeaponSlot2);
            Assert.AreEqual(WeaponSlotType.Slot1, rig.Equipment.ActiveWeaponSlot);
        }

        [Test]
        public void E03_SwitchWeapon_ChangesActiveSlotAndWeapon()
        {
            var rig = _builder.CreatePlayerRig();
            var klewang = _builder.CreateWeapon("weapon.klewang", "Klewang", "KlewangVisual");
            var pedang = _builder.CreateWeapon("weapon.pedang_cepat", "Pedang Cepat", "PedangCepatVisual");
            rig.Inventory.TryAddItem(klewang);
            rig.Inventory.TryAddItem(pedang);

            rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot1, klewang);
            rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot2, pedang);

            bool switched = rig.Equipment.TrySwitchWeaponSlot(WeaponSlotType.Slot2);

            Assert.IsTrue(switched);
            Assert.AreEqual(WeaponSlotType.Slot2, rig.Equipment.ActiveWeaponSlot);
            Assert.AreEqual(pedang, rig.Equipment.ActiveWeapon);
        }

        [Test]
        public void E04_WeaponVisual_TogglesSceneAuthoredSocketsOnSwitch()
        {
            var rig = _builder.CreatePlayerRig();
            var klewang = _builder.CreateWeapon("weapon.klewang", "Klewang", "KlewangVisual");
            var pedang = _builder.CreateWeapon("weapon.pedang_cepat", "Pedang Cepat", "PedangCepatVisual");
            rig.Inventory.TryAddItem(klewang);
            rig.Inventory.TryAddItem(pedang);

            rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot1, klewang);
            rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot2, pedang);

            Assert.IsTrue(rig.WeaponSlot01Root.activeSelf);
            Assert.IsFalse(rig.WeaponSlot02Root.activeSelf);

            rig.Equipment.TrySwitchWeaponSlot(WeaponSlotType.Slot2);

            Assert.IsFalse(rig.WeaponSlot01Root.activeSelf);
            Assert.IsTrue(rig.WeaponSlot02Root.activeSelf);
        }

        [Test]
        public void E05_AttackDefinition_CombatReceiverReceivesMatchingComboChain()
        {
            var rig = _builder.CreatePlayerRig();
            var klewang = _builder.CreateWeapon("weapon.klewang", "Klewang", "KlewangVisual");
            var pedang = _builder.CreateWeapon("weapon.pedang_cepat", "Pedang Cepat", "PedangCepatVisual");
            rig.Inventory.TryAddItem(klewang);
            rig.Inventory.TryAddItem(pedang);

            rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot1, klewang);
            rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot2, pedang);
            Assert.AreEqual(klewang, rig.CombatReceiver.ActiveLoadout);

            rig.Equipment.TrySwitchWeaponSlot(WeaponSlotType.Slot2);
            Assert.AreEqual(pedang, rig.CombatReceiver.ActiveLoadout);
            Assert.AreSame(pedang.ComboChain, rig.CombatReceiver.ActiveLoadout.ComboChain);
        }

        [Test]
        public void E06_Animator_AppliesWeaponAnimatorOverrideOnSwitch()
        {
            var rig = _builder.CreatePlayerRig();
            var overrideCtrl = _builder.Track(new AnimatorOverrideController());
            var klewang = _builder.CreateWeapon("weapon.klewang", "Klewang", "KlewangVisual", overrideCtrl: overrideCtrl);
            rig.Inventory.TryAddItem(klewang);

            rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot1, klewang);

            Assert.AreSame(overrideCtrl, rig.Animator.runtimeAnimatorController);
        }

        [Test]
        public void E07_SwitchDuringAttack_RejectedWhenActionGateDisallows()
        {
            var rig = _builder.CreatePlayerRig();
            var klewang = _builder.CreateWeapon("weapon.klewang", "Klewang", "KlewangVisual");
            var pedang = _builder.CreateWeapon("weapon.pedang_cepat", "Pedang Cepat", "PedangCepatVisual");
            rig.Inventory.TryAddItem(klewang);
            rig.Inventory.TryAddItem(pedang);

            rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot1, klewang);
            rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot2, pedang);

            // Simulasi pemain sedang menyerang
            rig.ActionGate.AllowSwitch = false;
            bool switched = rig.Equipment.TrySwitchWeaponSlot(WeaponSlotType.Slot2);

            Assert.IsFalse(switched);
            Assert.AreEqual(WeaponSlotType.Slot1, rig.Equipment.ActiveWeaponSlot);
        }

        [Test]
        public void E08_EquipUnownedItem_RejectedWhenNotInInventory()
        {
            var rig = _builder.CreatePlayerRig();
            var unownedKlewang = _builder.CreateWeapon("weapon.klewang", "Klewang", "KlewangVisual");

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("tidak dimiliki di Inventory"));
            bool equipped = rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot1, unownedKlewang);

            Assert.IsFalse(equipped);
            Assert.IsNull(rig.Equipment.ActiveWeapon);
        }

        [Test]
        public void E09_InvalidEquipmentType_MaterialRejectedByTransactionHandler()
        {
            var rig = _builder.CreatePlayerRig();
            var material = _builder.CreateMaterial("material.serpihan_roh", "Serpihan Roh", 10);
            rig.Inventory.TryAddItem(material, 1);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("tidak dapat dipasang"));
            bool result = rig.TransactionHandler.TryEquipFromInventory(material);

            Assert.IsFalse(result);
        }

        [Test]
        public void E10_ArmorEquip_UpdatesEquippedStateAndRendererMaterial()
        {
            var rig = _builder.CreatePlayerRig();
            var armorMat = _builder.Track(new Material(Shader.Find("Standard")));
            var besiAmbat = _builder.CreateArmor("armor.besi_ambat", "Besi Ambat", armorMat);
            rig.Inventory.TryAddItem(besiAmbat);

            bool equipped = rig.Equipment.TryEquipArmor(besiAmbat);

            Assert.IsTrue(equipped);
            Assert.AreEqual(besiAmbat, rig.Equipment.EquippedArmor);
            Assert.AreSame(armorMat, rig.ArmorRenderer.sharedMaterial);
        }
    }
}
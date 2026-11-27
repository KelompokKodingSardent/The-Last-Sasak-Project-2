using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TheLastSasak.Items.Equipment;
using TheLastSasak.Items.Modifiers;
using TheLastSasak.Player.Stats;

namespace TheLastSasak.Tests.EditMode
{
    [TestFixture]
    public class StatAndArtifactDomainTests
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
        public void S01_BaseStats_MatchesPlayerBaseStatsSO()
        {
            var baseStats = _builder.CreateBaseStats(maxHp: 120f, damage: 30f, attackSpeed: 1.2f);
            var rig = _builder.CreatePlayerRig(baseStats);

            Assert.AreEqual(120f, rig.StatController.FinalMaxHealth, 0.001f);
            Assert.AreEqual(30f, rig.StatController.FinalAttackDamage, 0.001f);
            Assert.AreEqual(1.2f, rig.StatController.FinalAttackSpeed, 0.001f);
        }

        [Test]
        public void S02_WeaponModifier_AppliesOnlyForActiveWeapon()
        {
            var baseStats = _builder.CreateBaseStats(damage: 25f);
            var rig = _builder.CreatePlayerRig(baseStats);

            var klewang = _builder.CreateWeapon(
                "weapon.klewang", "Klewang", "KlewangVisual",
                modifiers: new StatModifier(StatType.AttackDamage, ModifierOperation.Additive, 10f));
            var pedang = _builder.CreateWeapon(
                "weapon.pedang_cepat", "Pedang Cepat", "PedangCepatVisual",
                modifiers: new StatModifier(StatType.AttackDamage, ModifierOperation.Additive, 5f));

            rig.Inventory.TryAddItem(klewang);
            rig.Inventory.TryAddItem(pedang);

            rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot1, klewang);
            rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot2, pedang);
            Assert.AreEqual(35f, rig.StatController.FinalAttackDamage, 0.001f);

            rig.Equipment.TrySwitchWeaponSlot(WeaponSlotType.Slot2);
            Assert.AreEqual(30f, rig.StatController.FinalAttackDamage, 0.001f);
        }

        [Test]
        public void S03_ArmorModifier_AppliesDefenseAndMaxHealthModifiers()
        {
            var baseStats = _builder.CreateBaseStats(maxHp: 100f);
            var rig = _builder.CreatePlayerRig(baseStats);

            var armor = _builder.CreateArmor(
                "armor.besi_ambat", "Besi Ambat",
                modifiers: new[]
                {
                    new StatModifier(StatType.MaxHealth, ModifierOperation.Additive, 20f),
                    new StatModifier(StatType.DamageReduction, ModifierOperation.Additive, 0.15f),
                    new StatModifier(StatType.PoiseResistance, ModifierOperation.Additive, 25f)
                });
            rig.Inventory.TryAddItem(armor);

            rig.Equipment.TryEquipArmor(armor);

            Assert.AreEqual(120f, rig.StatController.FinalMaxHealth, 0.001f);
            Assert.AreEqual(0.15f, rig.StatController.FinalDamageReduction, 0.001f);
            Assert.AreEqual(25f, rig.StatController.FinalPoiseResistance, 0.001f);
        }

        [Test]
        public void S04_ArtifactModifier_ActivatesPassiveStatBonus()
        {
            var baseStats = _builder.CreateBaseStats(damage: 25f);
            var rig = _builder.CreatePlayerRig(baseStats);

            var pecahanArwah = _builder.CreateArtifact(
                "artifact.pecahan_arwah", "Pecahan Arwah",
                new StatModifier(StatType.AttackDamage, ModifierOperation.Multiplicative, 0.20f));
            rig.Inventory.TryAddItem(pecahanArwah);

            rig.Artifact.TryEquipArtifact(0, pecahanArwah);

            // 25 * (1 + 0.20) = 30
            Assert.AreEqual(30f, rig.StatController.FinalAttackDamage, 0.001f);
        }

        [Test]
        public void S05_UnequipArtifact_RemovesPassiveModifierCleanly()
        {
            var baseStats = _builder.CreateBaseStats(damage: 25f);
            var rig = _builder.CreatePlayerRig(baseStats);

            var pecahanArwah = _builder.CreateArtifact(
                "artifact.pecahan_arwah", "Pecahan Arwah",
                new StatModifier(StatType.AttackDamage, ModifierOperation.Additive, 15f));
            rig.Inventory.TryAddItem(pecahanArwah);

            rig.Artifact.TryEquipArtifact(0, pecahanArwah);
            Assert.AreEqual(40f, rig.StatController.FinalAttackDamage, 0.001f);

            rig.Artifact.TryUnequipArtifact(0);
            Assert.AreEqual(25f, rig.StatController.FinalAttackDamage, 0.001f);
        }

        [Test]
        public void S06_MultipleModifiers_FollowsDeterministicFormula()
        {
            // Formula: (Base + Sum(Additive)) * Product(1 + Multiplicative)
            // Base = 20, Weapon = +10 Additive, Artifact1 = +10 Additive, Artifact2 = +0.50 Multiplicative
            // Expected = (20 + 10 + 10) * 1.50 = 60
            var baseStats = _builder.CreateBaseStats(damage: 20f);
            var rig = _builder.CreatePlayerRig(baseStats);

            var weapon = _builder.CreateWeapon(
                "weapon.klewang", "Klewang", "KlewangVisual",
                modifiers: new StatModifier(StatType.AttackDamage, ModifierOperation.Additive, 10f));
            var art1 = _builder.CreateArtifact(
                "artifact.batu_ambang", "Batu Ambang",
                new StatModifier(StatType.AttackDamage, ModifierOperation.Additive, 10f));
            var art2 = _builder.CreateArtifact(
                "artifact.pecahan_arwah", "Pecahan Arwah",
                new StatModifier(StatType.AttackDamage, ModifierOperation.Multiplicative, 0.50f));

            rig.Inventory.TryAddItem(weapon);
            rig.Inventory.TryAddItem(art1);
            rig.Inventory.TryAddItem(art2);

            rig.Equipment.TryEquipWeapon(WeaponSlotType.Slot1, weapon);
            rig.Artifact.TryEquipArtifact(0, art1);
            rig.Artifact.TryEquipArtifact(1, art2);

            Assert.AreEqual(60f, rig.StatController.FinalAttackDamage, 0.001f);
        }

        [Test]
        public void S07_MaxHpChange_KeepCurrentAndClampDoesNotAutoHeal()
        {
            var baseStats = _builder.CreateBaseStats(maxHp: 100f);
            var rig = _builder.CreatePlayerRig(baseStats);
            rig.HealthReceiver.MaxHealth = 100f;
            rig.HealthReceiver.CurrentHealth = 50f;

            var armor = _builder.CreateArmor(
                "armor.besi_ambat", "Besi Ambat",
                modifiers: new StatModifier(StatType.MaxHealth, ModifierOperation.Additive, 20f));
            rig.Inventory.TryAddItem(armor);

            // Naik dari 100 ke 120: CurrentHealth harus tetap 50 (tidak full heal)
            rig.Equipment.TryEquipArmor(armor);
            Assert.AreEqual(120f, rig.HealthReceiver.MaxHealth, 0.001f);
            Assert.AreEqual(50f, rig.HealthReceiver.CurrentHealth, 0.001f);

            // Jika heal ke 115 lalu armor dilepas (MaxHP turun ke 100): CurrentHealth di-clamp ke 100
            rig.HealthReceiver.CurrentHealth = 115f;
            rig.Equipment.TryUnequipArmor();
            Assert.AreEqual(100f, rig.HealthReceiver.MaxHealth, 0.001f);
            Assert.AreEqual(100f, rig.HealthReceiver.CurrentHealth, 0.001f);
        }

        [Test]
        public void S08_RepeatedEquip_PreventsDuplicateArtifactAcrossSlots()
        {
            var baseStats = _builder.CreateBaseStats(maxHp: 100f);
            var rig = _builder.CreatePlayerRig(baseStats);

            var batuAmbang = _builder.CreateArtifact(
                "artifact.batu_ambang", "Batu Ambang",
                new StatModifier(StatType.MaxHealth, ModifierOperation.Additive, 25f));
            rig.Inventory.TryAddItem(batuAmbang);

            Assert.IsTrue(rig.Artifact.TryEquipArtifact(0, batuAmbang));

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("sudah terpasang di slot 0"));
            Assert.IsFalse(rig.Artifact.TryEquipArtifact(1, batuAmbang));

            // Bonus hanya aktif 1x (+25), bukan 2x (+50)
            Assert.AreEqual(125f, rig.StatController.FinalMaxHealth, 0.001f);
        }

        [Test]
        public void S09_SOIntegrity_ScriptableObjectAssetsRemainUnmutatedDuringRun()
        {
            var baseStats = _builder.CreateBaseStats(maxHp: 100f, damage: 25f);
            var rig = _builder.CreatePlayerRig(baseStats);

            var artifact = _builder.CreateArtifact(
                "artifact.batu_ambang", "Batu Ambang",
                new StatModifier(StatType.MaxHealth, ModifierOperation.Additive, 25f));
            rig.Inventory.TryAddItem(artifact);
            rig.Artifact.TryEquipArtifact(0, artifact);

            // Verifikasi nilai pada ScriptableObject asli tidak berubah setelah kalkulasi
            Assert.AreEqual(100f, baseStats.MaxHealth, 0.001f);
            Assert.AreEqual(25f, baseStats.AttackDamage, 0.001f);
            Assert.AreEqual(1, artifact.StatModifiers.Count);
            Assert.AreEqual(25f, artifact.StatModifiers[0].Value, 0.001f);
            Assert.IsTrue(string.IsNullOrEmpty(artifact.StatModifiers[0].SourceId));
        }
    }
}
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using TheLastSasak.Items.Artifact;
using TheLastSasak.Items.Catalog;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Equipment;
using TheLastSasak.Items.Inventory;
using TheLastSasak.Items.Loot;
using TheLastSasak.Items.Modifiers;
using TheLastSasak.Player.Stats;
using TheLastSasak.Systems.Save;
using PlayerInventory = TheLastSasak.Items.Inventory.Inventory;

namespace TheLastSasak.Tests.EditMode
{
    public sealed class TestMaterialItemSO : ItemDefinitionSO
    {
        public void Configure(string id, string title, int maxStack)
        {
            ItemTestFixtureBuilder.SetPrivateField(this, "itemId", id);
            ItemTestFixtureBuilder.SetPrivateField(this, "displayName", title);
            SetCategory(ItemCategory.Material);
            SetMaxStackSize(maxStack);
        }
    }

    public sealed class TestActionGate : MonoBehaviour, IWeaponSwitchGate
    {
        public bool AllowSwitch = true;
        public bool AllowChange = true;

        public bool CanSwitchWeapon() => AllowSwitch;
        public bool CanChangeEquipment() => AllowChange;
    }

    public sealed class TestCombatReceiver : MonoBehaviour, IWeaponCombatReceiver
    {
        public WeaponDefinitionSO ActiveLoadout { get; private set; }
        public int SetLoadoutCallCount { get; private set; }
        public int ClearLoadoutCallCount { get; private set; }

        public void SetWeaponLoadout(WeaponDefinitionSO weapon)
        {
            ActiveLoadout = weapon;
            SetLoadoutCallCount++;
        }

        public void ClearWeaponLoadout()
        {
            ActiveLoadout = null;
            ClearLoadoutCallCount++;
        }
    }

    public sealed class TestHealthReceiver : MonoBehaviour, IHealthStatReceiver
    {
        public float CurrentHealth = 50f;
        public float MaxHealth = 100f;

        public void ApplyMaxHealthChange(float previousMaxHealth, float newMaxHealth, MaxHealthRecalculationPolicy policy)
        {
            CurrentHealth = HealthPolicyUtility.ResolveNewCurrentHealth(
                CurrentHealth, previousMaxHealth, newMaxHealth, policy);
            MaxHealth = newMaxHealth;
        }
    }

    public sealed class TestWalletAndExp : MonoBehaviour, ICurrencyReceiver, IExperienceReceiver
    {
        public int Souls { get; private set; }
        public int Experience { get; private set; }

        public void AddCurrency(int amount) => Souls += amount;
        public void AddExperience(int amount) => Experience += amount;
    }

    public sealed class ItemTestFixtureBuilder
    {
        private readonly List<Object> _createdObjects = new List<Object>();
        private readonly List<string> _createdFiles = new List<string>();

        public T Track<T>(T obj) where T : Object
        {
            if (obj != null) _createdObjects.Add(obj);
            return obj;
        }

        public void TrackFile(string path)
        {
            if (!string.IsNullOrEmpty(path)) _createdFiles.Add(path);
        }

        public static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);

            if (field == null && target.GetType().BaseType != null)
            {
                field = target.GetType().BaseType.GetField(
                    fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);
            }

            if (field == null && target.GetType().BaseType?.BaseType != null)
            {
                field = target.GetType().BaseType.BaseType.GetField(
                    fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);
            }

            field?.SetValue(target, value);
        }

        public TestMaterialItemSO CreateMaterial(string id, string title, int maxStack = 10)
        {
            var mat = Track(ScriptableObject.CreateInstance<TestMaterialItemSO>());
            mat.name = id;
            mat.Configure(id, title, maxStack);
            return mat;
        }

        public WeaponDefinitionSO CreateWeapon(
            string id,
            string title,
            string visualId,
            AttackDefinitionSO[] comboChain = null,
            AnimatorOverrideController overrideCtrl = null,
            params StatModifier[] modifiers)
        {
            var weapon = Track(ScriptableObject.CreateInstance<WeaponDefinitionSO>());
            weapon.name = id;
            SetPrivateField(weapon, "itemId", id);
            SetPrivateField(weapon, "displayName", title);
            SetPrivateField(weapon, "category", ItemCategory.Weapon);
            SetPrivateField(weapon, "maxStackSize", 1);
            SetPrivateField(weapon, "weaponVisualId", visualId);
            SetPrivateField(weapon, "animatorOverride", overrideCtrl);

            if (comboChain == null)
            {
                var atk1 = Track(ScriptableObject.CreateInstance<AttackDefinitionSO>());
                var atk2 = Track(ScriptableObject.CreateInstance<AttackDefinitionSO>());
                comboChain = new[] { atk1, atk2 };
            }
            SetPrivateField(weapon, "comboChain", comboChain);
            SetPrivateField(weapon, "baseModifiers", new List<StatModifier>(modifiers));
            return weapon;
        }

        public ArmorDefinitionSO CreateArmor(
            string id,
            string title,
            Material material = null,
            string visualId = "",
            params StatModifier[] modifiers)
        {
            var armor = Track(ScriptableObject.CreateInstance<ArmorDefinitionSO>());
            armor.name = id;
            SetPrivateField(armor, "itemId", id);
            SetPrivateField(armor, "displayName", title);
            SetPrivateField(armor, "category", ItemCategory.Armor);
            SetPrivateField(armor, "maxStackSize", 1);
            SetPrivateField(armor, "armorMaterial", material);
            SetPrivateField(armor, "armorVisualId", visualId);
            SetPrivateField(armor, "statModifiers", new List<StatModifier>(modifiers));
            return armor;
        }

        public ArtifactDefinitionSO CreateArtifact(
            string id,
            string title,
            params StatModifier[] modifiers)
        {
            var artifact = Track(ScriptableObject.CreateInstance<ArtifactDefinitionSO>());
            artifact.name = id;
            SetPrivateField(artifact, "itemId", id);
            SetPrivateField(artifact, "displayName", title);
            SetPrivateField(artifact, "category", ItemCategory.Artifact);
            SetPrivateField(artifact, "maxStackSize", 1);
            SetPrivateField(artifact, "statModifiers", new List<StatModifier>(modifiers));
            return artifact;
        }

        public ItemCatalogSO CreateCatalog(params ItemDefinitionSO[] items)
        {
            var catalog = Track(ScriptableObject.CreateInstance<ItemCatalogSO>());
            SetPrivateField(catalog, "items", new List<ItemDefinitionSO>(items));
            catalog.RebuildCache();
            return catalog;
        }

        public LootDefinitionSO CreateLoot(
            string lootId,
            LootRewardType rewardType,
            ItemDefinitionSO itemDef = null,
            int amount = 1,
            LootPickup pickupPrefab = null)
        {
            var loot = Track(ScriptableObject.CreateInstance<LootDefinitionSO>());
            SetPrivateField(loot, "lootId", lootId);
            SetPrivateField(loot, "rewardType", rewardType);
            SetPrivateField(loot, "itemDefinition", itemDef);
            SetPrivateField(loot, "amount", amount);
            SetPrivateField(loot, "pickupPrefab", pickupPrefab);
            return loot;
        }

        public PlayerBaseStatsSO CreateBaseStats(float maxHp = 100f, float damage = 25f, float attackSpeed = 1f)
        {
            var stats = Track(ScriptableObject.CreateInstance<PlayerBaseStatsSO>());
            SetPrivateField(stats, "maxHealth", maxHp);
            SetPrivateField(stats, "attackDamage", damage);
            SetPrivateField(stats, "attackSpeed", attackSpeed);
            return stats;
        }

        public PlayerRig CreatePlayerRig(PlayerBaseStatsSO baseStats = null)
        {
            var root = Track(new GameObject("TestPlayer"));
            var actionGate = root.AddComponent<TestActionGate>();
            var combatReceiver = root.AddComponent<TestCombatReceiver>();
            var healthReceiver = root.AddComponent<TestHealthReceiver>();
            var walletAndExp = root.AddComponent<TestWalletAndExp>();
            var animator = root.AddComponent<Animator>();

            var inventory = root.AddComponent<PlayerInventory>();
            var equipment = root.AddComponent<EquipmentController>();
            SetPrivateField(equipment, "allowEquipWithoutInventoryInEditor", false);
            equipment.SetActionGate(actionGate);
            equipment.SetOwnershipQuery(inventory);

            var artifact = root.AddComponent<ArtifactController>();
            SetPrivateField(artifact, "allowEquipWithoutInventoryInEditor", false);
            artifact.SetActionGate(actionGate);
            artifact.SetOwnershipQuery(inventory);

            var txHandler = root.AddComponent<EquipmentTransactionHandler>();
            SetPrivateField(txHandler, "inventory", inventory);
            SetPrivateField(txHandler, "equipmentController", equipment);

            // Scene-authored sockets
            var slot1Root = Track(new GameObject("WeaponSlot_01"));
            slot1Root.transform.SetParent(root.transform);
            var klewangVis1 = Track(new GameObject("KlewangVisual"));
            klewangVis1.transform.SetParent(slot1Root.transform);

            var slot2Root = Track(new GameObject("WeaponSlot_02"));
            slot2Root.transform.SetParent(root.transform);
            var pedangVis2 = Track(new GameObject("PedangCepatVisual"));
            pedangVis2.transform.SetParent(slot2Root.transform);

            var armorObj = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            armorObj.name = "ArmorVisual";
            armorObj.transform.SetParent(root.transform);
            var armorRenderer = armorObj.GetComponent<Renderer>();

            var visualCtrl = root.AddComponent<EquipmentVisualController>();
            SetPrivateField(visualCtrl, "equipmentController", equipment);
            SetPrivateField(visualCtrl, "weaponSlot01Root", slot1Root);
            SetPrivateField(visualCtrl, "weaponSlot02Root", slot2Root);
            SetPrivateField(visualCtrl, "slot01Visuals", new List<EquipmentVisualController.VisualEntry>
            {
                new EquipmentVisualController.VisualEntry { visualId = "KlewangVisual", visualObject = klewangVis1 }
            });
            SetPrivateField(visualCtrl, "slot02Visuals", new List<EquipmentVisualController.VisualEntry>
            {
                new EquipmentVisualController.VisualEntry { visualId = "PedangCepatVisual", visualObject = pedangVis2 }
            });
            SetPrivateField(visualCtrl, "armorRenderers", new[] { armorRenderer });

            var combatAdapter = root.AddComponent<WeaponCombatAdapter>();
            SetPrivateField(combatAdapter, "equipmentController", equipment);
            SetPrivateField(combatAdapter, "playerAnimator", animator);
            SetPrivateField(combatAdapter, "combatReceiverComponent", combatReceiver);

            var statCtrl = root.AddComponent<PlayerStatController>();
            if (baseStats != null)
            {
                SetPrivateField(statCtrl, "baseStatsAsset", baseStats);
            }
            SetPrivateField(statCtrl, "equipmentController", equipment);
            SetPrivateField(statCtrl, "artifactController", artifact);

            var lootCollector = root.AddComponent<LootCollector>();
            SetPrivateField(lootCollector, "inventory", inventory);
            lootCollector.SetCurrencyReceiver(walletAndExp);
            lootCollector.SetExperienceReceiver(walletAndExp);

            // Aktifkan subscription OnEnable
            visualCtrl.enabled = false;
            visualCtrl.enabled = true;
            combatAdapter.enabled = false;
            combatAdapter.enabled = true;
            statCtrl.enabled = false;
            statCtrl.enabled = true;

            return new PlayerRig
            {
                Root = root,
                ActionGate = actionGate,
                CombatReceiver = combatReceiver,
                HealthReceiver = healthReceiver,
                WalletAndExp = walletAndExp,
                Animator = animator,
                Inventory = inventory,
                Equipment = equipment,
                Artifact = artifact,
                TransactionHandler = txHandler,
                VisualController = visualCtrl,
                CombatAdapter = combatAdapter,
                StatController = statCtrl,
                LootCollector = lootCollector,
                WeaponSlot01Root = slot1Root,
                WeaponSlot02Root = slot2Root,
                ArmorRenderer = armorRenderer
            };
        }

        public void Dispose()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }
            _createdObjects.Clear();

            for (int i = 0; i < _createdFiles.Count; i++)
            {
                if (File.Exists(_createdFiles[i]))
                {
                    File.Delete(_createdFiles[i]);
                }
            }
            _createdFiles.Clear();
        }
    }

    public struct PlayerRig
    {
        public GameObject Root;
        public TestActionGate ActionGate;
        public TestCombatReceiver CombatReceiver;
        public TestHealthReceiver HealthReceiver;
        public TestWalletAndExp WalletAndExp;
        public Animator Animator;
        public PlayerInventory Inventory;
        public EquipmentController Equipment;
        public ArtifactController Artifact;
        public EquipmentTransactionHandler TransactionHandler;
        public EquipmentVisualController VisualController;
        public WeaponCombatAdapter CombatAdapter;
        public PlayerStatController StatController;
        public LootCollector LootCollector;
        public GameObject WeaponSlot01Root;
        public GameObject WeaponSlot02Root;
        public Renderer ArmorRenderer;
    }
}
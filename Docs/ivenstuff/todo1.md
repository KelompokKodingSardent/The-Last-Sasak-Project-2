Implementasi **Phase 1 — Item Definition & Catalog** disusun dengan prinsip *immutable definitions*, validasi otomatis di Unity Inspector (`OnValidate`), dan properti kompatibilitas (`WeaponId`, `ArmorId`, `ArtifactId`) agar transisi dari asset lama tidak memutus referensi Inspector.

### Struktur Target Phase 1

```text
Assets/_Project/Scripts/
├── Items/
│   ├── Modifiers/
│   │   ├── StatType.cs
│   │   ├── ModifierOperation.cs
│   │   ├── IStatModifier.cs
│   │   └── StatModifier.cs
│   ├── Definitions/
│   │   ├── ItemCategory.cs
│   │   ├── ItemDefinitionSO.cs
│   │   ├── EquipmentDefinitionSO.cs
│   │   ├── WeaponDefinitionSO.cs
│   │   ├── ArmorDefinitionSO.cs
│   │   └── ArtifactDefinitionSO.cs
│   └── Catalog/
│       └── ItemCatalogSO.cs

```

---

### 1. Stat Modifier Foundation (`Items/Modifiers/`)

Kontrak modifier ini menjadi fondasi tunggal bagi `WeaponDefinitionSO`, `ArmorDefinitionSO`, dan `ArtifactDefinitionSO` sebelum `PlayerStatController` diintegrasikan penuh pada Phase 5.

#### `StatType.cs` & `ModifierOperation.cs`

```csharp
namespace TheLastSasak.Items.Modifiers
{
    public enum StatType
    {
        MaxHealth,
        AttackDamage,
        AttackSpeed,
        DamageReduction,
        PoiseResistance,
        KnockbackResistance,
        MoveSpeed,
        StaminaRegen
    }

    public enum ModifierOperation
    {
        Additive,       // Base + Value
        Multiplicative, // * (1 + Value) atau faktor pengali sesuai konvensi PlayerStatController
        Override        // Mengganti nilai akhir (gunakan hanya untuk kondisi khusus)
    }
}

```

#### `IStatModifier.cs`

```csharp
namespace TheLastSasak.Items.Modifiers
{
    public interface IStatModifier
    {
        StatType StatType { get; }
        ModifierOperation Operation { get; }
        float Value { get; }
        string SourceId { get; }
    }
}

```

#### `StatModifier.cs`

```csharp
using System;
using UnityEngine;

namespace TheLastSasak.Items.Modifiers
{
    [Serializable]
    public struct StatModifier : IStatModifier
    {
        [SerializeField] private StatType statType;
        [SerializeField] private ModifierOperation operation;
        [SerializeField] private float value;

        [Tooltip("Opsional di Inspector; otomatis diisi ItemId saat runtime jika kosong.")]
        [SerializeField] private string sourceId;

        public StatType StatType => statType;
        public ModifierOperation Operation => operation;
        public float Value => value;
        public string SourceId => sourceId;

        public StatModifier(StatType statType, ModifierOperation operation, float value, string sourceId = "")
        {
            this.statType = statType;
            this.operation = operation;
            this.value = value;
            this.sourceId = sourceId;
        }

        public StatModifier WithSource(string newSourceId)
        {
            return new StatModifier(statType, operation, value, newSourceId);
        }
    }
}

```

---

### 2. Base Item & Equipment Definitions (`Items/Definitions/`)

#### `ItemCategory.cs`

```csharp
namespace TheLastSasak.Items.Definitions
{
    public enum ItemCategory
    {
        Weapon = 0,
        Armor = 1,
        Artifact = 2,
        Consumable = 3,
        Material = 4
    }
}

```

#### `ItemDefinitionSO.cs`

```csharp
using UnityEngine;

namespace TheLastSasak.Items.Definitions
{
    public abstract class ItemDefinitionSO : ScriptableObject
    {
        [Header("Persistent Identity")]
        [Tooltip("ID permanen unik (contoh: weapon.klewang). Jangan bergantung pada nama file.")]
        [SerializeField] private string itemId;

        [Header("Presentation")]
        [SerializeField] private string displayName;
        [TextArea(2, 5)]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;

        [Header("Classification & Stacking")]
        [SerializeField] private ItemCategory category;
        [Min(1)]
        [SerializeField] private int maxStackSize = 1;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemCategory Category => category;
        public int MaxStackSize => maxStackSize;

        public bool IsStackable => maxStackSize > 1;

        protected void SetCategory(ItemCategory enforcedCategory)
        {
            category = enforcedCategory;
        }

        protected void SetMaxStackSize(int enforcedStackSize)
        {
            maxStackSize = Mathf.Max(1, enforcedStackSize);
        }

        protected virtual void OnValidate()
        {
            if (!string.IsNullOrEmpty(itemId))
            {
                itemId = itemId.Trim().ToLowerInvariant();
            }

            if (maxStackSize < 1)
            {
                maxStackSize = 1;
            }

            if (string.IsNullOrWhiteSpace(itemId))
            {
                Debug.LogWarning($"[ItemDefinitionSO] '{name}' belum memiliki ItemId.", this);
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                Debug.LogWarning($"[ItemDefinitionSO] '{name}' ({itemId}) belum memiliki DisplayName.", this);
            }

            if (icon == null)
            {
                Debug.LogWarning($"[ItemDefinitionSO] '{name}' ({itemId}) belum memiliki Icon untuk UI.", this);
            }
        }
    }
}

```

#### `EquipmentDefinitionSO.cs`

```csharp
namespace TheLastSasak.Items.Definitions
{
    public abstract class EquipmentDefinitionSO : ItemDefinitionSO
    {
        protected override void OnValidate()
        {
            // Aturan v1.0: Weapon, Armor, dan Artifact tidak dapat ditumpuk (MaxStackSize = 1)
            SetMaxStackSize(1);
            base.OnValidate();
        }
    }
}

```

---

### 3. Concrete Equipment Definitions (`Items/Definitions/`)

> **Catatan Migrasi Asset Lama:** Jika di proyek sudah ada class `WeaponDefinitionSO` atau `ArmorDefinitionSO` pada namespace combat/player lama, pindahkan field lama ke struktur ini secara bertahap dan pertahankan GUID script (`*.cs.meta`) agar seluruh aset `.asset` di `ScriptableObjects/` tidak berubah menjadi *Missing Script*.

#### `WeaponDefinitionSO.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Modifiers;
// Sesuaikan namespace AttackDefinitionSO dengan folder Combat proyek Anda
// using TheLastSasak.Combat;

namespace TheLastSasak.Items.Definitions
{
    public enum WeaponType
    {
        Klewang,
        FastSword,
        Spear,
        HeavyBlade
    }

    [CreateAssetMenu(fileName = "Weapon_", menuName = "The Last Sasak/Items/Weapon Definition")]
    public class WeaponDefinitionSO : EquipmentDefinitionSO
    {
        [Header("Weapon Classification")]
        [SerializeField] private WeaponType weaponType = WeaponType.Klewang;

        [Header("Combat & Animation")]
        [Tooltip("Urutan serangan combo. Damage, timing, dan hitbox tetap berada di masing-masing AttackDefinitionSO.")]
        [SerializeField] private AttackDefinitionSO[] comboChain;
        [Tooltip("Opsional. Kosongkan jika menggunakan default Animator player.")]
        [SerializeField] private AnimatorOverrideController animatorOverride;

        [Header("Presentation & Stats")]
        [Tooltip("ID object visual di bawah WeaponSocket (contoh: KlewangVisual).")]
        [SerializeField] private string weaponVisualId;
        [TextArea(2, 4)]
        [SerializeField] private string weaponDescription;
        [SerializeField] private List<StatModifier> baseModifiers = new List<StatModifier>();

        // Alias kompatibilitas dengan rancangan & kode lama
        public string WeaponId => ItemId;
        public WeaponType WeaponType => weaponType;
        public IReadOnlyList<AttackDefinitionSO> ComboChain => comboChain;
        public AnimatorOverrideController AnimatorOverride => animatorOverride;
        public string WeaponVisualId => weaponVisualId;
        public string WeaponDescription => string.IsNullOrWhiteSpace(weaponDescription) ? Description : weaponDescription;
        public IReadOnlyList<StatModifier> BaseModifiers => baseModifiers;

        public IEnumerable<StatModifier> GetScopedModifiers()
        {
            for (int i = 0; i < baseModifiers.Count; i++)
            {
                yield return baseModifiers[i].WithSource(ItemId);
            }
        }

        protected override void OnValidate()
        {
            SetCategory(ItemCategory.Weapon);
            base.OnValidate();

            if (comboChain == null || comboChain.Length == 0)
            {
                Debug.LogWarning($"[WeaponDefinitionSO] '{name}' ({ItemId}) memiliki ComboChain kosong.", this);
            }
            else
            {
                for (int i = 0; i < comboChain.Length; i++)
                {
                    if (comboChain[i] == null)
                    {
                        Debug.LogError($"[WeaponDefinitionSO] '{name}' ({ItemId}) memiliki elemen null pada ComboChain index {i}.", this);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(weaponVisualId))
            {
                Debug.LogWarning($"[WeaponDefinitionSO] '{name}' ({ItemId}) belum memiliki WeaponVisualId.", this);
            }
        }
    }
}

```

#### `ArmorDefinitionSO.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Modifiers;

namespace TheLastSasak.Items.Definitions
{
    [CreateAssetMenu(fileName = "Armor_", menuName = "The Last Sasak/Items/Armor Definition")]
    public class ArmorDefinitionSO : EquipmentDefinitionSO
    {
        [Header("Armor Visuals")]
        [Tooltip("Material visual yang diterapkan ke Renderer di ArmorSocket.")]
        [SerializeField] private Material armorMaterial;
        [Tooltip("ID child visual di bawah ArmorSocket bila menggunakan pergantian mesh.")]
        [SerializeField] private string armorVisualId;

        [Header("Gameplay Info & Modifiers")]
        [TextArea(2, 4)]
        [SerializeField] private string armorDescription;
        [SerializeField] private List<StatModifier> statModifiers = new List<StatModifier>();

        // Alias kompatibilitas
        public string ArmorId => ItemId;
        public Material ArmorMaterial => armorMaterial;
        public string ArmorVisualId => armorVisualId;
        public string ArmorDescription => string.IsNullOrWhiteSpace(armorDescription) ? Description : armorDescription;
        public IReadOnlyList<StatModifier> StatModifiers => statModifiers;

        public IEnumerable<StatModifier> GetScopedModifiers()
        {
            for (int i = 0; i < statModifiers.Count; i++)
            {
                yield return statModifiers[i].WithSource(ItemId);
            }
        }

        protected override void OnValidate()
        {
            SetCategory(ItemCategory.Armor);
            base.OnValidate();

            if (armorMaterial == null && string.IsNullOrWhiteSpace(armorVisualId))
            {
                Debug.LogWarning($"[ArmorDefinitionSO] '{name}' ({ItemId}) belum memiliki ArmorMaterial maupun ArmorVisualId.", this);
            }
        }
    }
}

```

#### `ArtifactDefinitionSO.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Modifiers;

namespace TheLastSasak.Items.Definitions
{
    [CreateAssetMenu(fileName = "Artifact_", menuName = "The Last Sasak/Items/Artifact Definition")]
    public class ArtifactDefinitionSO : EquipmentDefinitionSO
    {
        [Header("Artifact Passive & Visual")]
        [TextArea(2, 4)]
        [SerializeField] private string passiveDescription;
        [Tooltip("Visual khusus artefak di UI/HUD (jika berbeda dari Icon utama).")]
        [SerializeField] private Sprite artifactVisual;
        [SerializeField] private List<StatModifier> statModifiers = new List<StatModifier>();

        // Alias kompatibilitas
        public string ArtifactId => ItemId;
        public string PassiveDescription => string.IsNullOrWhiteSpace(passiveDescription) ? Description : passiveDescription;
        public Sprite ArtifactVisual => artifactVisual != null ? artifactVisual : Icon;
        public IReadOnlyList<StatModifier> StatModifiers => statModifiers;

        public IEnumerable<StatModifier> GetScopedModifiers()
        {
            for (int i = 0; i < statModifiers.Count; i++)
            {
                yield return statModifiers[i].WithSource(ItemId);
            }
        }

        protected override void OnValidate()
        {
            SetCategory(ItemCategory.Artifact);
            base.OnValidate();

            if (statModifiers == null || statModifiers.Count == 0)
            {
                Debug.LogWarning($"[ArtifactDefinitionSO] '{name}' ({ItemId}) belum memiliki StatModifiers pasif.", this);
            }
        }
    }
}

```

---

### 4. Item Catalog & Validation (`Items/Catalog/`)

#### `ItemCatalogSO.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Definitions;

namespace TheLastSasak.Items.Catalog
{
    [CreateAssetMenu(fileName = "ItemCatalog_Main", menuName = "The Last Sasak/Items/Item Catalog")]
    public class ItemCatalogSO : ScriptableObject
    {
        [SerializeField] private List<ItemDefinitionSO> items = new List<ItemDefinitionSO>();

        private Dictionary<string, ItemDefinitionSO> _lookupCache;

        public IReadOnlyList<ItemDefinitionSO> AllItems => items;

        private void OnEnable()
        {
            RebuildCache();
        }

        public void RebuildCache()
        {
            _lookupCache = new Dictionary<string, ItemDefinitionSO>(items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinitionSO item = items[i];
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
                {
                    continue;
                }

                if (!_lookupCache.ContainsKey(item.ItemId))
                {
                    _lookupCache.Add(item.ItemId, item);
                }
            }
        }

        public bool TryGetItem(string itemId, out ItemDefinitionSO item)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                item = null;
                return false;
            }

            if (_lookupCache == null)
            {
                RebuildCache();
            }

            return _lookupCache.TryGetValue(itemId.Trim().ToLowerInvariant(), out item);
        }

        public bool TryGetItem<T>(string itemId, out T typedItem) where T : ItemDefinitionSO
        {
            if (TryGetItem(itemId, out ItemDefinitionSO item) && item is T casted)
            {
                typedItem = casted;
                return true;
            }

            typedItem = null;
            return false;
        }

        public bool Contains(string itemId)
        {
            return TryGetItem(itemId, out _);
        }

        [ContextMenu("Validate Catalog")]
        public void ValidateCatalog()
        {
            HashSet<string> seenIds = new HashSet<string>();
            int errorCount = 0;

            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinitionSO item = items[i];
                if (item == null)
                {
                    Debug.LogError($"[ItemCatalogSO] '{name}' memiliki slot null pada index {i}.", this);
                    errorCount++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.ItemId))
                {
                    Debug.LogError($"[ItemCatalogSO] Item '{item.name}' pada index {i} memiliki ItemId kosong.", item);
                    errorCount++;
                    continue;
                }

                if (!seenIds.Add(item.ItemId))
                {
                    Debug.LogError($"[ItemCatalogSO] DUPLIKAT ItemId ditemukan: '{item.ItemId}' pada asset '{item.name}'.", item);
                    errorCount++;
                }
            }

            if (errorCount == 0)
            {
                Debug.Log($"[ItemCatalogSO] Validasi '{name}' sukses: {seenIds.Count} item unik terverifikasi.", this);
            }
        }

        private void OnValidate()
        {
            ValidateCatalog();
            RebuildCache();
        }
    }
}

```

---

### 5. Panduan Setup Asset Baseline Phase 1

Setelah seluruh script di atas ter-compile tanpa error, buat asset baseline di folder `Assets/_Project/ScriptableObjects/`:

| Folder Target | Nama Asset | `ItemId` | Konfigurasi Kunci |
| --- | --- | --- | --- |
| `Items/Weapons/` | `Weapon_Klewang` | `weapon.klewang` | `WeaponType = Klewang`, `ComboChain` = 3 `AttackDefinitionSO`, `WeaponVisualId = KlewangVisual` |
| `Items/Weapons/` | `Weapon_PedangCepat` | `weapon.pedang_cepat` | `WeaponType = FastSword`, `ComboChain` = 3 `AttackDefinitionSO`, `WeaponVisualId = PedangCepatVisual` |
| `Items/Armors/` | `Armor_BesiAmbat` | `armor.besi_ambat` | `ArmorMaterial` terisi, `StatModifiers`: `MaxHealth` (+20 Additive), `DamageReduction` (+0.10 Additive) |
| `Items/Artifacts/` | `Artifact_BatuAmbang` | `artifact.batu_ambang` | `StatModifiers`: `MaxHealth` (+25 Additive) |
| `Items/Artifacts/` | `Artifact_PecahanArwah` | `artifact.pecahan_arwah` | `StatModifiers`: `AttackDamage` (+0.15 Multiplicative) |
| `ItemCatalog/` | `ItemCatalog_Main` | — | Daftarkan kelima asset di atas, lalu klik **Context Menu - Validate Catalog** untuk memastikan 0 duplikat ID |
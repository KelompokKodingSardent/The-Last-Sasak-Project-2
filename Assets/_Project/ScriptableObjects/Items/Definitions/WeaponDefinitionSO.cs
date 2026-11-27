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
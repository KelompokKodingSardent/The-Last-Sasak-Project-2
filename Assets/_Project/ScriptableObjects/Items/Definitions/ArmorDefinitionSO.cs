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
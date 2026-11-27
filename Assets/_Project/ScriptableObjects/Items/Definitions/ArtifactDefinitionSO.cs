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
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
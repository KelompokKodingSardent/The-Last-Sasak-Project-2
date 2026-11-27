using System.Collections.Generic;
using UnityEngine;
using TheLastSasak.Items.Modifiers;

namespace TheLastSasak.Items.Definitions
{
    public abstract class EquipmentDefinitionSO : ItemDefinitionSO
    {
        [Header("Stat Modifiers")]
        [SerializeField] private List<StatModifier> statModifiers = new List<StatModifier>();

        public virtual IReadOnlyList<StatModifier> StatModifiers => statModifiers;
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace TheLastSasak.Items.Modifiers
{
    public readonly struct StatBounds
    {
        public readonly float Min;
        public readonly float Max;

        public StatBounds(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Clamp(float value)
        {
            return Mathf.Clamp(value, Min, Max);
        }
    }

    public static class ModifierCalculator
    {
        private static readonly Dictionary<StatType, StatBounds> DefaultBounds = new Dictionary<StatType, StatBounds>
        {
            { StatType.MaxHealth, new StatBounds(1f, 99999f) },
            { StatType.AttackDamage, new StatBounds(0f, 99999f) },
            { StatType.AttackSpeed, new StatBounds(0.1f, 10f) },
            { StatType.DamageReduction, new StatBounds(0f, 0.85f) },
            { StatType.PoiseResistance, new StatBounds(0f, 1000f) },
            { StatType.KnockbackResistance, new StatBounds(0f, 1f) },
            { StatType.MoveSpeed, new StatBounds(0f, 50f) },
            { StatType.StaminaRegen, new StatBounds(0f, 1000f) }
        };

        public static StatBounds GetBounds(StatType statType)
        {
            return DefaultBounds.TryGetValue(statType, out StatBounds bounds)
                ? bounds
                : new StatBounds(float.MinValue, float.MaxValue);
        }

        public static float CalculateStat(float baseValue, StatType targetStat, IEnumerable<IStatModifier> modifiers)
        {
            return CalculateStat(baseValue, targetStat, modifiers, GetBounds(targetStat));
        }

        public static float CalculateStat(
            float baseValue,
            StatType targetStat,
            IEnumerable<IStatModifier> modifiers,
            StatBounds bounds)
        {
            if (modifiers == null)
            {
                return bounds.Clamp(baseValue);
            }

            float additiveSum = 0f;
            float multiplicativeFactor = 1f;
            bool hasOverride = false;
            float overrideValue = 0f;

            foreach (IStatModifier mod in modifiers)
            {
                if (mod == null || mod.StatType != targetStat)
                {
                    continue;
                }

                switch (mod.Operation)
                {
                    case ModifierOperation.Additive:
                        additiveSum += mod.Value;
                        break;

                    case ModifierOperation.Multiplicative:
                        // Konvensi: +0.15f berarti +15% (pengali 1.15f)
                        multiplicativeFactor *= Mathf.Max(0f, 1f + mod.Value);
                        break;

                    case ModifierOperation.Override:
                        hasOverride = true;
                        overrideValue = mod.Value;
                        break;
                }
            }

            float rawResult = hasOverride
                ? overrideValue
                : (baseValue + additiveSum) * multiplicativeFactor;

            return bounds.Clamp(rawResult);
        }

        public static void CalculateAllStats(
            IReadOnlyDictionary<StatType, float> baseStats,
            IReadOnlyList<IStatModifier> modifiers,
            Dictionary<StatType, float> outputResults)
        {
            outputResults.Clear();

            foreach (KeyValuePair<StatType, float> pair in baseStats)
            {
                StatType stat = pair.Key;
                float finalValue = CalculateStat(pair.Value, stat, modifiers);
                outputResults[stat] = finalValue;
            }
        }
    }
}
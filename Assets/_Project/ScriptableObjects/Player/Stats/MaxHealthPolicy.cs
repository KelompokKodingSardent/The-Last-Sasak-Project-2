namespace TheLastSasak.Player.Stats
{
    public enum MaxHealthRecalculationPolicy
    {
        /// <summary>
        /// Default v1.0: Current HP tetap (50/100 menjadi 50/120), hanya di-clamp jika melebihi MaxHealth baru.
        /// </summary>
        KeepCurrentAndClamp = 0,

        /// <summary>
        /// Mempertahankan rasio persentase HP (50/100 menjadi 60/120).
        /// </summary>
        PreservePercentage = 1
    }

    public interface IHealthStatReceiver
    {
        void ApplyMaxHealthChange(float previousMaxHealth, float newMaxHealth, MaxHealthRecalculationPolicy policy);
    }

    public static class HealthPolicyUtility
    {
        public static float ResolveNewCurrentHealth(
            float currentHealth,
            float previousMaxHealth,
            float newMaxHealth,
            MaxHealthRecalculationPolicy policy)
        {
            if (newMaxHealth <= 0f)
            {
                return 0f;
            }

            if (policy == MaxHealthRecalculationPolicy.PreservePercentage && previousMaxHealth > 0f)
            {
                float ratio = currentHealth / previousMaxHealth;
                return UnityEngine.Mathf.Clamp(newMaxHealth * ratio, 0f, newMaxHealth);
            }

            // KeepCurrentAndClamp: tidak pernah auto-heal saat MaxHealth naik
            return UnityEngine.Mathf.Clamp(currentHealth, 0f, newMaxHealth);
        }
    }
}
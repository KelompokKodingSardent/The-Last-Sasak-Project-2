using System.Collections.Generic;
using UnityEngine;

namespace TheLastSasak.Items.Loot
{
    public class RewardCollector : MonoBehaviour
    {
        [Header("Direct Kill Rewards (Non-Inventory)")]
        [Min(0)]
        [SerializeField] private int directSoulsReward = 25;
        [Min(0)]
        [SerializeField] private int directExperienceReward = 50;

        [Header("World Loot Drops")]
        [SerializeField] private List<LootDropEntry> lootDrops = new List<LootDropEntry>();
        [SerializeField] private LootDropController lootDropController;

        private bool _rewardsDistributed;

        private void Awake()
        {
            if (lootDropController == null)
            {
                lootDropController = FindFirstObjectByType<LootDropController>();
            }
        }

        /// <summary>
        /// Panggil saat musuh kembali di-spawn dari Enemy Pool agar reward siap dipakai lagi.
        /// </summary>
        public void ResetRewardState()
        {
            _rewardsDistributed = false;
        }

        /// <summary>
        /// Hubungkan ke event OnDeath milik EnemyHealth / EnemyController.
        /// </summary>
        public void DistributeRewards(GameObject killerObject)
        {
            // Jaminan anti double-reward apabila OnDeath terpicu lebih dari sekali
            if (_rewardsDistributed)
            {
                return;
            }

            _rewardsDistributed = true;

            if (killerObject != null)
            {
                if (directSoulsReward > 0)
                {
                    ICurrencyReceiver currencyReceiver = killerObject.GetComponentInParent<ICurrencyReceiver>();
                    currencyReceiver?.AddCurrency(directSoulsReward);
                }

                if (directExperienceReward > 0)
                {
                    IExperienceReceiver expReceiver = killerObject.GetComponentInParent<IExperienceReceiver>();
                    expReceiver?.AddExperience(directExperienceReward);
                }
            }

            if (lootDropController != null && lootDrops != null && lootDrops.Count > 0)
            {
                lootDropController.RollAndSpawnDrops(lootDrops, transform.position);
            }
        }
    }
}
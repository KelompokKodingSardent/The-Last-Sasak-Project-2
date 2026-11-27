using System.Collections.Generic;
using UnityEngine;

public class RewardCollector : MonoBehaviour
{
    private struct ClaimedDeathKey : System.IEquatable<ClaimedDeathKey>
    {
        public readonly int InstanceId;
        public readonly int LeaseId;

        public ClaimedDeathKey(int instanceId, int leaseId)
        {
            InstanceId = instanceId;
            LeaseId = leaseId;
        }

        public bool Equals(ClaimedDeathKey other) => InstanceId == other.InstanceId && LeaseId == other.LeaseId;
        public override bool Equals(object obj) => obj is ClaimedDeathKey other && Equals(other);
        public override int GetHashCode() => (InstanceId * 397) ^ LeaseId;
    }

    private readonly HashSet<ClaimedDeathKey> _claimedDeaths = new HashSet<ClaimedDeathKey>();

    private void OnEnable()
    {
        GameEvents.OnEntityDied += HandleEntityDied;
        GameEvents.OnRunStarted += HandleRunStarted;
    }

    private void OnDisable()
    {
        GameEvents.OnEntityDied -= HandleEntityDied;
        GameEvents.OnRunStarted -= HandleRunStarted;
        _claimedDeaths.Clear();
    }

    private void HandleRunStarted(RunId runId, string sceneName)
    {
        _claimedDeaths.Clear();
    }

    private void HandleEntityDied(Health health)
    {
        if (health == null || health.CompareTag("Player"))
            return;

        ClaimedDeathKey key = new ClaimedDeathKey(health.GetInstanceID(), health.CurrentLeaseId.Value);
        if (!_claimedDeaths.Add(key))
        {
            // Kematian instance & lease ini sudah pernah diproses; cegah double-payout
            return;
        }

        // Jika musuh memakai komponen RewardCollector domain Loot, delegasikan sepenuhnya ke sana
        var itemLootReward = health.GetComponent<TheLastSasak.Items.Loot.RewardCollector>();
        if (itemLootReward != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            itemLootReward.DistributeRewards(player);
            return;
        }

        EnemyBrain brain = health.GetComponent<EnemyBrain>();
        int exp = brain != null && brain.Definition != null ? brain.Definition.ExpReward : 10;
        int souls = brain != null && brain.Definition != null ? brain.Definition.SoulsReward : 1;

        ExperienceSystem.Instance?.Add(exp);
        CurrencyWallet.Instance?.Add(souls);
    }
}
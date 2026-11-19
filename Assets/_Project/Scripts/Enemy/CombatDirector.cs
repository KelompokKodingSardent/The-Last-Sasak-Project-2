
using System.Collections.Generic;
using UnityEngine;

public class CombatDirector : MonoBehaviour
{
    public static CombatDirector Instance { get; private set; }

    [Header("Combat Settings")]
    [Min(1)]
    public int maxAggressors = 2;

    private readonly List<EnemyController> activeAggressors =
        new List<EnemyController>();

    private readonly Queue<EnemyController> waitingQueue =
        new Queue<EnemyController>();

    private readonly HashSet<EnemyController> waitingSet =
        new HashSet<EnemyController>();

    private readonly HashSet<EnemyBrain> activeBrains = new HashSet<EnemyBrain>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool RequestBrainAggro(EnemyBrain brain)
    {
        if (brain == null)
            return false;
        if (activeBrains.Contains(brain))
            return true;
        if (activeBrains.Count >= Mathf.Max(1, maxAggressors))
            return false;
        activeBrains.Add(brain);
        return true;
    }

    public void ReleaseBrain(EnemyBrain brain)
    {
        if (brain != null)
            activeBrains.Remove(brain);
    }

    public bool RequestAggro(EnemyController enemy)
    {
        if (enemy == null)
            return false;

        // Musuh sudah mendapatkan slot.
        if (activeAggressors.Contains(enemy))
            return true;

        // Masukkan ke antrean jika belum terdaftar.
        if (waitingSet.Add(enemy))
        {
            waitingQueue.Enqueue(enemy);
        }

        FillOpenSlots();

        return activeAggressors.Contains(enemy);
    }

    public bool HasAggroSlot(EnemyController enemy)
    {
        return activeAggressors.Contains(enemy);
    }

    public void ReleaseAggro(EnemyController enemy)
    {
        if (enemy == null)
            return;

        activeAggressors.Remove(enemy);
        waitingSet.Remove(enemy);

        FillOpenSlots();
    }

    public void Unregister(EnemyController enemy)
    {
        if (enemy == null)
            return;

        activeAggressors.Remove(enemy);
        waitingSet.Remove(enemy);

        FillOpenSlots();
    }

    private void FillOpenSlots()
    {
        while (
            activeAggressors.Count < Mathf.Max(1, maxAggressors)
            && waitingQueue.Count > 0
        )
        {
            EnemyController candidate = waitingQueue.Dequeue();

            // Lewati musuh yang sudah dibatalkan atau dihancurkan.
            if (candidate == null || !waitingSet.Remove(candidate))
                continue;

            if (!candidate.isActiveAndEnabled)
                continue;

            activeAggressors.Add(candidate);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

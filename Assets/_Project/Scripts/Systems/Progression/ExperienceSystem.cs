using UnityEngine;
using TheLastSasak.Items.Loot;

public class ExperienceSystem : MonoBehaviour, IExperienceReceiver
{
    public static ExperienceSystem Instance { get; private set; }

    public int CurrentExp { get; private set; }
    public int Level { get; private set; } = 1;
    [Min(1)] public int ExpPerLevel = 50;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        ExpPerLevel = Mathf.Max(1, ExpPerLevel);
    }

    public void AddExperience(int amount) => Add(amount);

    public void Add(int amount)
    {
        if (amount <= 0)
            return;

        int threshold = Mathf.Max(1, ExpPerLevel);
        CurrentExp += amount;

        while (CurrentExp >= threshold)
        {
            CurrentExp -= threshold;
            Level++;
        }

        GameEvents.RaiseExperienceChanged(CurrentExp, Level);
    }

    public void ResetRun()
    {
        CurrentExp = 0;
        Level = 1;
        GameEvents.RaiseExperienceChanged(CurrentExp, Level);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
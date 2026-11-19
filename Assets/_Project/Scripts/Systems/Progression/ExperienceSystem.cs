using UnityEngine;

public class ExperienceSystem : MonoBehaviour
{
    public static ExperienceSystem Instance { get; private set; }

    public int CurrentExp { get; private set; }
    public int Level { get; private set; } = 1;
    public int ExpPerLevel = 50;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void Add(int amount)
    {
        CurrentExp += Mathf.Max(0, amount);
        while (CurrentExp >= ExpPerLevel)
        {
            CurrentExp -= ExpPerLevel;
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

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

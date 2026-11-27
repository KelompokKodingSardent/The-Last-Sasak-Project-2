using UnityEngine;
using TheLastSasak.Player.Stats;

public class PlayerProgression : MonoBehaviour
{
    public float HealthPerLevel = 8f;

    private Health _health;
    private PlayerStatController _statController;
    private int _appliedLevel = 1;

    private void Awake()
    {
        _health = GetComponent<Health>();
        _statController = GetComponent<PlayerStatController>();
    }

    private void OnEnable()
    {
        GameEvents.OnExperienceChanged += HandleExperienceChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnExperienceChanged -= HandleExperienceChanged;
    }

    private void Start()
    {
        if (ExperienceSystem.Instance != null)
        {
            HandleExperienceChanged(ExperienceSystem.Instance.CurrentExp, ExperienceSystem.Instance.Level);
        }
    }

    private void HandleExperienceChanged(int exp, int level)
    {
        int clampedLevel = Mathf.Max(1, level);

        // Reset bonus level saat run baru dimulai ulang (level kembali ke 1)
        if (clampedLevel < _appliedLevel)
        {
            _appliedLevel = clampedLevel;
            float resetBonus = HealthPerLevel * (_appliedLevel - 1);
            if (_statController != null)
                _statController.SetRunLevelHealthBonus(resetBonus);
            return;
        }

        if (clampedLevel == _appliedLevel)
            return;

        int levelDiff = clampedLevel - _appliedLevel;
        _appliedLevel = clampedLevel;
        float totalLevelBonus = HealthPerLevel * (_appliedLevel - 1);

        if (_statController != null)
            _statController.SetRunLevelHealthBonus(totalLevelBonus);
        else if (_health != null)
            _health.SetMaxHealth(_health.MaxHealth + (HealthPerLevel * levelDiff), healDifference: false);
    }
}
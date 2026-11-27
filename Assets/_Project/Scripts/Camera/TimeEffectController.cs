using System.Collections.Generic;
using UnityEngine;

public struct HitstopRequest
{
    public float Duration;
    public float TimeScale;
    public int Priority;

    public HitstopRequest(float duration, float timeScale = 0.04f, int priority = 0)
    {
        Duration = Mathf.Max(0f, duration);
        TimeScale = Mathf.Clamp(timeScale, 0.01f, 1f);
        Priority = priority;
    }
}

public class TimeEffectController : MonoBehaviour
{
    public static TimeEffectController Instance { get; private set; }

    [Header("Hitstop Limits")]
    [SerializeField] private float minHitstopDuration = 0.01f;
    [SerializeField] private float maxHitstopDuration = 0.25f;
    [SerializeField] private float defaultHitstopScale = 0.04f;

    private readonly List<HitstopRequest> _activeRequests = new List<HitstopRequest>(8);
    private float _baselineTimeScale = 1f;
    private bool _isHitstopActive;

    public bool IsHitstopActive => _isHitstopActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        CancelAllHitstops(restoreScale: true);
    }

    public static void Play(float duration)
    {
        Instance?.Run(duration);
    }

    public static void PlayRequest(HitstopRequest request)
    {
        Instance?.EnqueueHitstop(request);
    }

    public void Run(float duration)
    {
        if (duration <= 0f)
            return;

        EnqueueHitstop(new HitstopRequest(duration, defaultHitstopScale, 0));
    }

    public void EnqueueHitstop(HitstopRequest request)
    {
        if (request.Duration <= 0f)
            return;

        if (IsGamePaused())
            return;

        float clampedDuration = Mathf.Clamp(request.Duration, minHitstopDuration, maxHitstopDuration);
        request.Duration = clampedDuration;

        if (!_isHitstopActive)
        {
            if (Time.timeScale <= 0.01f)
                return;

            _baselineTimeScale = Time.timeScale;
            _isHitstopActive = true;
        }

        _activeRequests.Add(request);
        ApplyStrongestActiveRequest();
    }

    public void CancelAllHitstops(bool restoreScale = true)
    {
        _activeRequests.Clear();
        if (_isHitstopActive)
        {
            _isHitstopActive = false;
            if (restoreScale && !IsGamePaused())
            {
                Time.timeScale = _baselineTimeScale <= 0.01f ? 1f : _baselineTimeScale;
            }
        }
    }

    private void Update()
    {
        if (!_isHitstopActive)
            return;

        if (IsGamePaused())
        {
            // Jangan pernah menimpa Time.timeScale saat game di-pause
            _activeRequests.Clear();
            _isHitstopActive = false;
            return;
        }

        float dt = Time.unscaledDeltaTime;
        for (int i = _activeRequests.Count - 1; i >= 0; i--)
        {
            HitstopRequest req = _activeRequests[i];
            req.Duration -= dt;
            if (req.Duration <= 0f)
                _activeRequests.RemoveAt(i);
            else
                _activeRequests[i] = req;
        }

        if (_activeRequests.Count == 0)
        {
            _isHitstopActive = false;
            Time.timeScale = _baselineTimeScale <= 0.01f ? 1f : _baselineTimeScale;
        }
        else
        {
            ApplyStrongestActiveRequest();
        }
    }

    private void ApplyStrongestActiveRequest()
    {
        if (_activeRequests.Count == 0)
            return;

        int bestPriority = int.MinValue;
        float lowestScale = 1f;

        for (int i = 0; i < _activeRequests.Count; i++)
        {
            HitstopRequest req = _activeRequests[i];
            if (req.Priority > bestPriority)
            {
                bestPriority = req.Priority;
                lowestScale = req.TimeScale;
            }
            else if (req.Priority == bestPriority && req.TimeScale < lowestScale)
            {
                lowestScale = req.TimeScale;
            }
        }

        Time.timeScale = lowestScale;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.Paused)
        {
            _activeRequests.Clear();
            _isHitstopActive = false;
            Time.timeScale = 0f;
        }
        else if (newState == GameState.InRun && Time.timeScale <= 0.01f && !_isHitstopActive)
        {
            Time.timeScale = 1f;
        }
    }

    private static bool IsGamePaused()
    {
        return GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (!IsGamePaused() && Time.timeScale < 0.2f)
            Time.timeScale = 1f;
    }
}
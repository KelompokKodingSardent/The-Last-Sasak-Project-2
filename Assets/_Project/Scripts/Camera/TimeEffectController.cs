using System.Collections;
using UnityEngine;

public class TimeEffectController : MonoBehaviour
{
    public static TimeEffectController Instance { get; private set; }

    bool playing;
    float remainingRealtime;
    float pausedScale = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public static void Play(float duration)
    {
        Instance?.Run(duration);
    }

    public void Run(float duration)
    {
        if (duration <= 0f)
            return;

        if (playing)
        {
            remainingRealtime = Mathf.Max(remainingRealtime, duration);
            return;
        }

        if (Time.timeScale <= 0.01f)
            return;

        StartCoroutine(Hitstop(duration));
    }

    IEnumerator Hitstop(float duration)
    {
        playing = true;
        remainingRealtime = Mathf.Max(0.01f, duration);
        pausedScale = Time.timeScale;
        Time.timeScale = 0.04f;

        while (remainingRealtime > 0f)
        {
            remainingRealtime -= Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = pausedScale <= 0.01f ? 1f : pausedScale;
        playing = false;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        if (Time.timeScale < 0.2f)
            Time.timeScale = 1f;
    }
}
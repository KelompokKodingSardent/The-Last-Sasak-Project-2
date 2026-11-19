using System.Collections;
using UnityEngine;

public class TimeEffectController : MonoBehaviour
{
    public static TimeEffectController Instance { get; private set; }

    bool playing;
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
        if (Instance == null)
        {
            GameObject go = new GameObject("TimeEffectController");
            Instance = go.AddComponent<TimeEffectController>();
        }

        Instance.Run(duration);
    }

    public void Run(float duration)
    {
        if (playing || Time.timeScale <= 0.01f)
            return;
        StartCoroutine(Hitstop(duration));
    }

    IEnumerator Hitstop(float duration)
    {
        playing = true;
        pausedScale = Time.timeScale;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, duration));
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

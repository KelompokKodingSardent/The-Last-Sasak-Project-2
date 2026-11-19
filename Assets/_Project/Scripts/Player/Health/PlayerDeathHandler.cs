using UnityEngine;

public class PlayerDeathHandler : MonoBehaviour
{
    Health health;
    bool raised;

    void Awake()
    {
        health = GetComponent<Health>();
    }

    void OnEnable()
    {
        if (health != null)
            health.OnDied += Handle;
    }

    void OnDisable()
    {
        if (health != null)
            health.OnDied -= Handle;
    }

    void Handle()
    {
        if (raised)
            return;
        raised = true;
        GameEvents.RaisePlayerDied();
    }
}

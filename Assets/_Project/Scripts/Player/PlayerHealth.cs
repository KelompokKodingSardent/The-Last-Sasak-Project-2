using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    Health health;

    void Awake()
    {
        health = GetComponent<Health>();
        if (health == null)
            health = gameObject.AddComponent<Health>();
    }

    public void TakeDamage(int amount)
    {
        health.TakeDamage(DamageInfo.Create(amount, gameObject, false));
    }
}

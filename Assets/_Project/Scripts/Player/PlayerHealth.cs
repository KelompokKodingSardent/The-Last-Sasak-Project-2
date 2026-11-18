using UnityEngine;
using UnityEngine.InputSystem; // 1. Tambahkan ini di paling atas

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Hit Camera Impulse")]
    public float hitShakeForce = 3.0f;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        // 2. Ganti deteksi tombol 'K' ke New Input System
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            TakeDamage(15);
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"Player Kena Hit! Sisa HP: {currentHealth}");
        CameraEventManager.TriggerCameraShake(hitShakeForce);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player Mati!");
    }
}
using UnityEngine;
using UnityEngine.InputSystem; // 1. Tambahkan ini di paling atas

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public Transform attackPoint;
    public float attackRange = 1.2f;
    public LayerMask enemyLayers;
    public int attackDamage = 25;
    public float attackRate = 2f; 
    private float nextAttackTime = 0f;

    private void Update()
    {
        if (Time.time >= nextAttackTime)
        {
            // 2. Ganti deteksi klik mouse ke New Input System
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Attack();
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
    }

    private void Attack()
    {
        Debug.Log("Menebas senjata / menyerang!");
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider enemy in hitEnemies)
        {
            Debug.Log("Mengenai: " + enemy.name);
            CameraEventManager.TriggerCameraShake(0.8f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

public enum TelegraphShape
{
    Cone,
    Circle,
    Line,
    TargetedZone
}

public enum AttackDeliveryType
{
    Melee,
    Projectile,
    TargetedZone,
    SupportEmpower
}

[Serializable]
public class AttackSwingStep
{
    [Header("Tipe Eksekusi (Phase 3)")]
    public AttackDeliveryType deliveryType = AttackDeliveryType.Melee;

    [Header("Timing (Detik)")]
    [Tooltip("Durasi peringatan visual di lantai sebelum serangan dimulai")]
    public float telegraphDuration = 0.45f;
    [Tooltip("Durasi ancang-ancang animasi setelah telegraph selesai")]
    public float startupDuration = 0.12f;
    [Tooltip("Durasi hitbox aktif / pelepasan serangan")]
    public float executionDuration = 0.10f;
    [Tooltip("Jeda setelah tahap ini selesai")]
    public float recoveryDuration = 0.35f;

    [Header("Area & Telegraph")]
    public TelegraphShape telegraphShape = TelegraphShape.Cone;
    public float range = 2.2f;
    [Range(15f, 360f)]
    public float angle = 95f;
    public float width = 1.2f;

    [Header("Combat & Movement")]
    public float damageMultiplier = 1.0f;
    public float staggerDamage = 15f;
    [Tooltip("Dorongan maju (+) atau mundur/retreat (-) saat startup")]
    public float lungeDistance = 0.6f;
    public float lungeDuration = 0.12f;
    public bool lockRotationOnStartup = true;

    [Header("Phase 3 — Projectile Settings (Ranger)")]
    [Min(1)] public int projectileCount = 1;
    [Range(0f, 120f)] public float projectileSpreadAngle = 18f;

    [Header("Phase 3 — Targeted Zone / Hazard (Ranger & Controller)")]
    public float zoneRadius = 2.0f;
    [Tooltip("0 = ledakan instan saat execution (Ground Mark), >0 = hazard bertahan di arena")]
    public float zonePersistDuration = 0f;
    public float zoneTickInterval = 0.65f;
    public bool trackPlayerDuringTelegraph = true;
    [Tooltip("Opsional: prefab VFX hazard. Jika kosong, memakai visual ground zone otomatis")]
    public GameObject customHazardPrefab;

    [Header("Phase 3 — Support Empower (Controller)")]
    public float empowerRadius = 7.5f;
    public float empowerDuration = 5.0f;
    public float empowerSpeedMultiplier = 1.25f;
    public float empowerDamageMultiplier = 1.20f;
}

[CreateAssetMenu(fileName = "NewAttackPattern", menuName = "TheLastSasak/Combat/Enemy Attack Pattern")]
public class EnemyAttackPatternSO : ScriptableObject
{
    [Header("Identitas Pattern")]
    public string patternName = "Quick Slash";
    [Range(1, 100)]
    public int selectionWeight = 50;
    public float cooldown = 1.5f;

    [Header("Syarat Jarak (Approach)")]
    public float minTriggerRange = 0.5f;
    public float maxTriggerRange = 2.4f;

    [Header("Aturan Interaksi Player")]
    public bool canBeParried = true;
    public float parryStaggerBonus = 35f;
    public float baseDamage = 12f;

    [Header("Rangkaian Tahap Serangan (1 = Single, 2+ = Combo/Burst)")]
    public List<AttackSwingStep> swings = new List<AttackSwingStep>();

    [Header("Post-Attack Reposition")]
    public bool repositionAfterAttack = false;
    public float repositionDistance = 1.8f;
}
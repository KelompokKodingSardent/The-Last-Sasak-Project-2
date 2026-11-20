using System;
using UnityEngine;

namespace TheLastSasak.Enemy
{
    public enum EnemyArchetype
    {
        Vanguard,
        Skirmisher,
        Ranger,
        Bulwark,
        Controller
    }

    public enum CombatRole
    {
        Hold,       // Menjaga jarak aman / menunggu giliran
        Flank,      // Mencari sisi kiri/kanan/belakang yang kosong
        Reposition, // Mundur atau bergeser setelah menyerang
        Attack      // Mendapat slot agresor untuk melakukan Commit
    }

    public enum TacticalSector
    {
        FrontPressure,  // 0° (di depan arah hadap player)
        SideFlankLeft,  // -90° (sisi kiri player)
        SideFlankRight, // +90° (sisi kanan player)
        RearApproach    // 180° (belakang player)
    }

    public enum TelegraphShape
    {
        Circular,
        Linear,
        Cone,
        Zone,
        Targeted
    }

    public enum EnemyBehaviorState
    {
        Idle,
        Observe,      // Mengamati posisi player & okupansi arena sebelum bergerak
        Positioning,  // Bergerak menuju sektor taktis atau jarak serang
        CommitAttack, // Mengeksekusi 6 fase serangan
        Recover,      // Jeda setelah menyerang (jendela punish bagi player)
        Staggered,    // Terkena stagger / parry stun
        Dead
    }

    [Serializable]
    public class EnemyAttackPattern
    {
        public string patternName = "Cross Slash";
        public TelegraphShape telegraphShape = TelegraphShape.Cone;

        [Header("1. Approach")]
        public float minTriggerRange = 0.5f;
        public float maxTriggerRange = 2.6f;

        [Header("2 - 5. Timing Phases (Seconds)")]
        public float telegraphDuration = 0.35f;
        public float startupDuration = 0.12f;
        public float executionDuration = 0.18f;
        public float recoveryDuration = 0.55f;

        [Header("Execution & Movement")]
        public float damage = 12f;
        public float hitRadius = 1.4f;
        public float hitForwardOffset = 1.1f;
        public float lungeSpeed = 7.5f; // Kecepatan maju/dash saat eksekusi
        public bool canBeParried = true;

        [Header("6. Post-Attack Reposition")]
        public bool retreatAfterAttack = true;
        public float retreatDistance = 3.2f;
        public float cooldown = 2.0f;
    }
}
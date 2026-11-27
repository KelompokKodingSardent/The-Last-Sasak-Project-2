using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TheLastSasak.Player.Stats;
using TheLastSasak.Systems.Save;

namespace TheLastSasak.Diagnostics
{
    public class SystemDiagnosticsRunner : MonoBehaviour
    {
        [Header("Auto Run")]
        [SerializeField] private bool runSmokeCheckOnStart = true;

        [Header("Pool Stress Test Settings")]
        [SerializeField] private GameObject stressTestPrefab;
        [SerializeField] private int stressSpawnCount = 100;

        private void Start()
        {
            if (runSmokeCheckOnStart)
            {
                RunFullSmokeCheck();
            }
        }

        [ContextMenu("1. Run Full Smoke Check")]
        public void RunFullSmokeCheck()
        {
            List<string> warnings = new List<string>();
            List<string> errors = new List<string>();

            string activeScene = SceneManager.GetActiveScene().name;
            Debug.Log($"[TLS Diagnostics] Memulai Smoke Check pada scene: '{activeScene}'...");

            VerifySingletons(warnings, errors);
            VerifyPlayerAuthority(warnings, errors);
            VerifyEconomyAndSave(warnings, errors);
            VerifyFeedbackControllers(warnings, errors);

            if (errors.Count == 0 && warnings.Count == 0)
            {
                Debug.Log($"[TLS Diagnostics] PASS — Semua sistem inti pada '{activeScene}' bersih dan tersinkronisasi.");
            }
            else
            {
                for (int i = 0; i < warnings.Count; i++)
                    Debug.LogWarning($"[TLS Diagnostics][WARN] {warnings[i]}");

                for (int i = 0; i < errors.Count; i++)
                    Debug.LogError($"[TLS Diagnostics][ERROR] {errors[i]}");
            }
        }

        private void VerifySingletons(List<string> warnings, List<string> errors)
        {
            if (GameManager.Instance == null)
                warnings.Add("GameManager.Instance tidak ditemukan di scene ini (normal jika scene menu terisolasi).");

            if (ObjectPoolManager.Instance == null)
                warnings.Add("ObjectPoolManager.Instance tidak ditemukan di scene ini.");

            if (CombatSystem.Instance == null)
                warnings.Add("CombatSystem.Instance tidak ditemukan di scene ini.");
        }

        private void VerifyPlayerAuthority(List<string> warnings, List<string> errors)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                warnings.Add("Objek dengan tag 'Player' tidak ditemukan di scene aktif.");
                return;
            }

            Health health = player.GetComponent<Health>();
            PlayerStatController stats = player.GetComponent<PlayerStatController>();

            if (health == null)
                errors.Add("Player tidak memiliki komponen Health.");

            if (stats == null)
                errors.Add("Player tidak memiliki komponen PlayerStatController.");

            if (health != null && stats != null)
            {
                if (!Mathf.Approximately(health.MaxHealth, stats.FinalMaxHealth))
                {
                    errors.Add($"Desync MaxHealth terdeteksi! Health.MaxHealth={health.MaxHealth} vs PlayerStatController.FinalMaxHealth={stats.FinalMaxHealth}.");
                }
            }
        }

        private void VerifyEconomyAndSave(List<string> warnings, List<string> errors)
        {
            if (SaveSystem.Instance == null)
            {
                warnings.Add("SaveSystem.Instance tidak ditemukan di scene ini.");
            }

            if (CurrencyWallet.Instance == null)
            {
                warnings.Add("CurrencyWallet.Instance tidak ditemukan di scene ini.");
            }
            else if (CurrencyWallet.Instance.SoulsCoin < 0)
            {
                errors.Add($"CurrencyWallet.SoulsCoin bernilai negatif: {CurrencyWallet.Instance.SoulsCoin}.");
            }
        }

        private void VerifyFeedbackControllers(List<string> warnings, List<string> errors)
        {
            if (TimeEffectController.Instance == null)
            {
                warnings.Add("TimeEffectController.Instance tidak ditemukan; hitstop tidak akan aktif.");
            }
            else if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.InRun && Time.timeScale <= 0.01f)
            {
                errors.Add($"GameState sedang InRun tetapi Time.timeScale={Time.timeScale} (terjebak slow-mo/pause).");
            }
        }

        [ContextMenu("2. Run Pool Stress Test (Get & Release)")]
        public void RunPoolStressTest()
        {
            if (ObjectPoolManager.Instance == null)
            {
                Debug.LogError("[TLS Diagnostics] Gagal menjalankan Pool Stress Test: ObjectPoolManager.Instance null.");
                return;
            }

            if (stressTestPrefab == null)
            {
                Debug.LogWarning("[TLS Diagnostics] Isi field 'stressTestPrefab' terlebih dahulu di Inspector untuk menjalankan Pool Stress Test.");
                return;
            }

            int count = Mathf.Clamp(stressSpawnCount, 1, 500);
            List<GameObject> rented = new List<GameObject>(count);

            float startTime = Time.realtimeSinceStartup;

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = new Vector3((i % 10) * 1.5f, 0f, (i / 10) * 1.5f);
                GameObject obj = ObjectPoolManager.Instance.Get(stressTestPrefab, pos, Quaternion.identity);
                if (obj != null)
                {
                    rented.Add(obj);
                }
            }

            for (int i = 0; i < rented.Count; i++)
            {
                ObjectPoolManager.Instance.Release(rented[i]);
            }

            float elapsedMs = (Time.realtimeSinceStartup - startTime) * 1000f;
            Debug.Log($"[TLS Diagnostics] Pool Stress Test selesai: {rented.Count}/{count} objek di-rent & di-release dalam {elapsedMs:F2} ms.");
        }
    }
}
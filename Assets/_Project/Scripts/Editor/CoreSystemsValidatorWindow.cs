#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TheLastSasak.Player.Stats;
using TheLastSasak.Systems.Save;

namespace TheLastSasak.EditorTools
{
    public class CoreSystemsValidatorWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private readonly List<string> _reportLines = new List<string>();

        [MenuItem("TLS/Diagnostics/Core Systems Validator")]
        public static void ShowWindow()
        {
            GetWindow<CoreSystemsValidatorWindow>("TLS Core Validator");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("The Last Sasak — Core Systems Audit", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Memeriksa kelengkapan komponen inti, duplikasi singleton, dan missing scripts pada scene yang sedang terbuka.",
                MessageType.Info);

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Audit Active Scene", GUILayout.Height(30)))
            {
                RunSceneAudit();
            }

            EditorGUILayout.Space(8);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            for (int i = 0; i < _reportLines.Count; i++)
            {
                EditorGUILayout.LabelField(_reportLines[i], EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        private void RunSceneAudit()
        {
            _reportLines.Clear();
            Scene activeScene = EditorSceneManager.GetActiveScene();
            _reportLines.Add($"=== Audit Scene: {activeScene.name} ===");

            // 1. Cek Missing Scripts
            int missingScriptCount = 0;
            GameObject[] rootObjects = activeScene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                Transform[] allTransforms = rootObjects[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < allTransforms.Length; j++)
                {
                    int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(allTransforms[j].gameObject);
                    if (missing > 0)
                    {
                        missingScriptCount += missing;
                        _reportLines.Add($"[ERROR] Missing script ({missing}) pada GameObject: {allTransforms[j].name}");
                    }
                }
            }

            if (missingScriptCount == 0)
                _reportLines.Add("[OK] Tidak ada Missing Script pada scene.");

            // 2. Cek Duplikasi Manager Utama
            CheckSingletonCount<GameManager>("GameManager");
            CheckSingletonCount<ObjectPoolManager>("ObjectPoolManager");
            CheckSingletonCount<CombatSystem>("CombatSystem");
            CheckSingletonCount<SaveSystem>("SaveSystem");
            CheckSingletonCount<CurrencyWallet>("CurrencyWallet");
            CheckSingletonCount<ExperienceSystem>("ExperienceSystem");
            CheckSingletonCount<TimeEffectController>("TimeEffectController");

            // 3. Cek Player Setup
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                _reportLines.Add("[WARN] Tidak ditemukan GameObject dengan tag 'Player' di scene ini.");
            }
            else
            {
                bool hasHealth = player.GetComponent<Health>() != null;
                bool hasStats = player.GetComponent<PlayerStatController>() != null;
                _reportLines.Add(hasHealth ? "[OK] Player memiliki Health." : "[ERROR] Player kehilangan komponen Health.");
                _reportLines.Add(hasStats ? "[OK] Player memiliki PlayerStatController." : "[ERROR] Player kehilangan komponen PlayerStatController.");
            }

            _reportLines.Add("=== Audit Selesai ===");
        }

        private void CheckSingletonCount<T>(string label) where T : Component
        {
            T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (found.Length == 0)
                _reportLines.Add($"[WARN] {label}: 0 instance di scene.");
            else if (found.Length == 1)
                _reportLines.Add($"[OK] {label}: 1 instance ditemukan ({found[0].gameObject.name}).");
            else
                _reportLines.Add($"[ERROR] {label}: Ditemukan {found.Length} instance duplikat! Sisakan hanya 1.");
        }
    }
}
#endif
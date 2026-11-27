using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TheLastSasak.Systems.Save
{
    public interface ISaveableSystem
    {
        void WriteToSave(TLS_SaveData data);
        void ReadFromSave(TLS_SaveData data);
    }

    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        [Header("File Settings")]
        [SerializeField] private string saveFileName = "tls_save.json";
        [SerializeField] private bool prettyPrintJson = true;
        [SerializeField] private bool autoLoadOnStart = true;

        [Header("Dependencies")]
        [SerializeField] private ItemPersistenceCoordinator itemPersistence;

        private readonly List<ISaveableSystem> _externalSaveables = new List<ISaveableSystem>();
        private TLS_SaveData _currentSaveData = new TLS_SaveData();

        public event Action<TLS_SaveData> OnGameSaved;
        public event Action<TLS_SaveData> OnGameLoaded;

        public string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);
        public TLS_SaveData CurrentSaveData => _currentSaveData;
        public bool HasLoadedData { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            if (itemPersistence == null)
            {
                itemPersistence = FindFirstObjectByType<ItemPersistenceCoordinator>();
            }
        }

        private void Start()
        {
            if (autoLoadOnStart && HasSaveFile())
            {
                LoadGame();
            }
        }

        public void RegisterSaveable(ISaveableSystem saveable)
        {
            if (saveable == null)
                return;

            if (!_externalSaveables.Contains(saveable))
            {
                _externalSaveables.Add(saveable);
            }

            // Jika SaveSystem sudah memuat file sebelumnya, langsung sinkronkan komponen yang baru mendaftar
            if (HasLoadedData && _currentSaveData != null)
            {
                saveable.ReadFromSave(_currentSaveData);
            }
        }

        public void UnregisterSaveable(ISaveableSystem saveable)
        {
            if (saveable != null)
            {
                _externalSaveables.Remove(saveable);
            }
        }

        public bool HasSaveFile()
        {
            return File.Exists(SaveFilePath);
        }

        [ContextMenu("Save Game")]
        public bool SaveGame()
        {
            try
            {
                _currentSaveData ??= new TLS_SaveData();
                _currentSaveData.SaveVersion = TLS_SaveData.LatestSaveVersion;
                _currentSaveData.LastSavedUtc = DateTime.UtcNow.ToString("o");
                _currentSaveData.EnsureValidStructure();

                for (int i = 0; i < _externalSaveables.Count; i++)
                {
                    _externalSaveables[i]?.WriteToSave(_currentSaveData);
                }

                if (itemPersistence != null)
                {
                    itemPersistence.CaptureToSaveData(_currentSaveData);
                }

                string json = JsonUtility.ToJson(_currentSaveData, prettyPrintJson);
                WriteAtomicText(SaveFilePath, json);

                OnGameSaved?.Invoke(_currentSaveData);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Gagal menyimpan game: {ex.Message}", this);
                return false;
            }
        }

        [ContextMenu("Load Game")]
        public bool LoadGame()
        {
            if (!HasSaveFile())
            {
                Debug.LogWarning($"[SaveSystem] File save tidak ditemukan di: {SaveFilePath}", this);
                return false;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogError("[SaveSystem] File tls_save.json kosong.", this);
                    return false;
                }

                TLS_SaveData loadedData = JsonUtility.FromJson<TLS_SaveData>(json);
                if (loadedData == null)
                {
                    Debug.LogError("[SaveSystem] Gagal parse JSON tls_save.json.", this);
                    return false;
                }

                if (!SaveVersionMigrator.TryMigrateToLatest(loadedData, out _))
                {
                    return false;
                }

                if (!SaveValidator.IsValid(loadedData))
                {
                    Debug.LogError("[SaveSystem] Struktur data save tidak valid.", this);
                    return false;
                }

                _currentSaveData = loadedData;
                HasLoadedData = true;

                for (int i = 0; i < _externalSaveables.Count; i++)
                {
                    _externalSaveables[i]?.ReadFromSave(_currentSaveData);
                }

                if (itemPersistence != null)
                {
                    itemPersistence.ApplyFromSaveData(_currentSaveData);
                }

                OnGameLoaded?.Invoke(_currentSaveData);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Gagal memuat game: {ex.Message}", this);
                return false;
            }
        }

        [ContextMenu("Delete Save File")]
        public void DeleteSaveFile()
        {
            HasLoadedData = false;
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                Debug.Log($"[SaveSystem] File save dihapus: {SaveFilePath}", this);
            }
        }

        private static void WriteAtomicText(string targetPath, string content)
        {
            string directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string tempPath = targetPath + ".tmp";
            File.WriteAllText(tempPath, content);

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            File.Move(tempPath, targetPath);
        }

        public static void SaveNow()
        {
            if (Instance == null)
                Instance = FindFirstObjectByType<SaveSystem>();

            Instance?.SaveGame();
        }

        public void AddPermanentHealth(float amount = 0f)
        {
            if (amount <= 0f)
                return;

            _currentSaveData ??= new TLS_SaveData();
            _currentSaveData.EnsureValidStructure();
            _currentSaveData.PlayerProgressionData.PermanentUpgradeIds.Add($"max_health_{amount}");
            SaveGame();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
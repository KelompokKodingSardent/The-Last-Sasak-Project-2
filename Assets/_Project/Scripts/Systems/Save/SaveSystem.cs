using System;
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
        [Header("File Settings")]
        [SerializeField] private string saveFileName = "tls_save.json";
        [SerializeField] private bool prettyPrintJson = true;
        [SerializeField] private bool autoLoadOnStart = false;

        [Header("Dependencies")]
        [SerializeField] private ItemPersistenceCoordinator itemPersistence;

        private readonly System.Collections.Generic.List<ISaveableSystem> _externalSaveables =
            new System.Collections.Generic.List<ISaveableSystem>();

        private TLS_SaveData _currentSaveData = new TLS_SaveData();

        public event Action<TLS_SaveData> OnGameSaved;
        public event Action<TLS_SaveData> OnGameLoaded;

        public string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);
        public TLS_SaveData CurrentSaveData => _currentSaveData;

        private void Awake()
        {
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
            if (saveable != null && !_externalSaveables.Contains(saveable))
            {
                _externalSaveables.Add(saveable);
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
                Debug.Log($"[SaveSystem] Berhasil menyimpan ke: {SaveFilePath}", this);
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
                // Langkah 1: Baca JSON
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

                // Langkah 2 & 3: Periksa SaveVersion & validasi struktur data
                if (!SaveVersionMigrator.TryMigrateToLatest(loadedData, out _))
                {
                    return false;
                }

                _currentSaveData = loadedData;

                // Pulihkan sistem eksternal (CurrencyWallet, PlayerProgression, Settings)
                for (int i = 0; i < _externalSaveables.Count; i++)
                {
                    _externalSaveables[i]?.ReadFromSave(_currentSaveData);
                }

                // Langkah 4–10: Resolusi ItemCatalogSO, Inventory, Equipment, Artifacts & RecalculateStats
                if (itemPersistence != null)
                {
                    itemPersistence.ApplyFromSaveData(_currentSaveData);
                }

                OnGameLoaded?.Invoke(_currentSaveData);
                Debug.Log("[SaveSystem] Berhasil memuat tls_save.json.", this);
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
    }
}
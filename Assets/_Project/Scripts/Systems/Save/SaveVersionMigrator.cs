using UnityEngine;

namespace TheLastSasak.Systems.Save
{
    public static class SaveVersionMigrator
    {
        public static bool TryMigrateToLatest(TLS_SaveData data, out string migrationReport)
        {
            if (data == null)
            {
                migrationReport = "SaveData null.";
                return false;
            }

            data.EnsureValidStructure();

            if (data.SaveVersion > TLS_SaveData.LatestSaveVersion)
            {
                migrationReport = $"SaveVersion {data.SaveVersion} lebih baru dari versi aplikasi ({TLS_SaveData.LatestSaveVersion}).";
                Debug.LogError($"[SaveVersionMigrator] {migrationReport}");
                return false;
            }

            int originalVersion = data.SaveVersion;

            // Migrasi dari v0 (pre-Item Domain) ke v1
            if (data.SaveVersion < 1)
            {
                MigrateV0ToV1(data);
                data.SaveVersion = 1;
            }

            // Tambahkan langkah migrasi berikutnya di sini saat LatestSaveVersion naik:
            // if (data.SaveVersion == 1) { MigrateV1ToV2(data); data.SaveVersion = 2; }

            migrationReport = originalVersion == TLS_SaveData.LatestSaveVersion
                ? $"SaveVersion {data.SaveVersion} valid."
                : $"Migrasi SaveVersion {originalVersion} ke {data.SaveVersion} berhasil.";

            if (originalVersion != TLS_SaveData.LatestSaveVersion)
            {
                Debug.Log($"[SaveVersionMigrator] {migrationReport}");
            }

            return true;
        }

        private static void MigrateV0ToV1(TLS_SaveData data)
        {
            // Normalisasi seluruh ItemId lama ke format huruf kecil tanpa spasi
            for (int i = 0; i < data.InventorySaveData.Items.Count; i++)
            {
                InventorySlotSaveEntry entry = data.InventorySaveData.Items[i];
                if (!string.IsNullOrWhiteSpace(entry.ItemId))
                {
                    entry.ItemId = entry.ItemId.Trim().ToLowerInvariant();
                    data.InventorySaveData.Items[i] = entry;
                }
            }

            if (!string.IsNullOrWhiteSpace(data.EquipmentSaveData.WeaponSlot1Id))
            {
                data.EquipmentSaveData.WeaponSlot1Id = data.EquipmentSaveData.WeaponSlot1Id.Trim().ToLowerInvariant();
            }

            if (!string.IsNullOrWhiteSpace(data.EquipmentSaveData.WeaponSlot2Id))
            {
                data.EquipmentSaveData.WeaponSlot2Id = data.EquipmentSaveData.WeaponSlot2Id.Trim().ToLowerInvariant();
            }

            if (!string.IsNullOrWhiteSpace(data.EquipmentSaveData.ArmorId))
            {
                data.EquipmentSaveData.ArmorId = data.EquipmentSaveData.ArmorId.Trim().ToLowerInvariant();
            }
        }
    }
}
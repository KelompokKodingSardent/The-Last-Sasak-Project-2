using System;
using System.Collections.Generic;

namespace TheLastSasak.Systems.Save
{
    [Serializable]
    public class ArtifactSaveData
    {
        public int MaxSlots = 2;
        public List<string> ArtifactIds = new List<string>();

        public void EnsureInitialized()
        {
            if (MaxSlots <= 0) MaxSlots = 2;
            ArtifactIds ??= new List<string>();
        }
    }

    [Serializable]
    public class EquipmentSaveData
    {
        public string WeaponSlot1Id = string.Empty;
        public string WeaponSlot2Id = string.Empty;
        public int ActiveWeaponSlot = 0; // 0 = None, 1 = Slot1, 2 = Slot2
        public string ArmorId = string.Empty;
        public ArtifactSaveData Artifacts = new ArtifactSaveData();

        public void EnsureInitialized()
        {
            WeaponSlot1Id ??= string.Empty;
            WeaponSlot2Id ??= string.Empty;
            ArmorId ??= string.Empty;
            Artifacts ??= new ArtifactSaveData();
            Artifacts.EnsureInitialized();
        }
    }
}
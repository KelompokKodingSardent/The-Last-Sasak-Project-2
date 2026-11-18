using System.Collections.Generic;
using UnityEngine;

public enum CutsceneActionType 
{ 
    SwitchCamera, 
    PlayDialogue, 
    WaitDuration, 
    EndCutscene 
}

public enum VNSlot 
{ 
    Hide,       // Hilangkan karakter dari layar
    Left,       // Posisi Kiri
    CenterLeft, // Kiri Tengah
    Center,     // Tengah
    CenterRight,// Kanan Tengah
    Right       // Posisi Kanan
}

[System.Serializable]
public class VNActorCommand
{
    [Tooltip("Nama ID Karakter (misal: 'Bapak Ade' atau 'Rucky')")]
    public string actorName;
    
    [Tooltip("Sprite ekspresi (marah, senyum, kaget, dll)")]
    public Sprite expression;
    
    [Tooltip("Posisi munculnya sprite di UI Canvas")]
    public VNSlot slot = VNSlot.Left;
    
    [Tooltip("Centang jika karakter ini yang bicara. UI Manager nanti akan membuat sprite ini terang, dan sprite yang tidak dicentang jadi agak redup.")]
    public bool isTalking = true;
}

[System.Serializable]
public class CutsceneAction
{
    [Header("=== General Control ===")]
    public CutsceneActionType actionType;
    [Tooltip("Hapus centang jika ingin step ini jalan bersamaan dengan step di bawahnya (misal: pindah kamera Sambil dialog muncul)")]
    public bool waitForCompletion = true;

    [Header("=== Camera / Wait Settings ===")]
    public string cameraID; 
    public float waitTime = 1f; 

    [Header("=== Dialogue Settings ===")]
    public string speakerName;
    [TextArea(2, 4)]
    public string dialogueText;
    
    [Header("=== VN Sprites on Screen ===")]
    [Tooltip("Daftar karakter yang tampil, berubah ekspresi, atau hilang di baris dialog ini.")]
    public List<VNActorCommand> actorCommands = new List<VNActorCommand>();
}
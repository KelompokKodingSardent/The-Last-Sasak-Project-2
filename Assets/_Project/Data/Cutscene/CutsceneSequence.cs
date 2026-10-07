using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCutsceneSequence", menuName = "Kasantara/Cutscene Sequence")]
public class CutsceneSequence : ScriptableObject
{
    [Tooltip("Daftar urutan aksi cutscene dari atas ke bawah")]
    public List<CutsceneAction> sequenceSteps;
}
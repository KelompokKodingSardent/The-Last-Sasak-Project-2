using Unity.Cinemachine;
using UnityEngine;

// Kamera diletakkan di scene. Script ini tidak membuat objek baru.
public static class CinemachineRig
{
    public static CinemachineCamera FindFollow()
    {
        return Object.FindFirstObjectByType<CinemachineCamera>();
    }
}

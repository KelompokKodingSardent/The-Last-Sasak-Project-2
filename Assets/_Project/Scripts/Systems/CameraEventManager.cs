using System;
using UnityEngine;

public static class CameraEventManager
{
    public static event Action<string> OnSwitchCamera;
    public static event Action<float> OnCameraShake;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticEventsOnLoad()
    {
        ClearAll();
    }

    public static void TriggerSwitchCamera(string cameraID)
    {
        if (!string.IsNullOrWhiteSpace(cameraID))
            OnSwitchCamera?.Invoke(cameraID);
    }

    public static void TriggerCameraShake(float force)
    {
        if (force > 0f)
            OnCameraShake?.Invoke(force);
    }

    public static void ClearAll()
    {
        OnSwitchCamera = null;
        OnCameraShake = null;
    }
}
// CameraEventManager.cs
using System;

public static class CameraEventManager
{
    // Event untuk berpindah kamera
    public static Action<string> OnSwitchCamera;
    
    // Event untuk camera shake (parameter: force)
    public static Action<float> OnCameraShake;

    // Method pembantu agar pemanggilan lebih rapi
    public static void TriggerSwitchCamera(string cameraID) => OnSwitchCamera?.Invoke(cameraID);
    public static void TriggerCameraShake(float force) => OnCameraShake?.Invoke(force);
}
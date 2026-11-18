// CameraManager.cs
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine; // <-- Update namespace di sini

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraManager : MonoBehaviour
{
    [System.Serializable]
    public struct VCamData
    {
        public string id; 
        public CinemachineCamera vcam; // <-- Update nama class di sini
    }

    [Header("Virtual Cameras")]
    public List<VCamData> virtualCameras;

    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void OnEnable()
    {
        CameraEventManager.OnSwitchCamera += SwitchCamera;
        CameraEventManager.OnCameraShake += ShakeCamera;
    }

    private void OnDisable()
    {
        CameraEventManager.OnSwitchCamera -= SwitchCamera;
        CameraEventManager.OnCameraShake -= ShakeCamera;
    }

    private void SwitchCamera(string targetID)
    {
        foreach (var data in virtualCameras)
        {
            // Cinemachine otomatis melakukan blending/transisi ke kamera dengan Priority tertinggi
            data.vcam.Priority = (data.id == targetID) ? 10 : 0;
        }
    }

    private void ShakeCamera(float force)
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulseWithForce(force);
        }
    }
}
using UnityEngine;

[ExecuteAlways]
public class CameraFollow : MonoBehaviour
{
    public enum CameraPreset
    {
        Custom,
        Diablo,
        GodOfWar
    }

    public Transform target;
    public CameraPreset preset = CameraPreset.Custom;
    
    [Header("Settings")]
    public float distance = 5.0f;
    public float pitch = 20.0f;
    public float yaw = 0.0f;
    public float smoothSpeed = 0.125f;

    private void LateUpdate()
    {
        if (target == null) return;

        ApplyPresets();

        // Calculate rotation based on preset
        Quaternion targetRotation;
        if (preset == CameraPreset.GodOfWar)
        {
            // For GoW, follow the target's yaw but keep fixed pitch
            targetRotation = Quaternion.Euler(pitch, target.eulerAngles.y + yaw, 0);
        }
        else
        {
            // For Diablo/Custom, use fixed world rotation
            targetRotation = Quaternion.Euler(pitch, yaw, 0);
        }
        
        // Calculate position based on rotation and distance
        Vector3 offset = targetRotation * new Vector3(0, 0, -distance);
        Vector3 desiredPosition = target.position + offset;
        
        // Smoothly move the camera
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        // Always look at the target
        transform.LookAt(target.position + Vector3.up * 1.5f); 
    }

    private void ApplyPresets()
    {
        switch (preset)
        {
            case CameraPreset.Diablo:
                distance = 12.0f;
                pitch = 55.0f;
                yaw = 45.0f;
                break;
            case CameraPreset.GodOfWar:
                distance = 5.0f;
                pitch = 15.0f;
                yaw = 0.0f;
                break;
        }
    }
}

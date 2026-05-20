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
    public float rotationSmoothSpeed = 0.2f;

    [Header("Combat Polish")]
public bool isCombatOrbiting = false;
    public float orbitSpeed = 40f; // Faster spin
    private float combatOrbitYaw = 0f;

    [Header("Juice")]
    public float shakeDuration = 0f;
    public float shakeMagnitude = 0.1f;
    private Vector3 shakeOffset = Vector3.zero;

    public void Shake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }

    private void LateUpdate()
    {
        if (target == null) 
        {
            // Auto-fallback to Steve if target is lost or destroyed
            GameObject steve = GameObject.Find("Steve") ?? GameObject.Find("Player");
            if (steve != null) target = steve.transform;
            else return;
        }

        // Handle Shake
        if (shakeDuration > 0)
        {
            shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            shakeDuration -= Time.deltaTime;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }

        ApplyPresets();

        // Calculate rotation based on preset
        Quaternion targetRotation;
        if (isCombatOrbiting)
        {
            combatOrbitYaw += orbitSpeed * Time.deltaTime;
            // Add a fixed pitch for combat showcase
            targetRotation = Quaternion.Euler(pitch + 10f, yaw + combatOrbitYaw, 0);
        }
        else if (preset == CameraPreset.GodOfWar)
        {
            combatOrbitYaw = 0; 
            // Follow target's rotation on Y axis
            targetRotation = Quaternion.Euler(pitch, target.eulerAngles.y + yaw, 0);
        }
        else
        {
            // Diablo/Custom: Fixed world rotation
            targetRotation = Quaternion.Euler(pitch, yaw, 0);
        }
        
        // Calculate position based on rotation and distance
        Vector3 offset = targetRotation * new Vector3(0, 0, -distance);
        Vector3 desiredPosition = target.position + offset;
        
        // Smoothly move the camera
        float t = 1.0f - Mathf.Exp(-smoothSpeed * Time.deltaTime * 10f);
        if (!Application.isPlaying) t = 1f;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, t) + shakeOffset;
        
        // Look at target with vertical offset
        Vector3 lookTarget = target.position + Vector3.up * 1.2f;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookTarget - transform.position), t);
    }

    public void SetTarget(Transform newTarget, bool instant = false)
    {
        target = newTarget;
        if (instant)
        {
            ApplyPresets();
            Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 offset = targetRotation * new Vector3(0, 0, -distance);
            transform.position = target.position + offset;
            transform.LookAt(target.position + Vector3.up * 1.2f);
        }
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
                distance = 6.0f;
                pitch = 25.0f;
                yaw = 0.0f;
                break;
        }
    }
}

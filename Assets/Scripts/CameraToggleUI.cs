using UnityEngine;
using UnityEngine.UI;

public class CameraToggleUI : MonoBehaviour
{
    public CameraFollow cameraFollow;
    public Button toggleButton;
    public Image iconImage;
    
    [Header("Icons")]
    public Sprite diabloIcon;
    public Sprite gowIcon;

    private void Start()
    {
        if (cameraFollow == null) cameraFollow = Camera.main.GetComponent<CameraFollow>();
        
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleCamera);
        }
        
        UpdateUI();
    }

    public void ToggleCamera()
    {
        if (cameraFollow == null) return;

        if (cameraFollow.preset == CameraFollow.CameraPreset.Diablo)
        {
            cameraFollow.preset = CameraFollow.CameraPreset.GodOfWar;
        }
        else
        {
            cameraFollow.preset = CameraFollow.CameraPreset.Diablo;
        }

        UpdateUI();
        Debug.Log("[CameraToggleUI] Switched camera to: " + cameraFollow.preset);
    }

    private void UpdateUI()
    {
        if (cameraFollow == null || iconImage == null) return;

        if (cameraFollow.preset == CameraFollow.CameraPreset.Diablo)
        {
            if (gowIcon != null) iconImage.sprite = gowIcon; // Show next option? Or current? Let's show current
            // Actually usually toggles show the "state" or "action". 
            // Let's make it show the current mode's representative icon.
            if (diabloIcon != null) iconImage.sprite = diabloIcon;
        }
        else
        {
            if (gowIcon != null) iconImage.sprite = gowIcon;
        }
    }
}

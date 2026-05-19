using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null) return;
        }

        // Match the camera's rotation exactly to stay "flat" to the screen
        transform.rotation = mainCam.transform.rotation;
    }
}

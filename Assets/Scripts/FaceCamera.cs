using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCam != null)
        {
            // Match the camera's rotation exactly to stay "flat" to the screen
            transform.rotation = mainCam.transform.rotation;
        }
        else
        {
            mainCam = Camera.main;
        }
    }
}

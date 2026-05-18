using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private void LateUpdate()
    {
        if (Camera.main != null)
        {
            // Match the camera's rotation exactly to stay "flat" to the screen
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}

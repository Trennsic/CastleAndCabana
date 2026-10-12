using UnityEngine;

public class UiBillboardingAddon : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    void Start()
    {
        // Find the main camera in the scene
        mainCamera = Camera.main;

        // Handle the case where no camera is tagged as Main
        if (mainCamera == null)
        {
            Debug.LogError("No Main Camera found. Please ensure you have a camera tagged as MainCamera.");
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        // Get the direction to the camera, keeping only the Y-axis rotation
        Vector3 directionToCamera = mainCamera.transform.position - transform.position;
        directionToCamera.y = 0; // Ignore vertical alignment

        // Rotate the object to face the camera
        if (directionToCamera != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
            transform.rotation = targetRotation;
        }
    }
}
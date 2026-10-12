
using UnityEngine;

public class RotatorAddon : MonoBehaviour
{
    [Header("Axis Lock Toggles")]
    public bool lockX = false;
    public bool lockY = false;
    public bool lockZ = false;

    [Header("Rotation Speeds")]
    public int speedX = 0;
    public int speedY = 0;
    public int speedZ = 0;

    [Header("Oscillation")]
    public bool oscillate = false;

    [Range(0f, 1f)]
    public float oscillationSpeed = 0.5f; // Speed at which oscillation occurs

    private Vector3 currentRotationSpeed;
    private Vector3 targetRotationSpeed;
    private float oscillationFactor;

    void Start()
    {
        targetRotationSpeed = new Vector3(speedX, speedY, speedZ);
        currentRotationSpeed = targetRotationSpeed;
    }

    void Update()
    {
        if (oscillate)
        {
            oscillationFactor = (Mathf.Sin(Time.time * oscillationSpeed * Mathf.PI * 2) + 1) / 2;
            currentRotationSpeed = targetRotationSpeed * oscillationFactor;
        }
        else
        {
            currentRotationSpeed = targetRotationSpeed;
        }

        float xRotation = lockX ? 0 : currentRotationSpeed.x * Time.deltaTime;
        float yRotation = lockY ? 0 : currentRotationSpeed.y * Time.deltaTime;
        float zRotation = lockZ ? 0 : currentRotationSpeed.z * Time.deltaTime;

        transform.Rotate(xRotation, yRotation, zRotation);
    }
}

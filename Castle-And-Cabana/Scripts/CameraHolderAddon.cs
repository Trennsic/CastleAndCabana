using Unity.Cinemachine;
using UnityEngine;

public class CameraHolderAddon : MonoBehaviour
{
    [SerializeField] private GameObject Model;
    [SerializeField] private GameObject Vcam;
    [SerializeField] private CinemachineCamera cinemachineCam;
    [SerializeField] private int cameraFloorNumber;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        InitializeCameraHolder();
    }
    public void InitializeCameraHolder()
    {
        if (Vcam == null) { Debug.LogError("Vcam isn't filled check camera inspector"); return; }
        if (Model == null) { Debug.LogWarning("Model isn't filled check camera inspector"); }

        cinemachineCam = Vcam.GetComponent<CinemachineCamera>();
        if (cinemachineCam == null) { cinemachineCam = Vcam.GetComponentInChildren<CinemachineCamera>(); }
        if (cinemachineCam == null) 
        {
            Debug.LogError("Unable to find cinemachine camera on Vcam");
        }
    }

    public GameObject GetModelObject() => Model;
    public GameObject GetVirtualCameraObject() => Vcam;
    public CinemachineCamera GetCamera() => cinemachineCam;
    public int GetCameraFloorNumber() => cameraFloorNumber;
    public void SetCameraFloorNumber(int newCameraFloorNumber) => cameraFloorNumber = newCameraFloorNumber;

}

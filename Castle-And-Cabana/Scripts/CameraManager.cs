using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public enum CameraStates
    {
        FullTower,
        ZoomedIn,
        ZoomedOut,
        MonsterZoom,
        HeroZoom,
        CharactersZoom,
    }

    [SerializeField] private TowerDefenseManager gameManager; // New reference to the GameManager

    [Header("Main Camera Cinemachine Brain")]
    public CinemachineBrain cinemachineBrain;

    [Header("Camera State and Transition")]
    public CameraStates cameraState;
    public int cameraPosition; // Determines the current camera position as an integer
    public int floorPosition; // Determines the current camera position as an integer

    [Header("Camera Holders")]
    public GameObject fullTowerCameraHolder;
    public List<GameObject> closeCameraHolders = new List<GameObject>();
    public List<GameObject> farCameraHolders = new List<GameObject>();
    public List<GameObject> monsterCameraHolders = new List<GameObject>();
    public List<GameObject> heroCameraHolders = new List<GameObject>();
    public List<GameObject> allCharactersCameraHolders = new List<GameObject>();
    [SerializeField] private float cameraSpeed = 1f;
    [SerializeField] private float baseCameraSpeed = 5f;

    private void Start()
    {
        
    }

    private void Update()
    {
        HandleInput();
    }

    public void InitializeCameraManager()
    {
        if (cinemachineBrain == null)
        {
            cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
            if (cinemachineBrain == null)
            {
                Debug.LogError("No CinemachineBrain found on the Main Camera!");
            }
        }
        UpdateCameraSpeed();
    }

    private void HandleInput()
    {
        /*
        if (GetGameManager().GetInputManager().GetMoveLeftPressed())
        {
            // Decrement state and wrap around, including MonsterZoom
            cameraState = (CameraStates)(((int)cameraState - 1 + 4) % 4);
            UpdateActiveCameras();
        }
        else if (GetGameManager().GetInputManager().GetMoveRightPressed())
        {
            // Increment state and wrap around, including MonsterZoom
            cameraState = (CameraStates)(((int)cameraState + 1) % 4);
            UpdateActiveCameras();
        }

        if (GetGameManager().GetInputManager().GetMoveUpPressed())
        {
            SetCameraStateAndPosition(GetCurrentState(), (cameraPosition + 1) % GetMaxCameraPosition());
            UpdateActiveCameras();
        }
        else if (GetGameManager().GetInputManager().GetMoveDownPressed())
        {
            SetCameraStateAndPosition(GetCurrentState(), (cameraPosition - 1 + GetMaxCameraPosition()) % GetMaxCameraPosition());
            UpdateActiveCameras();
        }*/
    }


    private int GetMaxCameraPosition()
    {
        List<GameObject> activeList = cameraState == CameraStates.ZoomedIn ? closeCameraHolders : farCameraHolders;
        return activeList.Count;
    }

    public void SetCameraState(CameraStates state)
    {
        cameraState = state;
        UpdateActiveCameras();
    }

    public void SetFullTowerCameraHolder(GameObject holder)
    {
        fullTowerCameraHolder = holder;
    }

    public void AddCloseCameraHolder(GameObject cameraHolder)
    {
        if (cameraHolder != null && !closeCameraHolders.Contains(cameraHolder))
        {
            closeCameraHolders.Add(cameraHolder);
        }
        else
        {
            Debug.LogWarning("Invalid or duplicate CameraHolder for closeCameraHolders!");
        }
    }

    public void AddFarCameraHolder(GameObject cameraHolder)
    {
        if (cameraHolder != null && !farCameraHolders.Contains(cameraHolder))
        {
            farCameraHolders.Add(cameraHolder);
        }
        else
        {
            Debug.LogWarning("Invalid or duplicate CameraHolder for farCameraHolders!");
        }
    }
    public void AddMonsterCameraHolder(GameObject cameraHolder)
    {
        if (cameraHolder != null && !monsterCameraHolders.Contains(cameraHolder))
        {
            monsterCameraHolders.Add(cameraHolder);
        }
        else
        {
            Debug.LogWarning("Invalid or duplicate CameraHolder for monsterCameraHolders!");
        }
    }
    public void AddHeroCameraHolder(GameObject cameraHolder)
    {
        if (cameraHolder != null && !heroCameraHolders.Contains(cameraHolder))
        {
            heroCameraHolders.Add(cameraHolder);
        }
        else
        {
            Debug.LogWarning("Invalid or duplicate CameraHolder for heroCameraHolders!");
        }
    }
    public void AddAllCharactersCameraHolder(GameObject cameraHolder)
    {
        if (cameraHolder != null && !allCharactersCameraHolders.Contains(cameraHolder))
        {
            allCharactersCameraHolders.Add(cameraHolder);
        }
        else
        {
            Debug.LogWarning("Invalid or duplicate CameraHolder for heroCameraHolders!");
        }
    }

    public void RemoveCloseCameraHolder(GameObject cameraHolder)
    {
        if (cameraHolder != null && closeCameraHolders.Contains(cameraHolder))
        {
            closeCameraHolders.Remove(cameraHolder);
            UpdateActiveCameras();  // Update the active cameras after removal
        }
        else
        {
            Debug.LogWarning("CameraHolder not found in closeCameraHolders or is null!");
        }
    }

    public void RemoveFarCameraHolder(GameObject cameraHolder)
    {
        if (cameraHolder != null && farCameraHolders.Contains(cameraHolder))
        {
            farCameraHolders.Remove(cameraHolder);
            UpdateActiveCameras();  // Update the active cameras after removal
        }
        else
        {
            Debug.LogWarning("CameraHolder not found in farCameraHolders or is null!");
        }
    }
    public void RemoveMonsterCameraHolder(GameObject cameraHolder)
    {
        if (cameraHolder != null && monsterCameraHolders.Contains(cameraHolder))
        {
            monsterCameraHolders.Remove(cameraHolder);
            UpdateActiveCameras();  // Update the active cameras after removal
        }
        else
        {
            Debug.LogWarning("CameraHolder not found in monsterCameraHolders or is null!");
        }
    }
    public void RemoveHeroCameraHolder(GameObject cameraHolder)
    {
        if (cameraHolder != null && heroCameraHolders.Contains(cameraHolder))
        {
            heroCameraHolders.Remove(cameraHolder);
            UpdateActiveCameras();  // Update the active cameras after removal
        }
        else
        {
            Debug.LogWarning("CameraHolder not found in heroCameraHolders or is null!");
        }
    }
    public void RemoveAllCharactersCameraHolder(GameObject cameraHolder)
    {
        if (cameraHolder != null && allCharactersCameraHolders.Contains(cameraHolder))
        {
            allCharactersCameraHolders.Remove(cameraHolder);
            UpdateActiveCameras();  // Update the active cameras after removal
        }
        else
        {
            Debug.LogWarning("CameraHolder not found in allCharactersCameraHolders or is null!");
        }
    }


    private void UpdateActiveCameras()
    {
        DeactivateAllCameras();

        // Ensure the camera holder lists are ordered by floor number
        closeCameraHolders.Sort((a, b) => CompareCameraHoldersByFloorNumber(a, b));
        farCameraHolders.Sort((a, b) => CompareCameraHoldersByFloorNumber(a, b));
        monsterCameraHolders.Sort((a, b) => CompareCameraHoldersByFloorNumber(a, b));

        if (cameraState == CameraStates.FullTower && fullTowerCameraHolder != null)
        {
            ActivateCameraHolder(fullTowerCameraHolder);
        }
        else if (cameraState == CameraStates.ZoomedIn)
        {
            ActivateCameraFromList(closeCameraHolders);
        }
        else if (cameraState == CameraStates.ZoomedOut)
        {
            ActivateCameraFromList(farCameraHolders);
        }
        else if (cameraState == CameraStates.MonsterZoom) // New condition for MonsterZoom
        {
            ActivateCameraFromList(monsterCameraHolders);
        }
        else if (cameraState == CameraStates.HeroZoom) // New condition for HeroZoom
        {
            ActivateCameraFromList(heroCameraHolders);
        }
        else if (cameraState == CameraStates.CharactersZoom) // New condition for CharactersZoom
        {
            ActivateCameraFromList(allCharactersCameraHolders);
        }
    }

    private void ActivateCameraFromList(List<GameObject> cameraHolders)
    {
        if (cameraHolders.Count > 0)
        {
            int index = Mathf.Clamp(cameraPosition, 0, cameraHolders.Count - 1);
            ActivateCameraHolder(cameraHolders[index]);
        }
    }


    private int CompareCameraHoldersByFloorNumber(GameObject a, GameObject b)
    {
        CameraHolderAddon addonA = a.GetComponent<CameraHolderAddon>();
        CameraHolderAddon addonB = b.GetComponent<CameraHolderAddon>();

        if (addonA != null && addonB != null)
        {
            return addonA.GetCameraFloorNumber().CompareTo(addonB.GetCameraFloorNumber());
        }
        return 0;
    }

    private void ActivateCameraHolder(GameObject holder, int priority = 10)
    {
        CameraHolderAddon addon = holder.GetComponent<CameraHolderAddon>();
        if (addon != null)
        {
            CinemachineCamera vcam = addon.GetCamera();
            if (vcam != null)
            {
                vcam.Priority = priority;
            }

            GameObject model = addon.GetModelObject();
            if (model != null)
            {
                model.SetActive(true);
            }
        }
        else
        {
            Debug.LogError($"CameraHolderAddon missing on {holder.name}");
        }
    }

    public void DeactivateAllCameras()
    {
        if (fullTowerCameraHolder != null)
        {
            DeactivateCameraHolder(fullTowerCameraHolder);
        }

        foreach (var holder in closeCameraHolders)
        {
            DeactivateCameraHolder(holder);
        }

        foreach (var holder in farCameraHolders)
        {
            DeactivateCameraHolder(holder);
        }
        foreach (var holder in monsterCameraHolders)
        {
            DeactivateCameraHolder(holder);
        }
    }

    private void DeactivateCameraHolder(GameObject holder)
    {
        CameraHolderAddon addon = holder.GetComponent<CameraHolderAddon>();
        if (addon != null)
        {
            CinemachineCamera vcam = addon.GetCamera();
            if (vcam != null)
            {
                vcam.Priority = 0;
            }

            GameObject model = addon.GetModelObject();
            if (model != null)
            {
                model.SetActive(false);
            }
        }
        else
        {
            Debug.LogError($"CameraHolderAddon missing on {holder.name}");
        }
    }
    public void SetCameraStateAndPosition(CameraStates state, int newFloorPosition = 0)
    {
        // Set the camera state
        SetCurrentState(state);

        SetFloorPosition(newFloorPosition);

        // Clamp the camera position based on the current camera state
        cameraPosition = Mathf.Clamp(floorPosition, 0, GetMaxCameraPosition() - 1);

        // Update the active cameras after state and position are set
        UpdateActiveCameras();
    }

    public void UpdateCameraSpeed()
    {

        // Get game Speed from game manager
        float gameSpeed = (int)GetGameManager().GetGameSpeed();
        // Adjust camera movement speed
        float cameraSpeed = GetBaseCameraSpeed() * ((float)gameSpeed / 2f);
        SetCameraSpeed(cameraSpeed);

        // Adjust blend speed (lower speed = faster blend)
        float blendSpeed = Mathf.Clamp(1f / gameSpeed, 0.1f, 2f);
        SetCameraBlendSpeed(blendSpeed);
    }
    public void SetCameraBlendSpeed(float blendTime)
    {
        if (cinemachineBrain != null)
        {
            cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blendTime);
        }
    }
    public TowerDefenseManager GetGameManager() { return gameManager; }
    public void SetCameraSpeed(float newSpeed) => cameraSpeed = newSpeed;
    public float GetCameraSpeed() => cameraSpeed;
    public void SetBaseCameraSpeed(float newBaseCameraSpeed) => baseCameraSpeed = newBaseCameraSpeed;
    public float GetBaseCameraSpeed() => baseCameraSpeed;
    private void SetCurrentState(CameraStates newCameraState) { cameraState = newCameraState; UpdateActiveCameras(); }
    public CameraStates GetCurrentState() => cameraState;
    public void SetFloorPosition(int newFloorPosition) => floorPosition = newFloorPosition;
    public int GetFloorPosition() => floorPosition;
}

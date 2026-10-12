using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnitInfo;

public class MonsterUnitPrefabAddon : MonoBehaviour
{
    [SerializeField] private bool isInitalized;
    [SerializeField] private MonsterUnit myMonsterUnit;
    private Transform mainCameraTransform;
    [Header("Animator")]
    [SerializeField] private GameObject model;
    [SerializeField] private Animator animator;
    [Header("Sliders")]
    [SerializeField] private GameObject UiHolder;
    [SerializeField] private Slider healthBarSlider; // Slider representing the health bar
    [SerializeField] private Slider actionBarSlider; // Slider representing the attack-ready timer
    [SerializeField] private float healthSliderMaxValue;
    [SerializeField] private float healthSliderValue;
    [SerializeField] private float actionSliderMaxValue;
    [SerializeField] private float actionSliderValue;
    public CircleSegmenter circleSegmenter; // Reference to CircleSegmenter
    public TextMeshProUGUI healthText; // Reference to TextMeshPro for health display
    [SerializeField] private TowerDefenseManager GameManager; // Link to Tower Manager
    [SerializeField] private GameObject facingReferenceObject;
    [SerializeField] private GameObject weaponAttachment;

    [Tooltip("Red transparent material to use during the flash")]
    [SerializeField] private Material RedFlashMaterial;
    [Tooltip("See through transparent material to use during the floor placement")]
    [SerializeField] private Material SeeThroughMaterial;
    private Material[][] originalMaterials;

    #region // Debugging
    [Header("Debugging")]
    [SerializeField] private DebugUtility debug;
    #endregion

    private void Awake()
    {
        SetIsInitalized(false);
    }

    private void Start()
    {
        // Initialize with debugging enabled
        debug = new DebugUtility(false);

        if (debug.IsDebugging)
        {
            Debug.Log($"[MonsterUnitAddon] Start method called on {gameObject.name}");
        }
    }

    private void Update()
    {
        if (myMonsterUnit == null)
        {
            if (debug.IsDebugging)
            {
                Debug.LogError("[MonsterUnitAddon] MonsterUnit information is not assigned. Update skipped.");
            }
            return;
        }
        if (GetIsInitalized())
        {
            // Update sliders based on the monster's current stats
            UpdateSliders();

            // Make the sliders billboard toward the camera on the Y-axis
            BillboardSliders();
        }

    }
    public void InitializePrefabAddon()
    {
        SetOriginalMaterials();
        if (UiHolder != null) { UiHolder.SetActive(false); }
    }

    public void InitializeSliders(MonsterUnit monsterUnit)
    {
        myMonsterUnit = monsterUnit;
        SetGameManager(monsterUnit.GetGameManager());
        SetFacingReferenceObject(GetTowerManager().GetFacingReference());
        if (UiHolder != null) { UiHolder.SetActive(true); }
        if (myMonsterUnit == null)
        {
            Debug.LogError("[MonsterUnitAddon] MonsterUnit information is not assigned. Sliders initialization failed.");
            return;
        }

        if (healthBarSlider != null)
        {
            healthSliderMaxValue = myMonsterUnit.GetUnitInfo().GetMaxHealth();
            healthSliderValue = myMonsterUnit.GetUnitInfo().GetHealth();

            if (debug.IsDebugging)
            {
                Debug.Log($"[MonsterUnitAddon] Initialized Health Bar: Max {healthSliderMaxValue}, Current {healthSliderValue}");
            }
        }
        else
        {
            Debug.LogError("[MonsterUnitAddon] Health bar slider is not assigned.");
        }

        if (actionBarSlider != null)
        {
            actionSliderMaxValue = myMonsterUnit.GetUnitInfo().GetActionBarMax();
            actionSliderValue = myMonsterUnit.GetUnitInfo().GetActionBar();

            if (debug.IsDebugging)
            {
                Debug.Log($"[MonsterUnitAddon] Initialized Action Bar: Max {actionSliderMaxValue}, Current {actionSliderValue}");
            }
        }
        else
        {
            Debug.LogError("[MonsterUnitAddon] Action bar slider is not assigned.");
        }
        // Link the main camera to mainCameraTransform
        Camera mainCamera = Camera.main; // Get the Main Camera
        if (mainCamera != null)
        {
            mainCameraTransform = mainCamera.transform; // Assign its Transform
            if (debug.IsDebugging)
            {
                Debug.Log($"[MonsterUnitAddon] Main camera linked: {mainCamera.name}");
            }
        }
        else
        {
            Debug.LogError("[MonsterUnitAddon] Main Camera not found! Please ensure a Main Camera is tagged.");
        }

        SetIsInitalized(true);
    }

    public void UpdateSliders()
    {
        if (healthBarSlider != null)
        {
            healthSliderMaxValue = myMonsterUnit.GetUnitInfo().GetMaxHealth();
            healthSliderValue = myMonsterUnit.GetUnitInfo().GetHealth();
            healthBarSlider.value = healthSliderValue;
            healthBarSlider.maxValue = healthSliderMaxValue;
        }

        if (actionBarSlider != null)
        {
            actionSliderValue = myMonsterUnit.GetUnitInfo().GetActionBar();
            actionSliderMaxValue = myMonsterUnit.GetUnitInfo().GetActionBarMax();
            actionBarSlider.value = actionSliderValue;
            actionBarSlider.maxValue = actionSliderMaxValue;
        }
        // Update TextMeshPro text
        if (healthText != null)
        {
            healthText.text = $"{healthSliderValue}";
        }
        else
        {
            Debug.LogWarning("[MonsterUnitAddon] Health TextMeshPro is not assigned. Update skipped.");
        }

        if (circleSegmenter != null)
        {
            // Pass the number of segments based on some logic
            int segments = Mathf.Max(1, (int)healthSliderMaxValue); // Example: use health value as segment count
            circleSegmenter.UpdateSegments(segments);
        }
        else
        {
            Debug.LogWarning("[MonsterUnitAddon] CircleSegmenter is not assigned. Update skipped.");
        }
    }
    private void BillboardSliders()
    {
        // Ensure the facingReferenceObject is assigned
        if (facingReferenceObject == null)
        {
            Debug.LogWarning("[MonsterUnitAddon] Facing Reference Object is not assigned.");
            return;
        }

        // Get the Y rotation angle in degrees (between -180 and 180)
        float rotationAngle = facingReferenceObject.transform.eulerAngles.y;

        // Normalize the angle to be between -360 and 360 (adjusting based on Unity's angle range of 0 to 360)
        if (rotationAngle > 180)
        {
            rotationAngle -= 360; // Normalize to negative range
        }

        //Debug.Log($"[MonsterUnitAddon] Facing Reference Object Angle: {rotationAngle}");

        // Apply the rotation to the health bar slider (rotate along the Y-axis)
        UiHolder.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
    }
    private void SetOriginalMaterials()
    {
        var renderers = gameObject.GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].materials;
        }

        Debug.Log("Original materials set for RoomPart.");
    }
    public void StartTemporarySeeThrough()
    {
        if (originalMaterials == null || originalMaterials.Length == 0)
        {
            Debug.LogError("Original materials have not been initialized.");
            return;
        }

        var renderers = gameObject.GetComponentsInChildren<Renderer>();

        if (renderers.Length != originalMaterials.Length)
        {
            Debug.LogError("Mismatch between saved original materials and renderers in RoomPart.");
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            var seeThroughMaterials = new Material[originalMaterials[i].Length];

            for (int j = 0; j < seeThroughMaterials.Length; j++)
            {
                // Assign the see throughmateria
                seeThroughMaterials[j] = SeeThroughMaterial;
            }

            renderers[i].materials = seeThroughMaterials;
        }

        Debug.Log("Temporary see-through effect started.");
    }





    public Slider GetHealthBarSlider() => healthBarSlider;
    public Slider GetActionBarSlider() => actionBarSlider;
    public GameObject GetWeaponAttachment() => weaponAttachment;
    private bool GetIsInitalized() => isInitalized;
    private void SetIsInitalized(bool newInitialize) => isInitalized = newInitialize;
    public Animator GetAnimator() { return animator; }
    public void SetFacingReferenceObject(GameObject newObject) { facingReferenceObject = newObject; }
    public void SetGameManager(TowerDefenseManager newManager) => GameManager = newManager;
    public TowerDefenseManager GetGameManager() => GameManager;
    public TowerManager GetTowerManager() { return GetGameManager().GetTowerManager(); }
    public MonsterManager GetMonsterManager() { return GetGameManager().GetMonsterManager(); }
    public HeroManager GetHeroManager() { return GetGameManager().GetHeroManager(); }
}
//[Header("Monster Unit Settings")]
//[SerializeField] private MonsterUnit myMonsterUnit;
//[SerializeField] private UnitInfo myUnitInfo;
//
//[Header("Sliders")]
//[SerializeField] private Slider healthBarSlider; // Slider representing the health bar
//[SerializeField] private Slider actionBarSlider; // Slider representing the attack-ready timer
//[SerializeField] private float healthSliderMaxValue;
//[SerializeField] private float healthSliderValue;
//[SerializeField] private float actionSliderMaxValue;
//[SerializeField] private float actionSliderValue;
//
//[Header("Unit Info")]
//[SerializeField] private UnitTypes unitType;
//[SerializeField] private MonsterUnit monsterUnit;
//[SerializeField] private MonsterUnit monsterUnit;
//[SerializeField] private int level;
//[SerializeField] private float health;
//[SerializeField] private float maxHealth;
//[SerializeField] private string name;
////Action Bar
//[SerializeField] private int actionBar;
//[SerializeField] private int actionBarMax;
//[SerializeField] private int actionBarSpeed;
//[SerializeField] private int actionBarRollover;
//// Main stats
//[SerializeField] private float offense;
//[SerializeField] private float defense;
//[SerializeField] private float utility;
//// Sub stats
//[SerializeField] private float physicalDamage;   // Based on Offense
//[SerializeField] private float magicDamage;   // Based on Offense
//[SerializeField] private float armor;   // Based on Defense
//[SerializeField] private float resistance;   // Based on Defense
//[SerializeField] private float actionSpeed;   // Based on Utility
//[SerializeField] private float escapeSpeed;   // Based on Utility
//
//[Header("Monster  Info")]
//[SerializeField] private float xxx3;
//
//[Header("Team Info")]
//[SerializeField] private float xx2x;
//
//[Header("Animator")]
//[SerializeField] private Animator animator;
//
//[Header("Debugging")]
//[SerializeField] private bool isDebugging;
//
//private Transform mainCameraTransform;
//
//// Public Getters
//public Animator Animator => animator;
//

//}



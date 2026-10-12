using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
using MoreMountains.Tools;

/// <summary>
/// Pure view/UI layer for a hero. Lives on the HeroUnitBase prefab alongside
/// <see cref="HeroUnit"/> and manages the world-space health/action sliders,
/// health text, circle segmenter, and billboard rotation. The Animator is owned
/// by the instantiated model and handled in <see cref="HeroUnit"/>, not here.
/// </summary>
public class HeroUnitUiAddon : MonoBehaviour
{
    [SerializeField] private bool isInitalized;
    [SerializeField] private HeroUnit myHeroUnit;
    private Transform mainCameraTransform;

    [Header("Sliders")]
    [SerializeField] private GameObject UiHolder;
    [SerializeField] private Slider healthBarSlider; // Slider representing the health bar
    [SerializeField] private Slider actionBarSlider; // Slider representing the attack-ready timer
    public CircleSegmenter circleSegmenter; // Reference to CircleSegmenter
    public TextMeshProUGUI healthText; // Reference to TextMeshPro for health display

    [Header("Read-only Values")]
    [SerializeField][MMReadOnly] private float healthSliderMaxValue;
    [SerializeField][MMReadOnly] private float healthSliderValue;
    [SerializeField][MMReadOnly] private float actionSliderMaxValue;
    [SerializeField][MMReadOnly] private float actionSliderValue;
    [SerializeField][MMReadOnly] private TowerDefenseManager GameManager; // Link to Tower Manager, Auto links
    [SerializeField][MMReadOnly] private GameObject facingReferenceObject; // , Auto links

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
        debug = new DebugUtility(false);

        if (debug.IsDebugging)
        {
            Debug.Log($"[HeroUnitUiAddon] Start method called on {gameObject.name}");
        }
    }

    private void Update()
    {
        if (myHeroUnit == null)
        {
            if (debug.IsDebugging)
            {
                Debug.LogError("[HeroUnitUiAddon] HeroUnit information is not assigned. Update skipped.");
            }
            return;
        }
        if (GetIsInitalized())
        {
            UpdateSliders();
            BillboardSliders();
        }
    }

    public void InitializeSliders(HeroUnit heroUnit)
    {
        myHeroUnit = heroUnit;
        SetGameManager(heroUnit.GetGameManager());
        SetFacingReferenceObject(GetTowerManager().GetFacingReference());
        if (myHeroUnit == null)
        {
            Debug.LogError("[HeroUnitUiAddon] HeroUnit information is not assigned. Sliders initialization failed.");
            return;
        }

        if (healthBarSlider != null)
        {
            healthSliderMaxValue = myHeroUnit.GetUnitInfo().GetMaxHealth();
            healthSliderValue = myHeroUnit.GetUnitInfo().GetHealth();

            if (debug.IsDebugging)
            {
                Debug.Log($"[HeroUnitUiAddon] Initialized Health Bar: Max {healthSliderMaxValue}, Current {healthSliderValue}");
            }
        }
        else
        {
            Debug.LogError("[HeroUnitUiAddon] Health bar slider is not assigned.");
        }

        if (actionBarSlider != null)
        {
            actionSliderMaxValue = myHeroUnit.GetUnitInfo().GetActionBarMax();
            actionSliderValue = myHeroUnit.GetUnitInfo().GetActionBar();

            if (debug.IsDebugging)
            {
                Debug.Log($"[HeroUnitUiAddon] Initialized Action Bar: Max {actionSliderMaxValue}, Current {actionSliderValue}");
            }
        }
        else
        {
            Debug.LogError("[HeroUnitUiAddon] Action bar slider is not assigned.");
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCameraTransform = mainCamera.transform;
            if (debug.IsDebugging)
            {
                Debug.Log($"[HeroUnitUiAddon] Main camera linked: {mainCamera.name}");
            }
        }
        else
        {
            Debug.LogError("[HeroUnitUiAddon] Main Camera not found! Please ensure a Main Camera is tagged.");
        }

        SetIsInitalized(true);
    }

    public void UpdateSliders()
    {
        if (healthBarSlider != null)
        {
            healthSliderMaxValue = myHeroUnit.GetUnitInfo().GetMaxHealth();
            healthSliderValue = myHeroUnit.GetUnitInfo().GetHealth();
            healthBarSlider.value = healthSliderValue;
            healthBarSlider.maxValue = healthSliderMaxValue;
        }

        if (actionBarSlider != null)
        {
            actionSliderValue = myHeroUnit.GetUnitInfo().GetActionBar();
            actionSliderMaxValue = myHeroUnit.GetUnitInfo().GetActionBarMax();
            actionBarSlider.value = actionSliderValue;
            actionBarSlider.maxValue = actionSliderMaxValue;
        }

        if (healthText != null)
        {
            healthText.text = $"{healthSliderValue}";
        }
        else
        {
            Debug.LogWarning("[HeroUnitUiAddon] Health TextMeshPro is not assigned. Update skipped.");
        }

        if (circleSegmenter != null)
        {
            int segments = Mathf.Max(1, (int)healthSliderMaxValue);
            circleSegmenter.UpdateSegments(segments);
        }
        else
        {
            Debug.LogWarning("[HeroUnitUiAddon] CircleSegmenter is not assigned. Update skipped.");
        }
    }

    private void BillboardSliders()
    {
        if (facingReferenceObject == null)
        {
            Debug.LogWarning("[HeroUnitUiAddon] Facing Reference Object is not assigned.");
            return;
        }

        float rotationAngle = facingReferenceObject.transform.eulerAngles.y;
        if (rotationAngle > 180)
        {
            rotationAngle -= 360;
        }

        UiHolder.transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
    }

    public Slider GetHealthBarSlider() => healthBarSlider;
    public Slider GetActionBarSlider() => actionBarSlider;
    private bool GetIsInitalized() => isInitalized;
    private void SetIsInitalized(bool newInitialize) => isInitalized = newInitialize;
    public void SetFacingReferenceObject(GameObject newObject) { facingReferenceObject = newObject; }
    public void SetGameManager(TowerDefenseManager newManager) => GameManager = newManager;
    public TowerDefenseManager GetGameManager() => GameManager;
    public TowerManager GetTowerManager() { return GetGameManager().GetTowerManager(); }
    public HeroManager GetHeroManager() { return GetGameManager().GetHeroManager(); }
    public MonsterManager GetMonsterManager() { return GetGameManager().GetMonsterManager(); }
}
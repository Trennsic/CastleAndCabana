using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MonsterUnit;
using MoreMountains.Tools;

/// <summary>
/// Represents a single hero unit in the tower defense combat loop.
/// Manages initialization, stat assignment, pathfinding movement, combat actions,
/// damage handling, death sequencing, and animator state - delegating prefab-side
/// visuals to <see cref="HeroUnitUiAddon"/>.
/// </summary>
public class HeroUnit : MonoBehaviour
{
    #region // Variables

    #region // Enums

    /// <summary>
    /// Available hero archetypes, each with a distinct stat profile.
    /// </summary>
    public enum HeroClass
    {
        Trainee,
        Guard,
        Archer,
        Mage,
        Cleric,
        DarkKnight,
        Warrior,
        Paladin,
    }

    /// <summary>
    /// Animation states that can be applied to the hero's Animator.
    /// Maps to integer values used by the "animation" Animator parameter.
    /// </summary>
    public enum HeroAnimations
    {
        Idle,
        Run,
        Action1,
        Action2,
        TakeHit,
        Die,
        Tired,
        Victory,
        Extra1,
        Extra2,
    }

    #endregion

    #region // Model

    [Header("Model")]
    [Tooltip("The instantiated model GameObject parented to this unit.")]
    [SerializeField] private GameObject modelPrefab;

    [Tooltip("Animator component sourced from the instantiated model prefab.")]
    [SerializeField] private Animator animator;

    [Tooltip("Current animation state, mirrored from the Animator parameter.")]
    [SerializeField] private HeroAnimations animationState;

    #endregion

    #region // References

    [Header("References")]

    [Tooltip("Prefab-side addon that manages sliders, billboarding, and the Animator reference.")]
    [SerializeField] private HeroUnitUiAddon heroAddon;

    [Tooltip("Addon used to spawn floating damage/energy popup text.")]
    [SerializeField] private PopupTextAddon popupTextAddon;

    [Tooltip("VFX prefab instantiated at the moment of death.")]
    [SerializeField] private GameObject DeathVfx;

    #endregion

    #region // Unit Info

    [Header("Unit Info")]
    [Tooltip("Whether this hero has been fully initialized.")]
    [SerializeField][MMReadOnly] private bool isInitialize = false;

    [Tooltip("Shared stat container for health, action bar, power, and speed.")]
    [SerializeField][MMReadOnly] private UnitInfo unitInfo;

    [Tooltip("The archetype of this hero, which determines its base stats.")]
    [SerializeField] private HeroClass heroClass;

    [Tooltip("Unique ID assigned to this hero unit.")]
    [SerializeField][MMReadOnly] private int huID;

    [Tooltip("Global speed multiplier applied to both movement and animation.")]
    [SerializeField][MMReadOnly] private float overallSpeed = 1;

    [Tooltip("World-space units per second the hero moves along a path.")]
    [SerializeField][MMReadOnly] private float moveSpeed = 1;

    [Tooltip("Value written to the 'animationSpeed' Animator float parameter.")]
    [SerializeField][MMReadOnly] private float animationSpeed = 1;

    [Tooltip("Angular speed used when Lerping rotation toward a target.")]
    [SerializeField][MMReadOnly] private int rotationSpeed = 1;

    [Tooltip("Whether this hero is currently active in the scene.")]
    [SerializeField][MMReadOnly] private bool isActive = false;

    [Tooltip("True while the hero is playing out its death sequence.")]
    [SerializeField][MMReadOnly] private bool isDying = false;

    [Tooltip("Countdown timer driving the death sequence duration.")]
    [SerializeField][MMReadOnly] private float dyingTimer;

    [Tooltip("True while the hero is en route to its victory position.")]
    [SerializeField][MMReadOnly] private bool movingToVictoryPosition;

    [Tooltip("True once the hero has arrived at its victory position.")]
    [SerializeField][MMReadOnly] private bool atVictoryPosition;

    #endregion

    #region // Abilities

    [Header("Abilities")]
    [Tooltip("The ability this hero fires when the action bar fills.")]
    [SerializeField] private AbilityId actionAbilityId;

    [Tooltip("Passive, reaction, and conditional abilities this hero carries into combat.")]
    [SerializeField] private List<AbilityId> passiveAbilityIds = new List<AbilityId>();

    #endregion

    #region // References - Readonly

    [Header("References - Readonly")]
    [Tooltip("The team this hero belongs to.")]
    [SerializeField][MMReadOnly] private HeroTeam heroTeam;

    [Tooltip("Index of this hero within its team.")]
    [SerializeField][MMReadOnly] private int numberInTeam;

    [Tooltip("Reference to the central TowerDefenseManager.")]
    [SerializeField][MMReadOnly] private TowerDefenseManager GameManager; // Auto Set

    #endregion

    #region // Debugging

    [Header("Debugging")]
    [SerializeField] private DebugUtility debug;

    [Tooltip("The path currently being traversed, for Inspector visualization.")]
    [SerializeField][MMReadOnly] private List<GameObject> currentPath = new List<GameObject>();

    [Tooltip("The waypoint the hero is currently moving toward, for Inspector visualization.")]
    [SerializeField][MMReadOnly] private GameObject nextTargetObject;

    #endregion

    #endregion

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates the <see cref="DebugUtility"/> instance for this unit.
    /// </summary>
    private void Start()
    {
        debug = new DebugUtility(false);
    }

    /// <summary>
    /// Each frame, checks whether the hero's health has reached zero and advances
    /// the death countdown timer if a death sequence is in progress.
    /// </summary>
    private void Update()
    {
        CheckForDeath();
        UpdateDeathTimer();
        UpdateDebugWatches();
    }

    // -------------------------------------------------------------------------
    // Initialization
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fully initializes the hero unit: assigns its ID, manager, team, stats,
    /// class, and spawns the visual model. Must be called before any other
    /// gameplay method is invoked.
    /// </summary>
    /// <param name="newManager">The central <see cref="TowerDefenseManager"/>.</param>
    /// <param name="newHeroTeam">The <see cref="HeroTeam"/> this hero belongs to.</param>
    /// <param name="huId">Unique hero unit ID.</param>
    /// <param name="newClass">The <see cref="HeroClass"/> archetype to assign.</param>
    /// <param name="level">Starting level, passed to <see cref="UnitInfo"/>.</param>
    public void InitializeHero(TowerDefenseManager newManager, HeroTeam newHeroTeam, int huId, HeroClass newClass, int level)
    {
        debug = new DebugUtility(false);

        SetHuID(huId);

        SetGameManager(newManager);
        if (debug.IsDebugging) Debug.Log($"TowerDefenseManager set to: {newManager}");

        SetHeroTeam(newHeroTeam);
        if (debug.IsDebugging) Debug.Log($"HeroTeam set to: {newHeroTeam}");

        SetUnitInfo(new UnitInfo(this, level));
        if (debug.IsDebugging) Debug.Log($"UnitInfo created with level {level} and linked to hero.");

        SetHeroClass(newClass);
        if (debug.IsDebugging) Debug.Log($"Hero class set to: {newClass}");

        InitializeHeroStats();
        SetMovingToVictoryPosition(false);
        SetAtVictoryPosition(false);
        UpdateHeroSpeeds();
        if (debug.IsDebugging) Debug.Log("Hero stats initialized.");

        SpawnHero();
        if (debug.IsDebugging) Debug.Log("Hero spawning process triggered.");

        SetIsInitialize(true);
        if (debug.IsDebugging) Debug.Log("Hero initialization complete.");
    }

    /// <summary>
    /// Wires up the components already present on this baked hero prefab: the UI addon
    /// and popup addon on the root, and the Animator/model from the baked Model child.
    /// No instantiation - the per-class model is part of the prefab.
    /// </summary>
    public void SpawnHero()
    {
        // Components that live on this hero root.
        if (GetHeroAddon() == null) SetHeroAddon(GetComponent<HeroUnitUiAddon>());
        if (GetHeroAddon() == null) SetHeroAddon(GetComponentInChildren<HeroUnitUiAddon>());

        if (GetPopupTextAddon() == null) SetPopupTextAddon(GetComponent<PopupTextAddon>());
        if (GetPopupTextAddon() == null) SetPopupTextAddon(GetComponentInChildren<PopupTextAddon>());

        // Model is baked into the prefab as a child; grab its Animator + model object.
        if (GetAnimator() == null) SetAnimator(GetComponentInChildren<Animator>());
        if (GetModel() == null && GetAnimator() != null) SetModel(GetAnimator().gameObject);

        if (GetHeroAddon() != null) GetHeroAddon().InitializeSliders(this);
        SetAnimationState(HeroAnimations.Idle);

        if (GetHeroAddon() == null) Debug.LogError("Error! No HeroUnitUiAddon found on the hero base prefab.");
        if (GetPopupTextAddon() == null) Debug.LogError("Error! No PopupTextAddon found on the hero base prefab.");
        if (GetAnimator() == null) Debug.LogError("Error! No Animator found in the hero's model children.");

        if (debug.IsDebugging)
        {
            if (GetHeroAddon() != null) Debug.Log($"{gameObject.name} HeroUnitUiAddon reference set successfully.");
            if (GetPopupTextAddon() != null) Debug.Log($"{gameObject.name} PopupTextAddon reference set successfully.");
            if (GetAnimator() != null) Debug.Log($"{gameObject.name} Animator reference set successfully.");
        }
    }


    /// <summary>
    /// Assigns base stats (power, speed, max health) to the hero's
    /// <see cref="UnitInfo"/> based on its <see cref="HeroClass"/>, sets the active
    /// ability from the Inspector-authored abilities list, then resets the action bar.
    /// </summary>
    private void InitializeHeroStats()
    {
        switch (heroClass)
        {
            case HeroClass.Trainee:
                GetUnitInfo().SetPower(2);
                GetUnitInfo().SetSpeed(3);
                GetUnitInfo().SetMaxHealth(3);
                break;
            case HeroClass.Guard:
                GetUnitInfo().SetPower(1);
                GetUnitInfo().SetSpeed(1);
                GetUnitInfo().SetMaxHealth(8);
                break;
            case HeroClass.Archer:
                GetUnitInfo().SetPower(2);
                GetUnitInfo().SetSpeed(4);
                GetUnitInfo().SetMaxHealth(4);
                break;
            case HeroClass.Mage:
                GetUnitInfo().SetPower(5);
                GetUnitInfo().SetSpeed(5);
                GetUnitInfo().SetMaxHealth(5);
                break;
            case HeroClass.Cleric:
                GetUnitInfo().SetPower(2);
                GetUnitInfo().SetSpeed(5);
                GetUnitInfo().SetMaxHealth(5);
                break;
            case HeroClass.DarkKnight:
                GetUnitInfo().SetPower(7);
                GetUnitInfo().SetSpeed(5);
                GetUnitInfo().SetMaxHealth(5);
                break;
            case HeroClass.Warrior:
                GetUnitInfo().SetPower(5);
                GetUnitInfo().SetSpeed(5);
                GetUnitInfo().SetMaxHealth(5);
                break;
            case HeroClass.Paladin:
                GetUnitInfo().SetPower(5);
                GetUnitInfo().SetSpeed(5);
                GetUnitInfo().SetMaxHealth(5);
                break;
        }

        GetUnitInfo().SetAction(actionAbilityId);
        GetUnitInfo().SetPassives(passiveAbilityIds);

        GetUnitInfo().SetHealth(GetUnitInfo().GetMaxHealth());
        ResetActionBar();

        if (debug.IsDebugging)
            Debug.Log($"{unitInfo.GetName()} Hero initialization complete. " +
                $"Stats: " +
                $"\nOffense       - {unitInfo.GetPower()}, " +
                $"\nMax Health    - {unitInfo.GetMaxHealth()}, " +
                $"\nSpeed         - {unitInfo.GetSpeed()}.");
    }

    // -------------------------------------------------------------------------
    // Movement
    // -------------------------------------------------------------------------

    /// <summary>
    /// Moves the hero through an ordered list of waypoints, playing the Run
    /// animation during travel and returning to Idle on arrival.
    /// </summary>
    /// <param name="path">Ordered list of waypoint GameObjects to traverse.</param>
    public IEnumerator MoveHeroAlongPath(List<GameObject> path)
    {
        currentPath = path;
        if (debug.IsDebugging) Debug.Log($"Starting MoveHeroAlongPath for hero '{name}'.");

        SetAnimationState(HeroUnit.HeroAnimations.Run);
        UpdateAnimationSpeed();

        foreach (GameObject targetPoint in path)
        {
            if (targetPoint == null)
            {
                if (debug.IsDebugging) Debug.LogError("Target point is null. Skipping.");
                continue;
            }

            yield return StartCoroutine(MoveToTarget(targetPoint));
        }

        SetAnimationState(HeroUnit.HeroAnimations.Idle);
        UpdateAnimationSpeed();
        if (debug.IsDebugging) Debug.Log($"Hero '{name}' completed MoveHeroAlongPath.");
    }

    /// <summary>
    /// Moves the hero to an explicit world position and rotation (e.g. the end crystal),
    /// then returns to Idle. Clears <see cref="currentPath"/> on completion.
    /// </summary>
    /// <param name="targetPosition">World-space destination.</param>
    /// <param name="targetRotation">Desired rotation at the destination.</param>
    public IEnumerator MoveHeroToEndCrystal(Vector3 targetPosition, Quaternion targetRotation)
    {
        if (debug.IsDebugging)
            Debug.Log($"Starting MoveHeroToEndCrystal for hero '{name}' to position at {targetPosition} with {targetRotation} rotation.");

        yield return StartCoroutine(MoveToPosition(targetPosition, targetRotation));

        if (debug.IsDebugging)
            Debug.Log($"Hero '{name}' completed MoveHeroAlongPath.");

        SetAnimationState(HeroUnit.HeroAnimations.Idle);

        if (debug.IsDebugging)
            Debug.Log($"Hero '{name}' animation state set to Idle.");

        currentPath = null;
    }

    /// <summary>
    /// Moves the hero toward a single waypoint GameObject, smoothly rotating
    /// to match its orientation. Snaps to an exact position on arrival.
    /// </summary>
    /// <param name="targetObject">The waypoint to move toward.</param>
    public IEnumerator MoveToTarget(GameObject targetObject)
    {
        if (targetObject == null)
        {
            if (debug.IsDebugging) Debug.LogError($"Target object is null for hero '{name}'. Aborting MoveToTarget.");
            yield break;
        }

        nextTargetObject = targetObject;

        if (debug.IsDebugging) Debug.Log($"Hero '{name}' moving towards '{targetObject.name}'.");

        while (Vector3.Distance(transform.position, targetObject.transform.position) > 0.1f)
        {
            UpdateHeroSpeeds();

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetObject.transform.position,
                GetMoveSpeed() * Time.deltaTime
            );

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetObject.transform.rotation,
                rotationSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetObject.transform.position;
        transform.rotation = targetObject.transform.rotation;

        nextTargetObject = null;
    }

    /// <summary>
    /// Moves the hero to an explicit world position and rotation, playing Run
    /// during travel and Idle on arrival. Sets <see cref="atVictoryPosition"/>
    /// if <see cref="movingToVictoryPosition"/> is true.
    /// </summary>
    /// <param name="targetPosition">World-space destination.</param>
    /// <param name="targetRotation">Desired rotation at the destination.</param>
    public IEnumerator MoveToPosition(Vector3 targetPosition, Quaternion targetRotation)
    {
        GameObject hero = this.gameObject;

        if (debug.IsDebugging)
            Debug.Log($"Hero '{name}' starting MoveToPosition towards position {targetPosition}, rotation {targetRotation}.");

        SetAnimationState(HeroAnimations.Run);
        UpdateAnimationSpeed();

        while (Vector3.Distance(hero.transform.position, targetPosition) > 0.1f)
        {
            float remainingDistance = Vector3.Distance(hero.transform.position, targetPosition);

            if (debug.IsDebugging)
                Debug.Log($"Hero '{name}' is {remainingDistance} units away from target position {targetPosition}.");

            Vector3 overshootPosition = targetPosition + (targetPosition - hero.transform.position).normalized * 0.5f;
            Debug.DrawLine(hero.transform.position, overshootPosition, Color.green, 0f, false);
            Debug.DrawLine(hero.transform.position + Vector3.up * 0.2f, overshootPosition + Vector3.up * 0.2f, Color.green, 0f, false);
            Debug.DrawLine(hero.transform.position + Vector3.right * 0.2f, overshootPosition + Vector3.right * 0.2f, Color.green, 0f, false);

            UpdateHeroSpeeds();

            hero.transform.position = Vector3.MoveTowards(
                hero.transform.position,
                targetPosition,
                GetMoveSpeed() * Time.deltaTime
            );

            hero.transform.rotation = Quaternion.Lerp(
                hero.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            if (debug.IsDebugging)
                Debug.Log($"Hero '{name}' current position: {hero.transform.position}, rotation: {hero.transform.rotation}");

            yield return null;
        }

        hero.transform.position = targetPosition;
        hero.transform.rotation = targetRotation;

        if (debug.IsDebugging)
            Debug.Log($"Hero '{name}' snapped to target position {targetPosition}, rotation {targetRotation}.");

        if (GetMovingToVictoryPosition()) { SetAtVictoryPosition(true); }

        SetAnimationState(HeroAnimations.Idle);
        UpdateAnimationSpeed();
    }

    /// <summary>
    /// Immediately teleports the hero to the position and rotation of a target
    /// GameObject with no interpolation.
    /// </summary>
    /// <param name="targetObject">The object to snap to.</param>
    public void SnapToTarget(GameObject targetObject)
    {
        GameObject hero = this.gameObject;

        if (targetObject == null)
        {
            if (debug.IsDebugging)
                Debug.LogError($"Target object is null for hero '{name}'. Aborting SnapToTarget.");
            return;
        }

        hero.transform.position = targetObject.transform.position;
        hero.transform.rotation = targetObject.transform.rotation;

        if (debug.IsDebugging)
            Debug.Log($"Hero '{name}' snapped to target '{targetObject.name}' at position {targetObject.transform.position}, rotation {targetObject.transform.rotation}.");
    }

    /// <summary>
    /// Pulls the current move and animation speeds from <see cref="HeroManager"/>
    /// and applies them to this unit, then refreshes the Animator parameter.
    /// </summary>
    public void UpdateHeroSpeeds()
    {
        SetMoveSpeed(GetGameManager().GetHeroManager().GetMoveSpeed());
        SetAnimationSpeed(GetGameManager().GetHeroManager().GetAnimationSpeed());
        UpdateAnimationSpeed();
    }

    // -------------------------------------------------------------------------
    // Combat Actions
    // -------------------------------------------------------------------------

    /// <summary>
    /// Executes this hero's assigned ability against the provided teams, then
    /// plays the <see cref="HeroAnimations.Action1"/> animation and waits for it
    /// to complete before returning.
    /// </summary>
    /// <param name="user">The <see cref="UnitInfo"/> of the acting hero.</param>
    /// <param name="hostileTeam">The monster team to target.</param>
    /// <param name="friendlyTeam">The hero team for friendly targeting.</param>
    public IEnumerator ExecuteAction(UnitInfo user, MonsterTeam hostileTeam = null, HeroTeam friendlyTeam = null)
    {
        if (debug.IsDebugging)
            Debug.Log("[HeroUnit] Hero is executing action.");

        List<UnitInfo> hostileTargets = new List<UnitInfo>();
        List<UnitInfo> friendlyTargets = new List<UnitInfo>();

        foreach (MonsterUnit monster in hostileTeam.GetMonsters()) { hostileTargets.Add(monster.GetUnitInfo()); }
        foreach (HeroUnit hero in friendlyTeam.GetHeroes()) { friendlyTargets.Add(hero.GetUnitInfo()); }

        GetUnitInfo().ExecuteAction(user, hostileTargets, friendlyTargets);

        yield return StartCoroutine(PlayActionAnimation(HeroAnimations.Action1));
    }

    /// <summary>
    /// Executes the <see cref="HitBossAbility"/> against the boss monster and
    /// plays the appropriate action animation.
    /// </summary>
    /// <param name="user">The <see cref="UnitInfo"/> of the acting hero.</param>
    /// <param name="hostileTeam">The monster team containing the boss.</param>
    /// <param name="friendlyTeam">The hero team for friendly targeting.</param>
    public void ExecuteHeroHitBoss(UnitInfo user, MonsterTeam hostileTeam = null, HeroTeam friendlyTeam = null)
    {
        List<UnitInfo> hostileTargets = new List<UnitInfo>();
        List<UnitInfo> friendlyTargets = new List<UnitInfo>();

        foreach (MonsterUnit monster in hostileTeam.GetMonsters()) { hostileTargets.Add(monster.GetUnitInfo()); }
        foreach (HeroUnit hero in friendlyTeam.GetHeroes()) { friendlyTargets.Add(hero.GetUnitInfo()); }

        user.ExecuteAction(user, hostileTargets, friendlyTargets);

        SetAnimationState(HeroAnimations.Action1);
    }

    /// <summary>
    /// Executes the <see cref="CoreHitAbility"/> against a <see cref="CoreUnit"/>
    /// and plays <see cref="HeroAnimations.Action1"/>.
    /// </summary>
    /// <param name="user">The <see cref="UnitInfo"/> of the acting hero.</param>
    /// <param name="core">The core unit being attacked.</param>
    public void ExecuteHeroHitCore(UnitInfo user, CoreUnit core)
    {
        user.ExecuteAbilityCoreHit(user, core);
        SetAnimationState(HeroAnimations.Action1);
    }

    /// <summary>Resets the hero's action bar to its starting value via <see cref="UnitInfo"/>.</summary>
    public void ResetActionBar()
    {
        GetUnitInfo().ResetActionBar();

        if (debug.IsDebugging)
            Debug.Log("[HeroUnit] Action bar reset.");
    }

    /// <summary>
    /// Adds the given amount to the hero's action bar and spawns a floating
    /// energy gain popup near the model. Does nothing if the bar is already full
    /// or the value is negative.
    /// </summary>
    /// <param name="valueToAdd">The positive amount to add to the action bar.</param>
    public void AddToActionBar(int valueToAdd)
    {
        if (valueToAdd < 0) return;
        if (GetUnitInfo().GetActionBar() >= GetUnitInfo().GetActionBarMax()) return;

        GetUnitInfo().AddToActionBar(valueToAdd);

        if (modelPrefab != null && popupTextAddon != null)
        {
            GameObject facingReference = GetTowerManager().GetFacingReference();

            if (facingReference != null)
            {
                Vector3 modelPosition = modelPrefab.transform.position;
                Vector3 directionToReference = (facingReference.transform.position - modelPosition).normalized;
                Vector3 actionPopupPosition = modelPosition + directionToReference * 2f + Vector3.up * 1f;

                popupTextAddon.SpawnPopupText(PopupTextAddon.PopupTextTypes.GainEnergy, actionPopupPosition, valueToAdd);

                if (debug.IsDebugging)
                    Debug.Log($"[HeroUnit] Added {valueToAdd} to action bar. Popup text spawned at position {actionPopupPosition}.");
            }
            else
            {
                Debug.LogWarning("[HeroUnit] Facing Reference is null. Cannot spawn popup text.");
            }
        }
        else
        {
            Debug.LogWarning("[HeroUnit] modelPrefab or popupTextAddon is null. Cannot add to action bar.");
        }
    }

    // -------------------------------------------------------------------------
    // Damage
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies incoming damage to the hero, spawns a floating damage popup near
    /// the model, and plays the <see cref="HeroAnimations.TakeHit"/> animation.
    /// </summary>
    /// <param name="damage">Amount of damage received.</param>
    public void TakeDamage(int damage)
    {
        if (modelPrefab != null && popupTextAddon != null)
        {
            GameObject facingReference = GetTowerManager().GetFacingReference();

            if (facingReference != null)
            {
                Vector3 modelPosition = modelPrefab.transform.position;
                Vector3 directionToReference = (facingReference.transform.position - modelPosition).normalized;
                Vector3 damagePopupPosition = modelPosition + directionToReference * 2f + Vector3.up * 2f;

                popupTextAddon.SpawnPopupText(PopupTextAddon.PopupTextTypes.LoseHealth, damagePopupPosition, damage);

                if (debug.IsDebugging)
                    Debug.Log($"[HeroUnit] Took damage: {damage}. Popup text spawned at {damagePopupPosition}.");
            }
            else
            {
                Debug.LogWarning("[HeroUnit] Facing Reference is null. Cannot spawn popup text.");
            }
        }
        else
        {
            Debug.LogWarning("[HeroUnit] modelPrefab or popupTextAddon is null. Cannot take damage.");
        }

        StartCoroutine(PlayTakeHitAnimation());
    }

    // -------------------------------------------------------------------------
    // Death
    // -------------------------------------------------------------------------

    /// <summary>
    /// Polls the hero's health each frame and begins the death coroutine the
    /// first time health reaches zero or below.
    /// </summary>
    private void CheckForDeath()
    {
        if (GetIsDying()) return;
        if (GetUnitInfo() == null) return;
        if (GetUnitInfo().GetHealth() <= 0) { StartCoroutine(StartDyingCoroutine()); }
    }

    /// <summary>
    /// Counts down <see cref="dyingTimer"/> each frame while the hero is dying
    /// and calls <see cref="ExecuteDeath"/> when the timer expires.
    /// </summary>
    private void UpdateDeathTimer()
    {
        if (!GetIsDying()) return;
        if (GetDyingTimer() > 0) { dyingTimer -= Time.deltaTime; }
        else { ExecuteDeath(); }
    }

    /// <summary>
    /// Full death sequence coroutine: disables UI sliders, detaches the hero from
    /// its team, moves it to a random death point, blasts it out of the tower,
    /// fades it out, then calls <see cref="ExecuteDeath"/>.
    /// </summary>
    public IEnumerator StartDyingCoroutine()
    {
        SetIsDying(true);
        SetDyingTimer(50f);

        Debug.Log("Disabling health and action bar sliders.");
        GetHeroAddon().GetHealthBarSlider().gameObject.SetActive(false);
        GetHeroAddon().GetActionBarSlider().gameObject.SetActive(false);

        Debug.Log("Getting tower death points.");
        List<GameObject> deathpoints = GetTowerManager()
            .GetTowerSectionAddon(GetTowerManager().GetFloor(GetHeroTeam().GetCurrentFloor()))
            .GetDeathPoints();

        GameObject closestDeathPoint = FindRandomDeathPoint(deathpoints);
        Debug.Log("Closest death point found: " + closestDeathPoint.name);

        RemoveUnitFromTeam();

        Debug.Log("Starting move to the death point.");
        yield return StartCoroutine(MoveToDeathPointCoroutine(closestDeathPoint));
        yield return StartCoroutine(BlastOutOfTowerCoroutine(closestDeathPoint.transform));
        yield return StartCoroutine(FadeOutAndExecuteDeath());

        ExecuteDeath();
    }

    /// <summary>
    /// Moves the hero rapidly to a death point at a fixed speed, then spawns
    /// the death VFX on arrival.
    /// </summary>
    /// <param name="deathPoint">The death point waypoint to move toward.</param>
    private IEnumerator MoveToDeathPointCoroutine(GameObject deathPoint)
    {
        float speed = 20f;
        float distance = Vector3.Distance(transform.position, deathPoint.transform.position);

        while (distance > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, deathPoint.transform.position, speed * Time.deltaTime);
            distance = Vector3.Distance(transform.position, deathPoint.transform.position);
            yield return null;
        }

        Debug.Log("Reached the death point.");
        SpawnDeathVFX(deathPoint.transform.position);
    }

    /// <summary>
    /// Launches the hero out of the tower from a death point using randomized
    /// upward and forward velocities with simulated gravity, over a fixed duration.
    /// </summary>
    /// <param name="deathPointTransform">Transform of the death point, used for forward direction.</param>
    private IEnumerator BlastOutOfTowerCoroutine(Transform deathPointTransform)
    {
        Vector3 forward = deathPointTransform.forward;
        float upVelocity = UnityEngine.Random.Range(10f, 15f);
        float forwardVelocity = UnityEngine.Random.Range(20f, 50f);
        float elapsedTime = 0f;
        float duration = 2f;

        Vector3 velocity = new Vector3(forward.x * forwardVelocity, upVelocity, forward.z * forwardVelocity);

        while (elapsedTime < duration)
        {
            transform.position += velocity * Time.deltaTime;
            velocity.y -= 9.81f * Time.deltaTime;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// Waits for a fixed duration before the death sequence concludes, providing
    /// a window for any fade or outro effects.
    /// </summary>
    private IEnumerator FadeOutAndExecuteDeath()
    {
        float timeToWait = 5f;
        float elapsedTime = 0f;

        while (elapsedTime < timeToWait)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>Destroys this hero's GameObject, ending the death sequence.</summary>
    private void ExecuteDeath()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Removes this hero from its <see cref="HeroTeam"/>, detaches from the team's
    /// transform hierarchy, and clears the team reference. Safe to call if already removed.
    /// </summary>
    private void RemoveUnitFromTeam()
    {
        if (GetHeroTeam() == null) return;
        GetHeroTeam().GetHeroes().Remove(this);
        transform.SetParent(null);
        SetHeroTeam(null);
    }

    /// <summary>
    /// Returns the death point nearest to the hero's current position.
    /// </summary>
    /// <param name="deathpoints">List of candidate death point GameObjects.</param>
    /// <returns>The closest death point, or <c>null</c> if the list is empty.</returns>
    private GameObject FindClosestDeathPoint(List<GameObject> deathpoints)
    {
        GameObject closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject deathPoint in deathpoints)
        {
            float distance = Vector3.Distance(transform.position, deathPoint.transform.position);
            if (distance < closestDistance)
            {
                closest = deathPoint;
                closestDistance = distance;
            }
        }

        return closest;
    }

    /// <summary>
    /// Returns a randomly selected death point from the provided list.
    /// </summary>
    /// <param name="deathpoints">List of candidate death point GameObjects.</param>
    /// <returns>A random death point, or <c>null</c> if the list is empty.</returns>
    private GameObject FindRandomDeathPoint(List<GameObject> deathpoints)
    {
        if (deathpoints.Count == 0)
        {
            Debug.LogWarning("No death points available.");
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, deathpoints.Count);
        Debug.Log("Random death point selected: " + deathpoints[randomIndex].name);
        return deathpoints[randomIndex];
    }

    /// <summary>Instantiates the death VFX prefab at the given world position.</summary>
    /// <param name="deathPoint">World-space position at which to spawn the VFX.</param>
    private void SpawnDeathVFX(Vector3 deathPoint)
    {
        if (DeathVfx == null) return;
        Instantiate(DeathVfx, deathPoint, Quaternion.identity);
    }

    /// <summary>
    /// Manually simulates launching the hero out of the tower by applying a velocity
    /// vector with gravity over a fixed duration. Used as an alternative to
    /// <see cref="BlastOutOfTowerCoroutine"/>.
    /// </summary>
    /// <param name="forwardDirection">World-space forward direction for the launch.</param>
    /// <param name="upVelocity">Initial upward velocity.</param>
    /// <param name="forwardVelocity">Initial forward velocity.</param>
    private IEnumerator MoveCharacterOutOfTower(Vector3 forwardDirection, float upVelocity, float forwardVelocity)
    {
        float elapsedTime = 0f;
        float duration = 2f;

        Vector3 velocity = new Vector3(forwardDirection.x * forwardVelocity, upVelocity, forwardDirection.z * forwardVelocity);

        while (elapsedTime < duration)
        {
            transform.position += velocity * Time.deltaTime;
            velocity.y -= 9.81f * Time.deltaTime;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>Destroys the instantiated model prefab and nulls the reference.</summary>
    public void DestroyModel()
    {
        if (modelPrefab != null)
        {
            GameObject.Destroy(modelPrefab);
            modelPrefab = null;

            if (debug.IsDebugging)
                Debug.Log("[HeroUnit] Model prefab destroyed and set to null.");
        }
    }

    // -------------------------------------------------------------------------
    // Animations
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts <see cref="animationState"/> to its integer index and writes it
    /// to the Animator's "animation" integer parameter.
    /// </summary>
    private void UpdateAnimatorParameter()
    {
        if (animator == null)
        {
            Debug.LogError("[HeroUnit] Animator is not assigned. Cannot update animation parameters.");
            return;
        }

        int animationInt = animationState switch
        {
            HeroAnimations.Idle => 0,
            HeroAnimations.Run => 1,
            HeroAnimations.Action1 => 2,
            HeroAnimations.Action2 => 3,
            HeroAnimations.TakeHit => 4,
            HeroAnimations.Die => 5,
            HeroAnimations.Tired => 6,
            HeroAnimations.Victory => 7,
            HeroAnimations.Extra1 => 8,
            HeroAnimations.Extra2 => 9,
            _ => 0,
        };

        animationInt = Mathf.Clamp(animationInt, 0, 31);
        animator.SetInteger("animation", animationInt);

        if (debug.IsDebugging)
            Debug.Log($"[HeroUnit] Animator parameter set to {animationInt}.");
    }

    /// <summary>
    /// Writes the current <see cref="animationSpeed"/> value to the Animator's
    /// "animationSpeed" float parameter. Skips if the unit is not yet initialized
    /// or the parameter does not exist.
    /// </summary>
    private void UpdateAnimationSpeed()
    {
        if (!GetIsInitialize()) return;

        if (animator == null)
        {
            Debug.LogError("[HeroUnit] Animator is not assigned. Cannot update animation speed.");
            return;
        }

        if (!HasAnimatorParameter("animationSpeed", AnimatorControllerParameterType.Float))
        {
            Debug.LogError("[HeroUnit] Animator does not have a float parameter named 'animationSpeed'.");
            return;
        }

        float speed = GetAnimationSpeed();
        animator.SetFloat("animationSpeed", speed);

        if (debug.IsDebugging)
            Debug.Log($"[HeroUnit] Animator parameter 'animationSpeed' set to {speed}.");
    }

    /// <summary>
    /// Returns whether the Animator exposes a parameter with the given name and type.
    /// Used to guard against missing parameter errors before writing values.
    /// </summary>
    /// <param name="paramName">The name of the Animator parameter to check for.</param>
    /// <param name="type">The expected parameter type.</param>
    /// <returns><c>true</c> if the parameter exists with the correct type.</returns>
    private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == type)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Sets the given animation state, waits for it to begin playing, waits for
    /// it to complete, then returns to <see cref="HeroAnimations.Idle"/>.
    /// </summary>
    /// <param name="animation">The action animation to play.</param>
    private IEnumerator PlayActionAnimation(HeroAnimations animation)
    {
        SetAnimationState(animation);

        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName(animation.ToString()));
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);

        SetAnimationState(HeroAnimations.Idle);
        UpdateAnimationSpeed();
    }

    /// <summary>
    /// Plays the <see cref="HeroAnimations.TakeHit"/> animation for its full
    /// duration, then returns the hero to <see cref="HeroAnimations.Idle"/>.
    /// </summary>
    private IEnumerator PlayTakeHitAnimation()
    {
        if (animator == null)
        {
            Debug.LogError("[HeroUnit] Animator is not assigned. Cannot play TakeHit animation.");
            yield break;
        }

        SetAnimationState(HeroAnimations.TakeHit);
        UpdateAnimationSpeed();

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float animationLength = stateInfo.length;

        if (debug.IsDebugging)
            Debug.Log($"[HeroUnit] Playing TakeHit animation. Duration: {animationLength}s");

        yield return new WaitForSeconds(animationLength);

        SetAnimationState(HeroAnimations.Idle);
        UpdateAnimationSpeed();

        if (debug.IsDebugging)
            Debug.Log("[HeroUnit] TakeHit animation complete. Returning to Idle.");
    }

    /// <summary>
    /// Sets the animation state enum, pushes the integer to the Animator via
    /// <see cref="UpdateAnimatorParameter"/>, and refreshes animation speed.
    /// This is the single entry point for all animation state changes.
    /// </summary>
    /// <param name="newState">The animation state to transition to.</param>
    public void SetAnimationState(HeroAnimations newState)
    {
        animationState = newState;
        UpdateAnimatorParameter();
        UpdateAnimationSpeed();
    }

    // -------------------------------------------------------------------------
    // Debug Watches
    // -------------------------------------------------------------------------

    /// <summary>
    /// Refreshes all Inspector watch entries each frame with the hero's current
    /// runtime state. Called from <see cref="Update"/>.
    /// </summary>
    private void UpdateDebugWatches()
    {
        debug.SetWatch("Class", heroClass.ToString());
        debug.SetWatch("Dying", isDying.ToString());
        debug.SetWatch("Active", isActive.ToString());
        debug.SetWatch("Anim State", animationState.ToString());

        if (unitInfo != null)
        {
            debug.SetWatch("Health", $"{unitInfo.GetHealth()} / {unitInfo.GetMaxHealth()}");
            debug.SetWatch("Action Bar", $"{unitInfo.GetActionBar()} / {unitInfo.GetActionBarMax()}");
            debug.SetWatch("Power", unitInfo.GetPower().ToString());
            debug.SetWatch("Speed", unitInfo.GetSpeed().ToString());
        }

        debug.SetWatch("Move Speed", moveSpeed.ToString("F2"));
        debug.SetWatch("At Victory", atVictoryPosition.ToString());
        debug.SetWatch("Team Slot", numberInTeam.ToString());
    }

    // -------------------------------------------------------------------------
    // Getters & Setters
    // -------------------------------------------------------------------------

    private void SetIsInitialize(bool newIsInitialize) => isInitialize = newIsInitialize;
    private bool GetIsInitialize() => isInitialize;

    /// <returns>World-space movement speed in units per second.</returns>
    public float GetMoveSpeed() => moveSpeed;
    /// <param name="newMoveSpeed">New movement speed to assign.</param>
    public void SetMoveSpeed(float newMoveSpeed) => moveSpeed = newMoveSpeed;

    /// <returns>Current animation speed multiplier.</returns>
    public float GetAnimationSpeed() => animationSpeed;
    /// <param name="newAnimationSpeed">New animation speed multiplier to assign.</param>
    public void SetAnimationSpeed(float newAnimationSpeed) => animationSpeed = newAnimationSpeed;

    /// <returns>Global overall speed multiplier.</returns>
    public float GetOverallSpeed() => overallSpeed;
    /// <param name="newOverallSpeed">New overall speed multiplier to assign.</param>
    public void SetOverallSpeed(float newOverallSpeed) => overallSpeed = newOverallSpeed;

    /// <returns>The hero's <see cref="UnitInfo"/> stat container.</returns>
    public UnitInfo GetUnitInfo() => unitInfo;
    /// <param name="newInfo">New <see cref="UnitInfo"/> to assign.</param>
    public void SetUnitInfo(UnitInfo newInfo) => unitInfo = newInfo;

    /// <returns>The <see cref="AbilityId"/> assigned to this hero's action bar slot.</returns>
    public AbilityId GetActionAbilityId() => actionAbilityId;

    /// <returns>The hero's <see cref="HeroClass"/> archetype.</returns>
    public HeroClass GetHeroClass() => heroClass;
    /// <param name="newClass">New hero class to assign.</param>
    public void SetHeroClass(HeroClass newClass) => heroClass = newClass;

    /// <param name="newModel">The instantiated model GameObject to assign.</param>
    public void SetModel(GameObject newModel) => modelPrefab = newModel;
    /// <returns>The hero's instantiated model GameObject.</returns>
    public GameObject GetModel() => modelPrefab;

    /// <returns><c>true</c> if the hero is currently moving toward its victory position.</returns>
    public bool GetMovingToVictoryPosition() => movingToVictoryPosition;
    /// <param name="newMovingToVictoryPosition">New moving-to-victory state.</param>
    public void SetMovingToVictoryPosition(bool newMovingToVictoryPosition) => movingToVictoryPosition = newMovingToVictoryPosition;

    /// <returns><c>true</c> if the hero has reached its victory position.</returns>
    public bool GetAtVictoryPosition() => atVictoryPosition;
    /// <param name="newAtVictoryPosition">New at-victory-position state.</param>
    public void SetAtVictoryPosition(bool newAtVictoryPosition) => atVictoryPosition = newAtVictoryPosition;

    /// <param name="newAnimator">Animator to assign, sourced from the model prefab.</param>
    public void SetAnimator(Animator newAnimator) => animator = newAnimator;
    /// <returns>The Animator component used by this hero.</returns>
    public Animator GetAnimator() => animator;

    /// <returns>The current <see cref="HeroAnimations"/> state.</returns>
    public HeroAnimations GetAnimationState() => animationState;

    /// <returns>The hero's unique unit ID.</returns>
    public int GetHuID() => huID;
    /// <param name="newHuID">New hero unit ID to assign.</param>
    public void SetHuID(int newHuID) => huID = newHuID;

    /// <returns><c>true</c> while the hero is in its death sequence.</returns>
    public bool GetIsDying() => isDying;
    /// <param name="newIsDying">New dying state to assign.</param>
    public void SetIsDying(bool newIsDying) => isDying = newIsDying;

    /// <returns>Remaining time on the death countdown timer.</returns>
    public float GetDyingTimer() => dyingTimer;
    /// <param name="newDyingTimer">New dying timer value to assign.</param>
    public void SetDyingTimer(float newDyingTimer) => dyingTimer = newDyingTimer;

    /// <param name="newHeroAddon">New <see cref="HeroUnitUiAddon"/> to assign.</param>
    public void SetHeroAddon(HeroUnitUiAddon newHeroAddon) => heroAddon = newHeroAddon;
    /// <returns>The prefab-side addon managing this hero's UI and visual components.</returns>
    public HeroUnitUiAddon GetHeroAddon() => heroAddon;

    /// <param name="newPopupTextAddon">New <see cref="PopupTextAddon"/> to assign.</param>
    public void SetPopupTextAddon(PopupTextAddon newPopupTextAddon) => popupTextAddon = newPopupTextAddon;
    /// <returns>The addon used to spawn floating popup text.</returns>
    public PopupTextAddon GetPopupTextAddon() => popupTextAddon;

    /// <summary>
    /// Sets the hero's active state, toggling the GameObject on or off accordingly.
    /// </summary>
    /// <param name="newActive">Whether the hero should be active.</param>
    public void SetIsActive(bool newActive)
    {
        isActive = newActive;
        gameObject.SetActive(isActive);
    }
    /// <returns><c>true</c> if the hero is currently active.</returns>
    public bool GetIsActive() => isActive;

    /// <param name="newTeam">The <see cref="HeroTeam"/> to assign to this hero.</param>
    public void SetHeroTeam(HeroTeam newTeam) => heroTeam = newTeam;
    /// <returns>The team this hero belongs to.</returns>
    public HeroTeam GetHeroTeam() => heroTeam;

    /// <param name="newTeamNumber">This hero's index within its team.</param>
    public void SetNumberInTeam(int newTeamNumber) => numberInTeam = newTeamNumber;
    /// <returns>This hero's index within its team.</returns>
    public int GetNumberInTeam() => numberInTeam;

    /// <param name="newManager">The <see cref="TowerDefenseManager"/> to bind.</param>
    public void SetGameManager(TowerDefenseManager newManager) => GameManager = newManager;
    /// <returns>The central <see cref="TowerDefenseManager"/>.</returns>
    public TowerDefenseManager GetGameManager() => GameManager;

    /// <returns>The <see cref="TowerManager"/> retrieved via the game manager.</returns>
    public TowerManager GetTowerManager() => GetGameManager().GetTowerManager();
    /// <returns>The <see cref="HeroManager"/> retrieved via the game manager.</returns>
    public HeroManager GetHeroManager() => GetGameManager().GetHeroManager();
    /// <returns>The <see cref="MonsterManager"/> retrieved via the game manager.</returns>
    public MonsterManager GetMonsterManager() => GetGameManager().GetMonsterManager();
}
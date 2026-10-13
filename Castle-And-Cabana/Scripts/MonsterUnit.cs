using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HeroUnit;
using NUnit.Framework;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.TextCore.Text;

/// <summary>
/// Represents a single monster unit in the tower defense combat loop.
/// Manages initialization, stat assignment, slot-based positioning, combat actions
/// (including boss swipe), damage handling, death sequencing, and animator state.
/// Delegates prefab-side visuals to <see cref="MonsterUnitPrefabAddon"/>.
/// </summary>
public class MonsterUnit : MonoBehaviour
{
    #region // Variables

    #region // Enums

    /// <summary>
    /// Available monster archetypes, each with a distinct stat profile.
    /// Integer values are used for serialization and ordering.
    /// </summary>
    public enum MonsterTypes
    {
        Slime = 0,
        Bat = 1,
        Spider = 2,
        Dummii = 3,
        BossKnight = 4,
    }

    /// <summary>
    /// Animation states that can be applied to the monster's Animator.
    /// Maps to integer values used by the "animation" Animator parameter.
    /// </summary>
    public enum MonsterAnimations
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
        SittingOnThrone,
    }

    #endregion

    #region // Unit Info

    [Header("Unit Info")]
    [Tooltip("Shared stat container for health, action bar, power, and speed.")]
    [SerializeField] private UnitInfo unitInfo;

    [Tooltip("The archetype of this monster, which determines its base stats.")]
    [SerializeField] private MonsterTypes monsterClass;

    [Tooltip("Unique ID for this monster unit, e.g. 'MT1U2'.")]
    [SerializeField] private string muID;

    [Tooltip("World-space units per second this monster moves.")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("Value written to the 'animationSpeed' Animator float parameter.")]
    [SerializeField] private float animationSpeed = 1f;

    [Tooltip("Angular speed used when Lerping rotation toward a target.")]
    [SerializeField] private int rotationSpeed = 5;

    [Tooltip("True if this unit is the boss monster.")]
    [SerializeField] private bool isBoss = false;

    [Tooltip("Whether this monster is currently active in the scene.")]
    [SerializeField] private bool isActive = false;

    [Tooltip("True while the monster is playing out its death sequence.")]
    [SerializeField] private bool isDying = false;

    [Tooltip("Initial value for the death countdown timer.")]
    [SerializeField] private float dyingTimerInit = 10f;

    [Tooltip("Remaining time on the death countdown timer.")]
    [SerializeField] private float dyingTimer;

    #endregion

    #region // Model

    [Header("Model")]
    [Tooltip("The instantiated model GameObject parented to this unit.")]
    [SerializeField] private GameObject modelPrefab;

    [Tooltip("Animator component sourced from the instantiated model prefab.")]
    [SerializeField] private Animator animator;

    [Tooltip("Current animation state, mirrored from the Animator parameter.")]
    [SerializeField] private MonsterAnimations animationState;

    #endregion

    #region // References

    [Header("References")]
    [Tooltip("Reference to the central TowerDefenseManager.")]
    [SerializeField] private TowerDefenseManager GameManager;

    [Tooltip("The team this monster belongs to.")]
    [SerializeField] private MonsterTeam monsterTeam;

    [Tooltip("1-based slot index this monster occupies within its team.")]
    [SerializeField] private int slotPosition;

    [Tooltip("The MonsterSlotAddon component for this monster's assigned slot.")]
    [SerializeField] private MonsterSlotAddon slotAddon;

    /// <summary>Prefab-side addon managing sliders and the Animator reference.</summary>
    private MonsterUnitPrefabAddon monsterAddon;

    /// <summary>Addon used to spawn floating damage/energy popup text.</summary>
    private PopupTextAddon popupTextAddon;

    [Tooltip("VFX prefab instantiated at the moment of death.")]
    [SerializeField] private GameObject DeathVfx;

    #endregion

    #region // Debugging

    [Header("Debugging")]
    [SerializeField] private DebugUtility debug;

    [Tooltip("The path currently being traversed, for Inspector visualization.")]
    [SerializeField] private List<GameObject> currentPath = new List<GameObject>();

    [Tooltip("The object this monster is currently positioned at, for Inspector visualization.")]
    [SerializeField] private GameObject positioningObject;

    #endregion

    #endregion

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Start() { }

    /// <summary>
    /// Each frame: checks whether health has reached zero and advances the death
    /// countdown timer if a death sequence is in progress. Also refreshes debug watches.
    /// </summary>
    private void Update()
    {
        CheckForDeath();
        UpdateDeathTimer();
        UpdateDebugWatches();
    }

    // -------------------------------------------------------------------------
    // Debug Watches
    // -------------------------------------------------------------------------

    /// <summary>
    /// Refreshes all Inspector watch entries each frame with this unit's current
    /// runtime state. Called from <see cref="Update"/>.
    /// </summary>
    private void UpdateDebugWatches()
    {
        debug.SetWatch("Class", monsterClass.ToString());
        debug.SetWatch("Slot", slotPosition.ToString());
        debug.SetWatch("Dying", isDying.ToString());
        debug.SetWatch("Is Boss", isBoss.ToString());
        debug.SetWatch("Anim State", animationState.ToString());

        if (unitInfo != null)
        {
            debug.SetWatch("Health", $"{unitInfo.GetHealth()} / {unitInfo.GetMaxHealth()}");
            debug.SetWatch("Action Bar", $"{unitInfo.GetActionBar()} / {unitInfo.GetActionBarMax()}");
            debug.SetWatch("Power", unitInfo.GetPower().ToString());
            debug.SetWatch("Speed", unitInfo.GetSpeed().ToString());
        }

        debug.SetWatch("Move Speed", moveSpeed.ToString("F2"));
        debug.SetWatch("ID", muID ?? "null");
    }

    // -------------------------------------------------------------------------
    // Initialization
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fully initializes the monster unit: assigns its ID, manager, team, stats,
    /// class, and spawns the visual model. Must be called before any other
    /// gameplay method is invoked.
    /// </summary>
    /// <param name="newManager">The central <see cref="TowerDefenseManager"/>.</param>
    /// <param name="newMonsterTeam">The <see cref="MonsterTeam"/> this unit belongs to.</param>
    /// <param name="muID">Unique monster unit ID string.</param>
    /// <param name="newClass">The <see cref="MonsterTypes"/> archetype to assign.</param>
    /// <param name="level">Starting level, passed to <see cref="UnitInfo"/>.</param>
    public void InitializeMonster(TowerDefenseManager newManager, MonsterTeam newMonsterTeam, string muID, MonsterTypes newClass, int level)
    {
        InitalizeDebug();

        if (debug.IsDebugging)
        {
            Debug.Log("Initializing monster...");
            Debug.Log($"New Manager: {newManager}, New Monster Team: {newMonsterTeam}, Monster Class: {newClass}, Level: {level}");
        }

        SetMuID(muID);
        if (debug.IsDebugging) Debug.Log($"Monster Unit ID set to: {muID}");

        SetGameManager(newManager);
        if (debug.IsDebugging) Debug.Log($"TowerDefenseManager set to: {newManager}");

        SetMonsterTeam(newMonsterTeam);
        if (debug.IsDebugging) Debug.Log($"MonsterTeam set to: {newMonsterTeam}");

        SetUnitInfo(new UnitInfo(this, level));
        GetUnitInfo().SetName(GetMuID());
        if (debug.IsDebugging) Debug.Log($"UnitInfo created with level {level} and linked to monster.");

        SetMonsterClass(newClass);
        if (debug.IsDebugging) Debug.Log($"Monster class set to: {newClass}");

        InitializeMonsterStats();
        if (debug.IsDebugging) Debug.Log("Monster stats initialized.");

        SpawnMonster();
        if (debug.IsDebugging) Debug.Log("Monster initialization complete.");
    }

    /// <summary>Creates a fresh <see cref="DebugUtility"/> for this unit.</summary>
    private void InitalizeDebug() { debug = new DebugUtility(false); }

    /// <summary>
    /// Assigns base stats (power, max health, speed, action ability) to the
    /// monster's <see cref="UnitInfo"/> based on its <see cref="MonsterTypes"/>,
    /// then resets the action bar to its starting value.
    /// </summary>
    private void InitializeMonsterStats()
    {
        switch (monsterClass)
        {
            case MonsterTypes.Slime:
                unitInfo.SetPower(2); unitInfo.SetMaxHealth(6); unitInfo.SetSpeed(2);
                unitInfo.SetAction(AbilityId.Attack);
                break;
            case MonsterTypes.Bat:
                unitInfo.SetPower(3); unitInfo.SetMaxHealth(4); unitInfo.SetSpeed(3);
                unitInfo.SetAction(AbilityId.Attack);
                break;
            case MonsterTypes.Spider:
                unitInfo.SetPower(5); unitInfo.SetMaxHealth(2); unitInfo.SetSpeed(4);
                unitInfo.SetAction(AbilityId.Attack);
                break;
            case MonsterTypes.Dummii:
                unitInfo.SetPower(3); unitInfo.SetMaxHealth(3); unitInfo.SetSpeed(3);
                unitInfo.SetAction(AbilityId.Attack);
                break;
            case MonsterTypes.BossKnight:
                unitInfo.SetPower(5); unitInfo.SetMaxHealth(5); unitInfo.SetSpeed(0);
                SetIsBoss(true);
                unitInfo.SetAction(AbilityId.BossSwipe);
                break;
        }

        unitInfo.SetHealth(unitInfo.GetMaxHealth());
        ResetActionBar();

        if (debug.IsDebugging)
            Debug.Log($"{unitInfo.GetName()} Monster initialization complete. " +
                $"Stats:\nOffense - {unitInfo.GetPower()}, Max Health - {unitInfo.GetMaxHealth()}, Speed - {unitInfo.GetSpeed()}.");
    }

    /// <summary>
    /// Instantiates the monster's model prefab, parents it to this transform, and
    /// links the <see cref="MonsterUnitPrefabAddon"/>, <see cref="PopupTextAddon"/>,
    /// and <see cref="Animator"/> components.
    /// </summary>
    public void SpawnMonster()
    {
        MonsterManager monsterManager = GetMonsterManager();
        GameObject monsterPrefab = monsterManager.GetMonsterModelPrefab(monsterClass);
        GameObject monsterObject = Instantiate(monsterPrefab);
        monsterObject.transform.SetParent(this.gameObject.transform);
        SetModel(monsterObject);

        SetMonsterAddon(monsterObject.GetComponent<MonsterUnitPrefabAddon>());
        if (GetMonsterAddon() == null) SetMonsterAddon(monsterObject.GetComponentInChildren<MonsterUnitPrefabAddon>());
        GetMonsterAddon().InitializeSliders(this);

        SetPopupTextAddon(monsterObject.GetComponent<PopupTextAddon>());
        if (GetPopupTextAddon() == null) SetPopupTextAddon(monsterObject.GetComponentInChildren<PopupTextAddon>());

        SetAnimator(GetMonsterAddon().GetAnimator());

        if (GetMonsterAddon() == null) Debug.LogError("Error! Unable to find GetMonsterAddon on object or children.");
        if (GetPopupTextAddon() == null) Debug.LogError("Error! Unable to find GetPopupTextAddon on object or children.");
        if (GetAnimator() == null) Debug.LogError("Error! Unable to find animator on object or children.");
    }

    // -------------------------------------------------------------------------
    // Movement
    // -------------------------------------------------------------------------

    /// <summary>
    /// Moves the monster toward a world-space position, smoothly rotating to face
    /// the direction of travel. Snaps to the exact position on arrival.
    /// </summary>
    /// <param name="targetPosition">World-space destination.</param>
    public IEnumerator MoveToTarget(Vector3 targetPosition)
    {
        GameObject monster = this.gameObject;

        if (debug.IsDebugging)
            Debug.Log($"Monster '{name}' starting MoveToTarget towards position {targetPosition}.");

        while (Vector3.Distance(monster.transform.position, targetPosition) > 0.1f)
        {
            Vector3 overshootPosition = targetPosition + (targetPosition - monster.transform.position).normalized * 0.5f;
            Debug.DrawLine(monster.transform.position, overshootPosition, Color.green, 0f, false);
            Debug.DrawLine(monster.transform.position + Vector3.up * 0.2f, overshootPosition + Vector3.up * 0.2f, Color.green, 0f, false);
            Debug.DrawLine(monster.transform.position + Vector3.right * 0.2f, overshootPosition + Vector3.right * 0.2f, Color.green, 0f, false);

            monster.transform.position = Vector3.MoveTowards(
                monster.transform.position, targetPosition, GetMoveSpeed() * Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(targetPosition - monster.transform.position);
            monster.transform.rotation = Quaternion.Lerp(
                monster.transform.rotation, targetRotation, GetMoveSpeed() * Time.deltaTime);

            yield return null;
        }

        monster.transform.position = targetPosition;

        if (debug.IsDebugging)
            Debug.Log($"Monster '{name}' snapped to exact position {targetPosition}.");
    }

    /// <summary>
    /// Immediately teleports the monster to the position and rotation of a target
    /// GameObject with no interpolation. Caches the target as <see cref="positioningObject"/>.
    /// </summary>
    /// <param name="targetObject">The object to snap to.</param>
    public void SnapToTarget(GameObject targetObject)
    {
        if (targetObject == null)
        {
            if (debug.IsDebugging)
                Debug.LogError($"Target object is null for monster '{name}'. Aborting SnapToTarget.");
            return;
        }

        this.gameObject.transform.position = targetObject.transform.position;
        this.gameObject.transform.rotation = targetObject.transform.rotation;
        positioningObject = targetObject;

        if (debug.IsDebugging)
            Debug.Log($"Monster '{name}' snapped to target '{targetObject.name}' at position {targetObject.transform.position}.");
    }

    // -------------------------------------------------------------------------
    // Boss Movement
    // -------------------------------------------------------------------------

    /// <summary>
    /// Plays the stand-up sequence for the boss: transitions to Idle animation,
    /// enables the weapon attachment, and waits 1.5 s.
    /// </summary>
    public IEnumerator BossUnitStandUpFromThrone()
    {
        SetAnimationState(MonsterUnit.MonsterAnimations.Idle);

        GameObject weaponAttachment = GetMonsterAddon().GetWeaponAttachment();
        if (weaponAttachment != null) weaponAttachment.SetActive(true);

        yield return new WaitForSeconds(1.5f);
    }

    /// <summary>
    /// Moves the boss from the throne to its first combat slot, playing the Run
    /// animation during travel and returning to Idle on arrival.
    /// </summary>
    public IEnumerator BossUnitMoveToCombatPosition()
    {
        List<GameObject> slots = GetMonsterTeam().GetSlotsAddon().GetSlots()
            .Select(slot => slot.gameObject).ToList();

        SetAnimationState(MonsterUnit.MonsterAnimations.Run);
        yield return MoveToTarget(slots[0].transform.position);
        SetAnimationState(MonsterUnit.MonsterAnimations.Idle);
    }

    /// <summary>
    /// Moves the boss from its current position back toward the throne, playing
    /// the Run animation during travel.
    /// </summary>
    public IEnumerator BossUnitMoveToThronePosition()
    {
        GameObject throne = GetTowerManager().GetThrone();

        if (throne == null)
        {
            if (debug.IsDebugging) Debug.LogError("Throne object is null! Cannot move boss to throne.");
            yield break;
        }

        SetAnimationState(MonsterUnit.MonsterAnimations.Run);
        Vector3 targetPosition = throne.transform.position - throne.transform.forward * 0.75f;
        yield return MoveToTarget(targetPosition);
    }

    /// <summary>
    /// Snaps the boss to the throne transform, plays the
    /// <see cref="MonsterAnimations.SittingOnThrone"/> animation,
    /// disables the weapon attachment, and waits 0.5 s.
    /// </summary>
    public IEnumerator BossUnitSitOnThrone()
    {
        GameObject throne = GetTowerManager().GetThrone();

        if (throne == null)
        {
            if (debug.IsDebugging) Debug.LogError("Throne object is null! Cannot sit boss on throne.");
            yield break;
        }

        transform.position = throne.transform.position;
        transform.rotation = throne.transform.rotation;
        SetAnimationState(MonsterUnit.MonsterAnimations.SittingOnThrone);

        GameObject weaponAttachment = GetMonsterAddon().GetWeaponAttachment();
        if (weaponAttachment != null) weaponAttachment.SetActive(false);

        yield return new WaitForSeconds(0.5f);
    }

    // -------------------------------------------------------------------------
    // Death
    // -------------------------------------------------------------------------

    /// <summary>
    /// Polls the monster's health each frame and begins the death coroutine the
    /// first time health reaches zero or below.
    /// </summary>
    public void CheckForDeath()
    {
        if (GetIsDying()) return;
        if (GetUnitInfo() == null) return;
        if (GetUnitInfo().GetHealth() <= 0) { StartCoroutine(StartDyingCoroutine()); }
    }

    /// <summary>
    /// Counts down <see cref="dyingTimer"/> each frame while the monster is dying
    /// and calls <see cref="ExecuteDeath"/> when the timer expires.
    /// </summary>
    private void UpdateDeathTimer()
    {
        if (!GetIsDying()) return;
        if (GetDyingTimer() > 0) { dyingTimer -= Time.deltaTime; }
        else { ExecuteDeath(); }
    }

    /// <summary>
    /// Full death sequence coroutine: marks the unit as dying, disables UI sliders,
    /// detaches from the team, fades out, then destroys the GameObject.
    /// </summary>
    public IEnumerator StartDyingCoroutine()
    {
        SetIsDying(true);
        SetDyingTimer(50f);

        Debug.Log("Disabling health and action bar sliders.");
        GetMonsterAddon().GetHealthBarSlider().gameObject.SetActive(false);
        GetMonsterAddon().GetActionBarSlider().gameObject.SetActive(false);

        RemoveUnitFromTeam();

        yield return StartCoroutine(FadeOut());
        ExecuteDeath();
    }

    /// <summary>
    /// Waits a fixed duration before the death sequence concludes, providing
    /// a window for any fade or outro effects.
    /// </summary>
    private IEnumerator FadeOut()
    {
        float timeToWait = 5f;
        float elapsedTime = 0f;
        while (elapsedTime < timeToWait) { elapsedTime += Time.deltaTime; yield return null; }
    }

    /// <summary>Destroys this monster's GameObject, ending the death sequence.</summary>
    private void ExecuteDeath() { Destroy(gameObject); }

    /// <summary>
    /// Removes this monster from its <see cref="MonsterTeam"/>, detaches from
    /// the team's transform hierarchy, and clears the team reference.
    /// Safe to call if already removed.
    /// </summary>
    private void RemoveUnitFromTeam()
    {
        if (GetMonsterTeam() == null) return;
        GetMonsterTeam().GetMonsters().Remove(this);
        transform.SetParent(null);
        SetMonsterTeam(null);
    }

    /// <summary>Instantiates the death VFX prefab at the given world position.</summary>
    /// <param name="deathPoint">World-space position at which to spawn the VFX.</param>
    private void SpawnDeathVFX(Vector3 deathPoint)
    {
        if (DeathVfx == null) return;
        Instantiate(DeathVfx, deathPoint, Quaternion.identity);
    }

    /// <summary>Destroys the instantiated model prefab and nulls the reference.</summary>
    public void DestroyModel()
    {
        if (modelPrefab != null)
        {
            GameObject.Destroy(modelPrefab);
            modelPrefab = null;
            if (debug.IsDebugging) Debug.Log("[MonsterUnit] Model prefab destroyed and set to null.");
        }
    }

    /// <summary>
    /// Pulls the current move and animation speeds from <see cref="MonsterManager"/>
    /// and applies them to this unit, then refreshes the Animator parameter.
    /// </summary>
    public void UpdateMonsterSpeeds()
    {
        SetMoveSpeed(GetGameManager().GetMonsterManager().GetMoveSpeed());
        SetAnimationSpeed(GetGameManager().GetMonsterManager().GetAnimationSpeed());
        UpdateMonsterAnimationSpeed();
    }

    // -------------------------------------------------------------------------
    // Animations
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts <see cref="animationState"/> to its integer index and writes it
    /// to the Animator's "animation" integer parameter. Also syncs animation speed.
    /// </summary>
    private void UpdateAnimatorParameter()
    {
        if (animator == null) { Debug.LogError("[MonsterUnit] Animator is not assigned."); return; }

        int animationInt = animationState switch
        {
            MonsterAnimations.Idle => 0,
            MonsterAnimations.Run => 1,
            MonsterAnimations.Action1 => 2,
            MonsterAnimations.Action2 => 3,
            MonsterAnimations.TakeHit => 4,
            MonsterAnimations.Die => 5,
            MonsterAnimations.Tired => 6,
            MonsterAnimations.Victory => 7,
            MonsterAnimations.Extra1 => 7,
            MonsterAnimations.Extra2 => 7,
            _ => 0,
        };

        animationInt = Mathf.Clamp(animationInt, 0, 31);
        animator.SetInteger("animation", animationInt);
        UpdateMonsterSpeeds();

        if (debug.IsDebugging) Debug.Log($"[MonsterUnit] Animator parameter set to {animationInt}.");
    }

    /// <summary>
    /// Writes the current <see cref="animationSpeed"/> value to the Animator's
    /// "animationSpeed" float parameter. Skips if the parameter does not exist.
    /// </summary>
    public void UpdateMonsterAnimationSpeed()
    {
        if (animator == null) { Debug.LogError("[MonsterUnit] Animator is not assigned."); return; }
        if (!HasAnimatorParameter("animationSpeed", AnimatorControllerParameterType.Float))
        {
            Debug.LogError("[MonsterUnit] Animator does not have a float parameter named 'animationSpeed'.");
            return;
        }

        animator.SetFloat("animationSpeed", GetAnimationSpeed());
        if (debug.IsDebugging)
            Debug.Log($"[MonsterUnit] Animator parameter 'animationSpeed' set to {GetAnimationSpeed()}.");
    }

    /// <summary>
    /// Returns whether the Animator exposes a parameter with the given name and type.
    /// </summary>
    private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == type) return true;
        }
        return false;
    }

    /// <summary>
    /// Sets the given animation state, waits for it to begin playing, waits for
    /// it to complete, then returns to <see cref="MonsterAnimations.Idle"/>.
    /// </summary>
    private IEnumerator PlayActionAnimation(MonsterAnimations animation)
    {
        SetAnimationState(animation);
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName(animation.ToString()));
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        SetAnimationState(MonsterAnimations.Idle);
    }

    /// <summary>
    /// Plays the <see cref="MonsterAnimations.TakeHit"/> animation for its full
    /// duration, then returns the monster to <see cref="MonsterAnimations.Idle"/>.
    /// </summary>
    private IEnumerator PlayTakeHitAnimation()
    {
        if (animator == null) { Debug.LogError("[MonsterUnit] Animator is not assigned."); yield break; }

        SetAnimationState(MonsterAnimations.TakeHit);
        float animationLength = animator.GetCurrentAnimatorStateInfo(0).length;
        if (debug.IsDebugging) Debug.Log($"[MonsterUnit] Playing TakeHit animation. Duration: {animationLength}s");

        yield return new WaitForSeconds(animationLength);
        SetAnimationState(MonsterAnimations.Idle);
        if (debug.IsDebugging) Debug.Log("[MonsterUnit] TakeHit animation complete. Returning to Idle.");
    }

    /// <summary>
    /// Sets the animation state enum and pushes the integer to the Animator via
    /// <see cref="UpdateAnimatorParameter"/>. This is the single entry point for
    /// all animation state changes.
    /// </summary>
    /// <param name="newState">The animation state to transition to.</param>
    public void SetAnimationState(MonsterAnimations newState)
    {
        animationState = newState;
        UpdateAnimatorParameter();
    }

    // -------------------------------------------------------------------------
    // Comparison / Equality Overrides
    // -------------------------------------------------------------------------

    /// <summary>Two <see cref="MonsterUnit"/>s are equal if they share the same <see cref="muID"/>.</summary>
    public bool Equals(MonsterUnit other)
    {
        if (other == null) return false;
        return this.muID == other.muID;
    }

    /// <inheritdoc/>
    public override bool Equals(object obj) => Equals(obj as MonsterUnit);

    /// <inheritdoc/>
    public override int GetHashCode() => muID?.GetHashCode() ?? 0;

    /// <summary>Compares two monsters by slot position for sorting purposes.</summary>
    public int CompareTo(MonsterUnit other)
    {
        if (other == null) return 1;
        return this.slotPosition.CompareTo(other.slotPosition);
    }

    public static bool operator >(MonsterUnit a, MonsterUnit b)
    {
        if (a == null) return false;
        if (b == null) return true;
        return string.Compare(a.muID, b.muID, StringComparison.Ordinal) > 0;
    }

    public static bool operator <(MonsterUnit a, MonsterUnit b)
    {
        if (a == null) return b != null;
        if (b == null) return false;
        return string.Compare(a.muID, b.muID, StringComparison.Ordinal) < 0;
    }

    public static bool operator >=(MonsterUnit a, MonsterUnit b) => !(a < b);
    public static bool operator <=(MonsterUnit a, MonsterUnit b) => !(a > b);

    // -------------------------------------------------------------------------
    // Debug Output
    // -------------------------------------------------------------------------

    /// <summary>
    /// Logs this unit's slot, class, ID, name, and current health to the console.
    /// </summary>
    public void OutputMonsterUnitInfo()
    {
        UnitInfo info = GetUnitInfo();
        Debug.Log(
            $"Slot {GetSlotPosition()}: [Class: {GetMonsterClass()}] [ID: {GetMuID()}]\n" +
            $"Name: {info.GetName()}\nHealth: {info.GetHealth()}\n---------------------"
        );
    }

    // -------------------------------------------------------------------------
    // Combat Actions
    // -------------------------------------------------------------------------

    /// <summary>
    /// Resets the dying timer to its initial value.
    /// </summary>
    public void ResetDyingTimer() => dyingTimer = dyingTimerInit;

    /// <summary>
    /// Executes this monster's assigned ability against the provided hero team
    /// and plays the appropriate action animation.
    /// </summary>
    /// <param name="user">The <see cref="UnitInfo"/> of the acting monster.</param>
    /// <param name="hostileTeam">The hero team to target.</param>
    /// <param name="friendlyTeam">The monster team for friendly targeting.</param>
    public void ExecuteAction(UnitInfo user, HeroTeam hostileTeam = null, MonsterTeam friendlyTeam = null)
    {
        if (debug.IsDebugging) Debug.Log("[MonsterUnit] Monster is executing action.");

        List<UnitInfo> hostileTargets = new List<UnitInfo>();
        List<UnitInfo> friendlyTargets = new List<UnitInfo>();
        foreach (HeroUnit hero in hostileTeam.GetHeroes()) { hostileTargets.Add(hero.GetUnitInfo()); }
        foreach (MonsterUnit monster in friendlyTeam.GetMonsters()) { friendlyTargets.Add(monster.GetUnitInfo()); }

        unitInfo.ExecuteAction(user, hostileTargets, friendlyTargets);

        SetAnimationState(MonsterAnimations.Action1);
    }

    /// <summary>
    /// Executes the <see cref="BossSwipeAbility"/> ability, targeting
    /// the entire hero team simultaneously, and plays the action animation.
    /// </summary>
    /// <param name="user">The <see cref="UnitInfo"/> of the boss.</param>
    /// <param name="hostileTeam">The hero team to wipe.</param>
    /// <param name="friendlyTeam">The monster team for friendly targeting.</param>
    public void ExecuteBossSwipeTeam(UnitInfo user, HeroTeam hostileTeam = null, MonsterTeam friendlyTeam = null)
    {
        List<UnitInfo> hostileTargets = new List<UnitInfo>();
        List<UnitInfo> friendlyTargets = new List<UnitInfo>();
        foreach (HeroUnit hero in hostileTeam.GetHeroes()) { hostileTargets.Add(hero.GetUnitInfo()); }
        foreach (MonsterUnit monster in friendlyTeam.GetMonsters()) { friendlyTargets.Add(monster.GetUnitInfo()); }

        unitInfo.ExecuteAction(user, hostileTargets, friendlyTargets);

        SetAnimationState(MonsterAnimations.Action1);
    }

    /// <summary>
    /// Applies incoming damage to the monster and spawns a floating damage popup
    /// near the model.
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
                Vector3 directionToRef = (facingReference.transform.position - modelPosition).normalized;
                Vector3 damagePopupPosition = modelPosition + directionToRef * 2f + Vector3.up * 2f;
                popupTextAddon.SpawnPopupText(PopupTextAddon.PopupTextTypes.LoseHealth, damagePopupPosition, damage);

                if (debug.IsDebugging)
                    Debug.Log($"[MonsterUnit] Took damage: {damage}. Popup spawned at {damagePopupPosition}.");
            }
            else { Debug.LogWarning("[MonsterUnit] Facing Reference is null. Cannot spawn popup text."); }
        }
        else { Debug.LogWarning("[MonsterUnit] modelPrefab or popupTextAddon is null. Cannot take damage."); }
    }

    /// <summary>
    /// Spawns a floating heal popup near the model to visualize an incoming heal.
    /// Unlike <see cref="TakeDamage"/>, this does not change health itself - the health
    /// change happens in <see cref="UnitInfo.HealHealth"/>.
    /// </summary>
    /// <param name="amtHealed">The positive amount healed, shown in the popup.</param>
    public void PlayHealEffect(int amtHealed)
    {
        if (modelPrefab != null && popupTextAddon != null)
        {
            GameObject facingReference = GetTowerManager().GetFacingReference();
            if (facingReference != null)
            {
                Vector3 modelPosition = modelPrefab.transform.position;
                Vector3 directionToRef = (facingReference.transform.position - modelPosition).normalized;
                Vector3 healPopupPosition = modelPosition + directionToRef * 2f + Vector3.up * 2f;
                popupTextAddon.SpawnPopupText(PopupTextAddon.PopupTextTypes.GainHealth, healPopupPosition, amtHealed);

                if (debug.IsDebugging)
                    Debug.Log($"[MonsterUnit] Healed: {amtHealed}. Popup spawned at {healPopupPosition}.");
            }
            else { Debug.LogWarning("[MonsterUnit] Facing Reference is null. Cannot spawn popup text."); }
        }
        else { Debug.LogWarning("[MonsterUnit] modelPrefab or popupTextAddon is null. Cannot show heal popup."); }
    }

    /// <summary>Resets the action bar to its starting value via <see cref="UnitInfo"/>.</summary>
    public void ResetActionBar()
    {
        unitInfo.ResetActionBar();
        if (debug.IsDebugging) Debug.Log("[MonsterUnit] Action bar reset.");
    }

    /// <summary>
    /// Adds the given amount to the monster's action bar and spawns a floating
    /// energy gain popup near the model. Does nothing if the bar is already full
    /// or the value is negative.
    /// </summary>
    /// <param name="valueToAdd">The positive amount to add to the action bar.</param>
    public void AddToActionBar(int valueToAdd)
    {
        if (valueToAdd < 0) return;
        if (unitInfo.GetActionBar() >= unitInfo.GetActionBarMax()) return;

        unitInfo.AddToActionBar(valueToAdd);

        if (modelPrefab != null && popupTextAddon != null)
        {
            GameObject facingReference = GetTowerManager().GetFacingReference();
            if (facingReference != null)
            {
                Vector3 modelPosition = modelPrefab.transform.position;
                Vector3 directionToRef = (facingReference.transform.position - modelPosition).normalized;
                Vector3 actionPopupPosition = modelPosition + directionToRef * 2f + Vector3.up * 1f;
                popupTextAddon.SpawnPopupText(PopupTextAddon.PopupTextTypes.GainEnergy, actionPopupPosition, valueToAdd);

                if (debug.IsDebugging)
                    Debug.Log($"[MonsterUnit] Added {valueToAdd} to action bar. Popup spawned at {actionPopupPosition}.");
            }
            else { Debug.LogWarning("[MonsterUnit] Facing Reference is null. Cannot spawn popup text."); }
        }
        else { Debug.LogWarning("[MonsterUnit] modelPrefab or popupTextAddon is null. Cannot add to action bar."); }
    }

    // -------------------------------------------------------------------------
    // Getters & Setters
    // -------------------------------------------------------------------------

    /// <summary>
    /// Assigns the monster's unique unit ID and derives the slot position from it.
    /// Expects the format "{teamID}U{slotNumber}" (e.g. "MT1U2").
    /// </summary>
    public void SetMuID(string newMuID)
    {
        if (string.IsNullOrWhiteSpace(newMuID)) { Debug.LogError("Invalid MuID: cannot be null or empty."); return; }

        muID = newMuID;

        string[] parts = newMuID.Split('U');
        if (parts.Length > 1 && int.TryParse(parts[1], out int unitNumber))
        {
            SetSlotPosition(unitNumber);
            if (debug.IsDebugging) Debug.Log($"MuID '{newMuID}' set. SlotPosition set to {unitNumber}.");
        }
        else
        {
            Debug.LogError($"Failed to parse unit number from MuID '{newMuID}'. Format must be e.g. 'MT1U1'.");
        }
    }

    /// <summary>
    /// Sets the 1-based slot position for this monster. Valid range is 1-4.
    /// </summary>
    public void SetSlotPosition(int newSlotPosition)
    {
        if (newSlotPosition < 1 || newSlotPosition > 4)
        {
            Debug.LogError($"Invalid slot position: {newSlotPosition}. Must be between 1 and 4.");
            return;
        }
        slotPosition = newSlotPosition;
        if (debug.IsDebugging) Debug.Log($"Slot position successfully set to {slotPosition}.");
    }

    /// <returns>World-space movement speed in units per second.</returns>
    public float GetMoveSpeed() => moveSpeed;
    /// <param name="newMoveSpeed">New movement speed to assign.</param>
    public void SetMoveSpeed(float newMoveSpeed) => moveSpeed = newMoveSpeed;

    /// <returns>Current animation speed multiplier.</returns>
    public float GetAnimationSpeed() => animationSpeed;
    /// <param name="newAnimationSpeed">New animation speed multiplier to assign.</param>
    public void SetAnimationSpeed(float newAnimationSpeed) => animationSpeed = newAnimationSpeed;

    /// <returns>Remaining time on the death countdown timer.</returns>
    public float GetDyingTimer() => dyingTimer;
    /// <param name="newDyingTimer">New dying timer value to assign.</param>
    public void SetDyingTimer(float newDyingTimer) => dyingTimer = newDyingTimer;

    /// <returns>The monster's <see cref="UnitInfo"/> stat container.</returns>
    public UnitInfo GetUnitInfo() => unitInfo;
    /// <param name="newInfo">New <see cref="UnitInfo"/> to assign.</param>
    public void SetUnitInfo(UnitInfo newInfo) => unitInfo = newInfo;

    /// <returns>The monster's <see cref="MonsterTypes"/> archetype.</returns>
    public MonsterTypes GetMonsterClass() => monsterClass;
    /// <param name="newClass">New monster class to assign.</param>
    public void SetMonsterClass(MonsterTypes newClass) => monsterClass = newClass;

    /// <param name="newModel">The instantiated model GameObject to assign.</param>
    public void SetModel(GameObject newModel) => modelPrefab = newModel;
    /// <returns>The monster's instantiated model GameObject.</returns>
    public GameObject GetModel() => modelPrefab;

    /// <param name="newAnimator">Animator to assign, sourced from the model prefab.</param>
    public void SetAnimator(Animator newAnimator) => animator = newAnimator;
    /// <returns>The Animator component used by this monster.</returns>
    public Animator GetAnimator() => animator;

    /// <param name="newTeam">The <see cref="MonsterTeam"/> to assign.</param>
    public void SetMonsterTeam(MonsterTeam newTeam) => monsterTeam = newTeam;
    /// <returns>The team this monster belongs to.</returns>
    public MonsterTeam GetMonsterTeam() => monsterTeam;

    /// <param name="newAddon">New <see cref="MonsterSlotAddon"/> to assign.</param>
    public void SetSlotAddon(MonsterSlotAddon newAddon) => slotAddon = newAddon;
    /// <returns>The slot addon for this monster's assigned combat slot.</returns>
    public MonsterSlotAddon GetSlotAddon() => slotAddon;

    /// <returns>The 1-based slot index this monster occupies.</returns>
    public int GetSlotPosition() => slotPosition;
    /// <returns>The unique unit ID string for this monster.</returns>
    public string GetMuID() => muID;

    /// <returns><c>true</c> if this unit is the boss monster.</returns>
    public bool GetIsBoss() => isBoss;
    private void SetIsBoss(bool isBossUnit) { isBoss = isBossUnit; GetUnitInfo().SetIsBoss(isBossUnit); }

    /// <returns>The current <see cref="MonsterAnimations"/> state.</returns>
    public MonsterAnimations GetAnimationState() => animationState;

    /// <param name="newMonsterAddon">New <see cref="MonsterUnitPrefabAddon"/> to assign.</param>
    public void SetMonsterAddon(MonsterUnitPrefabAddon newMonsterAddon) => monsterAddon = newMonsterAddon;
    /// <returns>The prefab-side addon managing this monster's UI and visual components.</returns>
    public MonsterUnitPrefabAddon GetMonsterAddon() => monsterAddon;

    /// <param name="newPopupTextAddon">New <see cref="PopupTextAddon"/> to assign.</param>
    public void SetPopupTextAddon(PopupTextAddon newPopupTextAddon) => popupTextAddon = newPopupTextAddon;
    /// <returns>The addon used to spawn floating popup text.</returns>
    public PopupTextAddon GetPopupTextAddon() => popupTextAddon;

    /// <summary>Sets the monster's active state, toggling the GameObject on or off.</summary>
    public void SetIsActive(bool newActive) { isActive = newActive; gameObject.SetActive(isActive); }
    /// <returns><c>true</c> if the monster is currently active.</returns>
    public bool GetIsActive() => isActive;

    /// <param name="newIsDying">New dying state to assign.</param>
    public void SetIsDying(bool newIsDying) => isDying = newIsDying;
    /// <returns><c>true</c> while the monster is in its death sequence.</returns>
    public bool GetIsDying() => isDying;

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

    // -------------------------------------------------------------------------
    // Private / Legacy Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Retrieves the <see cref="MonsterUnitPrefabAddon"/> from the model prefab.
    /// Currently unused in active code paths.
    /// </summary>
    private MonsterUnitPrefabAddon GetUnitAddon()
    {
        if (modelPrefab == null) { Debug.LogError("[MonsterUnit] modelPrefab is null."); return null; }
        return modelPrefab.GetComponent<MonsterUnitPrefabAddon>();
    }

    /// <summary>
    /// Re-validates that the Animator reference is set.
    /// Currently unused in active code paths.
    /// </summary>
    private void UpdateAnimator()
    {
        if (animator == null) Debug.LogError("[MonsterUnit] Animator not set in Monster Unit.");
        else if (debug.IsDebugging) Debug.Log("[MonsterUnit] Animator successfully updated.");
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MonsterUnit;

public class TrapUnit : MonoBehaviour
{
    
    //#region // Variables
    //#region // Enums
    //public enum TrapTypes
    //{
    //    Bow,
    //}
    //
    //public enum TrapAnimations
    //{
    //    Idle,
    //    Active,
    //}
    //
    //#endregion
    //#region // Unit Info
    //[Header("Unit Info")]
    //[SerializeField] private UnitInfo unitInfo;
    //[SerializeField] private TrapTypes trapType;
    //[SerializeField] private int tuID;
    //[SerializeField] private bool isActive = false;
    //[SerializeField] private bool isDying = false;
    //[SerializeField] private float dyingTimer;
    //#endregion
    //#region // Model
    //[Header("Model")]
    //[SerializeField] private GameObject modelPrefab;
    //[SerializeField] private Animator animator;
    //[SerializeField] private TrapAnimations animationState;
    //
    //#endregion
    //#region // References
    //[Header("References")]
    //[SerializeField] private HeroTeam heroTeam; // Link to my team
    //[SerializeField] private TrapUnitPrefabAddon trapAddon;
    //[SerializeField] private PopupTextAddon popupTextAddon;
    //[SerializeField] private TowerDefenseManager GameManager; // Link to Tower Manager
    //[SerializeField] private GameObject DeathVfx; // Link to Tower Manager
    //#endregion
    //#region // Debugging
    //[Header("Debugging")]
    //[SerializeField] private DebugUtility debug;
    //#endregion
    //#endregion
    /*
    #region // Methods ==================
    #region // Unity Functions
    private void Start()
    {

    }
    private void Update()
    {
        //if (GetUnitInfo() != null)
        //{
        //    if (GetUnitInfo().Health <= 0)
        //    {
        //        StartDying();
        //        if (isDying)
        //        {
        //            if (dyingTimer > 0)
        //            {
        //                dyingTimer -= Time.deltaTime;
        //            } 
        //            else
        //            {
        //                ExecuteDeath();
        //            }
        //        }
        //    }
        //}
    }
    #endregion
    #region // User Functions
    #region // Initalization
    public void InitializeHero(TowerDefenseManager newManager, HeroTeam newHeroTeam, int huId, HeroClass newClass, int level)
    {
        #region// Initialize debug object
        debug = new DebugUtility(false);
        #endregion

        #region // Debugging Ouput (Start)
        if (debug.IsDebugging)
        {

        }
        #endregion

        #region // Set the Monster Unit ID
        SetHuID(huId);
        #endregion

        #region // Link the Tower Defense manager
        SetGameManager(newManager);
        if (debug.IsDebugging) { Debug.Log($"TowerDefenseManager set to: {newManager}"); }
        #endregion

        #region // Link this object to its team
        SetHeroTeam(newHeroTeam);
        if (debug.IsDebugging) { Debug.Log($"HeroTeam set to: {newHeroTeam}"); }
        #endregion

        #region // Create a unit info (link it to this object)
        SetUnitInfo(new UnitInfo(this, level));
        if (debug.IsDebugging) { Debug.Log($"UnitInfo created with level {level} and linked to hero."); }
        #endregion

        #region // Update your monster class
        SetHeroClass(newClass);
        if (debug.IsDebugging) { Debug.Log($"Hero class set to: {newClass}"); }
        #endregion

        #region // Initialize the stats
        InitializeHeroStats();
        if (debug.IsDebugging) { Debug.Log("Hero stats initialized."); }
        #endregion

        #region // Spawn the character
        SpawnHero();
        if (debug.IsDebugging) { Debug.Log("Hero spawning process triggered."); }
        #endregion

        #region // Debugging Ouput (End)
        if (debug.IsDebugging) { Debug.Log("Hero initialization complete."); }
        #endregion

    }
    public void SpawnHero()
    {
        #region// Create from hero prefab
        // Pull the hero manager
        HeroManager heroManager = GetHeroManager();
        // Get the Model based on your class
        GameObject heroPrefab =  heroManager.GetHeroModelPrefab(heroClass);
        // Create the prefab at the location
        GameObject heroObject = Instantiate(heroPrefab);
        // Parent the transform to the unit transform
        heroObject.transform.SetParent(this.gameObject.transform);
        //Set the model to this 
        SetModel(heroObject);
        #endregion
        #region// Link Addons and Animator from prefab
        SetHeroAddon(heroObject.GetComponent<HeroUnitPrefabAddon>());
        if (GetHeroAddon() == null) SetHeroAddon(heroObject.GetComponentInChildren<HeroUnitPrefabAddon>());
        // Initali the hero add on
        GetHeroAddon().InitializeSliders(this);
        SetPopupTextAddon(heroObject.GetComponent<PopupTextAddon>());
        if (GetPopupTextAddon() == null) SetPopupTextAddon(heroObject.GetComponentInChildren<PopupTextAddon>());
        SetAnimator(GetHeroAddon().GetAnimator());
        #endregion
        #region // Output error Checking
        if (GetHeroAddon() == null) Debug.LogError("Error! Unable to find GetHeroAddon on object or children");
        if (GetPopupTextAddon() == null) Debug.LogError("Error! Unable to find GetPopupTextAddon on object or children");
        if (GetAnimator() == null) Debug.LogError("Error! Unable to find animator on object or children");
        #endregion
    }
    private void InitializeHeroStats()
    {
        switch (heroClass)
        {
            case HeroClass.Guard:
                unitInfo.SetOffense(3);
                unitInfo.SetDefense(5);
                unitInfo.SetUtility(2);
                unitInfo.SetMaxHealth(10);
                unitInfo.SetAction(AbilityId.Attack);
                break;
            case HeroClass.Archer:
                unitInfo.SetOffense(6);
                unitInfo.SetDefense(2);
                unitInfo.SetUtility(3);
                unitInfo.SetMaxHealth(5);
                unitInfo.SetAction(AbilityId.Attack);
                break;
            case HeroClass.Mage:
                unitInfo.SetOffense(5);
                unitInfo.SetDefense(1);
                unitInfo.SetUtility(4);
                unitInfo.SetMaxHealth(5);
                break;
            case HeroClass.Cleric:
                unitInfo.SetOffense(2);
                unitInfo.SetDefense(3);
                unitInfo.SetUtility(6);
                unitInfo.SetMaxHealth(5);
                break;
            case HeroClass.DarkKnight:
                unitInfo.SetOffense(7);
                unitInfo.SetDefense(3);
                unitInfo.SetUtility(1);
                unitInfo.SetMaxHealth(5);
                break;
            case HeroClass.Warrior:
                unitInfo.SetOffense(5);
                unitInfo.SetDefense(4);
                unitInfo.SetUtility(2);
                unitInfo.SetMaxHealth(5);
                break;
            case HeroClass.Paladin:
                unitInfo.SetOffense(5);
                unitInfo.SetDefense(4);
                unitInfo.SetUtility(2);
                unitInfo.SetMaxHealth(5);
                break;
        }

        // Calculate sub stats
        unitInfo.SetHealth(unitInfo.MaxHealth);
        unitInfo.SetPhysicalDamage(unitInfo.Offense * 5);
        unitInfo.SetMagicDamage(unitInfo.Offense * 0.05f);
        unitInfo.SetArmor(unitInfo.Defense * 4);
        unitInfo.SetResistance(unitInfo.Defense * 0.5f);
        unitInfo.SetActionSpeed(unitInfo.Defense * 10);
        unitInfo.SetEscapeSpeed(unitInfo.Utility * 2);
        unitInfo.SetActionBarSpeed(1);
        // Run Reset values
        ResetActionBar();

        if (debug.IsDebugging)
            Debug.Log($"[HeroUnit] Hero initialization complete. Stats: Offense {unitInfo.Offense}, Defense {unitInfo.Defense}, Utility {unitInfo.Utility}.");
    }
    #endregion
    #region // Move Unit
    public IEnumerator MoveHeroAlongPath(List<GameObject> path)
    {
        // Assign the path to the serialized field for Inspector visibility
        currentPath = path;

        if (debug.IsDebugging)
            Debug.Log($"Starting MoveHeroAlongPath for hero '{name}' with path containing {path.Count} points.");

        foreach (GameObject targetPoint in path)
        {
            if (targetPoint == null)
            {
                if (debug.IsDebugging)
                    Debug.LogError("Target point in path is null. Skipping.");
                continue;
            }

            if (debug.IsDebugging)
                Debug.Log($"Hero '{name}' moving towards target point: {targetPoint.name} at position {targetPoint.transform.position}.");

            // Start moving to the next target
            yield return StartCoroutine(MoveToTarget(targetPoint));

            if (debug.IsDebugging)
                Debug.Log($"Hero '{name}' reached target point: {targetPoint.name}.");
        }

        if (debug.IsDebugging)
            Debug.Log($"Hero '{name}' completed MoveHeroAlongPath.");

        // After the hero finishes moving, set animation back to Idle
        SetAnimationState(HeroUnit.HeroAnimations.Idle);

        if (debug.IsDebugging)
            Debug.Log($"Hero '{name}' animation state set to Idle.");

        // Clear the path after use (optional)
        currentPath = null;
    }
    public IEnumerator MoveToTarget(GameObject targetObject)
    {
        GameObject hero = this.gameObject;

        if (targetObject == null)
        {
            if (debug.IsDebugging)
                Debug.LogError($"Target object is null for hero '{name}'. Aborting MoveToTarget.");
            yield break;
        }

        // Set nextTargetObject for tracking in the Inspector
        nextTargetObject = targetObject;

        if (debug.IsDebugging)
            Debug.Log($"Hero '{name}' starting MoveToTarget towards '{targetObject.name}' at position {targetObject.transform.position}.");

        // After the hero finishes moving, set animation back to Idle
        SetAnimationState(HeroAnimations.Run);

        // Continue moving until the hero is very close to the target
        while (Vector3.Distance(hero.transform.position, targetObject.transform.position) > 0.1f)
        {
            float remainingDistance = Vector3.Distance(hero.transform.position, targetObject.transform.position);
            if (debug.IsDebugging)
                Debug.Log($"Hero '{name}' is {remainingDistance} units away from target '{targetObject.name}'.");

            // Overshoot the target position slightly
            Vector3 overshootPosition = targetObject.transform.position + (targetObject.transform.position - hero.transform.position).normalized * 0.5f;

            // Draw multiple lines around the hero's current position for thickness
            Debug.DrawLine(hero.transform.position, overshootPosition, Color.green, 0f, false);
            Debug.DrawLine(hero.transform.position + Vector3.up * 0.2f, overshootPosition + Vector3.up * 0.2f, Color.green, 0f, false);
            Debug.DrawLine(hero.transform.position + Vector3.right * 0.2f, overshootPosition + Vector3.right * 0.2f, Color.green, 0f, false);

            // Move the hero towards the target at a constant speed
            hero.transform.position = Vector3.MoveTowards(
                hero.transform.position,
                targetObject.transform.position,
                moveSpeed * Time.deltaTime
            );

            // Smoothly rotate towards the target's rotation
            hero.transform.rotation = Quaternion.Lerp(
                hero.transform.rotation,
                targetObject.transform.rotation,
                rotationSpeed * Time.deltaTime
            );

            if (debug.IsDebugging)
                Debug.Log($"Hero '{name}' current position: {hero.transform.position}, rotation: {hero.transform.rotation}");

            yield return null;
        }

        // Snap to exact position and rotation to ensure precision
        hero.transform.position = targetObject.transform.position;
        hero.transform.rotation = targetObject.transform.rotation;

        if (debug.IsDebugging)
            Debug.Log($"Hero '{name}' snapped to target '{targetObject.name}' at exact position {targetObject.transform.position}, rotation {targetObject.transform.rotation}.");

        // Clear nextTargetObject after reaching the target
        nextTargetObject = null;
    }
    #endregion
    #region // Unit Actions
    public void ExecuteAction(UnitInfo user, MonsterTeam hostileTeam = null, HeroTeam friendlyTeam = null)
    {
        if (debug.IsDebugging)
            Debug.Log("[HeroUnit] Hero is executing action.");
        // Initialize the list to hold targeted monsters (Unit info)
        List<UnitInfo> hostileTargets = new List<UnitInfo>();
        List<UnitInfo> friendlyTargets = new List<UnitInfo>();
        foreach (MonsterUnit monster in hostileTeam.GetMonsters()) { hostileTargets.Add(monster.GetUnitInfo()); }
        foreach (HeroUnit hero in friendlyTeam.GetHeroes()) { friendlyTargets.Add(hero.GetUnitInfo()); }
        // Pass Targets along
        unitInfo.ExecuteAction(user, hostileTargets, friendlyTargets);
        SetAnimationState(HeroAnimations.Action1);
    }
    public void ExecuteHeroHitBoss(UnitInfo user, MonsterTeam hostileTeam = null, HeroTeam friendlyTeam = null)
    {
        List<UnitInfo> hostileTargets = new List<UnitInfo>();
        List<UnitInfo> friendlyTargets = new List<UnitInfo>();
        foreach (MonsterUnit monster in hostileTeam.GetMonsters()) { hostileTargets.Add(monster.GetUnitInfo()); }
        foreach (HeroUnit hero in friendlyTeam.GetHeroes()) { friendlyTargets.Add(hero.GetUnitInfo()); }

        user.ExecuteAction(user, hostileTargets, friendlyTargets);
        SetAnimationState(HeroAnimations.Action1);
    }
    public void ResetActionBar()
    {
        unitInfo.ResetActionBar();

        if (debug.IsDebugging)
            Debug.Log("[HeroUnit] Action bar reset.");
    }
    public void AddToActionBar(int valueToAdd)
    {
        if (valueToAdd < 0) return; // No amount to add
        if (unitInfo.ActionBar >= unitInfo.ActionBarMax) return; // Return if we're already full

        // Add the value to the action bar
        unitInfo.AddToActionBar(valueToAdd);

        if (modelPrefab != null && popupTextAddon != null)
        {
            // Get the facing reference object (the target towards which the model is facing)
            GameObject facingReference = GetTowerManager().GetFacingReference();

            // Ensure facingReference is not null
            if (facingReference != null)
            {
                // Get the current position of the modelPrefab
                Vector3 modelPosition = modelPrefab.transform.position;

                // Calculate the direction vector from modelPrefab to the facingReference
                Vector3 directionToReference = (facingReference.transform.position - modelPosition).normalized;

                // Calculate the new position 2 units away from modelPrefab towards the facingReference
                // and also 2 units up (along the Y-axis)
                Vector3 actionPopupPosition = modelPosition + directionToReference * 2f + Vector3.up * 1f;

                // Spawn the popup text at the calculated position
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
    #endregion
    #region // Unit Damage
    public void TakeDamage(int damage)
    {
        if (modelPrefab != null && popupTextAddon != null)
        {
            // Get the facing reference object (the target towards which the model is facing)
            GameObject facingReference = GetTowerManager().GetFacingReference();

            // Ensure facingReference is not null
            if (facingReference != null)
            {
                // Get the current position of the modelPrefab
                Vector3 modelPosition = modelPrefab.transform.position;

                // Calculate the direction vector from modelPrefab to the facingReference
                Vector3 directionToReference = (facingReference.transform.position - modelPosition).normalized;

                // Calculate the new position 2 units away from modelPrefab towards the facingReference
                // and also 2 units up (along the Y-axis)
                Vector3 damagePopupPosition = modelPosition + directionToReference * 2f + Vector3.up * 2f;

                // Spawn the popup text at the calculated position
                popupTextAddon.SpawnPopupText(PopupTextAddon.PopupTextTypes.LoseHealth, damagePopupPosition, damage);

                if (debug.IsDebugging)
                    Debug.Log($"[HeroUnit] Took damage: {damage}. Popup text spawned at position {damagePopupPosition}.");
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
    }
    #endregion
    #region // Unit Death
    public void StartDying()
    {
        // Make sure not already dying
        if (isDying)
        {
            Debug.LogWarning("Already dying, exiting StartDying.");
            return;
        }

        isDying = true; // Mark as dying
        dyingTimer = 50f;
        Debug.Log("Starting the dying process.");

        // Disable/Hide your sliders
        Debug.Log("Disabling health and action bar sliders.");
        GetHeroAddon().GetHealthBarSlider().gameObject.SetActive(false);
        GetHeroAddon().GetActionBarSlider().gameObject.SetActive(false);

        // Get Tower Death Points
        Debug.Log("Getting tower death points.");
        List<GameObject> deathpoints = GetTowerManager().GetTowerSectionAddon(GetTowerManager().GetFloor(GetHeroTeam().GetCurrentFloor())).GetDeathPoints();

        // Find the closest of the Death Points
        GameObject closestDeathPoint = FindRandomDeathPoint(deathpoints);
        Debug.Log("Closest death point found: " + closestDeathPoint.name);

        // Start the move coroutine to the death point
        Debug.Log("Starting move to the death point.");
        StartCoroutine(MoveToDeathPointCoroutine(closestDeathPoint));

        // After death point is reached, create death VFX and blast out of tower
        // This will run sequentially after the move to the death point
    }
    private IEnumerator MoveToDeathPointCoroutine(GameObject deathPoint)
    {
        // Use a fast movement method, possibly with a coroutine
        Vector3 direction = (deathPoint.transform.position - transform.position).normalized;
        float speed = 20f; // Adjust speed as necessary
        float distance = Vector3.Distance(transform.position, deathPoint.transform.position);

        while (distance > 0.1f) // Move until near death point
        {
            transform.position = Vector3.MoveTowards(transform.position, deathPoint.transform.position, speed * Time.deltaTime);
            distance = Vector3.Distance(transform.position, deathPoint.transform.position);
            yield return null;
        }

        Debug.Log("Reached the death point.");

        // Once the death point is reached, spawn death VFX
        SpawnDeathVFX(deathPoint.transform.position);

        // Then to simulate the character being blasted out of the tower
        Debug.Log("Blasting character out of the tower.");
        StartCoroutine(BlastOutOfTowerCoroutine(deathPoint.transform));
    }
    private IEnumerator BlastOutOfTowerCoroutine(Transform deathPointTransform)
    {
        // Apply random upward and forward velocity
        Vector3 forward = deathPointTransform.forward;
        float upVelocity = UnityEngine.Random.Range(10f, 15f); // Random up velocity
        float forwardVelocity = UnityEngine.Random.Range(20f, 50f); // Random forward velocity

        // Start a coroutine to move the character manually
        float elapsedTime = 0f;
        float duration = 2f; // Duration to move the character (adjust as needed)

        Vector3 initialPosition = transform.position;
        Vector3 velocity = new Vector3(forward.x * forwardVelocity, upVelocity, forward.z * forwardVelocity);

        while (elapsedTime < duration)
        {
            // Update the position manually each frame
            transform.position += velocity * Time.deltaTime;

            // Apply gravity (decrease the upward velocity over time)
            velocity.y -= 9.81f * Time.deltaTime; // Simulate gravity (you can adjust the gravity effect)

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Debug.Log("Blast out of tower completed.");
        // After moving out of the tower, execute the death
        StartCoroutine(FadeOutAndExecuteDeath());
    }
    private IEnumerator FadeOutAndExecuteDeath()
    {
        // Wait for 50 units of time (or whatever unit you mean by "50 units")
        float timeToWait = 5f;
        float elapsedTime = 0f;

        while (elapsedTime < timeToWait)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Debug.Log("50 units of time passed, executing death.");
        ExecuteDeath();
    }
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
    private GameObject FindRandomDeathPoint(List<GameObject> deathpoints)
    {
        // Check if the deathpoints list is not empty
        if (deathpoints.Count == 0)
        {
            Debug.LogWarning("No death points available.");
            return null;
        }

        // Select a random death point from the list
        int randomIndex = UnityEngine.Random.Range(0, deathpoints.Count);
        GameObject randomDeathPoint = deathpoints[randomIndex];

        Debug.Log("Random death point selected: " + randomDeathPoint.name);
        return randomDeathPoint;
    }
    private void MoveToDeathPoint(GameObject deathPoint)
    {
        // Use a fast movement method, possibly with a coroutine or by setting velocity directly
        Vector3 direction = (deathPoint.transform.position - transform.position).normalized;
        float speed = 20f; // Adjust speed as necessary
        transform.position = Vector3.MoveTowards(transform.position, deathPoint.transform.position, speed * Time.deltaTime);
    }
    private void SpawnDeathVFX(Vector3 deathPoint)
    {
        // Instantiate the death VFX prefab at the death point location
        if (DeathVfx == null) return;
        Instantiate(DeathVfx, deathPoint, Quaternion.identity);
    }
    private void BlastOutOfTower(Transform deathPointTransform)
    {
        // Apply random upward and forward velocity
        Vector3 forward = deathPointTransform.forward;
        float upVelocity = UnityEngine.Random.Range(2f, 5f); // Random up velocity
        float forwardVelocity = UnityEngine.Random.Range(2f, 5f); // Random forward velocity

        // Start a coroutine to move the character manually
        StartCoroutine(MoveCharacterOutOfTower(forward, upVelocity, forwardVelocity));
    }
    private IEnumerator MoveCharacterOutOfTower(Vector3 forwardDirection, float upVelocity, float forwardVelocity)
    {
        float elapsedTime = 0f;
        float duration = 2f; // Duration to move the character (adjust as needed)

        Vector3 initialPosition = transform.position;
        Vector3 velocity = new Vector3(forwardDirection.x * forwardVelocity, upVelocity, forwardDirection.z * forwardVelocity);

        while (elapsedTime < duration)
        {
            // Update the position manually each frame
            transform.position += velocity * Time.deltaTime;

            // Apply gravity (decrease the upward velocity over time)
            velocity.y -= 9.81f * Time.deltaTime; // Simulate gravity (you can adjust the gravity effect)

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
    private void ExecuteDeath()
    {
        // Handle actual death logic (destroy unit, trigger animations, etc.)

        // Destroy Model
        Destroy(GetModel());

        // Remove this unit from the Hero Team
        GetHeroTeam().RemoveUnitFromTeam(GetNumberInTeam());
    }
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
    #endregion
    #region // Animations
    private void UpdateAnimatorParameter()
    {
        if (animator == null)
        {
            Debug.LogError("[HeroUnit] Animator is not assigned. Cannot update animation parameters.");
            return;
        }

        int animationInt = animationState switch
        {
            HeroAnimations.Idle => 13,
            HeroAnimations.Run => 20,
            HeroAnimations.Action1 => 2,
            HeroAnimations.Action2 => 3,
            HeroAnimations.Action3 => 4,
            HeroAnimations.Action4 => 5,
            HeroAnimations.TakeHit => 8,
            HeroAnimations.Die => 10,
            HeroAnimations.Tired => 30,
            HeroAnimations.Victory => 32,
            _ => 0
        };

        animationInt = Mathf.Clamp(animationInt, 1, 31);
        animator.SetInteger("animation", animationInt);

        if (debug.IsDebugging)
            Debug.Log($"[HeroUnit] Animator parameter set to {animationInt}.");
    }
    #endregion
    #region // Setters & Getters
    public UnitInfo GetUnitInfo() { return unitInfo; }
    public HeroClass GetHeroClass() { return heroClass; }
    public void SetModel(GameObject newModel)  {  modelPrefab = newModel; }
    public GameObject GetModel() { return modelPrefab; }
    public void SetAnimator(Animator newAnimator) { animator = newAnimator; }
    public Animator GetAnimator() { return animator; }
    public void SetAnimationState(HeroAnimations newState) { animationState = newState; UpdateAnimatorParameter(); }
    public HeroAnimations GetAnimationState() { return animationState; }
    public int GetHuID() => huID;
    public void SetHuID(int newHuID) => huID = newHuID;
    public void SetHeroAddon(HeroUnitPrefabAddon newHeroAddon) { heroAddon = newHeroAddon; }
    public HeroUnitPrefabAddon GetHeroAddon() { return heroAddon; }
    public void SetPopupTextAddon(PopupTextAddon newPopupTextAddon) { popupTextAddon = newPopupTextAddon; }
    public PopupTextAddon GetPopupTextAddon() { return popupTextAddon; }
    public void SetIsActive(bool newActive) 
    { 
        isActive = newActive; 
        if (!isActive)
        {
            gameObject.SetActive(false);
        } 
        else
        {
            gameObject.SetActive(true);
        }
    }
    public bool GetIsActive() { return isActive; }
    public void SetHeroTeam(HeroTeam newTeam) => heroTeam = newTeam;
    public HeroTeam GetHeroTeam() => heroTeam;
    public void SetNumberInTeam(int newTeamNumber) => numberInTeam = newTeamNumber;
    public int GetNumberInTeam() => numberInTeam;
    public void SetUnitInfo(UnitInfo newInfo) => unitInfo = newInfo;
    public void SetHeroClass(HeroClass newClass) => heroClass = newClass;
    public void SetGameManager(TowerDefenseManager newManager) => GameManager = newManager;
    public TowerDefenseManager GetGameManager() => GameManager;
    public TowerManager GetTowerManager() { return GetGameManager().GetTowerManager(); }
    public HeroManager GetHeroManager() { return GetGameManager().GetHeroManager(); }
    public MonsterManager GetMonsterManager() { return GetGameManager().GetMonsterManager(); }
    #endregion
    #endregion
    
#endregion =====================
    */
}

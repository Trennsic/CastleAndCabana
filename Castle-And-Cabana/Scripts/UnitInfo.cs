using System.Collections.Generic;
using UnityEngine;
using static HeroUnit;

[System.Serializable]
public class UnitInfo
{
    #region // Attributes
    // Debugging
    private bool isDebugging = false;

    // Basic Info
    public enum UnitTypes
    {
        Monster,
        Hero,
    }
    [SerializeField]private UnitTypes unitType;
    [SerializeField]private MonsterUnit monsterUnit;
    [SerializeField]private HeroUnit heroUnit;
    [SerializeField]private int level;
    [SerializeField]private int health;
    [SerializeField]private string name;
    [SerializeField]private bool isBoss;
    //Action Bar
    [SerializeField]private int actionBar;
    [SerializeField]private int actionBarMax; 
    [SerializeField]private int actionBarSpeed;
    [SerializeField]private int actionBarRollover; 
    // Main stats
    [SerializeField]private int power; 
    [SerializeField] private int maxHealth;
    [SerializeField]private int speed;
    // Abilities
    [SerializeField] private AbilityId actionAbility;
    [SerializeField] private List<AbilityId> passiveAbilities = new List<AbilityId>();
    #endregion
    #region // Methods
    #region// Public Setters
    public void SetUnitType(UnitTypes unitType) => this.unitType = unitType;
    public void SetHeroUnit(HeroUnit unit) => heroUnit = unit;
    public void SetMonsterUnit(MonsterUnit unit) => monsterUnit = unit;
    public void SetIsBoss(bool unitIsBoss) => isBoss = unitIsBoss;
    // Set Basic Info
    public void SetLevel(int value) => level = Mathf.Max(1, value);
    public void SetName(string newName) => name = newName;
    // Set ActionBar info
    public void SetActionBar(int value) => actionBar = Mathf.Clamp(value, 0, actionBarMax);
    public void SetActionBarMax(int value) => actionBarMax = value;
    public void SetActionBarSpeed(int value) => actionBarSpeed = value;
    public void SetActionBarRollover(int value) => actionBarRollover = value;
    // Set Main Stats
    public void SetPower(int value) => power = value;
    public void SetMaxHealth(int value) => maxHealth = value;
    public void SetSpeed(int value) => speed = value;
    public void SetHealth(int value) => health = Mathf.Clamp(value, 0, maxHealth);
    // Set abilites
    public void SetPassives(List<AbilityId> ids) => passiveAbilities = ids ?? new List<AbilityId>();
    public void AddPassive(AbilityId id) { if (!passiveAbilities.Contains(id)) passiveAbilities.Add(id); }
    public void SetAction(AbilityId id)
    {
        actionAbility = id;
        AbilityDefinition def = AbilityRegistry.Get(id);
        if (def == null) { Debug.LogError($"[UnitInfo] SetAction: no definition for {id}."); return; }
        SetActionBarMax(def.ActionPointCost);
    }
    #endregion
    #region // Public Getters
    public UnitTypes GetUnitType() => this.unitType;
    public HeroUnit GetHeroUnit() => heroUnit;
    public MonsterUnit GetMonsterUnit() => monsterUnit;
    public bool GetIsBoss() => isBoss;
    // Get Basic Info
    public int GetLevel() => level;
    public string GetName() => name;
    // Get ActionBar info
    public int GetActionBar() => actionBar;
    public int GetActionBarMax() => actionBarMax;
    public int GetActionBarSpeed() => actionBarSpeed;
    public int GetActionBarRollover() => actionBarRollover;
    // Get Main Stats
    public int GetPower() => power;
    public int GetMaxHealth() => maxHealth;
    public int GetSpeed() => speed;
    public int GetHealth() => health;
    // Get abilites
    public List<AbilityId> GetPassives() => passiveAbilities;
    public AbilityId GetAction() => actionAbility;
    #endregion
    #region // Constructors
    // Parameterized Constructor
    public UnitInfo(HeroUnit unit, int level)
    {
        SetHeroUnit(unit);
        SetIsBoss(false);
        InitializeUnit(UnitTypes.Hero, level); // Initialize hero with passed values
    }
    public UnitInfo(MonsterUnit unit, int level)
    {
        SetMonsterUnit(unit);
        InitializeUnit(UnitTypes.Monster, level); // Initialize monster with passed values
    }
    #endregion
    #region// Hero Initialization
    private void InitializeUnit(UnitTypes unitType, int level)
    {
        // Stats and sub stats will need to be set in each Hero/Monster class
        SetUnitType(unitType);
        SetLevel(level);
    }
    #endregion
    #region // Action Bar Managements
    public void ResetActionBar()
    {
        string debug = "";
        if (isDebugging) debug += ($"Resetting Action Bar - Initial actionBar: {actionBar}. ");
        // Reset the action Bar
        actionBar = 0;
        if (isDebugging) debug += ($"actionBar set to 0. ");
        // If there is any rollover save that amount
        int rollover = actionBarRollover;
        if (isDebugging) debug += ($"Rolling over {rollover} points. ");
        // Reset the base rollover value
        actionBarRollover = 0;
        // Readd old rollover to action bar, allowing for new rollover to happen
        AddToActionBar(rollover);
        // Put out the Debug
        if (isDebugging) Debug.Log(debug);
    }
    public void AddToActionBar(int valueToAdd)
    {
        string debug = "";
        if (isDebugging) debug += ($"Adding {valueToAdd} to actionBar. Initial actionBar: {actionBar}. ");

        // Add the value to actionBar
        actionBar += valueToAdd;

        // Cutt off overflor
        if (actionBar > actionBarMax) { actionBar = actionBarMax; }


        if (isDebugging) Debug.Log(debug);
    }
    #endregion
    #region // Damage
    public void ExecuteAction(UnitInfo user, List<UnitInfo> hostileTargets = null, List<UnitInfo> friendlyTargets = null)
    {
        AbilityDefinition def = AbilityRegistry.Get(actionAbility);
        if (def == null) { Debug.LogError($"[UnitInfo] ExecuteAction: no definition for {actionAbility}."); return; }

        foreach (AbilityEffect effect in def.Effects)
        {
            // Core effects are handled by ExecuteAbilityCoreHit, not here
            if (effect.Target == EffectTarget.Core) continue;

            float value = effect.ScalingStat switch
            {
                StatSource.Flat      => effect.Multiplier,
                StatSource.Power     => user.power     * effect.Multiplier,
                StatSource.MaxHealth => user.maxHealth * effect.Multiplier,
                StatSource.Speed     => user.speed     * effect.Multiplier,
                _                   => effect.Multiplier,
            };
            int intValue = (int)Mathf.Ceil(value);

            switch (effect.Target)
            {
                case EffectTarget.Self:
                    ApplyEffect(effect.Type, user, intValue);
                    break;
                case EffectTarget.SingleHostile:
                    if (hostileTargets != null && hostileTargets.Count > 0)
                        ApplyEffect(effect.Type, hostileTargets[0], intValue);
                    break;
                case EffectTarget.AllHostile:
                    if (hostileTargets != null)
                        foreach (UnitInfo t in hostileTargets) ApplyEffect(effect.Type, t, intValue);
                    break;
                case EffectTarget.SingleFriendly:
                    if (friendlyTargets != null && friendlyTargets.Count > 0)
                        ApplyEffect(effect.Type, friendlyTargets[0], intValue);
                    break;
                case EffectTarget.AllFriendly:
                    if (friendlyTargets != null)
                        foreach (UnitInfo t in friendlyTargets) ApplyEffect(effect.Type, t, intValue);
                    break;
            }
        }
    }

    private void ApplyEffect(EffectType type, UnitInfo target, int value)
    {
        switch (type)
        {
            case EffectType.Damage:    target.TakeDamage(value); break;
            case EffectType.Heal:      target.HealHealth(value); break;
            default: Debug.Log($"[UnitInfo] Buff effect {type} not yet implemented."); break;
        }
    }

    public void ExecuteAbilityCoreHit(UnitInfo user, CoreUnit core)
    {
        if (core == null) { Debug.LogError("[UnitInfo] ExecuteAbilityCoreHit: core is null."); return; }

        AbilityDefinition def = AbilityRegistry.Get(actionAbility);
        if (def == null) { Debug.LogError($"[UnitInfo] ExecuteAbilityCoreHit: no definition for {actionAbility}."); return; }

        foreach (AbilityEffect effect in def.Effects)
        {
            if (effect.Target != EffectTarget.Core) continue;

            float value = effect.ScalingStat switch
            {
                StatSource.Flat      => effect.Multiplier,
                StatSource.Power     => user.power     * effect.Multiplier,
                StatSource.MaxHealth => user.maxHealth * effect.Multiplier,
                StatSource.Speed     => user.speed     * effect.Multiplier,
                _                   => effect.Multiplier,
            };
            core.TakeDamage((int)Mathf.Ceil(value));
            return;
        }
    }
    public void TakeDamage(int damage)
    {
        health -= damage;
        if (unitType == UnitTypes.Hero) { heroUnit.TakeDamage(damage); }
        else if (unitType == UnitTypes.Monster) { monsterUnit.TakeDamage(damage); }
    }
    /// <summary>
    /// Heals the unit by <paramref name="amtHealed"/>, clamped so health cannot exceed
    /// <see cref="GetMaxHealth"/> (no overheal). Does nothing for a non-positive amount,
    /// a unit already at 0 health, or a unit mid-death-sequence. Shows a heal popup via
    /// the unit's own <c>PlayHealEffect</c> rather than routing through
    /// <see cref="TakeDamage"/>, so no hit animation or death check is triggered.
    /// </summary>
    /// <param name="amtHealed">The positive amount of health to restore.</param>
    public void HealHealth(int amtHealed)
    {
        if (amtHealed <= 0) return;
        if (health <= 0) return;
        if (unitType == UnitTypes.Hero && heroUnit.GetIsDying()) return;
        if (unitType == UnitTypes.Monster && monsterUnit.GetIsDying()) return;

        SetHealth(health + amtHealed);

        if (unitType == UnitTypes.Hero) { heroUnit.PlayHealEffect(amtHealed); }
        else if (unitType == UnitTypes.Monster) { monsterUnit.PlayHealEffect(amtHealed); }
    }
    #endregion
    #endregion
}

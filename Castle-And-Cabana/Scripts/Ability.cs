using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// Enumerations
// =============================================================================

/// <summary>
/// Canonical registry of every ability identifier, shared by heroes and monsters.
/// </summary>
public enum AbilityId
{
    Attack,
    Shoot,
    Heal,
    Shield,
    Block,
    Soothe,
    Escape,
    HitBoss,
    BossSwipe,
    CoreHit,

    #region // Monster-unique abilities
    // Reserved for monster-only entries
    #endregion
}

/// <summary>
/// Controls when an ability is triggered during combat.
/// Replaces the old <c>AbilityType</c> enum.
/// </summary>
public enum AbilityTrigger
{
    /// <summary>Fires when the unit's action bar fills. The only trigger that reads <see cref="AbilityDefinition.ActionPointCost"/>.</summary>
    ActionBar,
    OnHit,
    OnAllyDeath,
    OnHealthBelow50,
    Persistent,
}

/// <summary>
/// Determines which unit(s) an ability effect is applied to.
/// </summary>
public enum EffectTarget
{
    Self,
    SingleHostile,
    AllHostile,
    SingleFriendly,
    AllFriendly,
    /// <summary>Routes through the special-case <see cref="UnitInfo.ExecuteAbilityCoreHit"/> path rather than the normal target lists.</summary>
    Core,
}

/// <summary>
/// The kind of change applied to the target by an ability effect.
/// </summary>
public enum EffectType
{
    Damage,
    Heal,
    BuffPower,
    BuffSpeed,
    BuffDefense,
}

/// <summary>
/// Which unit stat is used to scale the effect value.
/// </summary>
public enum StatSource
{
    Power,
    MaxHealth,
    Speed,
    /// <summary><see cref="AbilityEffect.Multiplier"/> is used as a raw value and is not multiplied by any stat.</summary>
    Flat,
}

// =============================================================================
// Data classes
// =============================================================================

/// <summary>
/// A single discrete effect within an ability definition.
/// Read-only after construction.
/// </summary>
[System.Serializable]
public class AbilityEffect
{
    [Tooltip("Which unit(s) this effect targets.")]
    [SerializeField] private EffectTarget target;

    [Tooltip("The kind of change applied to the target.")]
    [SerializeField] private EffectType type;

    [Tooltip("Which stat scales the effect. Flat means the multiplier is a raw value, not stat-scaled.")]
    [SerializeField] private StatSource scalingStat;

    [Tooltip("Scaling factor applied to the chosen stat, or the raw value when StatSource is Flat.")]
    [SerializeField] private float multiplier;

    /// <summary>Which unit(s) this effect targets.</summary>
    public EffectTarget Target => target;

    /// <summary>The kind of change applied to the target.</summary>
    public EffectType Type => type;

    /// <summary>Which stat scales the effect value.</summary>
    public StatSource ScalingStat => scalingStat;

    /// <summary>Scaling factor, or raw value when <see cref="ScalingStat"/> is <see cref="StatSource.Flat"/>.</summary>
    public float Multiplier => multiplier;

    /// <summary>Constructs a fully-specified ability effect.</summary>
    public AbilityEffect(EffectTarget target, EffectType type, StatSource scalingStat, float multiplier)
    {
        this.target = target;
        this.type = type;
        this.scalingStat = scalingStat;
        this.multiplier = multiplier;
    }
}

/// <summary>
/// The complete definition of an ability: its trigger, cost, and ordered list of effects.
/// Read-only after construction.
/// </summary>
[System.Serializable]
public class AbilityDefinition
{
    [Tooltip("Unique identifier for this ability.")]
    [SerializeField] private AbilityId id;

    [Tooltip("Human-readable name shown in the UI.")]
    [SerializeField] private string label;

    [Tooltip("When this ability fires.")]
    [SerializeField] private AbilityTrigger trigger;

    [Tooltip("Action points required to fill the bar and fire this ability. Only used when Trigger == ActionBar.")]
    [SerializeField] private int actionPointCost;

    [Tooltip("Ordered list of effects applied when the ability executes.")]
    [SerializeField] private List<AbilityEffect> effects;

    /// <summary>Unique identifier for this ability.</summary>
    public AbilityId Id => id;

    /// <summary>Human-readable name shown in the UI.</summary>
    public string Label => label;

    /// <summary>When this ability fires.</summary>
    public AbilityTrigger Trigger => trigger;

    /// <summary>Action points required to fill the bar. Only meaningful when <see cref="Trigger"/> is <see cref="AbilityTrigger.ActionBar"/>.</summary>
    public int ActionPointCost => actionPointCost;

    /// <summary>Ordered list of effects applied when the ability executes.</summary>
    public List<AbilityEffect> Effects => effects;

    /// <summary>Constructs a fully-specified ability definition.</summary>
    public AbilityDefinition(AbilityId id, string label, AbilityTrigger trigger, int actionPointCost, List<AbilityEffect> effects)
    {
        this.id = id;
        this.label = label;
        this.trigger = trigger;
        this.actionPointCost = actionPointCost;
        this.effects = effects;
    }
}

// =============================================================================
// Registry
// =============================================================================

/// <summary>
/// Static lookup table mapping every <see cref="AbilityId"/> to its <see cref="AbilityDefinition"/>.
/// Populated once in the static constructor and never mutated after that.
/// </summary>
public static class AbilityRegistry
{
    /// <summary>All ability definitions keyed by <see cref="AbilityId"/>.</summary>
    public static readonly Dictionary<AbilityId, AbilityDefinition> Definitions;

    static AbilityRegistry()
    {
        Definitions = new Dictionary<AbilityId, AbilityDefinition>
        {
            // ---------------------------------------------------------------------------
            // Shared hero abilities
            // ---------------------------------------------------------------------------
            {
                AbilityId.Attack, new AbilityDefinition(
                    AbilityId.Attack, "Attack", AbilityTrigger.ActionBar, 5,
                    new List<AbilityEffect>
                    {
                        new AbilityEffect(EffectTarget.SingleHostile, EffectType.Damage, StatSource.Power, 1.2f),
                    })
            },
            {
                AbilityId.Shoot, new AbilityDefinition(
                    AbilityId.Shoot, "Shoot", AbilityTrigger.ActionBar, 5,
                    new List<AbilityEffect>
                    {
                        new AbilityEffect(EffectTarget.SingleHostile, EffectType.Damage, StatSource.Power, 0.8f),
                    })
            },
            {
                AbilityId.Heal, new AbilityDefinition(
                    AbilityId.Heal, "Heal", AbilityTrigger.OnHealthBelow50, 0,
                    new List<AbilityEffect>
                    {
                        new AbilityEffect(EffectTarget.Self, EffectType.Heal, StatSource.MaxHealth, 0.25f),
                    })
            },
            {
                AbilityId.Shield, new AbilityDefinition(
                    AbilityId.Shield, "Shield", AbilityTrigger.Persistent, 0,
                    new List<AbilityEffect>
                    {
                        new AbilityEffect(EffectTarget.Self, EffectType.BuffDefense, StatSource.Flat, 5f),
                    })
            },
            {
                AbilityId.Block, new AbilityDefinition(
                    AbilityId.Block, "Block", AbilityTrigger.OnHit, 0,
                    new List<AbilityEffect>
                    {
                        new AbilityEffect(EffectTarget.Self, EffectType.BuffDefense, StatSource.Flat, 3f),
                    })
            },
            {
                AbilityId.Soothe, new AbilityDefinition(
                    AbilityId.Soothe, "Soothe", AbilityTrigger.Persistent, 0,
                    new List<AbilityEffect>
                    {
                        new AbilityEffect(EffectTarget.SingleFriendly, EffectType.BuffSpeed, StatSource.Flat, 1f),
                    })
            },
            {
                AbilityId.Escape, new AbilityDefinition(
                    AbilityId.Escape, "Escape", AbilityTrigger.ActionBar, 3,
                    new List<AbilityEffect>())
            },
            {
                AbilityId.HitBoss, new AbilityDefinition(
                    AbilityId.HitBoss, "Hit Boss", AbilityTrigger.ActionBar, 5,
                    new List<AbilityEffect>
                    {
                        new AbilityEffect(EffectTarget.SingleHostile, EffectType.Damage, StatSource.Flat, 1f),
                    })
            },
            // ---------------------------------------------------------------------------
            // Monster abilities
            // ---------------------------------------------------------------------------
            {
                AbilityId.BossSwipe, new AbilityDefinition(
                    AbilityId.BossSwipe, "Boss Swipe", AbilityTrigger.ActionBar, 5,
                    new List<AbilityEffect>
                    {
                        new AbilityEffect(EffectTarget.AllHostile, EffectType.Damage, StatSource.Flat, 99f),
                    })
            },
            // ---------------------------------------------------------------------------
            // Core-targeting abilities
            // ---------------------------------------------------------------------------
            {
                AbilityId.CoreHit, new AbilityDefinition(
                    AbilityId.CoreHit, "Core Hit", AbilityTrigger.ActionBar, 5,
                    new List<AbilityEffect>
                    {
                        new AbilityEffect(EffectTarget.Core, EffectType.Damage, StatSource.Power, 1.0f),
                    })
            },
        };
    }

    // ---------------------------------------------------------------------------

    /// <summary>
    /// Returns the <see cref="AbilityDefinition"/> for the given <paramref name="id"/>,
    /// or logs an error and returns <c>null</c> if the id has no registered definition.
    /// </summary>
    public static AbilityDefinition Get(AbilityId id)
    {
        if (Definitions.TryGetValue(id, out AbilityDefinition def)) return def;
        Debug.LogError($"[AbilityRegistry] No definition found for AbilityId.{id}.");
        return null;
    }
}

using UnityEngine;
using System;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;

[System.Serializable]
public class Decree
{
    #region // Attributes

    // Enum for decrees
    public enum Decrees
    {
        Expel, // Damage
        Encourage, // Heal
        Cheer, // Energy boost
        RedCarpet, // Energy boost
        ByFire, // Energy boost
    }
    public enum DecreeTargetType
    {
        WholeFloor,
        AllCharacters,
        AllAllies,
        AllEnemies,
        OneAlly,
        OneEnemy,
        OneCharacter,

    }
    // Properties
    [SerializeField] private Decrees decreeEnum;
    [SerializeField] private DecreeTargetType targetType;
    [SerializeField] private int cost;
    [SerializeField] private string description;
    [SerializeField] private DecreeManager decreeManager;

    #endregion
    #region // Methods

    // Default constructor
    public Decree()
    {
        // No initialization by default
    }

    // Constructor with ability parameter
    public Decree(Decrees decree, DecreeManager decreeManager)
    {
        InitializeDecree(decree);
        this.decreeManager = decreeManager;
    }
    #region // Overrides
    public override bool Equals(System.Object obj)
    {
        if (obj == null)
            return false;

        Decree d = obj as Decree;
        if ((System.Object)d == null)
            return false;

        return GetDecreeEnum() == d.GetDecreeEnum();
    }

    public bool Equals(Decree d)
    {
        if ((object)d == null)
            return false;

        return GetDecreeEnum() == d.GetDecreeEnum();
    }
    public override int GetHashCode()
    {
        return GetDecreeEnum().GetHashCode();
    }

    #endregion

    // Method to initialize ability details
    public void InitializeDecree(Decrees decree)
    {
        decreeEnum = decree;
        string description = "N/A";
        int cost = 1;
        DecreeTargetType targetType = DecreeTargetType.AllCharacters;
        // Set details based on the ability
        switch (GetDecreeEnum())
        {
            case Decrees.Expel:
                description = "Damage a hero in an attempt to kick them out.";
                targetType = DecreeTargetType.OneEnemy;
                break;
            case Decrees.Encourage:
                description = "Exalt a Monster raising their health and moral";
                targetType = DecreeTargetType.OneAlly;
                break;
            case Decrees.Cheer:
                description = "let a mana filled cheer on an entire floor";
                targetType = DecreeTargetType.OneCharacter;
                break;
            case Decrees.ByFire:
                description = "let a mana filled cheer on an entire floor";
                targetType = DecreeTargetType.AllEnemies;
                break;
            case Decrees.RedCarpet:
                description = "let a mana filled cheer on an entire floor";
                targetType = DecreeTargetType.AllCharacters;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(decree), "Unknown decree type");
        }

        SetDecreeCost(cost);
        SetDecreeDescription(description);
        SetDecreeTargetType(targetType);
    }

    // Method to execute ability
    public void ExecuteDecree(List<UnitInfo> hostileTargets, List<UnitInfo> friendlyTargets)
    {
        switch (GetDecreeEnum())
        {
            case Decrees.Expel:
                Debug.Log("Executing Expel");
                ExecuteDecreeExpel(hostileTargets[0]);
                break;
            case Decrees.Encourage:
                Debug.Log("Executing Encourage");
                ExecuteDecreeEncourage(friendlyTargets[0]);
                break;
            default:
                Debug.Log("No ability selected to execute.");
                break;
        }
    }
    private void ExecuteDecreeExpel(UnitInfo enemy)
    {
        enemy.TakeDamage((int)Mathf.Ceil(5));
    }
    private void ExecuteDecreeEncourage(UnitInfo ally)
    {
        //// Going with a basic formula
        //// Get the relavent stats
        //float offenseStat = user.GetPower();
        //float power = Power; // We should be in the instance of a specific ability
        //// Max sure the final number is between 1 - 99
        //float dmg = (offenseStat * power);
        //float minCheck = Math.Max(1, dmg);
        //float maxCheck = Math.Min(minCheck, 99);
        //float final = maxCheck;
        //// Pass Final Damage number (rounded up)
        //target.TakeDamage((int)Mathf.Ceil(final));
    }
    public Decrees GetDecreeEnum() => decreeEnum;
    public void SetDecreeEnum(Decrees newDecreeEnum) => decreeEnum = newDecreeEnum;
    public int GetDecreeCost() => cost;
    public void SetDecreeCost(int newCost) => cost = newCost;
    public string GetDecreeDescription() => description;
    public void SetDecreeDescription(string newDescription) => description = newDescription;
    public string GetDecreeTitle() => GetDecreeEnum().ToString();
    public void SetDecreeTargetType(DecreeTargetType newTargetType) => targetType = newTargetType;
    public DecreeTargetType GetDecreeTargetType() => targetType;
    #endregion
}

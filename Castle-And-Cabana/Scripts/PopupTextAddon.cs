using DamageNumbersPro;
using UnityEngine;

public class PopupTextAddon: MonoBehaviour
{

    //Assign prefab in inspector.
    public DamageNumber gainEnergyPrefab        ;
    public DamageNumber loseHealthPrefab        ;
    public DamageNumber gainHealthPrefab        ;
    public DamageNumber badStatusEffectPrefab   ;
    public DamageNumber goodStatusEffectPrefab  ;
    public DamageNumber burningDamagePrefab     ;
    public DamageNumber poisionDamagePrefab     ;

    public enum PopupTextTypes
    {
        GainEnergy  ,
        LoseHealth  ,
        GainHealth  ,
        BadStatus   ,
        GoodStatus  ,
        Burning     ,
        Poision     ,
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //On leftclick.
        if (Input.GetMouseButtonDown(0))
        {
            //Spawn new popup at transform.position with a random number between 0 and 100.
            //
        }

    }
    public void SpawnPopupText(PopupTextTypes type, Vector3 position  ,int value = 0, string text = "")
    {
        DamageNumber damageNumber;
        switch (type)
        {
            case PopupTextTypes.GainEnergy  : damageNumber = gainEnergyPrefab.Spawn(position, value); break;
            case PopupTextTypes.LoseHealth  : damageNumber = loseHealthPrefab.Spawn(position, value); break;
            case PopupTextTypes.GainHealth  : damageNumber = gainHealthPrefab.Spawn(position, value); break;
            case PopupTextTypes.BadStatus   : damageNumber = badStatusEffectPrefab.Spawn(position, text); break;  // Make sure to set text for statuses
            case PopupTextTypes.GoodStatus  : damageNumber = goodStatusEffectPrefab.Spawn(position, text); break; // Make sure to set text for statuses
            case PopupTextTypes.Burning     : damageNumber = burningDamagePrefab.Spawn(position, value); break;
            case PopupTextTypes.Poision     : damageNumber = poisionDamagePrefab.Spawn(position, value); break;
            default                         : Debug.LogError("Unable to craete damage number unknown popup type"); break;
        }
    }
}

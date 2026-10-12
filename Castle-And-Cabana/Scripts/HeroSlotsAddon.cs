using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeroSlotsAddon : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> Slots = new List<GameObject>(); // 4 GameObjects representing hero slot positions
    
    [SerializeField] private HeroUnit slotHero1; // Unit occupying Slot 1
    [SerializeField] private HeroUnit slotHero2; // Unit occupying Slot 2
    [SerializeField] private HeroUnit slotHero3; // Unit occupying Slot 3
    [SerializeField] private HeroUnit slotHero4; // Unit occupying Slot 4
    [SerializeField] private HeroTeam heroTeam;

    #region // Targeting Fx
    [SerializeField] private Image heroTargetFx1; // Targeting fx for Slot 1
    [SerializeField] private Image heroTargetFx2; // Targeting fx for Slot 2
    [SerializeField] private Image heroTargetFx3; // Targeting fx for Slot 3
    [SerializeField] private Image heroTargetFx4; // Targeting fx for Slot 4
    private float fxFloatSpeed = 2f; // speed of rise/fall
    private float fxFloatAmount = 1.5f; // how much to move up/down
    private float fxFloatOffsetY = 5f; // how much to move up/down
    private bool targetingFxActive = false;
    private List<Image> activeTargetingFxs = new List<Image>();
    #endregion

    private bool slotsTaken; // Is there a HeroTeam in the slots
    
    // Debugging
    [SerializeField]
    private bool isDebugging = false;
    private void Update()
    {
        UpdateTargetFxPosition();
    }
    // Ensure there are 4 slots in the Slots list
    public void InitializeHeroSlots()
    {
        if (Slots.Count != 4)
        {
            Debug.LogError("Slots list must contain exactly 4 elements.");
            return;
        }
        // Check that all hero target FX are assigned
        if (heroTargetFx1 == null || heroTargetFx2 == null || heroTargetFx3 == null || heroTargetFx4 == null)
        {
            Debug.LogError("One or more heroTargetFx references are not assigned in the Inspector!");
            return;
        }

        // Turn off all targeting FX
        DisableAllTargetingFx();

        ClearHeroSlots();
        if (isDebugging) Debug.Log("Hero slots initialized.");
    }
    
    // Clears the hero slots
    public void ClearHeroSlots()
    {
        slotHero1 = null;
        slotHero2 = null;
        slotHero3 = null;
        slotHero4 = null;
        heroTeam = null;
        slotsTaken = false;
    
        if (isDebugging) Debug.Log("Hero slots cleared.");
    }


    #region // Targeting Fx
    private void UpdateTargetFxPosition()
    {
        if (!targetingFxActive) return;

        float offset = Mathf.Sin(Time.time * fxFloatSpeed) * fxFloatAmount;

        Camera mainCam = Camera.main;
        if (mainCam == null) return; // No camera found

        foreach (Image fx in activeTargetingFxs)
        {
            if (fx == null) continue;

            RectTransform rect = fx.rectTransform;

            // Animate up and down
            Vector3 basePosition = rect.anchoredPosition;
            rect.anchoredPosition = new Vector3(basePosition.x, (offset + fxFloatOffsetY), basePosition.z);

            // Make the FX face the camera
            rect.rotation = Quaternion.LookRotation(rect.position - mainCam.transform.position);
        }
    }
    public void EnableAllTargetingFx()
    {
        DisableAllTargetingFx(); // Reset first

        for (int slot = 1; slot <= 4; slot++)
        {
            HeroUnit hero = GetHeroInSlot(slot);
            if (hero != null)
            {
                switch (slot)
                {
                    case 1:
                        heroTargetFx1.gameObject.SetActive(true);
                        activeTargetingFxs.Add(heroTargetFx1);
                        break;
                    case 2:
                        heroTargetFx2.gameObject.SetActive(true);
                        activeTargetingFxs.Add(heroTargetFx2);
                        break;
                    case 3:
                        heroTargetFx3.gameObject.SetActive(true);
                        activeTargetingFxs.Add(heroTargetFx3);
                        break;
                    case 4:
                        heroTargetFx4.gameObject.SetActive(true);
                        activeTargetingFxs.Add(heroTargetFx4);
                        break;
                }
            }
        }

        targetingFxActive = activeTargetingFxs.Count > 0;
    }


    public void EnableTargetingFx(int whichFx)
    {
        DisableAllTargetingFx(); // Reset first

        switch (whichFx)
        {
            case 1:
                heroTargetFx1.gameObject.SetActive(true);
                activeTargetingFxs.Add(heroTargetFx1);
                break;
            case 2:
                heroTargetFx2.gameObject.SetActive(true);
                activeTargetingFxs.Add(heroTargetFx2);
                break;
            case 3:
                heroTargetFx3.gameObject.SetActive(true);
                activeTargetingFxs.Add(heroTargetFx3);
                break;
            case 4:
                heroTargetFx4.gameObject.SetActive(true);
                activeTargetingFxs.Add(heroTargetFx4);
                break;
            default:
                Debug.LogWarning("Invalid FX slot: " + whichFx);
                break;
        }

        targetingFxActive = true;
    }

    public void DisableAllTargetingFx()
    {
        heroTargetFx1.gameObject.SetActive(false);
        heroTargetFx2.gameObject.SetActive(false);
        heroTargetFx3.gameObject.SetActive(false);
        heroTargetFx4.gameObject.SetActive(false);

        activeTargetingFxs.Clear();
        targetingFxActive = false;
    }
    #endregion

    // Set a HeroTeam in the slots
    public void SetHeroTeam(HeroTeam heroTeam)
    {
        if (heroTeam == null || heroTeam.GetHeroes() == null || heroTeam.GetHeroes().Count == 0)
        {
            Debug.LogError("HeroTeam is null or empty.");
            return;
        }
    
        ClearHeroSlots(); // Clear existing slots before assigning new heroes
        slotsTaken = true;
    
        for (int i = 0; i < heroTeam.GetHeroes().Count && i < 4; i++)
        {
            HeroUnit hero = heroTeam.GetHeroes()[i];
            switch (i + 1) // Slot numbers are 1-based
            {
                case 1:
                    slotHero1 = hero;
                    break;
                case 2:
                    slotHero2 = hero;
                    break;
                case 3:
                    slotHero3 = hero;
                    break;
                case 4:
                    slotHero4 = hero;
                    break;
            }
    
            if (isDebugging) Debug.Log($"Hero set in slot {i + 1}: ");
        }
        this.heroTeam = heroTeam;
    }
    
    // Get the hero in a specific slot
    public HeroUnit GetHeroInSlot(int slot)
    {
        return slot switch
        {
            1 => slotHero1,
            2 => slotHero2,
            3 => slotHero3,
            4 => slotHero4,
            _ => null
        };
    }
    // Returns the index (1-4) of the first open (empty) hero slot
    public int GetFirstOpenSlot()
    {
        for (int i = 1; i <= 4; i++)
        {
            if (GetHeroInSlot(i) == null)
                return i;
        }
        return -1; // No open slots found
    }

    // Returns the index (1-4) of the first occupied hero slot
    public int GetFirstOccupiedSlot()
    {
        for (int i = 1; i <= 4; i++)
        {
            if (GetHeroInSlot(i) != null)
                return i;
        }
        return -1; // No occupied slots found
    }


    // Get all heroes in slots
    public List<HeroUnit> GetAllHeroes()
    {
        List<HeroUnit> heroes = new List<HeroUnit>();
    
        if (slotHero1 != null) heroes.Add(slotHero1);
        if (slotHero2 != null) heroes.Add(slotHero2);
        if (slotHero3 != null) heroes.Add(slotHero3);
        if (slotHero4 != null) heroes.Add(slotHero4);
    
        return heroes;
    }
    public bool GetIsTaken()
    {
        if (heroTeam == null) return false;
        return slotsTaken;
    }
    public HeroTeam GetHeroTeam()
    {
        return heroTeam;
    }
    public List<GameObject> GetSlots()
    {
        return Slots;
    }
}

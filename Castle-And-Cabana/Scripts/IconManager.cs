using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IconManager : MonoBehaviour
{
    public enum IconType
    {
        Attack,
        Shoot
    }

    [System.Serializable]
    private class IconEntry
    {
        public IconType iconType;
        [SerializeField] private Sprite iconSprite;

        public Sprite IconImage => iconSprite;
    }

    [SerializeField] private List<IconEntry> iconEntries;
    private Dictionary<IconType, Sprite> iconDictionary;

    private void Awake()
    {
        iconDictionary = new Dictionary<IconType, Sprite>();
        foreach (var entry in iconEntries)
        {
            if (!iconDictionary.ContainsKey(entry.iconType))
            {
                iconDictionary.Add(entry.iconType, entry.IconImage);
            }
        }
    }

    public void SetIcon(IconType type, Sprite sprite)
    {
        if (iconDictionary.ContainsKey(type))
        {
            iconDictionary[type] = sprite;
        }
        else
        {
            Debug.LogWarning($"Icon of type {type} not found.");
        }
    }

    public Sprite GetIcon(IconType type)
    {
        if (iconDictionary.ContainsKey(type))
        {
            return iconDictionary[type];
        }
        Debug.LogWarning($"Icon of type {type} not found.");
        return null;
    }
}

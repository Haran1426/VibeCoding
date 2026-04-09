using UnityEngine;

[System.Serializable]
public class AbilityData
{
    public string id;
    public string displayName;
    public string description;
    public int maxLevel;
    public int currentLevel;

    public AbilityData(string id, string name, string desc, int maxLv)
    {
        this.id = id;
        displayName = name;
        description = desc;
        maxLevel = maxLv;
        currentLevel = 0;
    }

    public bool IsMaxed => currentLevel >= maxLevel;
    public bool CanOffer => !IsMaxed;
}

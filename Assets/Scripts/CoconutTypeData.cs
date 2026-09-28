using UnityEngine;
using static GameManager;


[System.Serializable]
public class CoconutTypeData
{
    [HideInInspector] public string typeName;
    [HideInInspector] public int killsToUnlock;
    [HideInInspector] public float hpMultiplier;
    [HideInInspector] public float lootMultiplier;
    [HideInInspector] public CoconutAbility ability;
    [HideInInspector] public string specialAbilityId;  

    public GameObject prefab;
}
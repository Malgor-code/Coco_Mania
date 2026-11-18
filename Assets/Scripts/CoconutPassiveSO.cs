using UnityEngine;

public abstract class CoconutPassiveSO : ScriptableObject
{
    public abstract ICoconutPassive Create(ICoconutHost host);
}
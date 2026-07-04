using UnityEngine;

[CreateAssetMenu(fileName = "NewBarricade", menuName = "BloodRing/BarricadeData")]
public class BarricadeData : ScriptableObject
{
    public string barricadeName;
    public int woodCost;
    public int metalCost;
    public float hp;
    public float buildTime;
}

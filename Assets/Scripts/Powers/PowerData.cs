using UnityEngine;

public enum PowerType
{
    SpeedBoost,
    ShieldBurst,
    RageMode,
    Invisibility,
    HealSurge,
    DoubleJump,
    Magnet
}

[CreateAssetMenu(fileName = "NewPower", menuName = "BloodRing/PowerData")]
public class PowerData : ScriptableObject
{
    public PowerType powerType;
    public string powerName;
    public float duration;
    public float value;
    public Color iconColor = Color.white;

    public static PowerData GetDefaultPower(PowerType type)
    {
        PowerData p = ScriptableObject.CreateInstance<PowerData>();
        p.powerType = type;
        switch (type)
        {
            case PowerType.SpeedBoost:
                p.powerName = "Speed Boost";
                p.duration = 8f;
                p.value = 1.5f; // +50% speed
                p.iconColor = Color.yellow;
                break;
            case PowerType.ShieldBurst:
                p.powerName = "Shield Burst";
                p.duration = 0f; // Instant
                p.value = 50f; // +50 armor
                p.iconColor = Color.blue;
                break;
            case PowerType.RageMode:
                p.powerName = "Rage Mode";
                p.duration = 6f;
                p.value = 2f; // 2x damage & fire rate
                p.iconColor = Color.red;
                break;
            case PowerType.Invisibility:
                p.powerName = "Invisibility";
                p.duration = 5f;
                p.value = 0.3f; // Alpha 0.3
                p.iconColor = Color.gray;
                break;
            case PowerType.HealSurge:
                p.powerName = "Heal Surge";
                p.duration = 3f;
                p.value = 30f; // +30 HP over 3s
                p.iconColor = Color.green;
                break;
            case PowerType.DoubleJump:
                p.powerName = "Double Jump";
                p.duration = 15f;
                p.value = 1f;
                p.iconColor = Color.cyan;
                break;
            case PowerType.Magnet:
                p.powerName = "Magnet";
                p.duration = 10f;
                p.value = 15f; // 15u range
                p.iconColor = Color.magenta;
                break;
        }
        return p;
    }
}



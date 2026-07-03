using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CharacterSkill
{
    public string skillName;
    public string description;
    public float cooldown;
    public float duration;
}

[CreateAssetMenu(fileName = "NewCharacter", menuName = "BloodRing/CharacterData")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public string title;
    public CharacterSkill activeSkill;
    public int upgradeLevel = 1;
    public int requiredFragments = 500;
    public bool isUnlocked = false;

    public static CharacterData GetCharacter(string name)
    {
        CharacterData c = ScriptableObject.CreateInstance<CharacterData>(); c.characterName = name; c.upgradeLevel = 1; c.requiredFragments = 500; c.isUnlocked = true;
        c.activeSkill = new CharacterSkill();

        switch (name)
        {
            case "DJNeon": c.title = "Cyber Beatmaster"; c.activeSkill.skillName = "Surge Beat"; c.activeSkill.description = "Creates a 5m aura that increases movement speed by 15% and restores 5 HP/s."; c.activeSkill.cooldown = 45f; c.activeSkill.duration = 10f; break;
            case "Pulse": c.title = "Quantum Vanguard"; c.activeSkill.skillName = "Aegis Dome"; c.activeSkill.description = "Creates a force field that blocks 800 damage from enemies. Unable to attack outside."; c.activeSkill.cooldown = 60f; c.activeSkill.duration = 6f; break;
            case "Bolt": c.title = "Track Elite"; c.activeSkill.skillName = "Overdrive Sprint"; c.activeSkill.description = "Increases sprinting speed by 6%. Awakening: First shot deals 106% damage."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Ronin": c.title = "High-Tech Samurai"; c.activeSkill.skillName = "Crimson Edge"; c.activeSkill.description = "When max HP decreases by 10%, armor penetration increases by 10%."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Mirage": c.title = "Holographic Tactician"; c.activeSkill.skillName = "Optical Cloak"; c.activeSkill.description = "Transforms into a bush, hiding from enemy targeting for 15s. CD resets on kill."; c.activeSkill.cooldown = 50f; c.activeSkill.duration = 15f; break;
            case "Sonic": c.title = "Resonance Expert"; c.activeSkill.skillName = "Sub-Bass Wave"; c.activeSkill.description = "Unleashes a sonic wave that damages 5 Shield Barriers within 100m. Recovers HP on wall deploy."; c.activeSkill.cooldown = 40f; c.activeSkill.duration = 1f; break;
            case "Zero": c.title = "Apex Protocol Commander"; c.activeSkill.skillName = "Master Protocol"; c.activeSkill.description = "Max EP increases by 50. Overclock mode: allies get 500% EP conversion rate."; c.activeSkill.cooldown = 3f; c.activeSkill.duration = 999f; break;
            case "Cypher": c.title = "Cyber Intelligence"; c.activeSkill.skillName = "Target Lock"; c.activeSkill.description = "Tags shot enemies for 5s, sharing their locations with teammates."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 5f; break;
            case "Viper": c.title = "Biochem Expert"; c.activeSkill.skillName = "Nutrient Flow"; c.activeSkill.description = "Reduces time for eating Med Kits and Mushrooms by 25%."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Axiom": c.title = "Neural Medic"; c.activeSkill.skillName = "Cellular Reboot"; c.activeSkill.description = "Creates a 3.5m healing zone. Downed allies can self-recover to get up."; c.activeSkill.cooldown = 60f; c.activeSkill.duration = 12f; break;
            case "Echo": c.title = "Resonance Operative"; c.activeSkill.skillName = "Acoustic Revival"; c.activeSkill.description = "Increases help-up speed for downed teammates by 30%. Recovers 50 HP upon success."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Lynx": c.title = "Apex Recon"; c.activeSkill.skillName = "Steady Eye"; c.activeSkill.description = "Increases accuracy by 35% while scoped in."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Shadow": c.title = "Shadow Phantom"; c.activeSkill.skillName = "Absolute Silence"; c.activeSkill.description = "Sniper and Marksman rifles are silenced. Downed enemies lose HP 90% faster."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Specter": c.title = "Live Feeder"; c.activeSkill.skillName = "Viewer Surge"; c.activeSkill.description = "With each observer or kill, headshot damage taken decreases by 10%, damage to enemy limbs increases by 15%."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Ghost": c.title = "Silent Operator"; c.activeSkill.skillName = "Health Stack"; c.activeSkill.description = "Every kill increases max HP by 25, up to 50."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Titan": c.title = "Frontline Juggernaut"; c.activeSkill.skillName = "Armor Delivery"; c.activeSkill.description = "When hit by an enemy within 80m, attacker is marked. First shot on marked enemy has 100% armor pen."; c.activeSkill.cooldown = 10f; c.activeSkill.duration = 6f; break;
            case "Blaze": c.title = "Parkour Legend"; c.activeSkill.skillName = "Adrenaline Surge"; c.activeSkill.description = "Hitting enemies with guns recovers some HP. Knocking down an enemy recovers 20% max HP."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Revan": c.title = "Bounty Hunter"; c.activeSkill.skillName = "Threat Scanner"; c.activeSkill.description = "Locates positions of enemies within 75m who are not prone or squatting."; c.activeSkill.cooldown = 50f; c.activeSkill.duration = 10f; break;
            case "Helix": c.title = "Bionic Boxer"; c.activeSkill.skillName = "Hydraulic Fist"; c.activeSkill.description = "Increases fist damage by 400%."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Mako": c.title = "Munitions Master"; c.activeSkill.skillName = "Cargo Link"; c.activeSkill.description = "120 ammo will not take up inventory space."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Stryker": c.title = "Elite Striker"; c.activeSkill.skillName = "Takedown Specialist"; c.activeSkill.description = "Gain 80 EP for each knockdown."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Nexus": c.title = "Weapons Tactician"; c.activeSkill.skillName = "Sub-Mag Refit"; c.activeSkill.description = "Reload speed of SMGs increases by 20%. Last 6 bullets of SMG deal 30% more damage."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Nova": c.title = "Siren Medic"; c.activeSkill.skillName = "Aura Song"; c.activeSkill.description = "Increases effects of healing items by 20% and healing skills by 10%."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Atlas": c.title = "Quantum Nomad"; c.activeSkill.skillName = "Phase Sprint"; c.activeSkill.description = "Movement speed increases by 20% for 1s upon taking damage."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 1f; break;
            case "Cipher": c.title = "Stealth Medic"; c.activeSkill.skillName = "Advanced Defib"; c.activeSkill.description = "Players revived get up with extra 80 HP."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Talon": c.title = "Reinforced Veteran"; c.activeSkill.skillName = "Ceramic Refit"; c.activeSkill.description = "Vest durability loss decreased by 20%. Awakening: Armor damage reduction increased by 15%."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Phantom": c.title = "Deep-Sea Tactician"; c.activeSkill.skillName = "Ion Deflection"; c.activeSkill.description = "Reduces damage taken outside the safe zone by 24%."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Apex": c.title = "Canyon Jager"; c.activeSkill.skillName = "Canyon Dash"; c.activeSkill.description = "When holding a shotgun, movement speed increases by 13%."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Havoc": c.title = "Chemical Artist"; c.activeSkill.skillName = "Barricade Mist"; c.activeSkill.description = "Creates a graffiti field that blocks throwables and reduces bullet damage taken by 20%."; c.activeSkill.cooldown = 45f; c.activeSkill.duration = 15f; break;
            case "Jager": c.title = "Combat Engineer"; c.activeSkill.skillName = "Field Weld"; c.activeSkill.description = "Restore 30 armor durability after every kill. Extra durability upgrades vest."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Riven": c.title = "Tomboy Biker"; c.activeSkill.skillName = "Exhaust Thruster"; c.activeSkill.description = "When driving a vehicle, restores 5 HP every 2s to all teammates in vehicle."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Vex": c.title = "Ordnance Expert"; c.activeSkill.skillName = "Focused Detonation"; c.activeSkill.description = "Explosive weapon damage increases by 20%, damage range increases by 10%."; c.activeSkill.cooldown = 0f; c.activeSkill.duration = 999f; break;
            case "Krypton": c.title = "Bladesmith"; c.activeSkill.skillName = "Titan Shield"; c.activeSkill.description = "Forms a frontal shield that reduces weapon damage coming from the front by 60%."; c.activeSkill.cooldown = 50f; c.activeSkill.duration = 5f; break;
            default: c.title = "Apex Elite"; c.activeSkill.skillName = "Sprinting Strike"; c.activeSkill.description = "Bonus speed and fast handling."; c.activeSkill.cooldown = 30f; c.activeSkill.duration = 10f; break;
        }
        return c;
    }

    public static List<string> GetAllCharacterNames()
    {
        return new List<string> { "DJNeon", "Pulse", "Bolt", "Ronin", "Mirage", "Sonic", "Zero", "Cypher", "Viper", "Axiom", "Echo", "Lynx", "Shadow", "Specter", "Ghost", "Titan", "Blaze", "Revan", "Helix", "Mako", "Stryker", "Nexus", "Nova", "Atlas", "Cipher", "Talon", "Phantom", "Apex", "Havoc", "Jager", "Riven", "Vex", "Krypton" };
    }
}



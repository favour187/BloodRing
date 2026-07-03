using UnityEngine;

[CreateAssetMenu(fileName = "MatchData", menuName = "BloodRing/MatchData")]
public class MatchData : ScriptableObject
{
    public int kills = 0;
    public int placement = 1;
    public float damageDealt = 0f;
    public float matchDuration = 0f;
    public string characterChoice = "Striker";

    public static MatchData Load()
    {
        MatchData data = Resources.Load<MatchData>("MatchData");
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MatchData>();
            data.kills = 0;
            data.placement = 1;
            data.damageDealt = 0f;
            data.matchDuration = 0f;
            data.characterChoice = PlayerPrefs.GetString("SelectedCharacter", "Striker");
        }
        return data;
    }

    public void ResetData()
    {
        kills = 0;
        placement = 20;
        damageDealt = 0f;
        matchDuration = 0f;
        characterChoice = PlayerPrefs.GetString("SelectedCharacter", "Striker");
    }
}



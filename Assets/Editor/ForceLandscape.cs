using UnityEditor;
public class ForceLandscape
{
    [MenuItem("BloodRing/Force Landscape Orientation")]
    public static void ForceLandscapeOrientation()
    {
        PlayerSettings.defaultScreenOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        EditorUtility.SetDirty(PlayerSettings);
        AssetDatabase.SaveAssets();
        UnityEngine.Debug.Log("Blood Ring: Forced Landscape orientation (left/right allowed).");
    }
}

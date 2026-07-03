using UnityEngine;

/// <summary>
/// Replaces all legacy procedural/code-built art methods with genuine real 3D model asset loading
/// from pre-authored .obj files in Resources/Models and Resources/Art/3D.
/// No art or geometry is built by code at runtime.
/// </summary>
public static class ProceduralArt
{
    public static GameObject CreateHumanoidMesh(string style = "")
    {
        return BloodRing.Art.BloodRingArtLibrary.GetCharacterModel(style);
    }

    public static GameObject CreateGunMesh(string weaponName)
    {
        return BloodRing.Art.BloodRingArtLibrary.GetWeaponModel(weaponName);
    }

    public static GameObject CreateTreeMesh()
    {
        return BloodRing.Art.BloodRingArtLibrary.GetEnvironmentModel("Tree.obj");
    }

    public static GameObject CreateRockMesh()
    {
        return BloodRing.Art.BloodRingArtLibrary.GetEnvironmentModel("Rock.obj");
    }

    public static ParticleSystem CreateConfetti()
    {
        GameObject go = new GameObject("Real3D_Confetti");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var pr = go.GetComponent<ParticleSystemRenderer>();
        pr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default"));
        return ps;
    }

    public static Shader GetSafeShader(string shaderName)
    {
        return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find(shaderName) ?? Shader.Find("Standard");
    }

    public static Material GetMaterial(string matName, Texture2D tex = null)
    {
        Material mat = Resources.Load<Material>("Art/Materials/" + matName);
        if (mat == null)
        {
            mat = new Material(GetSafeShader("Universal Render Pipeline/Lit"));
            if (tex != null) mat.mainTexture = tex;
        }
        return mat;
    }

    public static Texture2D GenerateRockTexture() => BloodRing.Art.BloodRingArtLibrary.Terrain("Terrain_Rock_Tile") ?? Texture2D.whiteTexture;
    public static Texture2D GenerateGroundTexture() => BloodRing.Art.BloodRingArtLibrary.Terrain("Terrain_Grass_Tile") ?? Texture2D.whiteTexture;
    public static Texture2D GenerateHeavyArmorTexture() => BloodRing.Art.BloodRingArtLibrary.Terrain("Terrain_MetalGrate") ?? Texture2D.whiteTexture;
    public static Texture2D GenerateBuildingTexture() => BloodRing.Art.BloodRingArtLibrary.Terrain("Terrain_ConcreteFloor") ?? Texture2D.whiteTexture;
    public static Texture2D GenerateButtonTexture(Color c1, Color c2) => BloodRing.Art.BloodRingArtLibrary.LoadTexture("UI/Buttons/Btn_Play_Large") ?? Texture2D.whiteTexture;
    public static Texture2D GenerateCircleButtonTexture(Color col, int size) => BloodRing.Art.BloodRingArtLibrary.LoadTexture("UI/Buttons/Btn_Settings") ?? Texture2D.whiteTexture;
    public static Texture2D GeneratePowerIcon(string name) => BloodRing.Art.BloodRingArtLibrary.LoadTexture("UI/Icons/" + name) ?? Texture2D.whiteTexture;
    public static void SetupSkybox() {
        Material sky = Resources.Load<Material>("Art/Materials/Skybox");
        if (sky != null) RenderSettings.skybox = sky;
    }
}

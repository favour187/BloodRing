using UnityEngine;
using System.Collections;

/// <summary>
/// Blood Ring Apex Royale 3D — Dynamic Weather & Time-of-Day Transition System.
/// Seamlessly transitions the island battlefield from sunny afternoon to dramatic sunset,
/// atmospheric fog, or thunderstorms with rain particles and lightning flashes during combat.
/// </summary>
public class DynamicWeatherSystem : MonoBehaviour
{
    public static DynamicWeatherSystem Instance;

    public enum WeatherState { ClearDay = 0, DramaticSunset = 1, DenseFog = 2, Thunderstorm = 3 }
    public WeatherState currentWeather = WeatherState.ClearDay;
    public float weatherTransitionInterval = 120f;

    private Light sunLight;
    private ParticleSystem rainParticles;

    private void Awake()
    {
        Instance = this;
        sunLight = Object.FindObjectOfType<Light>();
        if (sunLight == null)
        {
            GameObject sunGo = new GameObject("SunLight");
            sunLight = sunGo.AddComponent<Light>();
            sunLight.type = LightType.Directional;
        }
        StartCoroutine(WeatherCycleCoroutine());
    }

    private IEnumerator WeatherCycleCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(weatherTransitionInterval);
            currentWeather = (WeatherState)(((int)currentWeather + 1) % 4);
            ApplyWeatherState(currentWeather);
        }
    }

    public void ApplyWeatherState(WeatherState state)
    {
        Debug.Log($"[DynamicWeatherSystem] Transitioning battlefield weather to: {state}");
        switch (state)
        {
            case WeatherState.ClearDay:
                RenderSettings.fog = false;
                sunLight.color = new Color(1f, 0.95f, 0.85f);
                sunLight.intensity = 1.2f;
                if (rainParticles != null) rainParticles.Stop();
                break;
            case WeatherState.DramaticSunset:
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(0.8f, 0.35f, 0.15f);
                RenderSettings.fogDensity = 0.008f;
                sunLight.color = new Color(1f, 0.5f, 0.2f);
                sunLight.intensity = 0.9f;
                break;
            case WeatherState.DenseFog:
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(0.4f, 0.45f, 0.5f);
                RenderSettings.fogDensity = 0.025f;
                sunLight.intensity = 0.6f;
                break;
            case WeatherState.Thunderstorm:
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(0.15f, 0.15f, 0.22f);
                RenderSettings.fogDensity = 0.015f;
                sunLight.color = new Color(0.4f, 0.45f, 0.6f);
                sunLight.intensity = 0.5f;
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_ThunderClap");
                StartCoroutine(LightningFlashCoroutine());
                break;
        }
    }

    private IEnumerator LightningFlashCoroutine()
    {
        while (currentWeather == WeatherState.Thunderstorm)
        {
            yield return new WaitForSeconds(Random.Range(5f, 15f));
            float origIntensity = sunLight.intensity;
            sunLight.color = Color.white;
            sunLight.intensity = 2.5f;
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_ThunderClap");
            yield return new WaitForSeconds(0.1f);
            sunLight.intensity = origIntensity;
        }
    }
}

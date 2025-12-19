using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[DefaultExecutionOrder(-100)]
public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("Fetch Settings")]
    [Tooltip("How often to refresh weather data (seconds). 300 = 5 minutes.")]
    public float refreshSeconds = 300f;

    [TextArea(2, 6)]
    public string url =
        "https://api.open-meteo.com/v1/forecast" +
        "?latitude=40.693&longitude=-73.986" +
        "&timezone=America%2FNew_York" +
        "&temperature_unit=fahrenheit" +
        "&wind_speed_unit=mph" +
        "&current=temperature_2m,apparent_temperature,precipitation,rain,showers,snowfall,weather_code,cloud_cover,wind_speed_10m,is_day";

    [Header("Runtime (Read-Only)")]
    public string lastUpdatedTimeLocal;   // e.g. 2025-12-12T01:15
    public float tempF;
    public float feelsLikeF;
    public float precipitationMm;
    public float rainMm;
    public float showersMm;
    public float snowfallCm;
    public int weatherCode;
    public float cloudCoverPercent;
    public float windMph;
    public bool isDay;

    public float TempC => (tempF - 32f) * (5f / 9f);
    public float FeelsLikeC => (feelsLikeF - 32f) * (5f / 9f);

    public bool IsRaining => (rainMm > 0f) || (showersMm > 0f) || (precipitationMm > 0f);
    public bool IsSnowing => snowfallCm > 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        StartCoroutine(FetchLoop());
    }

    IEnumerator FetchLoop()
    {
        while (true)
        {
            yield return FetchOnce();
            yield return new WaitForSeconds(refreshSeconds);
        }
    }

    IEnumerator FetchOnce()
    {
        using (var req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Weather fetch failed: {req.error}");
                yield break;
            }

            try
            {
                var json = req.downloadHandler.text;
                var data = JsonUtility.FromJson<OpenMeteoResponse>(json);

                if (data == null || data.current == null)
                {
                    Debug.LogWarning("Weather parse failed: current is null.");
                    yield break;
                }

                lastUpdatedTimeLocal = data.current.time;
                tempF = data.current.temperature_2m;
                feelsLikeF = data.current.apparent_temperature;
                precipitationMm = data.current.precipitation;
                rainMm = data.current.rain;
                showersMm = data.current.showers;
                snowfallCm = data.current.snowfall;
                weatherCode = data.current.weather_code;
                cloudCoverPercent = data.current.cloud_cover;
                windMph = data.current.wind_speed_10m;
                isDay = data.current.is_day == 1;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Weather parse exception: " + e.Message);
            }
        }
    }

    // --- JSON models for JsonUtility ---
    [Serializable] private class OpenMeteoResponse
    {
        public CurrentWeather current;
    }

    [Serializable] private class CurrentWeather
    {
        public string time;
        public float temperature_2m;
        public float apparent_temperature;
        public float precipitation;
        public float rain;
        public float showers;
        public float snowfall;
        public int weather_code;
        public float cloud_cover;
        public float wind_speed_10m;
        public int is_day;
    }

    public string WeatherDescription
{
    get
    {
        switch (weatherCode)
        {
            case 0:  return "Clear sky";
            case 1:  return "Mostly clear";
            case 2:  return "Partly cloudy";
            case 3:  return "Overcast";

            case 45:
            case 48: return "Fog";

            case 51:
            case 53:
            case 55: return "Drizzle";

            case 61:
            case 63:
            case 65: return "Rain";

            case 66:
            case 67: return "Freezing rain";

            case 71:
            case 73:
            case 75: return "Snow";

            case 77: return "Snow grains";

            case 80:
            case 81:
            case 82: return "Rain showers";

            case 85:
            case 86: return "Snow showers";

            case 95: return "Thunderstorm";

            case 96:
            case 99: return "Thunderstorm with hail";

            default: return "Unknown weather";
        }
    }
}
}
using UnityEngine;
using TMPro;

public class WeatherUI : MonoBehaviour
{
    public TextMeshProUGUI text;

    void Reset()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        var wm = WeatherManager.Instance;
        if (wm == null) return;

        string precipLabel =
            wm.IsSnowing ? "Snow" :
            wm.IsRaining ? "Rain" :
            "No precipitation";

        text.text =
            "Downtown Brooklyn (370 Jay)\n" +
            $"{wm.lastUpdatedTimeLocal}\n" +
            $"{wm.tempF:0.#}°F / {wm.TempC:0.#}°C\n" +
            $"Feels like {wm.feelsLikeF:0.#}°F / {wm.FeelsLikeC:0.#}°C\n" +
            $"{precipLabel}\n" +
            $"Clouds {wm.cloudCoverPercent:0}%\n" +
            $"Wind {wm.windMph:0.#} mph";
    }
}
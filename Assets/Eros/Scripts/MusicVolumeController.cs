using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MusicVolumeController : MonoBehaviour
{
    [Header("References")]
    public AudioSource musicSource;
    public Slider volumeSlider;
    public TMP_Text valueText; // 👈 texto que muestra el valor (opcional)

    private void Start()
    {
        if (musicSource != null && volumeSlider != null)
        {
            // El slider va de 0 a 10 visualmente
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 10f;
            volumeSlider.wholeNumbers = true;

            // Convertir volumen real (0–1) a valor visible (0–10)
            volumeSlider.value = musicSource.volume * 10f;

            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            UpdateLabel(volumeSlider.value);
        }
    }

    private void OnVolumeChanged(float value)
    {
        if (musicSource != null)
        {
            // Convertir de 0–10 (UI) a 0–1 (Audio)
            musicSource.volume = value / 10f;
        }

        UpdateLabel(value);
    }

    private void UpdateLabel(float value)
    {
        if (valueText != null)
        {
            valueText.text = Mathf.RoundToInt(value).ToString();
        }
    }
}
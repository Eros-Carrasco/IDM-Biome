using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIBiomeSlider : MonoBehaviour
{
    public enum WorldVar
    {
        // Grid
        MapSize,

        // Height Map (Perlin)
        Seed,
        Frequency,

        heightScale,

        // Voxel Block Levels
        WaterLevel,
        ShoreWidth,
        HighlandStart,

        // Decoration & Props
        ScatterFrequency,
        TreeDensity,
        FlowerDensity,
        RockPropDensity,
        MushroomDensity
    }

    [Header("Refs")]
    public WorldGeneratorForWeb world;   // 👈 your renamed generator
    public Slider slider;
    public TMP_Text valueText;
    public TMP_Text titleText;

    [Header("Config")]
    public WorldVar target = WorldVar.MapSize;

    [Tooltip("If empty, it will use the enum name.")]
    public string customTitle = "";

    // for 0-1 sliders
    public bool showAsPercent = false;

    private void Start()
    {
        if (slider == null) slider = GetComponent<Slider>();

        if (world != null && slider != null)
        {
            slider.value = GetCurrentValue();
            UpdateLabel(slider.value);
        }

        slider.onValueChanged.AddListener(OnSliderChanged);

        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(customTitle) ? target.ToString() : customTitle;
    }

    private void OnSliderChanged(float v)
    {
        if (world == null) return;

        SetValue(v);
        UpdateLabel(v);
        world.RegenerateImmediate();
    }

    float GetCurrentValue()
    {
        switch (target)
        {
            case WorldVar.MapSize:         return world.mapSize;
            case WorldVar.Seed:            return world.seed;
            case WorldVar.Frequency:       return world.frequency;
            case WorldVar.heightScale:       return world.heightScale;
            case WorldVar.WaterLevel:      return world.waterLevel;
            case WorldVar.ShoreWidth:      return world.shoreWidth;
            case WorldVar.HighlandStart:   return world.highlandStart;
            case WorldVar.ScatterFrequency:return world.scatterFrequency;
            case WorldVar.TreeDensity:     return world.treeDensity;
            case WorldVar.FlowerDensity:   return world.flowerDensity;
            case WorldVar.RockPropDensity: return world.rockPropDensity;
            case WorldVar.MushroomDensity: return world.mushroomDensity;
        }
        return 0f;
    }

    void SetValue(float v)
    {
        switch (target)
        {
            // ints
            case WorldVar.MapSize:
                world.mapSize = Mathf.RoundToInt(v);
                break;
            case WorldVar.Seed:
                world.seed = Mathf.RoundToInt(v);
                break;

            // floats
            case WorldVar.Frequency:
                world.frequency = v;
                break;
            case WorldVar.heightScale:
                world.heightScale = v;
                break;
            case WorldVar.WaterLevel:
                world.waterLevel = v;
                break;
            case WorldVar.ShoreWidth:
                world.shoreWidth = v;
                break;
            case WorldVar.HighlandStart:
                world.highlandStart = v;
                break;

            // 0-1 floats
            case WorldVar.ScatterFrequency:
                world.scatterFrequency = v;
                break;
            case WorldVar.TreeDensity:
                world.treeDensity = v;
                break;
            case WorldVar.FlowerDensity:
                world.flowerDensity = v;
                break;
            case WorldVar.RockPropDensity:
                world.rockPropDensity = v;
                break;
            case WorldVar.MushroomDensity:
                world.mushroomDensity = v;
                break;
        }
    }

    void UpdateLabel(float v)
    {
        if (valueText == null) return;

        // ints
        if (target == WorldVar.MapSize || target == WorldVar.Seed)
        {
            valueText.text = Mathf.RoundToInt(v).ToString();
            return;
        }

        // 0-1 stuff
        if (showAsPercent)
        {
            valueText.text = Mathf.RoundToInt(v * 100f) + "%";
            return;
        }

        // normal float
        valueText.text = v.ToString("0.00");
    }
}
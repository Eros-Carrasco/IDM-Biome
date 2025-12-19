using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[AddComponentMenu("Eros/World Generator - Web")]
public class WorldGeneratorForWeb : MonoBehaviour
{
    #region World & Noise
    [Header("Grid / World")]
    public int mapSize = 20;
    public float spacing = 1f;

    [Header("Height Map (Perlin)")]
    public int seed = 0;
    [Range(0.01f, 0.1f)] public float frequency = 0.1f;
    public float heightScale = 3f;
    #endregion

    #region Terrain Bands
    [Header("Voxel Block Levels (Water / Shore / Highlands)")]
    public float waterLevel = 1.1f;
    public float shoreWidth = 0.25f;
    public float highlandStart = 2.2f;
    #endregion

    #region Decoration & Props
    [Header("Decoration & Props")]
    [Range(0.01f, 1.0f)] public float scatterFrequency = 0.23f;
    [Range(0f, 1f)] public float treeDensity = 0.28f;
    [Range(0f, 1f)] public float flowerDensity = 0.35f;
    [Range(0f, 1f)] public float rockPropDensity = 0.22f;
    [Range(0f, 1f)] public float mushroomDensity = 0.18f;
    [Range(0.0f, 0.5f)] public float maxSlopeForProps = 0.22f;
    [Range(0f, 1f)] public float propsDensity = 0.03f;
    #endregion

    #region Prefabs
    [Header("Prefabs – Terrain Blocks")]
    public GameObject waterBlock;
    public GameObject waterHalfBlock;
    public GameObject[] sandBlocks;
    public GameObject[] grassBlocks;
    public GameObject[] rockBlocks;

    [Header("Prefabs – Decorations")]
    public GameObject[] treePrefabs;
    public GameObject[] flowerPrefabs;
    public GameObject[] rockPropPrefabs;
    public GameObject[] mushroomPrefabs;
    public GameObject[] propPrefabs;
    #endregion

    private System.Random prng;

    // NEW: snapshots para detectar cambios
    private int    _lastMapSize;
    private float  _lastSpacing;
    private int    _lastSeed;
    private float  _lastFrequency;
    private float  _lastHeightScale;
    private float  _lastWaterLevel;
    private float  _lastShoreWidth;
    private float  _lastHighlandStart;
    private float  _lastScatterFrequency;
    private float  _lastTreeDensity;
    private float  _lastFlowerDensity;
    private float  _lastRockPropDensity;
    private float  _lastMushroomDensity;
    private float  _lastMaxSlopeForProps;
    private float  _lastPropsDensity;

        [Header("Densidad base (para potenciómetro)")]
    [Range(0f, 1f)] public float baseTreeDensity     = 0.28f;
    [Range(0f, 1f)] public float baseFlowerDensity   = 0.35f;
    [Range(0f, 1f)] public float baseMushroomDensity = 0.18f;

    private void OnEnable()
    {
        RegenerateImmediate();
        SnapshotValues();   // NEW
    }

    private void Start()
    {
        RegenerateImmediate();
        SnapshotValues();   // NEW
    }

    private void Update()
    {
        // esto corre en play y en editor porque hay ExecuteAlways
        if (HasChanged())
        {
            RegenerateImmediate();
            SnapshotValues();
        }

        if (ArduinoInputManager.Instance != null) 
    {
        // 1. MAPEO VARIABLE 1 (Rango 1 a 100)
        // Pedimos el valor "limpio" (0 a 1) del Potenciómetro 1
        float entradaPot1 = ArduinoInputManager.Instance.Pot1_Filtrado;
        float entradaPot4 = ArduinoInputManager.Instance.Pot4_Filtrado;

        
        mapSize =  (int)Mathf.Lerp(1f, 35f, entradaPot4);
        // Mapeamos: Si entrada es 0 -> vale 1. Si entrada es 1 -> vale 100.
        seed = (int)Mathf.Lerp(1f, 100f, entradaPot1);


        // 2. MAPEO VARIABLE 2 (Rango 0 a 0.07)
        // Pedimos el valor "limpio" (0 a 1) del Potenciómetro 2
        float entradaPot2 = ArduinoInputManager.Instance.Pot2_Filtrado;
        
        // Mapeamos: Si entrada es 0 -> vale 0. Si entrada es 1 -> vale 0.07.
        frequency = Mathf.Lerp(0f, 0.07f, entradaPot2);
        
        
        // 3. MAPEO VARIABLE 3 (Si la usas)
        // float entradaPot3 = ArduinoInputManager.Instance.Pot3_Normalizado;
        // tuVariable3 = Mathf.Lerp(Min, Max, entradaPot3);
        float entradaPot3 = ArduinoInputManager.Instance.Pot3_Filtrado;
            float globalDensity = entradaPot3; // 0–1

            // Escalamos árboles / flores / hongos con un solo valor
            treeDensity     = baseTreeDensity     * globalDensity;
            flowerDensity   = baseFlowerDensity   * globalDensity;
            mushroomDensity = baseMushroomDensity * globalDensity;
    }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    RegenerateImmediate();
                    SnapshotValues();  // NEW
                }
            };
        }
    }
#endif

    [ContextMenu("Regenerate")]
    public void RegenerateImmediate() => Generate();

    public void Generate()
    {
        ClearChildren();
        prng = new System.Random(seed);

        for (int x = -mapSize; x < mapSize; x++)
        {
            for (int z = -mapSize; z < mapSize; z++)
            {
                float h = GetHeight01(x, z) * heightScale;
                GameObject floorPrefab = ChooseFloorPrefab(x, z, h);
                Vector3 floorPos = new Vector3(x * spacing, h, z * spacing);
                InstantiateSafe(floorPrefab, floorPos, Quaternion.identity);

                if (h < waterLevel) PlaceWaterSurfaceAt(x, z);

                float slope = GetLocalSlope(x, z);
                if (h >= waterLevel && slope <= maxSlopeForProps)
                    TryPlaceDecor(x, z, h);
            }
        }
    }

    private float GetHeight01(int x, int z)
    {
        float sx = (x + seed) * frequency;
        float sz = (z + seed) * frequency;
        return Mathf.PerlinNoise(sx, sz);
    }

    private float GetLocalSlope(int x, int z)
    {
        float h  = GetHeight01(x, z);
        float hr = GetHeight01(x + 1, z);
        float hu = GetHeight01(x, z + 1);
        return Mathf.Max(Mathf.Abs(h - hr), Mathf.Abs(h - hu));
    }

    private GameObject ChooseFloorPrefab(int x, int z, float h)
    {
        if (h < waterLevel) return PickVariant(sandBlocks, x, z);
        if (h < waterLevel + shoreWidth) return PickVariant(sandBlocks, x, z);
        if (h < highlandStart) return PickVariant(grassBlocks, x, z);
        return PickVariant(rockBlocks, x, z);
    }

    private GameObject PickVariant(GameObject[] options, int x, int z)
    {
        if (options == null || options.Length == 0) return null;
        float m = Mathf.PerlinNoise((x + seed) * scatterFrequency, (z + seed) * scatterFrequency);
        int idx = Mathf.Clamp(Mathf.FloorToInt(m * options.Length), 0, options.Length - 1);
        return options[idx];
    }

    private void PlaceWaterSurfaceAt(int x, int z)
    {
        GameObject surface = waterHalfBlock != null ? waterHalfBlock : waterBlock;
        if (surface == null) return;
        Vector3 pos = new Vector3(x * spacing, waterLevel, z * spacing);
        InstantiateSafe(surface, pos, Quaternion.identity);
    }

    private void TryPlaceDecor(int x, int z, float h)
    {
        float maskA = Mathf.PerlinNoise((x + seed) * (scatterFrequency * 0.77f),
                                        (z + seed) * (scatterFrequency * 0.77f));
        float maskB = Mathf.PerlinNoise((x + seed) * (scatterFrequency * 0.53f),
                                        (z + seed) * (scatterFrequency * 0.53f));

        bool isShore = h < waterLevel + shoreWidth;
        bool isGrass = h >= waterLevel + shoreWidth && h < highlandStart;
        bool isHigh  = h >= highlandStart;

        Vector3 basePos = new Vector3(x * spacing, h, z * spacing);

        if (propPrefabs != null && propPrefabs.Length > 0 && maskB > (1f - propsDensity))
        {
            InstantiateSafe(propPrefabs[prng.Next(propPrefabs.Length)], basePos + Vector3.up * 0.4f, Rot90());
        }

        if (isGrass && treePrefabs != null && treePrefabs.Length > 0 && maskA > (1f - treeDensity))
        {
            InstantiateSafe(PickVariant(treePrefabs, x, z), basePos + Vector3.up * 0.4f, Rot90());
            return;
        }

        if (isGrass && flowerPrefabs != null && flowerPrefabs.Length > 0 && maskA > (1f - flowerDensity))
        {
            InstantiateSafe(PickVariant(flowerPrefabs, x, z), basePos + Vector3.up * 0.35f, Quaternion.identity);
        }

        if ((isShore || !isHigh) && mushroomPrefabs != null && mushroomPrefabs.Length > 0 && maskA > (1f - mushroomDensity))
        {
            InstantiateSafe(PickVariant(mushroomPrefabs, x, z), basePos + Vector3.up * 0.35f, Quaternion.identity);
        }

        if (isHigh && rockPropPrefabs != null && rockPropPrefabs.Length > 0 && maskA > (1f - rockPropDensity))
        {
            InstantiateSafe(PickVariant(rockPropPrefabs, x, z), basePos + Vector3.up * 0.35f, Rot90());
        }
    }

    private GameObject InstantiateSafe(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return null;
        var go = Instantiate(prefab, pos, rot);
        go.transform.SetParent(this.transform, true);
        return go;
    }

    private Quaternion Rot90()
    {
        int k = prng.Next(0, 4);
        return Quaternion.Euler(0f, 90f * k, 0f);
    }

    private void ClearChildren()
    {
        var list = new List<Transform>();
        foreach (Transform c in transform) list.Add(c);
        for (int i = list.Count - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(list[i].gameObject);
            else Destroy(list[i].gameObject);
#else
            Destroy(list[i].gameObject);
#endif
        }
    }

    // NEW: guardar valores actuales
    private void SnapshotValues()
    {
        _lastMapSize          = mapSize;
        _lastSpacing          = spacing;
        _lastSeed             = seed;
        _lastFrequency        = frequency;
        _lastHeightScale      = heightScale;
        _lastWaterLevel       = waterLevel;
        _lastShoreWidth       = shoreWidth;
        _lastHighlandStart    = highlandStart;
        _lastScatterFrequency = scatterFrequency;
        _lastTreeDensity      = treeDensity;
        _lastFlowerDensity    = flowerDensity;
        _lastRockPropDensity  = rockPropDensity;
        _lastMushroomDensity  = mushroomDensity;
        _lastMaxSlopeForProps = maxSlopeForProps;
        _lastPropsDensity     = propsDensity;
    }

    // NEW: comparar
    private bool HasChanged()
    {
        return
            _lastMapSize          != mapSize          ||
            !Mathf.Approximately(_lastSpacing,          spacing)          ||
            _lastSeed             != seed              ||
            !Mathf.Approximately(_lastFrequency,        frequency)        ||
            !Mathf.Approximately(_lastHeightScale,      heightScale)      ||
            !Mathf.Approximately(_lastWaterLevel,       waterLevel)       ||
            !Mathf.Approximately(_lastShoreWidth,       shoreWidth)       ||
            !Mathf.Approximately(_lastHighlandStart,    highlandStart)    ||
            !Mathf.Approximately(_lastScatterFrequency, scatterFrequency) ||
            !Mathf.Approximately(_lastTreeDensity,      treeDensity)      ||
            !Mathf.Approximately(_lastFlowerDensity,    flowerDensity)    ||
            !Mathf.Approximately(_lastRockPropDensity,  rockPropDensity)  ||
            !Mathf.Approximately(_lastMushroomDensity,  mushroomDensity)  ||
            !Mathf.Approximately(_lastMaxSlopeForProps, maxSlopeForProps) ||
            !Mathf.Approximately(_lastPropsDensity,     propsDensity);
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(BiomeGenerator))]
    private class BiomeGeneratorEditor : Editor
    {
        private GUIStyle _titleStyle;
        private GUIStyle _signatureStyle;

        private void EnsureStyles()
        {
            if (_titleStyle == null)
                _titleStyle = new GUIStyle(EditorStyles.boldLabel)
                { fontSize = 16, alignment = TextAnchor.MiddleCenter };

            if (_signatureStyle == null)
                _signatureStyle = new GUIStyle(EditorStyles.boldLabel)
                { fontSize = 12, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        }

        public override void OnInspectorGUI()
        {
            EnsureStyles();

            const string headerTitle  = "Voxel Biome Generator";
            const string authorName   = "Eros Carrasco";
            const string versionLabel = "0.0.1";
            const string dateString   = "2025-10-06";

            GUILayout.Space(4);
            GUILayout.Label(headerTitle, _titleStyle);
            GUILayout.Space(2);
            GUILayout.Label(
                authorName + " — v" + versionLabel +
                "\nIDM - Immersive Media Exercise - " + dateString,
                _signatureStyle
            );
            GUILayout.Space(6);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            DrawDefaultInspector();
        }
    }
#endif
}

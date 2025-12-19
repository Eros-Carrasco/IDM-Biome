using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class VoxelBiomeGenerator : MonoBehaviour
{
    [Header("Grid / World")]
    public int mapSize = 20;
    public float spacing = 1f;

    [Header("Height Map (Perlin)")]
    public int seed = 0;
    [Range(0.01f, 1.0f)] public float frequency = 0.1f;
    public float heightScale = 3f;

    [Header("Height Remap / Quantization")]
    [Tooltip("Remapea el valor de ruido antes de escalar (opcional).")]
    public AnimationCurve heightCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Tooltip("Snapea la altura a escalones para evitar separaciones visuales.")]
    public bool snapHeights = true;

    [Tooltip("Tamaño del escalón vertical (normalmente 1 si tus cubos miden 1 de alto).")]
    public float verticalStep = 1f;

    [Header("Levels")]
    [Tooltip("Altura del espejo de agua (en mismas unidades que heightScale).")]
    public float waterLevel = 1.1f;

    [Tooltip("Ancho de la franja de orilla (arena) por encima del agua.")]
    public float shoreWidth = 0.25f;

    [Tooltip("Inicio de tierras altas/rocosas por encima de orilla.")]
    public float highlandStart = 2.2f;

    [Header("Water Options")]
    [Tooltip("Si está activado, rellena con agua todos los puntos bajo waterLevel (look escalonado). Si está desactivado, SOLO dibuja la lámina superficial.")]
    public bool fillUnderwater = false;

    [Tooltip("Offset vertical de la lámina de agua para evitar z-fighting/solapes con la orilla.")]
    public float surfaceYOffset = 0.0f;

    [Header("Material Variation")]
    [Range(0.01f, 1.0f)] public float variationFreq = 0.23f;

    [Header("Decoration Rules")]
    [Range(0f, 1f)] public float treeDensity = 0.28f;
    [Range(0f, 1f)] public float flowerDensity = 0.35f;
    [Range(0f, 1f)] public float rockDensity = 0.22f;
    [Range(0f, 1f)] public float mushroomDensity = 0.18f;
    [Range(0.0f, 1.0f)] public float slopeLimit = 0.22f;

    [Header("Prefabs (Floor)")]
    public GameObject waterA;     // opcional: agua sólida (para fillUnderwater=true)
    public GameObject waterHalfA; // recomendado para superficie
    public GameObject[] sandVariants;   // Ground_Sand_* + Mix_*
    public GameObject[] grassVariants;  // Ground_Grass_D + Ground_Mix_A
    public GameObject[] rockyVariants;  // variantes altas/rocosas

    [Header("Cliff / Walls")]
    [Tooltip("Rellena paredes entre celdas con distinta altura.")]
    public bool fillCliffs = true;

    [Tooltip("Límite de seguridad para evitar bucles muy profundos.")]
    public int maxCliffDepth = 12;

    [Tooltip("Variantes para paredes. Si está vacío, reusa rocky/sand según altura.")]
    public GameObject[] cliffVariants;

    [Header("Prefabs (Decor)")]
    public GameObject[] treeVariants;     // Tree_A/B
    public GameObject[] flowerVariants;   // Flower_A/B/C
    public GameObject[] rockVariants;     // Rock_A/B
    public GameObject[] mushroomVariants; // Mushroom_A
    public GameObject fenceWood;          // Fence_Wood_A (no usado aún)
    public GameObject[] rareProps;        // Gravestone_A/B, Treasure_A, Key_A

    [Header("Generation")]
    public bool generateOnStart = true;
    public bool clearBeforeGenerate = true;

    private System.Random prng;

    void Start()
    {
        if (generateOnStart) Generate();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) Generate();
    }

    public void Generate()
    {
        if (clearBeforeGenerate) ClearChildren();
        prng = new System.Random(seed);

        for (int x = -mapSize; x < mapSize; x++)
        {
            for (int z = -mapSize; z < mapSize; z++)
            {
                // Altura base (ruido -> curva -> escala -> snap opcional)
                float h = GetSnappedHeight(x, z);

                // --- Piso (si h < waterLevel y NO rellenamos, el piso será arena: ver ChooseFloorPrefab)
                GameObject floorPrefab = ChooseFloorPrefab(x, z, h);
                Vector3 floorPos = new Vector3(x * spacing, h, z * spacing);
                var floor = InstantiateSafe(floorPrefab, floorPos, Quaternion.identity);

                // --- Agua
                if (fillUnderwater)
                {
                    if (h < waterLevel && waterA != null)
                        InstantiateSafe(waterA, floorPos, Quaternion.identity);
                }
                else
                {
                    if (h < waterLevel)
                        PlaceWaterSurfaceAt(x, z);
                }

                // --- Decoración
                float slope = GetLocalSlope(x, z);
                if (slope <= slopeLimit) TryPlaceDecor(x, z, h, floor);

                // --- Relleno de paredes (cliffs)
                if (fillCliffs) FillCliffsAt(x, z, h);
            }
        }

        TryPlaceSimpleBridges(); // hook
    }

    // ----------------------- Perlin / altura / pendiente -----------------------
    private float GetHeight01(int x, int z)
    {
        float sx = (x + seed) * frequency;
        float sz = (z + seed) * frequency;
        return Mathf.PerlinNoise(sx, sz); // [0..1]
    }

    private float GetSnappedHeight(int x, int z)
    {
        float n = GetHeight01(x, z);                 // 0..1
        n = Mathf.Clamp01(heightCurve.Evaluate(n));  // remap por curva
        float h = n * heightScale;                   // escala a unidades

        if (snapHeights && verticalStep > 0f)
            h = Mathf.Round(h / verticalStep) * verticalStep;

        return h;
    }

    private float GetLocalSlope(int x, int z)
    {
        float h  = GetHeight01(x, z);
        float hr = GetHeight01(x + 1, z);
        float hu = GetHeight01(x, z + 1);
        return Mathf.Max(Mathf.Abs(h - hr), Mathf.Abs(h - hu));
    }

    // ----------------------- Piso por bioma -----------------------
    private GameObject ChooseFloorPrefab(int x, int z, float h)
    {
        if (!fillUnderwater && h < waterLevel)
            return PickVariant(sandVariants, x, z); // lecho de agua

        if (fillUnderwater && h < waterLevel)
            return waterA != null ? waterA : PickVariant(sandVariants, x, z);

        if (h < waterLevel + shoreWidth) return PickVariant(sandVariants, x, z);
        if (h < highlandStart)           return PickVariant(grassVariants, x, z);
        return PickVariant(rockyVariants, x, z);
    }

    private GameObject PickVariant(GameObject[] options, int x, int z)
    {
        if (options == null || options.Length == 0) return null;
        float m = Mathf.PerlinNoise((x + seed) * variationFreq, (z + seed) * variationFreq);
        int idx = Mathf.Clamp(Mathf.FloorToInt(m * options.Length), 0, options.Length - 1);
        return options[idx];
    }

    // ----------------------- Agua: sólo superficie -----------------------
    private void PlaceWaterSurfaceAt(int x, int z)
    {
        GameObject surfacePrefab = waterHalfA != null ? waterHalfA : waterA;
        if (surfacePrefab == null) return;

        float y = (snapHeights && verticalStep > 0f)
                  ? Mathf.Round((waterLevel + surfaceYOffset) / verticalStep) * verticalStep
                  : waterLevel + surfaceYOffset;

        Vector3 pos = new Vector3(x * spacing, y, z * spacing);
        InstantiateSafe(surfacePrefab, pos, Quaternion.identity);
    }

    // ----------------------- Cliffs / paredes -----------------------
    private void FillCliffsAt(int x, int z, float topH)
    {
        // alturas vecinas ya con snap/curva
        float hL = GetSnappedHeight(x - 1, z);
        float hR = GetSnappedHeight(x + 1, z);
        float hU = GetSnappedHeight(x, z + 1);
        float hD = GetSnappedHeight(x, z - 1);

        TryFillSide(x, z, topH, hL);
        TryFillSide(x, z, topH, hR);
        TryFillSide(x, z, topH, hU);
        TryFillSide(x, z, topH, hD);
    }

    private void TryFillSide(int x, int z, float topH, float neighborH)
    {
        if (neighborH >= topH) return;
        if (!snapHeights || verticalStep <= 0f) return; // requiere escalones

        float y = topH - verticalStep;
        int steps = 0;

        while (y > neighborH && steps < maxCliffDepth)
        {
            // Elegir material de pared según altura
            GameObject wallPrefab;
            if (y < waterLevel + shoreWidth)
                wallPrefab = PickVariant(sandVariants, x, z);
            else if (cliffVariants != null && cliffVariants.Length > 0)
                wallPrefab = PickVariant(cliffVariants, x, z);
            else
                wallPrefab = PickVariant(rockyVariants, x, z);

            Vector3 pos = new Vector3(x * spacing, y, z * spacing);
            InstantiateSafe(wallPrefab, pos, Quaternion.identity);

            y -= verticalStep;
            steps++;
        }
    }

    // ----------------------- Decorado -----------------------
    private void TryPlaceDecor(int x, int z, float h, GameObject floor)
    {
        if (h < waterLevel) return;

        float mask = Mathf.PerlinNoise((x + seed) * (variationFreq * 0.77f),
                                       (z + seed) * (variationFreq * 0.77f));

        bool isShore = h < waterLevel + shoreWidth;
        bool isGrass = h >= waterLevel + shoreWidth && h < highlandStart;
        bool isHigh  = h >= highlandStart;

        Vector3 basePos = new Vector3(x * spacing, h, z * spacing);

        if (isGrass && treeVariants != null && treeVariants.Length > 0 && mask > (1f - treeDensity))
        {
            InstantiateSafe(PickVariant(treeVariants, x, z), basePos + Vector3.up * 0.5f, Rot90());
            return;
        }

        if (isGrass && flowerVariants != null && flowerVariants.Length > 0 && mask > (1f - flowerDensity))
            InstantiateSafe(PickVariant(flowerVariants, x, z), basePos + Vector3.up * 0.4f, Quaternion.identity);

        if ((isShore || !isHigh) && mushroomVariants != null && mushroomVariants.Length > 0 && mask > (1f - mushroomDensity))
            InstantiateSafe(PickVariant(mushroomVariants, x, z), basePos + Vector3.up * 0.35f, Quaternion.identity);

        if (isHigh && rockVariants != null && rockVariants.Length > 0 && mask > (1f - rockDensity))
            InstantiateSafe(PickVariant(rockVariants, x, z), basePos + Vector3.up * 0.45f, Rot90());

        if (rareProps != null && rareProps.Length > 0)
            if (mask > 0.965f && prng.NextDouble() < 0.02)
                InstantiateSafe(rareProps[prng.Next(rareProps.Length)], basePos + Vector3.up * 0.5f, Rot90());
    }

    private void TryPlaceSimpleBridges() { /* hook opcional */ }

    // ----------------------- Helpers -----------------------
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
            if (Application.isEditor) DestroyImmediate(list[i].gameObject);
            else Destroy(list[i].gameObject);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Regenerate")]
    private void RegenerateContextMenu() => Generate();

    // Inspector con botones
    [CustomEditor(typeof(VoxelBiomeGenerator))]
    private class VoxelBiomeGeneratorInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var gen = (VoxelBiomeGenerator)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("🌍 Regenerate Terrain", GUILayout.Height(28))) gen.Generate();
                if (GUILayout.Button("🎲 Randomize Seed + Regenerate", GUILayout.Height(28)))
                {
                    gen.seed = UnityEngine.Random.Range(0, int.MaxValue);
                    gen.Generate();
                }
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("🧹 Clear Children", GUILayout.Height(24)))
            {
                var method = typeof(VoxelBiomeGenerator).GetMethod("ClearChildren",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(gen, null);
            }

            EditorGUILayout.HelpBox(
                "• snapHeights + verticalStep cierran ‘huecos’ entre escalones.\n" +
                "• fillCliffs rellena paredes entre alturas distintas.\n" +
                "• fillUnderwater OFF dibuja sólo la lámina de agua; usa surfaceYOffset para evitar z-fighting.",
                MessageType.Info);
        }
    }
#endif
}
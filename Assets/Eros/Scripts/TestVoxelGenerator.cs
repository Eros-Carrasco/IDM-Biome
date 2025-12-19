using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TestVoxelGenerator : MonoBehaviour
{
    [Header("Grid / World")]
    public int mapSize = 20;
    public float spacing = 1f;

    [Header("Height Map (Perlin)")]
    public int seed = 0;
    [Range(0.01f, 0.1f)] public float frequency = 0.1f;
    public float heightScale = 3f;

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
    [Range(0.0f, 0.2f)] public float slopeLimit = 0.22f;

    [Header("Extra Decoration Densities")]
    [Tooltip("Probabilidad por celda de colocar un rare item (decoración normal, no súper rara).")]
    [Range(0f, 1f)] public float rareDensity = 0.03f;
    [Tooltip("Probabilidad por celda de colocar una fence.")]
    [Range(0f, 1f)] public float fenceDensity = 0.01f;

    [Header("Prefabs (Floor)")]
    public GameObject waterA;     // opcional: agua sólida (para fillUnderwater=true)
    public GameObject waterHalfA; // recomendado para superficie
    public GameObject[] sandVariants;   // Ground_Sand_* + Mix_*
    public GameObject[] grassVariants;  // Ground_Grass_D + Ground_Mix_A
    public GameObject[] rockyVariants;  // variantes altas/rocosas

    [Header("Prefabs (Decor)")]
    public GameObject[] treeVariants;     // Tree_A/B
    public GameObject[] flowerVariants;   // Flower_A/B/C
    public GameObject[] rockVariants;     // Rock_A/B
    public GameObject[] mushroomVariants; // Mushroom_A
    public GameObject fenceWood;          // Fence_Wood_A
    public GameObject[] rareProps;        // Gravestone_A/B, Treasure_A, Key_A

    [Header("Generation")]
    public bool generateOnStart = true;
    public bool clearBeforeGenerate = true;

    [Header("Editor QoL")]
    [Tooltip("Si está activo, regenerará automáticamente al cambiar sliders en el inspector.")]
    public bool autoRegenerate = true;

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
                float h = GetHeight01(x, z) * heightScale;

                // --- Piso (si no rellenamos bajo el agua, el piso sumergido es arena)
                GameObject floorPrefab = ChooseFloorPrefab(x, z, h);
                Vector3 floorPos = new Vector3(x * spacing, h, z * spacing);
                var floor = InstantiateSafe(floorPrefab, floorPos, Quaternion.identity);

                // --- Agua
                if (fillUnderwater)
                {
                    // Relleno sólido para look escalonado
                    if (h < waterLevel && waterA != null)
                    {
                        InstantiateSafe(waterA, floorPos, Quaternion.identity);
                    }
                }
                else
                {
                    // Solo lámina superficial plana a waterLevel
                    if (h < waterLevel)
                    {
                        PlaceWaterSurfaceAt(x, z);
                    }
                }

                // --- Decoración
                float slope = GetLocalSlope(x, z);
                if (slope <= slopeLimit) TryPlaceDecor(x, z, h, floor);
            }
        }

        TryPlaceSimpleBridges(); // hook opcional
    }

    // ----------------------- Perlin y pendiente -----------------------
    private float GetHeight01(int x, int z)
    {
        float sx = (x + seed) * frequency;
        float sz = (z + seed) * frequency;
        return Mathf.PerlinNoise(sx, sz); // [0..1]
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
        // Bajo el agua: si no rellenamos, usa arena como lecho
        if (!fillUnderwater && h < waterLevel)
        {
            return PickVariant(sandVariants, x, z);
        }

        // Si rellenamos, podríamos devolver agua aquí, pero ya la instanciamos arriba
        if (fillUnderwater && h < waterLevel)
        {
            return waterA != null ? waterA : PickVariant(sandVariants, x, z);
        }

        // Orilla
        if (h < waterLevel + shoreWidth) return PickVariant(sandVariants, x, z);

        // Laderas bajas / pradera
        if (h < highlandStart) return PickVariant(grassVariants, x, z);

        // Alto / rocoso
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

        float y = waterLevel + surfaceYOffset;
        Vector3 pos = new Vector3(x * spacing, y, z * spacing);
        InstantiateSafe(surfacePrefab, pos, Quaternion.identity);
    }

    // ----------------------- Decorado -----------------------
    private void TryPlaceDecor(int x, int z, float h, GameObject floor)
    {
        if (h < waterLevel) return; // evita decorar bajo el agua

        // Un "mask" base para decor general,
        // y máscaras independientes para rare y fences para variar la distribución:
        float mask = Mathf.PerlinNoise((x + seed) * (variationFreq * 0.77f),
                                       (z + seed) * (variationFreq * 0.77f));
        float maskRare  = Mathf.PerlinNoise((x + seed) * (variationFreq * 0.53f),
                                            (z + seed) * (variationFreq * 0.53f));
        float maskFence = Mathf.PerlinNoise((x + seed) * (variationFreq * 0.61f),
                                            (z + seed) * (variationFreq * 0.61f));

        bool isShore = h < waterLevel + shoreWidth;
        bool isGrass = h >= waterLevel + shoreWidth && h < highlandStart;
        bool isHigh  = h >= highlandStart;

        Vector3 basePos = new Vector3(x * spacing, h, z * spacing);

        // --- ITEMS---
        if (rareProps != null && rareProps.Length > 0 && maskRare > (1f - rareDensity))
        {
            InstantiateSafe(rareProps[prng.Next(rareProps.Length)],
                            basePos + Vector3.up * 0.4f, Rot90());
            // sin return: pueden convivir con el resto
        }

        // --- FENCES (antes que árboles, con slider) ---
        if (fenceWood != null && (isShore || isGrass) && maskFence > (1f - fenceDensity))
        {
            InstantiateSafe(fenceWood, basePos, Rot90());
            // sin return
        }

        // --- ÁRBOLES (mantengo tus offsets tal cual) ---
        if (isGrass && treeVariants != null && treeVariants.Length > 0 && mask > (1f - treeDensity))
        {
            InstantiateSafe(PickVariant(treeVariants, x, z), basePos + Vector3.up * 0.4f, Rot90());
            return;
        }

        // --- FLORES (sin cambios) ---
        if (isGrass && flowerVariants != null && flowerVariants.Length > 0 && mask > (1f - flowerDensity))
        {
            InstantiateSafe(PickVariant(flowerVariants, x, z), basePos + Vector3.up * 0.4f, Quaternion.identity);
        }

        // --- HONGOS (sin cambios) ---
        if ((isShore || !isHigh) && mushroomVariants != null && mushroomVariants.Length > 0 && mask > (1f - mushroomDensity))
        {
            InstantiateSafe(PickVariant(mushroomVariants, x, z), basePos + Vector3.up * 0.35f, Quaternion.identity);
        }

        // --- ROCAS (sin cambios) ---
        if (isHigh && rockVariants != null && rockVariants.Length > 0 && mask > (1f - rockDensity))
        {
            InstantiateSafe(PickVariant(rockVariants, x, z), basePos + Vector3.up * 0.35f, Rot90());
        }
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

    // ===== Inspector con Auto Regenerate =====
    [CustomEditor(typeof(TestVoxelGenerator))]
    private class TestVoxelGeneratorInspector : Editor
    {
        // Para evitar múltiples regeneraciones por frame mientras se arrastra un slider.
        private bool _pendingRegen;

        public override void OnInspectorGUI()
        {
            var gen = (TestVoxelGenerator)target;

            // Aviso ligero si el mapa es grande (performance)
            if (gen.mapSize >= 60)
            {
                EditorGUILayout.HelpBox(
                    "mapSize alto puede producir regeneraciones costosas. Considera desactivar Auto Regenerate.",
                    MessageType.Warning);
            }

            // Dibuja el inspector default y detecta cambios
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            bool changed = EditorGUI.EndChangeCheck();

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
                var method = typeof(TestVoxelGenerator).GetMethod("ClearChildren",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(gen, null);
            }

            EditorGUILayout.HelpBox(
                "fillUnderwater OFF ⇒ sólo lámina superficial (recomendado para ríos/lagunas).\n" +
                "surfaceYOffset ayuda a evitar z-fighting con la orilla.\n" +
                "Pulsa R en Play para regenerar.",
                MessageType.Info);

            // Auto Regenerate: sólo si está activado, fuera de Play, y hubo cambios
            if (changed && gen.autoRegenerate && !Application.isPlaying)
            {
                // Debounce con delayCall para colapsar múltiples cambios del mismo frame
                if (_pendingRegen) return;
                _pendingRegen = true;
                EditorApplication.delayCall += () =>
                {
                    if (gen == null) return;
                    _pendingRegen = false;

                    // Evitar regenerar si se desactivó el toggle mientras tanto
                    if (gen.autoRegenerate && !Application.isPlaying)
                    {
                        gen.Generate();
                    }
                };
            }
        }
    }
#endif
}
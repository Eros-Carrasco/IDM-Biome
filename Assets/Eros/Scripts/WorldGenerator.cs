using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class WorldGenerator : MonoBehaviour
{
    // ---- Header (oculto en el inspector) ----
    [HideInInspector] public string headerTitle  = "World Generator";
    [HideInInspector] public string authorName   = "Eros Carrasco";
    [HideInInspector] public string versionLabel = "0.0.1";
    [HideInInspector] public string dateString   = "2025-10-06";

    // ---- Prefabs ----
    [Header("Prefabs")]
    public GameObject voxel;
    public GameObject water;
    public GameObject flower;

    // ---- Map ----
    [Header("Map")]
    public int mapSize = 10;
    public float spacing = 1f;

    // ---- Noise / Height ----
    [Header("Noise / Height")]
    public float continuity = 0.1f; // Perlin frequency
    public float maxHeight = 3f;    // Altura máxima

    // ---- Quantize / Columns ----
    [Header("Quantize / Columns")]
    public bool snapHeights = true; // Redondear alturas a escalones
    public float stepHeight = 1f;   // Altura del bloque
    public bool fillColumns = true; // Rellenar desde el suelo hasta la cima

    // ---- Water ----
    [Header("Water")]
    public float waterLevel = 1.1f; // Altura de la superficie de agua

    // ---- Generation ----
    public void Generate()
{
    ClearChildren();

    float randomSeed = Random.Range(0f, 1000f);

    // Ajustes de cuantización para terreno y agua
    float quantizedWaterHeight = snapHeights && stepHeight > 0f
        ? Mathf.Round(waterLevel / stepHeight) * stepHeight
        : waterLevel;

    for (int gridX = -mapSize; gridX < mapSize; gridX++)
    {
        for (int gridZ = -mapSize; gridZ < mapSize; gridZ++)
        {
            // Altura continua por Perlin en [0..maxHeight]
            float perlinValue = Mathf.PerlinNoise(gridX * continuity + randomSeed,
                                                  gridZ * continuity + randomSeed);
            float continuousTopHeight = perlinValue * maxHeight;

            // Cuantizamos a capas (enteros) del alto real del bloque
            float snappedTopHeight = snapHeights && stepHeight > 0f
                ? Mathf.Round(continuousTopHeight / stepHeight) * stepHeight
                : continuousTopHeight;

            // Nº de capas de terreno (0,1,2,...) — ¡ojo: índices, no altura!
            int terrainLayerCount = stepHeight > 0f
                ? Mathf.Max(0, Mathf.RoundToInt(snappedTopHeight / stepHeight))
                : 0;

            // Coordenadas en mundo para esta celda
            float positionX = gridX * spacing;
            float positionZ = gridZ * spacing;

            if (fillColumns && stepHeight > 0f)
            {
                // Terreno: apilar desde layer 0 hasta layer (terrainLayerCount - 1)
                for (int layerIndex = 0; layerIndex < terrainLayerCount; layerIndex++)
                {
                    float blockCenterY = (layerIndex + 0.5f) * stepHeight;
                    GameObject groundBlock = Object.Instantiate(
                        voxel, new Vector3(positionX, blockCenterY, positionZ), Quaternion.identity);
                    groundBlock.transform.SetParent(transform, true);
                }

                // Agua: si la cima está por debajo del agua, rellenar capas de agua
                int waterTopLayer = stepHeight > 0f
                    ? Mathf.RoundToInt(quantizedWaterHeight / stepHeight)
                    : 0;

                if (terrainLayerCount * stepHeight < quantizedWaterHeight)
                {
                    for (int layerIndex = terrainLayerCount; layerIndex < waterTopLayer; layerIndex++)
                    {
                        float waterCenterY = (layerIndex + 0.5f) * stepHeight;
                        GameObject waterBlock = Object.Instantiate(
                            water, new Vector3(positionX, waterCenterY, positionZ), Quaternion.identity);
                        waterBlock.transform.SetParent(transform, true);
                    }
                }
            }
            else
            {
                // Solo un bloque en la cima, centrado en su capa
                float topCenterY = stepHeight > 0f
                    ? (Mathf.Max(terrainLayerCount, 1) - 0.5f) * stepHeight
                    : snappedTopHeight;

                GameObject groundBlock = Object.Instantiate(
                    voxel, new Vector3(positionX, topCenterY, positionZ), Quaternion.identity);
                groundBlock.transform.SetParent(transform, true);

                // Lámina de agua: centrada en su capa más cercana
                if (snappedTopHeight < quantizedWaterHeight && stepHeight > 0f)
                {
                    int waterLayer = Mathf.RoundToInt(quantizedWaterHeight / stepHeight) - 1;
                    float waterCenterY = (waterLayer + 0.5f) * stepHeight;

                    GameObject waterSurface = Object.Instantiate(
                        water, new Vector3(positionX, waterCenterY, positionZ), Quaternion.identity);
                    waterSurface.transform.SetParent(transform, true);
                }
            }

            // Decoración: sobre la última capa de terreno
            if (terrainLayerCount > 0)
            {
                float flowerY = (terrainLayerCount - 0.5f) * stepHeight + 0.5f * stepHeight;
                if (snappedTopHeight >= quantizedWaterHeight &&
                    snappedTopHeight < quantizedWaterHeight + maxHeight * 0.4f &&
                    Random.Range(0, 100) < 12)
                {
                    GameObject flowerObject = Object.Instantiate(
                        flower, new Vector3(positionX, flowerY, positionZ), Quaternion.identity);
                    flowerObject.transform.SetParent(transform, true);
                }
            }
        }
    }
}

    public void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(child.gameObject);
                continue;
            }
#endif
            Object.Destroy(child.gameObject);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(WorldGenerator))]
    private class WorldGeneratorEditor : Editor
    {
        private GUIStyle titleStyle;
        private GUIStyle signatureStyle;

        private void EnsureStyles()
        {
            titleStyle ??= new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 16, alignment = TextAnchor.MiddleCenter };

            signatureStyle ??= new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 12, alignment = TextAnchor.MiddleCenter };
        }

        public override void OnInspectorGUI()
        {
            EnsureStyles();
            serializedObject.Update();

            var generator = (WorldGenerator)target;

            // Header
            GUILayout.Space(4);
            GUILayout.Label(generator.headerTitle, titleStyle);
            GUILayout.Space(2);
            GUILayout.Label($"by {generator.authorName} — v{generator.versionLabel}\nIDM - Immersive Media Exercise - {generator.dateString}", signatureStyle);
            GUILayout.Space(6);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(4);

            // Buttons
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("🌍 Regenerate Environment", GUILayout.Height(26)))
                {
                    Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Regenerate World");
                    generator.Generate();
                    EditorUtility.SetDirty(generator);
                }

                if (GUILayout.Button("🧹 Clear", GUILayout.Height(26)))
                {
                    Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Clear World");
                    generator.ClearChildren();
                    EditorUtility.SetDirty(generator);
                }
            }

            GUILayout.Space(8);
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
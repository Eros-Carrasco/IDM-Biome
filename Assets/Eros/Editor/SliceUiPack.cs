using UnityEngine;
using UnityEditor;

public class SliceUiPack : EditorWindow
{
    // 📂 Carpetas donde están tus imágenes
    string[] folders = {
        "Assets/Eros/UI/UI PACK PNG/Buttons",
        "Assets/Eros/UI/UI PACK PNG/Backgrounds",
        "Assets/Eros/UI/UI PACK PNG/Progress Bars",
        "Assets/Eros/UI/UI PACK PNG/UI Icons"
    };

    // 🔲 Bordes del 9-slice (ajústalos según tu sprite)
    int left = 16, right = 16, top = 16, bottom = 16;

    [MenuItem("Eros/Apply 9-Slice to UI Pack")]
    public static void ShowWindow()
    {
        GetWindow<SliceUiPack>("UI 9-Slice");
    }

    void OnGUI()
    {
        GUILayout.Label("📦 UI 9-Slice Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        left   = EditorGUILayout.IntField("Left", left);
        right  = EditorGUILayout.IntField("Right", right);
        top    = EditorGUILayout.IntField("Top", top);
        bottom = EditorGUILayout.IntField("Bottom", bottom);

        EditorGUILayout.Space();
        if (GUILayout.Button("✅ Apply 9-Slice to All UI Folders", GUILayout.Height(30)))
        {
            Apply();
        }
    }

    void Apply()
    {
        int totalProcessed = 0;

        foreach (var folder in folders)
        {
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.spriteBorder = new Vector4(left, bottom, right, top); // L,B,R,T

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                totalProcessed++;
            }
        }

        Debug.Log($"✅ 9-slice aplicado correctamente a {totalProcessed} sprites.");
    }
}
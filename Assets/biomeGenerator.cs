using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways] // This line makes this script run in editor-mode and prefab-editing-mode in adittion to runtime (playmode)
public class biomeGenerator : MonoBehaviour
{
    public GameObject flower;
    [Header("Voxel Settings")]
    public GameObject voxel;
    public int mapSize = 10;
    public float spacing = 1.0f;

    public float depth = 3.0f;
    

#if UNITY_EDITOR
    private void OnValidate() //this method is called automatically when a value is changed in the Inspector
    {
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += () => // Runs this code after the Unity Editor finishes processing the value modified in the inspector. Without this it would crash
            {
                if (this != null)
                    GenerateBiome();
            };
        }
    }
#endif

    private void GenerateBiome()
    {
        ClearChildren();

        for (int x = -mapSize; x < mapSize; x++)
        {
            for (int z = -mapSize; z < mapSize; z++)
            {
                float y = Mathf.PerlinNoise(x * 0.1f, z * 0.1f) * depth;
                Vector3 pos = new Vector3(x * spacing, y, z * spacing);
                var newVoxel = Instantiate(voxel, pos, Quaternion.identity);
                newVoxel.transform.parent = gameObject.transform;
//                Debug.Log(y);
                if (y > 6 && Random.Range(0, 100) < 50)
                {
                    var pos2 = new Vector3(x * spacing, y + 0.5f, z * spacing);
                    var newAgent = Instantiate(flower, pos2, Quaternion.identity);
                    newAgent.transform.parent = gameObject.transform;
                    
                }
            }
        }
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
}

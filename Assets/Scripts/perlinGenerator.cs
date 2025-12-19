using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using NavMeshBuilder = UnityEngine.AI.NavMeshBuilder;

// Build and update a localized navmesh from the sources marked by NavMeshSourceTag
[DefaultExecutionOrder(-102)]

public class perlinGenerator : MonoBehaviour
{
    public GameObject agent;
    [Header("Voxel Settings")]
    public GameObject voxel;
    public int mapSize = 10;
    public float spacing = 1.0f;
    // Start is called before the first frame update
    
    void Start()
    {
        for (int x = -mapSize; x < mapSize; x++)
        {
            for (int z = -mapSize; z < mapSize; z++)
            {
                float y = Mathf.PerlinNoise(x * 0.1f, z * 0.1f) * 3.0f;
                Vector3 pos = new Vector3(x * spacing, y, z * spacing);
                var newVoxel = Instantiate(voxel, pos, Quaternion.identity);
                newVoxel.transform.parent = gameObject.transform;
                if (y > 2.5f)
                {
                    var newAgent = Instantiate(agent, pos, Quaternion.identity);
                    newAgent.transform.parent = GameObject.Find("Agents Holder").transform;
                }
            }
        }
    }

    

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class scatterer : MonoBehaviour
{
    public GameObject prefab;
    public int numberOfPrefabs;
    //I end up ignoring the y axis later
    public Vector3 scatterArea;
    public Texture[] textures;
    //unity people know this: start function is called when the script is enabled
    void Start()
    {
        ScatterPrefabs();
    }
    void ScatterPrefabs()
    {
        for (int i = 0; i < numberOfPrefabs; i++)
        {
            Vector3 randomPosition = transform.position + new Vector3(Random.Range(-scatterArea.x, scatterArea.x), 0f, Random.Range(-scatterArea.z, scatterArea.z));
            
            GameObject newPrefab = Instantiate(prefab, randomPosition, Quaternion.identity);
            
            // Assign a random texture to the prefab
            Renderer prefabRenderer = newPrefab.GetComponent<Renderer>();
            if (prefabRenderer != null)
            {
                Texture randomTexture = GetRandomTexture();
                prefabRenderer.material.mainTexture = randomTexture;
            }
        }
    }
    Texture GetRandomTexture()
    {
        int randomIndex = Random.Range(0, textures.Length);
        return textures[randomIndex];
    }
}

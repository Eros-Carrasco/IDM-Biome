using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaveGenerator : MonoBehaviour
{

    //public GameObject goblinPrefab;
    [Header("Cave Settings")]
    public GameObject column;

    public float continuity = 0.1f;
    public int mapSize = 10;
    public float spacing = 1.0f;

    public float depth = 1.0f;
    
    // Start is called before the first frame update
    void Start()
    {
        var randomSeed = Random.Range(0.0f, 1f);
        //loop through all the positions in our map
        //notice that we have a nested loop, one inside the other in order to create z coordinates for each x coordinate
        for (int x = -mapSize; x < mapSize; x++)
        {
            for (int z = -mapSize; z < mapSize; z++)
            {
                //grab a perlin noise value between 0 and 1 at our current position in the loop
                //scale the x and z values by 0.1 to make the perlin noise more gradual from point to point, otherwise our noise won't be very smooth
                float value = Mathf.PerlinNoise((x * continuity) + randomSeed, (z * continuity) + randomSeed) * depth;
                Vector3 pos = new Vector3(x * spacing, 0f, z * spacing);

                if (value > 0.2f)
                {
                    var newColumn = Instantiate(column, pos, transform.rotation);
                    newColumn.transform.parent = gameObject.transform;
                    
                }
                else {
	                 
								//Populate Cave!
                // if (value > 7f && Random.Range(0, 100) < 3){
                //     Vector3 pos = new Vector3(x * spacing, value + 0.5f, z * spacing);
                //     var goblin = Instantiate(goblinPrefab, pos, Quaternion.identity);
                //     goblin.transform.parent = gameObject.transform;
                // }
                }
               

            }
        }
    }

    

}

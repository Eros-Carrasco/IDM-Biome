using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class meshDipsplacement : MonoBehaviour
{
    //displace the mesh using perline noise
    public float scale = 1.0f;
    public float speed = 1.0f;
    public Vector3[] baseHeight;
    public Mesh mesh;
    void Start() {
        mesh = GetComponent<MeshFilter>().mesh;
        baseHeight = mesh.vertices;
        //loop through vertices and displace them
        for (int i = 0; i < baseHeight.Length; i++)
        {
            baseHeight[i].y = Mathf.PerlinNoise(baseHeight[i].x * scale + Time.time * speed, baseHeight[i].z * scale + Time.time * speed);
        }
    }
}

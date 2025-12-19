using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spawner : MonoBehaviour
{
    public GameObject boidPrefab;
    public int boids;
    public Vector3 scatterArea;
    // Start is called before the first frame update
    public List<GameObject> boidList;
    public float sepDistance = 3.0f;
    public float sepWeight = 1.0f;
    public float alignDistance = 12.0f;
    public float alignWeight = 1.0f;
    public float cohDistance = 6.0f;
    public float cohWeight = 1.0f;
    public float speed = 1.0f;
    public float rotationSpeed = 2.0f;
    public float goalWeight = 1.0f;
    public Vector3 goalPosition;

    void Start()
    {
        for (int i = 0; i < boids; i++)
        {
            Vector3 randomPosition = transform.position + new Vector3(Random.Range(-scatterArea.x, scatterArea.x), 10f, Random.Range(-scatterArea.z, scatterArea.z));
            Vector3 randomRotation = new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
            GameObject newPrefab = Instantiate(boidPrefab, randomPosition, Quaternion.Euler(randomRotation));
            newPrefab.transform.parent = gameObject.transform;
            boidList.Add(newPrefab);
        }
    }

    Vector3 Separation(GameObject boid) {
        if (Random.Range(0.0f, 1.0f) > 0.2f) {
           //rule 1: boids try to fly towards the centre of mass of neighbouring boids
        Vector3 separationVector = Vector3.zero;
        foreach (GameObject otherBoid in boidList)
        {
            //if the boid is not itself
            if (otherBoid != boid)
            {
                if (Vector3.Distance(otherBoid.transform.position, boid.transform.position) < sepDistance)
                {
                    Vector3 toNeighbor = boid.transform.position - otherBoid.transform.position;
                    separationVector += toNeighbor.normalized;
                }
            }
        }
        return separationVector.normalized;
        }
        return Vector3.zero;
    }

    Vector3 Alignment(GameObject boid) {
        //rule 1: boids try to fly towards the centre of mass of neighbouring boids
        Vector3 alignmentVector = Vector3.zero;
        int count = 0;
        foreach (GameObject otherBoid in boidList)
        {
            //if the boid is not itself
            if (otherBoid != boid)
            {
                if (Vector3.Distance(otherBoid.transform.position, boid.transform.position) < alignDistance)
                {
                    alignmentVector += otherBoid.transform.forward;
                    count++;
                }
            }
            if (count > 0)
            {
                alignmentVector /= count;
            }
        }
        return alignmentVector.normalized;
    }
    Vector3 Cohesion(GameObject boid) {
        //rule 1: boids try to fly towards the centre of mass of neighbouring boids
        Vector3 centerOfMass = Vector3.zero;
        int count = 0;
        foreach (GameObject otherBoid in boidList)
        {
            //if the boid is not itself
            if (otherBoid != boid)
            {
                if (Vector3.Distance(otherBoid.transform.position, boid.transform.position) < cohDistance)
                {
                    centerOfMass += otherBoid.transform.position;
                    count++;
                }
                if (count > 0)
                {
                    centerOfMass /= count;
                    Vector3 toCenter = centerOfMass - boid.transform.position;
                    return toCenter.normalized;
                }
                return Vector3.zero;
            }
        }
        return Vector3.zero;
    }

    Vector3 SeekGoal(GameObject boid) {
        Vector3 toGoal = goalPosition - boid.transform.position;
        return toGoal.normalized;
    }

    void Update()
    {
        //we dont want to apply the rules every frame
        if (Random.Range(0.0f, 1.0f) > 0.50f) {
            foreach (GameObject boid in boidList)
            {
                Vector3 sep = Separation(boid);
                Vector3 align = Alignment(boid);
                Vector3 coh = Cohesion(boid);
                Vector3 goalForce = SeekGoal(boid);
                Vector3 combined = ((sep * sepWeight) + (align * alignWeight) + (coh * cohWeight) + (goalForce * goalWeight));
                combined += boid.transform.forward;
                float newSpeed = speed + Random.Range(-0.5f, 0.5f);
                boid.transform.Translate(combined * Time.deltaTime * newSpeed);
                if (combined != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(combined.normalized);
                    boid.transform.rotation = Quaternion.Slerp(boid.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                }
            }  
        }
    }
}

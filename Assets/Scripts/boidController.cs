using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class boidController : MonoBehaviour
{
    public GameObject swarmThing;
    public Slider speedSlider;
    // Start is called before the first frame update
    void Start()
    {
        if (swarmThing == null) {
            swarmThing = GameObject.Find("swarm");
        }
        speedSlider.onValueChanged.AddListener(SpeedMethod);
    }
    public void SpeedMethod(float value)
    {
        Debug.Log("Speed: " + value);
        swarmThing.GetComponent<spawnerOptimized>().speed = value;
    }
}

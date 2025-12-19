using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class AI : MonoBehaviour
{
    public Transform player;
    public UnityEngine.AI.NavMeshAgent agent;
    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > 1f)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            agent.SetDestination(Vector3.zero);
        }
    }
}

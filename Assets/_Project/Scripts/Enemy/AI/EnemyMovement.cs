using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    NavMeshAgent agent;
    public float DestinationInterval = 0.25f;
    float nextSet;

    public NavMeshAgent Agent => agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void Configure(float speed)
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.speed = speed;
    }

    public void Stop()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;
        agent.isStopped = true;
        agent.ResetPath();
    }

    public void MoveTo(Vector3 destination)
    {
        if (agent == null || !agent.enabled)
            return;
        if (!agent.isOnNavMesh)
            return;
        if (Time.time < nextSet)
            return;

        nextSet = Time.time + DestinationInterval;
        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    public void Face(Vector3 worldPoint, float turnSpeed)
    {
        Vector3 dir = worldPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            return;
        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
    }
}

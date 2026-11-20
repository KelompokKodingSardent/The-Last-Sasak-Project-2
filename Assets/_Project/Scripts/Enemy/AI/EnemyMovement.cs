using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Coroutine _lungeRoutine;

    public bool IsMoving => _agent != null && _agent.enabled && _agent.velocity.sqrMagnitude > 0.05f;
    public bool IsLunging => _lungeRoutine != null;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false; // Rotasi dikontrol manual agar responsif saat telegraph
    }

    public void MoveTo(Vector3 destination, float speed)
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

        _agent.isStopped = false;
        _agent.speed = speed;
        _agent.SetDestination(destination);
    }

    public void Stop()
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

        _agent.isStopped = true;
        _agent.ResetPath();
    }

    public void FacePosition(Vector3 worldTarget, float rotationSpeed)
    {
        Vector3 dir = worldTarget - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    public void FacePositionImmediate(Vector3 worldTarget)
    {
        Vector3 dir = worldTarget - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    /// <summary>
    /// Mendukung jarak positif (lunge maju) maupun negatif (retreat backstep).
    /// </summary>
    public void PerformLunge(Vector3 direction, float distance, float duration)
    {
        if (Mathf.Abs(distance) <= 0.01f || duration <= 0.01f) return;

        if (_lungeRoutine != null)
            StopCoroutine(_lungeRoutine);

        Vector3 moveDir = direction.normalized * Mathf.Sign(distance);
        _lungeRoutine = StartCoroutine(LungeRoutine(moveDir, Mathf.Abs(distance), duration));
    }

    public void CancelLunge()
    {
        if (_lungeRoutine != null)
        {
            StopCoroutine(_lungeRoutine);
            _lungeRoutine = null;
        }
    }

    private IEnumerator LungeRoutine(Vector3 dir, float distance, float duration)
    {
        Stop();
        float elapsed = 0f;
        float speed = distance / duration;

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.Move(dir * (speed * dt));
            }
            elapsed += dt;
            yield return null;
        }

        _lungeRoutine = null;
    }

    public void ApplyNeighborSeparation(IReadOnlyList<EnemyBrain> neighbors, float radius, float strength, float dt)
    {
        if (neighbors == null || radius <= 0.01f || strength <= 0.01f) return;
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh || IsLunging) return;

        Vector3 push = Vector3.zero;
        float radiusSqr = radius * radius;
        Vector3 myPos = transform.position;

        for (int i = 0; i < neighbors.Count; i++)
        {
            EnemyBrain other = neighbors[i];
            if (other == null || other.gameObject == gameObject || other.CurrentState == EnemyBrainState.Dead)
                continue;

            Vector3 diff = myPos - other.transform.position;
            diff.y = 0f;
            float sqrDist = diff.sqrMagnitude;

            if (sqrDist < 0.0001f)
            {
                push += transform.right * 0.5f;
                continue;
            }

            if (sqrDist < radiusSqr)
            {
                float dist = Mathf.Sqrt(sqrDist);
                float weight = (radius - dist) / radius;
                push += (diff / dist) * weight;
            }
        }

        if (push.sqrMagnitude > 0.0001f)
        {
            Vector3 clampedPush = Vector3.ClampMagnitude(push, 1.25f) * (strength * dt);
            _agent.Move(clampedPush);
        }
    }

    public Vector3 GetRepositionPoint(Vector3 playerPos, float retreatDistance)
    {
        Vector3 away = (transform.position - playerPos);
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = -transform.forward;
        away.Normalize();

        float sideAngle = Random.Range(-42f, 42f);
        Vector3 candidate = transform.position + (Quaternion.Euler(0f, sideAngle, 0f) * away * retreatDistance);

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            return hit.position;

        return transform.position;
    }

    public Vector3 GetFlankDodgePoint(Vector3 playerPos, float preferredDistance, float sideSign, float dashDistance)
    {
        Vector3 toPlayer = playerPos - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.01f) toPlayer = transform.forward;
        toPlayer.Normalize();

        Vector3 lateral = Vector3.Cross(Vector3.up, toPlayer) * Mathf.Sign(sideSign);
        float currentDist = Vector3.Distance(transform.position, playerPos);
        float radialBias = currentDist > preferredDistance ? 0.35f : -0.25f;

        Vector3 dashDir = (lateral + toPlayer * radialBias).normalized;
        Vector3 candidate = transform.position + dashDir * dashDistance;

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.8f, NavMesh.AllAreas))
            return hit.position;

        Vector3 oppositeCandidate = transform.position - lateral * dashDistance;
        if (NavMesh.SamplePosition(oppositeCandidate, out NavMeshHit oppHit, 1.8f, NavMesh.AllAreas))
            return oppHit.position;

        return transform.position;
    }

    /// <summary>
    /// Mencari titik geser menyamping di NavMesh ketika garis tembak Ranger terhalang pilar/obstacle.
    /// </summary>
    public Vector3 GetLateralSightlinePoint(Vector3 playerPos, float sideSign, float stepDistance)
    {
        Vector3 toPlayer = playerPos - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.01f) toPlayer = transform.forward;
        toPlayer.Normalize();

        Vector3 lateral = Vector3.Cross(Vector3.up, toPlayer) * Mathf.Sign(sideSign);
        Vector3 candidate = transform.position + lateral * stepDistance;

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.2f, NavMesh.AllAreas))
            return hit.position;

        Vector3 opposite = transform.position - lateral * stepDistance;
        if (NavMesh.SamplePosition(opposite, out NavMeshHit oppHit, 2.2f, NavMesh.AllAreas))
            return oppHit.position;

        return transform.position;
    }
}
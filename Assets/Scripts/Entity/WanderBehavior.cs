using UnityEngine;

[System.Serializable]
public class WanderBehavior
{
    public enum WanderMode
    {
        AroundAnchor,
        AroundSpawn,
        AroundSelf
    }

    [SerializeField] private WanderMode mode = WanderMode.AroundSelf;
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float wanderSpeedMultiplier = 0.5f;
    [SerializeField] private float idleTimeMin = 0.5f;
    [SerializeField] private float idleTimeMax = 1.5f;

    private Vector3 wanderTarget;
    private Vector3 spawnPoint;
    private bool spawnPointSet;
    private float idleTimer;
    private bool isIdling;

    public bool IsIdling => isIdling;

    public void Tick(Transform self, Transform anchor, MovementBase movement)
    {
        if (!spawnPointSet)
        {
            spawnPoint = self.position;
            spawnPointSet = true;
        }

        Vector3 center = GetCenter(self, anchor);
        float distToCenter = Vector3.Distance(self.position, center);

        if (distToCenter > wanderRadius)
        {
            isIdling = false;
            wanderTarget = Vector3.zero;
            movement.ResetSpeedMultiplier();
            movement.MoveToTarget(center);
            return;
        }

        if (isIdling)
        {
            movement.StopMoving();
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                isIdling = false;
                wanderTarget = GetRandomPoint(center, wanderRadius);
            }
            return;
        }

        if (wanderTarget == Vector3.zero)
            wanderTarget = GetRandomPoint(center, wanderRadius);

        var agent = movement.GetAgent();
        bool reached = agent != null && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance;

        if (reached)
        {
            isIdling = true;
            idleTimer = Random.Range(idleTimeMin, idleTimeMax);
            movement.StopMoving();
            return;
        }

        movement.SetSpeedMultiplier(wanderSpeedMultiplier);
        movement.MoveToTarget(wanderTarget);
    }

    public void Reset(MovementBase movement)
    {
        isIdling = false;
        wanderTarget = Vector3.zero;
        movement.ResetSpeedMultiplier();
    }

    private Vector3 GetCenter(Transform self, Transform anchor)
    {
        switch (mode)
        {
            case WanderMode.AroundSpawn:
                return spawnPoint;
            case WanderMode.AroundSelf:
                return self.position;
            case WanderMode.AroundAnchor:
            default:
                return anchor != null ? anchor.position : self.position;
        }
    }

    private Vector3 GetRandomPoint(Vector3 center, float radius)
    {
        Vector2 random = Random.insideUnitCircle * radius;
        return center + new Vector3(random.x, 0f, random.y);
    }
}
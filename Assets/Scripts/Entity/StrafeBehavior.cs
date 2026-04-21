using UnityEngine;

[System.Serializable]
public class StrafeBehavior
{
    [SerializeField] private float preferredDistMin = 0f;
    [SerializeField] private float preferredDistMax = 0f;
    [SerializeField] private float angleMin = 30f;
    [SerializeField] private float angleMax = 90f;
    [SerializeField] private float idleTimeMin = 0.3f;
    [SerializeField] private float idleTimeMax = 0.8f;

    private bool isIdling = false;
    private float idleTimer = 0f;
    private Vector3 strafeTarget = Vector3.zero;
    private int strafeDir = 0;

    public void Init(float attackRange)
    {
        if (preferredDistMin <= 0f) preferredDistMin = attackRange * 0.7f;
        if (preferredDistMax <= 0f) preferredDistMax = attackRange;
    }

    public void Tick(Transform self, Vector3 targetPos, MovementBase movement)
    {
        movement.FaceTarget(targetPos);

        if (isIdling)
        {
            movement.StopMoving();
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f) Reset();
            return;
        }

        if (strafeTarget == Vector3.zero)
        {
            float preferredDist = Random.Range(preferredDistMin, preferredDistMax);
            strafeTarget = GetStrafePoint(self.position, targetPos, preferredDist);
        }

        var agent = movement.GetAgent();
        bool reached = agent != null && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.1f;
        if (reached)
        {
            isIdling = true;
            idleTimer = Random.Range(idleTimeMin, idleTimeMax);
            movement.StopMoving();
            return;
        }

        movement.MoveToTarget(strafeTarget);
    }

    public void Reset()
    {
        isIdling = false;
        strafeTarget = Vector3.zero;
    }

    private Vector3 GetStrafePoint(Vector3 selfPos, Vector3 targetPos, float preferredDist)
    {
        if (strafeDir == 0) strafeDir = Random.value > 0.5f ? 1 : -1;
        if (Random.value < 0.25f) strafeDir *= -1;

        Vector3 toEnemy = selfPos - targetPos;
        toEnemy.y = 0f;
        if (toEnemy.sqrMagnitude < 0.001f) toEnemy = Vector3.forward;

        float currentAngle = Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg;
        float angle = currentAngle + strafeDir * Random.Range(angleMin, angleMax);
        Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

        Vector3 candidate = targetPos + dir * preferredDist;
        candidate.y = selfPos.y;
        return candidate;
    }
}
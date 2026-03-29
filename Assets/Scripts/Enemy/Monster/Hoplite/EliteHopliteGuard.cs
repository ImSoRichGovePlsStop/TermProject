using System.Collections;
using UnityEngine;

public class EliteHopliteGuard : MonoBehaviour
{
    [Header("Guard")]
    [SerializeField] private float guardDuration = 1.0f;
    [SerializeField] private float guardCooldown = 3.0f;
    [SerializeField] private float triggerRange = 2.2f;
    [SerializeField] private float triggerChance = 0.35f;

    [Header("References")]
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyAttack attack;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform player;

    private float lastGuardTime = -Mathf.Infinity;
    private bool isGuarding = false;

    public bool IsGuarding => isGuarding;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<EnemyMovement>();

        if (attack == null)
            attack = GetComponentInChildren<EnemyAttack>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (player == null) return;
        if (isGuarding) return;
        if (Time.time < lastGuardTime + guardCooldown) return;

        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = player.position; b.y = 0f;
        float distance = Vector3.Distance(a, b);

        if (distance <= triggerRange && Random.value <= triggerChance)
        {
            StartCoroutine(GuardRoutine());
        }
    }

    private IEnumerator GuardRoutine()
    {
        isGuarding = true;
        lastGuardTime = Time.time;

        if (movement != null)
        {
            movement.StopMoving();
            movement.SetCanMove(false);
        }

        if (attack != null)
            attack.ForceStopAttack();

        // หันครั้งเดียวก่อนเข้า block
        if (movement != null && player != null)
            movement.FaceTarget(player.position);

        if (animator != null)
            animator.SetBool("IsGuarding", true);

        yield return new WaitForSeconds(guardDuration);

        if (animator != null)
            animator.SetBool("IsGuarding", false);

        if (movement != null)
            movement.SetCanMove(true);

        isGuarding = false;
    }
}
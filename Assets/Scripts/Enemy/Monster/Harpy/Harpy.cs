using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class Harpy : MonoBehaviour
{
    enum HarpyState
    {
        Ground,
        Hover, //fly in the sky
        DiveAttack,
        Recover
    }

    HarpyState currentState;
    private Rigidbody rb;
    private EnemyController controller;
    private EnemyMovement movement;
    private EnemyAttack attack;
    private EnemyHealth health;
    private bool hasEnteredAirPhase = false;
    Vector3 diveTarget;

    [SerializeField] private Animator animator;
    [SerializeField] float diveSpeed = 7f;
    [SerializeField] float groundOffset = 0.2f;
    [SerializeField] Transform player;
    [SerializeField] SpriteRenderer spriteRenderer;
    
    float diveStartTime;
    float lastDiveTime = -Mathf.Infinity;
    private bool isDiving = false;
    private float baseY;
    private float groundEnterTime;

    [Header("Variant Settings")]
    [SerializeField] float groundCooldown = 2.5f;
    [SerializeField] float diveCooldown = 2f;
    [SerializeField] float flyPhaseHPPercent = 0.5f;
    [SerializeField] float sizeMultiplier = 1f;
    [SerializeField] float damageMultiplier = 1f;
    [SerializeField] float attackRangeMultiplier = 1f;
    [SerializeField] float diveSpeedMultiplier = 1f;
    [SerializeField] float hoverHeight = 1.3f;
    [SerializeField] float hoverSpeed = 1f;
    [SerializeField] float hoverAmplitude = 0.3f;
    [SerializeField] float diveTriggerDistance = 3.5f;
    [SerializeField] float groundOffsetMultiplier = 1f;

    void Awake()
    {
        controller = GetComponent<EnemyController>();
        movement = GetComponent<EnemyMovement>();
        attack = GetComponentInChildren<EnemyAttack>();
        health = GetComponent<EnemyHealth>();
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        baseY = transform.position.y;
        currentState = HarpyState.Ground;
        
        transform.localScale *= sizeMultiplier;
        diveSpeed *= diveSpeedMultiplier;
        attack.SetAttackRangeMultiplier(attackRangeMultiplier);

        if (attack != null)
        {
            attack.SetDamageMultiplier(damageMultiplier);
        }
    }

    void Update()
    {
        if(player == null)
        {
            GameObject playerOBJ = GameObject.FindWithTag("Player");
            player = playerOBJ.transform;
        }

        if (!hasEnteredAirPhase && health != null)
        {
            float hpPercent = health.CurrentHP / health.MaxHP;

            if (hpPercent <= flyPhaseHPPercent)
            {
                EnterAirPhase();
            }
        }

        switch (currentState)
        {
            case HarpyState.Ground:
                if (hasEnteredAirPhase 
                    && Time.time > groundEnterTime + groundCooldown
                    && CanDive())
                {
                    EnterAirPhase();
                }
                break;

            case HarpyState.Hover:
                Hover();

                //if the player is close enough, dive!
                Vector3 dir = player.position - transform.position;
                dir.y = 0f;
                if (dir.magnitude < diveTriggerDistance && CanDive())
                {
                    StartDive();
                }
                break;

            case HarpyState.DiveAttack:
                Dive();
                break;

            case HarpyState.Recover:
                Recover();
                break;
        }
    }

    void LateUpdate()
    {
        float maxY = baseY + hoverHeight + 3f;
        float minY = baseY - 3f;

        Vector3 pos = transform.position;

        if (pos.y > maxY || pos.y < minY)
        {
            pos.y = baseY + hoverHeight;
            transform.position = pos;

            isDiving = false;
            EnterGroundPhase();
        }
    }

    void EnterGroundPhase()
    {
        currentState = HarpyState.Ground;
        groundEnterTime = Time.time;

        movement.SetCanMove(true);
        controller.enabled = true;
        rb.useGravity = true;
        animator.SetBool("IsFlying", false);
    }

    void Hover()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        FaceDirection(dir);

        float hoverOffset = Mathf.Sin(Time.time * 3f) * hoverAmplitude;

        Vector3 offset = (transform.position - player.position).normalized * 1.2f; //2f
        Vector3 targetPos = player.position + offset;
        targetPos.y = baseY + hoverHeight + hoverOffset;

        Vector3 newPos = Vector3.Lerp(rb.position, targetPos, Time.deltaTime * hoverSpeed);
        rb.MovePosition(newPos);
    }

    void StartHover()
    {
        currentState = HarpyState.Hover;

        movement.SetCanMove(false); //disable EnemyMovement
        animator.SetBool("IsFlying", true);
    }

    public bool CanDive()
    {
        if (isDiving) return false;
        if (Time.time < lastDiveTime + diveCooldown) return false;
        return true;
    }

    void StartDive()
    {
        if (!CanDive()) return;

        isDiving = true;
        currentState = HarpyState.DiveAttack;
        diveStartTime = Time.time;
        animator.SetTrigger("Dive");
    }

    void Dive()
    {
        diveTarget = player.position;
        Vector3 direction = (diveTarget - transform.position).normalized;
        Vector3 nextPos = rb.position + diveSpeed * Time.deltaTime * direction;
        float minY = baseY / groundOffsetMultiplier + groundOffset / groundOffsetMultiplier;

        rb.MovePosition(nextPos);

        Vector3 dir = (diveTarget - transform.position).normalized;
        FaceDirection(dir);

        //when crash with player
        if (Vector3.Distance(transform.position, player.position) < 1.2f)
        {
            // Debug.Log("Crash with player");
            attack.DealDamage(player.gameObject);
            StartRecover();
            return;
        }

        //when reach ground
        if (transform.position.y <= minY - 0.2f) //+0.05f
        {
            // Debug.Log("Reach ground " + minY);
            StartRecover();
            return;
        }

        if (Time.time > diveStartTime + 1.1f)
        {
            // Debug.Log("Time out");
            StartRecover();
        }
    }

    public void FinishDive()
    {
        isDiving = false;
        lastDiveTime = Time.time;

        Debug.Log("Cooldown after dive");
    }

    void StartRecover()
    {
        currentState = HarpyState.Recover;
    }

    void Recover()
    {
        Vector3 targetPos = new Vector3(player.position.x, baseY + hoverHeight, player.position.z);
        rb.MovePosition(Vector3.Lerp(rb.position, targetPos, Time.deltaTime * hoverSpeed));

        if (Mathf.Abs(transform.position.y - targetPos.y) < 0.1f)
        {
            FinishDive();
            EnterGroundPhase();
        }
    }

    void EnterAirPhase()
    {
        Debug.Log("Enter Air Phase!");
        hasEnteredAirPhase = true;

        currentState = HarpyState.Hover;

        movement.SetCanMove(false); //disable walking on ground
        controller.enabled = false;
        rb.useGravity = false;
        StartHover();
    }

    public void FaceDirection(Vector3 dir)
    {
        if (spriteRenderer == null) return;

        if (dir.x > 0.05f)
            spriteRenderer.flipX = false;
        else if (dir.x < -0.05f)
            spriteRenderer.flipX = true;    
    }
}
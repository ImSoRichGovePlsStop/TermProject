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
    [SerializeField] float flyPhaseHPPercent = 0.5f;
    [SerializeField] float diveSpeed = 7f;
    [SerializeField] float hoverHeight = 1.3f;
    [SerializeField] float hoverSpeed = 1f;
    [SerializeField] float hoverAmplitude = 0.3f;
    [SerializeField] float groundOffset = 0.2f;
    [SerializeField] Transform player;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] float diveTriggerDistance = 2.5f;
    [SerializeField] float diveCooldown = 2f;
    
    float diveStartTime;
    float lastDiveTime = -999f;
    private float baseY;

    void Awake()
    {
        controller = GetComponent<EnemyController>();
        movement = GetComponent<EnemyMovement>();
        attack = GetComponent<EnemyAttack>();
        health = GetComponent<EnemyHealth>();
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        baseY = transform.position.y;
        currentState = HarpyState.Ground;
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
                // CheckWakeUp(distanceToPlayer);
                break;

            case HarpyState.Hover:
                Hover();

                //if the player is close enough, dive!
                Vector3 dir = player.position - transform.position;
                dir.y = 0f;
                if (dir.magnitude < diveTriggerDistance && Time.time > lastDiveTime + diveCooldown)
                {
                    Debug.Log("dir.magnitude < 2f!!!");
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

    void Hover()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        FaceDirection(dir);

        float hoverOffset = Mathf.Sin(Time.time * 3f) * hoverAmplitude;

        Vector3 offset = (transform.position - player.position).normalized * 2f; //1.5f
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

    void StartDive()
    {
        currentState = HarpyState.DiveAttack;
        diveTarget = player.position;
        diveStartTime = Time.time;
        lastDiveTime = Time.time;
        animator.SetTrigger("Dive");
    }

    void Dive()
    {
        Vector3 direction = (diveTarget - transform.position).normalized;
        Vector3 nextPos = rb.position + diveSpeed * Time.deltaTime * direction;
        float minY = baseY + groundOffset;

        rb.MovePosition(nextPos);

        Vector3 dir = (diveTarget - transform.position).normalized;
        FaceDirection(dir);

        //when crash with player
        if (Vector3.Distance(transform.position, player.position) < 1.2f)
        {
            attack.DealDamage(player.gameObject);
            StartRecover();
            return;
        }

        //when reach ground
        if (transform.position.y <= minY + 0.05f)
        {
            StartRecover();
            return;
        }

        if (Time.time > diveStartTime + 1.1f)
        {
            StartRecover();
        }
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
            currentState = HarpyState.Hover;
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
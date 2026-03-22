using System;
using UnityEngine;
using UnityEngine.EventSystems;

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
    [SerializeField] float diveSpeed = 8f;
    [SerializeField] float hoverHeight = 2f;
    [SerializeField] float hoverSpeed = 2f;
    [SerializeField] float hoverAmplitude = 0.3f;
    [SerializeField] float wakeDistance = 5f;
    [SerializeField] float groundOffset = 0.2f;
    [SerializeField] Transform player;
    [SerializeField] SpriteRenderer spriteRenderer;

    float targetHoverY;
    private Vector3 startPosition;
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
        startPosition = transform.position;
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

        // float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
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
                if (dir.magnitude < 2f)
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
        Debug.Log("State: " + currentState);
    }

    void Hover()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        FaceDirection(dir);

        float hoverOffset = Mathf.Sin(Time.time * 3f) * hoverAmplitude;

        //should i use this offset?
        Vector3 offset = (transform.position - player.position).normalized * 2f;
        Vector3 targetPos = player.position + offset;
        targetPos.y = baseY + hoverHeight + hoverOffset;

        Vector3 newPos = Vector3.Lerp(rb.position, targetPos, Time.deltaTime * hoverSpeed);
        rb.MovePosition(newPos);
    }

    // void CheckWakeUp(float distance)
    // {
    //     if (distance <= wakeDistance)
    //     {
    //         StartHover();
    //     }
    // }

    void StartHover()
    {
        currentState = HarpyState.Hover;
        targetHoverY = transform.position.y + hoverHeight;

        movement.SetCanMove(false); //disable EnemyMovement
        animator.SetBool("IsFlying", true);
    }

    void StartDive()
    {
        currentState = HarpyState.DiveAttack;
        diveTarget = player.position;
        animator.SetTrigger("Dive");
    }

    void Dive()
    {
        Vector3 direction = (diveTarget - transform.position).normalized;
        Vector3 nextPos = rb.position + diveSpeed * Time.deltaTime * direction;
        rb.MovePosition(rb.position + diveSpeed * Time.deltaTime * direction);

        Vector3 dir = (diveTarget - transform.position).normalized;
        FaceDirection(dir);
        Debug.Log("Distance: " + dir.magnitude);

        //if reach the ground, revocer
        if (Vector3.Distance(transform.position, diveTarget) < 0.2f)
        {
            attack.ForceStopAttack();
            StartRecover();
        }
    }
    //What we need?
    // ให้ตอน dive พุ่งตาม pos ของ player ด้วย
    //ให้มี delay ระหว่างการ dive หน่อย
    //แก้ animation ให้ได้
    //อย่าให้ collider ไปติดกับพื้น
    //ให้ collider ไม่ทำ entity ตัวอื่นลอยขึ้นฟ้า

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
        targetHoverY = baseY + hoverHeight;

        movement.SetCanMove(false); //disable walking on ground
        controller.enabled = false;
        rb.useGravity = false;
        // attack.ForceStopAttack();
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
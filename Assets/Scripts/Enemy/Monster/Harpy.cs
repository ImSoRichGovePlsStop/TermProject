using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Harpy : MonoBehaviour
{
    enum HarpyState
    {
        IdleOnGround,
        Hover,
        DiveAttack,
        Recover
    }

    HarpyState currentState;
    private Rigidbody rb;
    private EnemyController controller;
    private EnemyMovement movement;
    private EnemyAttack attack;

    [SerializeField] float hoverHeight = 2f;
    [SerializeField] float hoverSpeed = 2f;
    [SerializeField] float hoverAmplitude = 0.3f;
    [SerializeField] float wakeDistance = 5f;
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
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        startPosition = transform.position;
        baseY = transform.position.y;
        currentState = HarpyState.IdleOnGround;
    }

    void Update()
    {
        Vector3 dir = movement.MoveDirection;

        if (dir.x > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (dir.x < 0)
        {
            spriteRenderer.flipX = true;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        switch (currentState)
        {
            case HarpyState.IdleOnGround:
                CheckWakeUp(distanceToPlayer);
                break;

            case HarpyState.Hover:
                Hover();
                break;
        }
    }

    void Hover()
    {
        float hoverOffset = Mathf.Sin(Time.time * 3f) * hoverAmplitude;

        Vector3 pos = rb.position;
        pos.y = Mathf.Lerp(pos.y, targetHoverY + hoverOffset, Time.deltaTime * hoverSpeed);
        rb.MovePosition(pos);
    }

    void CheckWakeUp(float distance)
    {
        if (distance <= wakeDistance)
        {
            StartHover();
        }
    }

    void StartHover()
    {
        currentState = HarpyState.Hover;
        targetHoverY = transform.position.y + hoverHeight;
    }
}
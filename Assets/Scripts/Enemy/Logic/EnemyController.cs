using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Chase,
        Attack,
        Hurt,
        Dead
    }

    [Header("Detection")]
    [SerializeField] private float detectRange = 6f;
    [SerializeField] private float loseTargetRange = 8f;
    [SerializeField] private float attackBuffer = 0.5f;

    [Header("References")]
    private Transform player;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Animator animator;


    private EnemyState currentState = EnemyState.Idle;
    private bool isDead = false;
    private Coroutine attackLoopCoroutine;

    private void Awake()
    {
        GameObject playerInScene = GameObject.FindWithTag("Player");
        if (playerInScene != null)
            player = playerInScene.transform;

        if (movement == null)
            movement = GetComponent<EnemyMovement>();


        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

    }


    
}
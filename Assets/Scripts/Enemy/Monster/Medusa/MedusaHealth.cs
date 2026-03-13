using System.Collections;
using UnityEngine;

public class MedusaHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHP = 50f;
    [SerializeField] private float hurtStunDuration = 0.2f;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Collider mainCollider;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private MedusaArcAttack arcAttack;
    [SerializeField] private MedusaBackAttack backAttack;

    [Header("Animation Parameter Names")]
    [SerializeField] private string hurtTriggerName = "Hurt";

    [Header("Optional Scripts To Disable On Death")]
    [SerializeField] private MonoBehaviour[] scriptsToDisableOnDeath;

    private float currentHP;
    private bool isDead;
    private bool isHurt;
    private Coroutine hurtCoroutine;

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public bool IsDead => isDead;
    public bool IsHurt => isHurt;

    private void Awake()
    {
        currentHP = maxHP;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (mainCollider == null)
            mainCollider = GetComponent<Collider>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (arcAttack == null)
            arcAttack = GetComponent<MedusaArcAttack>();

        if (backAttack == null)
            backAttack = GetComponent<MedusaBackAttack>();
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        currentHP -= damage;
        Debug.Log($"Medusa took damage: {damage}, HP left: {currentHP}");

        if (currentHP <= 0f)
        {
            currentHP = 0f;
            Die();
            return;
        }

        if (hurtCoroutine != null)
            StopCoroutine(hurtCoroutine);

        hurtCoroutine = StartCoroutine(HurtRoutine());
    }

    private IEnumerator HurtRoutine()
    {
        isHurt = true;

        if (animator != null && HasAnimatorParameter(hurtTriggerName, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(hurtTriggerName);

        yield return new WaitForSeconds(hurtStunDuration);

        isHurt = false;
        hurtCoroutine = null;
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        isHurt = false;

        if (hurtCoroutine != null)
        {
            StopCoroutine(hurtCoroutine);
            hurtCoroutine = null;
        }

        if (arcAttack != null)
            arcAttack.ClearSpawnedIndicators();

        if (backAttack != null)
            backAttack.ClearSpawnedIndicators();

        if (mainCollider != null)
            mainCollider.enabled = false;

        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        if (scriptsToDisableOnDeath != null)
        {
            foreach (var script in scriptsToDisableOnDeath)
            {
                if (script != null)
                    script.enabled = false;
            }
        }

        Destroy(gameObject);
    }

    private bool HasAnimatorParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;

        foreach (var param in animator.parameters)
        {
            if (param.name == paramName && param.type == type)
                return true;
        }

        return false;
    }
}
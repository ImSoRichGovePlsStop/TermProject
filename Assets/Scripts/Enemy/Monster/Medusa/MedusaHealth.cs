using System.Collections;
using UnityEngine;

public class MedusaHealth : EnemyHealth
{
    [Header("Medusa References")]
    [SerializeField] private MedusaArcAttack arcAttack;
    [SerializeField] private MedusaBackAttack backAttack;
    [SerializeField] private MonoBehaviour[] scriptsToDisableOnDeath;

    [Header("Animation Parameter Names")]
    [SerializeField] private string hurtTriggerName = "Hurt";

    protected override void CacheComponents()
    {
        if (arcAttack == null)
            arcAttack = GetComponent<MedusaArcAttack>();

        if (backAttack == null)
            backAttack = GetComponent<MedusaBackAttack>();
    }

    protected override void OnDamageTaken(float finalDamage)
    {
        Debug.Log($"Medusa took damage: {finalDamage}, HP left: {currentHP}");
    }

    protected override void TriggerHurtAnimation()
    {
        if (animator != null && HasAnimatorParameter(hurtTriggerName, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(hurtTriggerName);
    }

    protected override void OnDeathStart()
    {
        if (arcAttack != null)
            arcAttack.ClearSpawnedIndicators();

        if (backAttack != null)
            backAttack.ClearSpawnedIndicators();

        if (scriptsToDisableOnDeath != null)
        {
            foreach (var script in scriptsToDisableOnDeath)
            {
                if (script != null)
                    script.enabled = false;
            }
        }
    }

    protected override void HandleRigidbodyOnDeath()
    {
        if (rb == null) return;

        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        rb.isKinematic = true;
        rb.detectCollisions = false;
    }

    protected override void TriggerDeathAnimation()
    {
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

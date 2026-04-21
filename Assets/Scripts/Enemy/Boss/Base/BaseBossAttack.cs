using UnityEngine;

public abstract class BaseBossAttack : MonoBehaviour
{
    [Header("Base Attack References")]
    [SerializeField] protected Animator animator;

    protected bool isBusy = false;

    public bool IsBusy => isBusy;

    protected virtual void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public virtual bool CanChooseNewAttack()
    {
        return !isBusy;
    }

    public virtual void ForceStopAllAttacks()
    {
        isBusy = false;
    }

    public virtual void EndAttack()
    {
        isBusy = false;
    }
}
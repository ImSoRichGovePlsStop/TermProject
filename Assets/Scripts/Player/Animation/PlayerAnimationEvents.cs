using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    private PlayerController controller;
    private WandAttack wandAttack;

    void Start()
    {
        controller = GetComponentInParent<PlayerController>();
        wandAttack = GetComponentInParent<WandAttack>();
    }

    public void OnAttackActive()
    {
        controller.OnAttackActive();
    }

    public void OnPrimaryAttackEnd()
    {
        controller.OnPrimaryAttackEnd();
    }

    public void OnSecondaryAttackEnd()
    {
        controller.OnSecondaryAttackEnd();
    }

    public void OnHitVFX()
    {
        controller.OnHitVFX();
    }

    public void PlaceTotem()
    {
        wandAttack?.PlaceTotem();
    }
}
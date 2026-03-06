using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    private PlayerController controller;

    void Start()
    {
        controller = GetComponentInParent<PlayerController>();
    }

    public void OnAttackActive()
    {
        controller.OnAttackActive();
    }

    public void OnAttackEnd()
    {
        controller.OnAttackEnd();
    }
}
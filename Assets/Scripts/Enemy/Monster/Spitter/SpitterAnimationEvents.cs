using UnityEngine;

public class SpitterAnimationEvents : MonoBehaviour
{
    private SpitterController controller;
    private EnemyHealthBase health;

    private void Awake()
    {
        controller = GetComponentInParent<SpitterController>();
        health = GetComponentInParent<EnemyHealthBase>();
    }

    public void FireSpread(int dirIndex) => controller?.FireSpread(dirIndex);
    public void FinishAttack() => controller?.FinishAttack();

    public void StartFlashBuildup(string args) => controller?.StartFlashBuildup(args);
    public void FlashWhite() => controller?.FlashWhite();
}
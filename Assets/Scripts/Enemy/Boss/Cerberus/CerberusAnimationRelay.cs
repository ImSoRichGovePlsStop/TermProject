using UnityEngine;

public class CerberusAnimationRelay : MonoBehaviour
{
    [SerializeField] private CerberusAttack attack;
    [SerializeField] private CerberusController controller;

    public void DealBiteLeft() => attack.DealBiteLeft();
    public void DealBiteRight() => attack.DealBiteRight();
    public void DealBiteCenter() => attack.DealBiteCenter();
    public void DealFlame() => attack.DealFlame();
    public void BeginPounceDash() => attack.BeginPounceDash();
    public void DealPounceImpact() => attack.DealPounceImpact();
    public void SpawnSwordProjectile() => attack.SpawnSwordProjectile();
    public void EndAttack() => attack.EndAttack();
    public void FinishPhaseTransition() => controller.FinishPhaseTransition();
}
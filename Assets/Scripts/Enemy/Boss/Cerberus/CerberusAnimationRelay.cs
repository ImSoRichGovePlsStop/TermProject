using UnityEngine;

public class CerberusAnimationRelay : MonoBehaviour
{
    [SerializeField] private CerberusAttack attack;
    [SerializeField] private CerberusController controller;

    private void Awake()
    {
        if (attack == null)
            attack = GetComponentInParent<CerberusAttack>();

        if (controller == null)
            controller = GetComponentInParent<CerberusController>();
    }

    public void DealBiteHit1() => attack.DealBiteHit1();
    public void DealBiteHit2() => attack.DealBiteHit2();
    public void DealBiteHit3() => attack.DealBiteHit3();

    public void ShowFlameCone() => attack.ShowFlameCone();
    public void DealFlameCone() => attack.DealFlameCone();
    public void HideFlameCone() => attack.HideFlameCone();

    public void BeginPounceDash() => attack.BeginPounceDash();
    public void DealPounceImpact() => attack.DealPounceImpact();

    public void SpawnSwordProjectile() => attack.SpawnSwordProjectile();

    public void EndAttack() => attack.EndAttack();
    public void FinishPhaseTransition() => controller.FinishPhaseTransition();
}
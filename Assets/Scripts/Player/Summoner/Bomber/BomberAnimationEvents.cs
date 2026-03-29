using UnityEngine;

public class BomberAnimationEvents : MonoBehaviour
{
    private BomberSummoner bomber;

    private void Start()
    {
        bomber = GetComponentInParent<BomberSummoner>();
    }

    public void DealExplosionDamage() => bomber?.DealExplosionDamage();
    public void FinishExplosion() => bomber?.FinishExplosion();
    public void SetExplosionScale() => bomber?.SetExplosionScale();
}
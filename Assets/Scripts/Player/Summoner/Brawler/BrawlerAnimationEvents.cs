using UnityEngine;

public class BrawlerAnimationEvents : MonoBehaviour
{
    private BrawlerSummoner brawler;

    private void Start()
    {
        brawler = GetComponentInParent<BrawlerSummoner>();
    }

    public void DealDamage() => brawler?.DealDamage();
    public void FinishAttack() => brawler?.FinishAttack();
}
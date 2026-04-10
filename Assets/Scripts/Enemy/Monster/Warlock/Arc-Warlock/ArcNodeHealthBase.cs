using UnityEngine;

public class ArcNodeHealthBase : HealthBase
{
    [SerializeField] private float maxHp = 30f;

    protected override void Awake()
    {
        maxHP = maxHp;
        base.Awake();
    }

    public void SetMaxHp(float value)
    {
        maxHP = value;
        currentHP = value;
    }

    protected override void OnDie()
    {
        Destroy(gameObject);
    }
}
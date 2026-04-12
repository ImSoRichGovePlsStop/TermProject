using UnityEngine;

public class DestructibleHealth : HealthBase
{
    [SerializeField] private float maxHp = 1f;

    protected override void Awake()
    {
        maxHP = maxHp;
        base.Awake();
    }

    protected override void OnDie()
    {
        Destroy(gameObject);
    }
}
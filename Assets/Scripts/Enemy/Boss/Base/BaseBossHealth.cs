using UnityEngine;

public abstract class BaseBossHealth : EnemyHealth
{
    [Header("Boss Health Override")]
    [SerializeField] protected float bossMaxHP = 100f;
    [SerializeField] protected float bossHurtDuration = 0.15f;
    [SerializeField] protected float bossDestroyDelay = 3f;

    protected override void Awake()
    {
        // map ค่า boss ลง field ของ EnemyHealth ก่อน
        maxHP = bossMaxHP;
        hurtStunDuration = bossHurtDuration;
        destroyDelay = bossDestroyDelay;

        base.Awake();
    }

    protected override void CacheComponents()
    {
        base.CacheComponents();
    }
}
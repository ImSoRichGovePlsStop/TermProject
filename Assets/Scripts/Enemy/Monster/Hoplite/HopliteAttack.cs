using UnityEngine;

public class HopliteAttack : EnemyAttack
{
    [Header("Hoplite Attack")]
    [SerializeField] private EnemyMovement enemyMovement;
    [SerializeField] private SpriteRenderer facingSprite;
    [SerializeField] private float forwardOffset = 0.8f;
    [SerializeField] private float heightOffset = 0f;
    [SerializeField] private bool useAttackPointY = true;

    protected override void Awake()
    {
        base.Awake();

        if (enemyMovement == null)
            enemyMovement = GetComponentInParent<EnemyMovement>();

        if (facingSprite == null && enemyMovement != null)
            facingSprite = enemyMovement.GetComponentInChildren<SpriteRenderer>();

        if (facingSprite == null)
            facingSprite = GetComponentInChildren<SpriteRenderer>();

        if (facingSprite == null)
            facingSprite = GetComponentInParent<SpriteRenderer>();
    }

    protected override Vector3 GetAttackCenter()
    {
        float dirX = 1f;

        if (facingSprite != null && facingSprite.flipX)
            dirX = -1f;

        Vector3 basePos = transform.position;

        float y = basePos.y + heightOffset;
        if (useAttackPointY && attackPoint != null)
            y = attackPoint.position.y;

        return new Vector3(
            basePos.x + dirX * forwardOffset,
            y,
            basePos.z
        );
    }

    protected override void TryDamageFromHits(Collider[] hits)
    {
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
                continue;

            if (hits[i].transform == transform || hits[i].transform.IsChildOf(transform))
                continue;

            PlayerStats playerStats = hits[i].GetComponent<PlayerStats>();

            if (playerStats == null)
                playerStats = hits[i].GetComponentInParent<PlayerStats>();

            if (playerStats != null)
            {
                playerStats.TakeDamage(GetFinalDamage(), enemyHealth);
                break;
            }
        }
    }
}
using UnityEngine;

public class MedusaHealthBase : EnemyHealthBase
{
    [Header("Medusa References")]
    [SerializeField] private MedusaArcAttack arcAttack;
    [SerializeField] private MedusaBackAttack backAttack;
    [SerializeField] private MonoBehaviour[] scriptsToDisableOnDeath;

    protected override void Awake()
    {
        base.Awake();

        if (arcAttack == null)
            arcAttack = GetComponent<MedusaArcAttack>();

        if (backAttack == null)
            backAttack = GetComponent<MedusaBackAttack>();
    }

    protected override void OnDeathStart()
    {
        base.OnDeathStart();

        if (arcAttack != null)
            arcAttack.ClearSpawnedIndicators();

        if (backAttack != null)
            backAttack.ClearSpawnedIndicators();

        if (scriptsToDisableOnDeath != null)
        {
            foreach (var script in scriptsToDisableOnDeath)
            {
                if (script != null)
                    script.enabled = false;
            }
        }
    }
}
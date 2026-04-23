using UnityEngine;

public class WishFountain : MonoBehaviour, IInteractable
{
    [SerializeField] private float activateChance  = 0.02f;
    [SerializeField] private float chanceIncrement = 0.00f;
    [SerializeField] private int   baseCost        = 1;
    [SerializeField] private int   costPerFloor    = 1;

    [Header("Wish Loot")]
    [SerializeField] private float wishMeanCost    = 150f;
    [SerializeField] private float wishSd          = 300f;
    [SerializeField] private bool  allowDuplicates = false;

    [Header("Coin Blessing")]
    [SerializeField] private int bonusCoinMin = 50;
    [SerializeField] private int bonusCoinMax = 200;

    [Header("Stat Blessing")]
    [SerializeField] private float statBoostMin = 0.01f;
    [SerializeField] private float statBoostMax = 0.05f;

    private bool      _activated = false;
    private UIManager _uiManager;

    private int CurrentCost()
    {
        int floor = RunManager.Instance?.CurrentFloor ?? 1;
        return baseCost + costPerFloor * (floor - 1);
    }

    public string GetPromptText() => $"[ E ]  $ {CurrentCost()} to make a wish";
    public InteractInfo GetInteractInfo() => new InteractInfo
    {
        name        = "Spirit Fountain",
        description = $"Offer some souls into the fountain for a chance to receive a blessing.",
        actionText  = "Offer",
        cost        = CurrentCost()
    };

    private void Start()
    {
        _uiManager = FindFirstObjectByType<UIManager>();
    }

    public void Interact(PlayerController playerController)
    {
        if (_activated) return;

        int cost = CurrentCost();
        if (!CurrencyManager.Instance.TrySpend(cost))
        {
            DamageNumberSpawner.Instance?.SpawnMessage(
                transform.position, "Not enough souls!", new Color(1f, 0.35f, 0.35f));
            return;
        }

        if (Random.value < activateChance)
        {
            _activated = true;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            int eventType = Random.Range(0,4);

            switch (eventType)
            {
                case 0:
                    DamageNumberSpawner.Instance?.SpawnMessage(
                        transform.position, "Blessing: Full Heal!", new Color(0f, 1f, 0.4f));
                    playerController.GetComponent<PlayerStats>()?.HealFull();
                    break;

                case 1:
                    DamageNumberSpawner.Instance?.SpawnMessage(
                        transform.position, "Blessing: A Gift!", new Color(1f, 0.85f, 0f));
                    GrantModuleReward();
                    break;

                case 2:
                    int coins = Random.Range(bonusCoinMin, bonusCoinMax + 1);
                    DamageNumberSpawner.Instance?.SpawnMessage(
                        transform.position, $"Blessing: Soul overflow {coins}  <sprite=0>", new Color(0f, 1f, 0.9f));
                    CurrencyManager.Instance.AddCoins(coins);
                    break;

                case 3:
                    string blessingText = ApplyStatBlessing(playerController.GetComponent<PlayerStats>());
                    DamageNumberSpawner.Instance?.SpawnMessage(
                        transform.position, blessingText, new Color(0f, 1f, 0.4f));
                    break;
            }
        }
        else
        {
            activateChance += chanceIncrement;
            DamageNumberSpawner.Instance?.SpawnMessage(
                transform.position, "The fountain stirs...", new Color(0.7f, 0.7f, 1f));
        }
    }

    private void GrantModuleReward()
    {
        if (_uiManager == null)
            _uiManager = FindFirstObjectByType<UIManager>();

        var rolled = Randomizer.Roll(1, 1, wishMeanCost, wishSd, allowDuplicates);
        if (rolled == null || rolled.Count == 0) return;

        var cfg = new LootConfig { optionCount = 1, meanCost = wishMeanCost, sd = wishSd, allowDuplicates = allowDuplicates };
        _uiManager.OpenRewardLoot(cfg, rolled);
    }

    private string ApplyStatBlessing(PlayerStats stats)
    {
        if (stats == null) return "Blessing Failed";

        float boost = Random.Range(statBoostMin, statBoostMax);
        int statIndex = Random.Range(0, 6);

        string statName;
        string displayValue;
        var bonus = new StatModifier();

        switch (statIndex)
        {
            case 0:
                bonus.health = stats.BaseHP * boost;
                statName = "Max HP";
                displayValue = $"{bonus.health:F1}";
                break;

            case 1:
                bonus.damage = stats.BaseDMG * boost;
                statName = "Damage";
                displayValue = $"{bonus.damage:F1}";
                break;

            case 2:
                bonus.attackSpeed = stats.BaseATKSPD * boost;
                statName = "Attack Speed";
                displayValue = $"{bonus.attackSpeed:P1}";
                break;

            case 3:
                bonus.moveSpeed = stats.BaseMOVSPD * boost;
                statName = "Move Speed";
                displayValue = $"{bonus.moveSpeed:F1}";
                break;

            case 4:
                bonus.critChance = stats.BaseCrit * boost;
                statName = "Crit Chance";
                displayValue = $"{bonus.critChance:P1}";
                break;

            default:
                bonus.critDamage = stats.BaseCritDMG * boost;
                statName = "Crit Damage";
                displayValue = $"{bonus.critDamage:P1}";
                break;
        }

        stats.AddFlatModifier(bonus);

        return $"Blessing: +{displayValue} {statName}";
    }
}

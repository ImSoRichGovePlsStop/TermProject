using UnityEngine;

public class WishFountain : MonoBehaviour, IInteractable
{
    [SerializeField] private float activateChance  = 0.05f;
    [SerializeField] private float chanceIncrement = 0.001f;
    [SerializeField] private int   cost            = 1;

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


    public string GetPromptText() => $"[ E ]  {cost} Gold to make a wish";

    private void Start()
    {
        _uiManager = FindFirstObjectByType<UIManager>();
    }

    public void Interact(PlayerController playerController)
    {
        if (_activated) return;

        if (!CurrencyManager.Instance.TrySpend(cost))
        {
            DamageNumberSpawner.Instance?.SpawnMessage(
                transform.position, "Not enough coins!", new Color(1f, 0.35f, 0.35f));
            return;
        }

        if (Random.value < activateChance)
        {
            _activated = true;
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
                        transform.position, $"Blessing: {coins} Gold!", new Color(1f, 0.9f, 0f));
                    CurrencyManager.Instance.AddCoins(coins);
                    break;

                case 3:
                    ApplyStatBlessing(playerController.GetComponent<PlayerStats>());
                    DamageNumberSpawner.Instance?.SpawnMessage(
                    transform.position, "Blessing: Body Enhancement", new Color(0f, 1f, 0.4f));
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

    private void ApplyStatBlessing(PlayerStats stats)
    {
        if (stats == null) return;
        float boost = Random.Range(statBoostMin, statBoostMax);
        int statIndex = Random.Range(0, 6);
        var bonus = new StatModifier();
       

        switch (statIndex)
        {
            case 0: bonus.health      = stats.BaseHP      * boost;       break;
            case 1: bonus.damage      = stats.BaseDMG     * boost;        break;
            case 2: bonus.attackSpeed = stats.BaseATKSPD  * boost;  break;
            case 3: bonus.moveSpeed   = stats.BaseMOVSPD  * boost;   break;
            case 4: bonus.critChance  = stats.BaseCrit    * boost;  break;
            default: bonus.critDamage = stats.BaseCritDMG * boost;   break;
        }

        stats.AddFlatModifier(bonus);

    }
}

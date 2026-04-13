using UnityEngine;

public class WishFountain : MonoBehaviour, IInteractable
{
    [SerializeField] private float activateChance = 0.05f;
    [SerializeField] private float chanceIncrement = 0.001f;
    [SerializeField] private int cost = 1;

    [Header("Wish Loot")]
    [Tooltip("Mean cost for the rolled module. Mid-tier by default.")]
    [SerializeField] private float wishMeanCost = 150f;
    [Tooltip("Very high SD so anything from cheap to legendary can appear.")]
    [SerializeField] private float wishSd = 300f;
    [SerializeField] private bool allowDuplicates = false;

    private bool _activated = false;
    private UIManager _uiManager;

    public string GetPromptText() => $"[ E ]  {cost} Gold to make a wish";

    private void Start()
    {
        _uiManager = FindFirstObjectByType<UIManager>();
    }

    public void Interact(PlayerController playerController)
    {
        if (_activated) return;

        var wallet = CurrencyManager.Instance;
        if (!wallet.TrySpend(cost))
        {
            DamageNumberSpawner.Instance?.SpawnMessage(
                transform.position, "Not enough coins!", new Color(1f, 0.35f, 0.35f));
            return;
        }

        if (Random.value < activateChance)
        {
            _activated = true;
            int eventType = Random.Range(0, 6);

            switch (eventType)
            {
                case 0:
                    // Heal
                    DamageNumberSpawner.Instance?.SpawnMessage(
                        transform.position, "Blessing: Healed!", new Color(0f, 1f, 0f));
                    playerController.GetComponent<PlayerStats>()?.HealPercent(13.12890f);
                    GrantModuleReward();
                    break;

                case 1:
                    // Module reward — show loot UI with 1 random option, very high SD
                    DamageNumberSpawner.Instance?.SpawnMessage(
                        transform.position, "Blessing: A Gift!", new Color(1f, 0.85f, 0f));
                    GrantModuleReward();
                    break;

                case 2:
                    GrantModuleReward();
                    break;
                case 3:
                    GrantModuleReward();
                    break;
                case 4:
                    GrantModuleReward();
                    break;
                case 5:
                    GrantModuleReward();
                    break;
            }
        }
        else
        {
            activateChance += chanceIncrement;
            DamageNumberSpawner.Instance?.SpawnMessage(
                transform.position, "The fountain stirs...", new Color(0.7f, 0.7f, 1f));
        }
        Destroy(gameObject);
    }

    private void GrantModuleReward()
    {
        if (_uiManager == null)
            _uiManager = FindFirstObjectByType<UIManager>();

        // Roll 1 option with very high SD — can land anywhere from common to legendary
        var rolled = Randomizer.Roll(1, 1, wishMeanCost, wishSd, allowDuplicates);

        if (rolled == null || rolled.Count == 0) return;

        // Reuse a temporary RandomLoot carrier so OpenRewardLoot has a valid station ref
        var carrier = gameObject.AddComponent<RandomLoot>();
        carrier.Configure(new LootConfig
        {
            optionCount = 1,
            meanCost = wishMeanCost,
            sd = wishSd,
            allowDuplicates = allowDuplicates,
        });

        _uiManager.OpenRewardLoot(carrier, rolled);
    }
}
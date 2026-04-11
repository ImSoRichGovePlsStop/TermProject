using UnityEngine;

public class PaidHealObject : MonoBehaviour, IInteractable
{
    [Header("Base Values (Floor 1)")]
    [Tooltip("Coins charged per use on floor 1.")]
    public int baseCost = 75;

    [Tooltip("Flat HP restored per use on floor 1, before missing-HP bonus.")]
    public int baseHeal = 40;

    [Header("Floor Scaling")]
    [Tooltip("Extra coins added per floor above 1.")]
    public int costPerFloor = 25;

    [Tooltip("Extra flat HP added per floor above 1.")]
    public int healPerFloor = 10;

    [Header("Missing HP Scaling")]
    [Tooltip("Fraction of missing HP added on top of the flat heal. 0.15 = 15%.")]
    [Range(0f, 1f)]
    public float missingHpHealPercent = 0.15f;

    [Header("Options")]
    [Tooltip("Destroy after one use. If false the station is reusable.")]
    public bool singleUse = false;



    public string GetPromptText()
    {
        int cost = CurrentCost();

        var stats = FindStats();
        string healStr = stats != null
            ? $"+{ComputeHeal(stats)} HP"   
            : $"+{CurrentFlatHeal()}+ HP";  


        return $"[ E ]  Heal  {healStr}  ({cost} coins)";
    }

    public void Interact(PlayerController playerController)
    {
        int cost = CurrentCost();

        var wallet = CurrencyManager.Instance;
        if (wallet == null) { Debug.LogWarning("[PaidHeal] CurrencyManager not found."); return; }

        if (!wallet.TrySpend(cost))
        {
            Debug.Log($"[PaidHeal] Not enough coins — need {cost}, have {wallet.Coins}.");
            DamageNumberSpawner.Instance?.SpawnMessage(
                transform.position,
                "Not enough coins!",
                new Color(1f, 0.35f, 0.35f));
            return;
        }

        var stats = playerController.GetComponent<PlayerStats>();
        if (stats != null)
        {
            int heal = ComputeHeal(stats);
            stats.Heal(heal);
            Debug.Log($"[PaidHeal] Healed {heal} HP (flat {CurrentFlatHeal()} + {heal - CurrentFlatHeal()} missing-HP bonus) for {cost} coins.");
        }

        if (singleUse)
            Destroy(gameObject);
    }


    int ComputeHeal(PlayerStats stats)
    {
        float missing = stats.MaxHealth - stats.CurrentHealth;
        float bonusHeal = missing * missingHpHealPercent;
        return Mathf.RoundToInt(CurrentFlatHeal() + bonusHeal);
    }


    int CurrentFloor() => RunManager.Instance?.CurrentFloor ?? 1;
    int CurrentCost() => baseCost + Mathf.Max(0, CurrentFloor() - 1) * costPerFloor;
    int CurrentFlatHeal() => baseHeal + Mathf.Max(0, CurrentFloor() - 1) * healPerFloor;

    PlayerStats FindStats()
    {
        var player = GameObject.FindWithTag("Player");
        return player != null ? player.GetComponent<PlayerStats>() : null;
    }
}
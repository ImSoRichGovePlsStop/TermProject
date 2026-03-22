using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [SerializeField] private int startingCoins = 1000;
    public int Coins { get; private set; }

    public event System.Action<int> OnCoinsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Coins = startingCoins;
    }

    public bool TrySpend(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        OnCoinsChanged?.Invoke(Coins);
        return true;
    }

    public void AddCoins(int amount)
    {
        Coins += amount;
        OnCoinsChanged?.Invoke(Coins);
    }


    [ContextMenu("Add100coins")]
    private void Add100coins()
    => AddCoins(100);
}

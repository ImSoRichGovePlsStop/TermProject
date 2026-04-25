using UnityEngine;

public class Crate : MonoBehaviour
{
    [Header("Drop Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float activateChance = 0.6f;
    [SerializeField] private int goldMin = 2;
    [SerializeField] private int goldMax = 4;

    private HealthBase health;

    private void Awake()
    {
        health = GetComponent<HealthBase>();
        health.OnDeath += OnDestroyed;
    }


    private void OnDestroyed()
    {
        if (Random.value <= activateChance)
        {

                int goldAmount = Random.Range(goldMin, goldMax + 1);
                CurrencyManager.Instance?.AddCoins(goldAmount);

                DamageNumberSpawner.Instance?.SpawnMessage(
                    transform.position,
                    $"{goldAmount}  <sprite=0>",
                    new Color(0.608f, 1f, 0.839f, 1f),
                    0.5f,
                    5f
                     );    
        }

        Destroy(gameObject);
    }
}

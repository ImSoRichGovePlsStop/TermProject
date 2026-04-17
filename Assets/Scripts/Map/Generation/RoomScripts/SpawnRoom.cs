using UnityEngine;
using Object = UnityEngine.Object;

public class SpawnRoom : MonoBehaviour
{
    public float spawnHeightOffset = 0.25f;

    [HideInInspector] public RoomNode node;

    void Start()
    {
        SpawnPlayer();
        FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);
    }

    void SpawnPlayer()
    {
        Vector3 spawnPoint = GetSpawnPoint();
        var player = GameObject.FindWithTag("Player");
        if (player == null) { Debug.LogWarning("[PlayerSpawner] Player not found!"); return; }
        player.transform.position = spawnPoint;

        int bonusCoins = RunManager.Instance?.EffectiveBonusCoinsOnEntry ?? 0;
        if (bonusCoins > 0)
            Object.FindFirstObjectByType<CurrencyManager>()?.AddCoins(bonusCoins);
    }

    public Vector3 GetSpawnPoint()
    {
        return transform.position + Vector3.up * spawnHeightOffset;
    }
}

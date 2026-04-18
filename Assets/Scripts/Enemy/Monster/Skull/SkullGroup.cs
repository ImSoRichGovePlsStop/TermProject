using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SkullGroup : MonoBehaviour, IGroupSpawner
{
    [Header("Skull")]
    [SerializeField] private GameObject skullPrefab;
    [SerializeField] private int spawnCountMin = 8;
    [SerializeField] private int spawnCountMax = 12;

    [Header("Spawn Layout")]
    [SerializeField] private float spawnRadius = 3f;
    [SerializeField] private float minDistanceBetweenSkulls = 0.8f;
    [SerializeField] private int maxAttemptsPerSkull = 20;

    [Header("Spawn Effect")]
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private float spawnEffectScale = 1f;
    [SerializeField] private float spawnDuration = 1f;
    [SerializeField] private float spawnFadeOutDuration = 0.4f;

    private int _resolvedCount;
    private bool _hasStatScale;
    private StatScale _statScale;


    public int GetSpawnCount() => _resolvedCount;

    public void SetGroupStatScale(StatScale scale)
    {
        _statScale    = scale;
        _hasStatScale = true;
    }


    private void Awake()
    {
        _resolvedCount = Random.Range(spawnCountMin, spawnCountMax + 1);
    }

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        var spawnedPositions = new List<Vector3>();

        for (int i = 0; i < _resolvedCount; i++)
        {
            Vector3 pos = FindSpawnPosition(spawnedPositions);
            if (pos == Vector3.zero) continue;

            spawnedPositions.Add(pos);
            var skull = Instantiate(skullPrefab, pos, Quaternion.identity);

            if (_hasStatScale && skull.TryGetComponent<EntityStats>(out var stats))
                stats.SetStatScale(_statScale);
        }

        if (spawnEffectPrefab != null)
        {
            GameObject effectGO = Instantiate(spawnEffectPrefab, transform.position, Quaternion.identity);
            effectGO.transform.localScale *= spawnEffectScale;
            EnemySpawnEffect effect = effectGO.GetComponent<EnemySpawnEffect>();
            effect.Init();
            effect.PlayFadeIn(spawnDuration);

            yield return new WaitForSeconds(spawnDuration);

            effect.PlayFadeOut(spawnFadeOutDuration);
        }

        Destroy(gameObject);
    }

    private Vector3 FindSpawnPosition(List<Vector3> existing)
    {
        for (int attempt = 0; attempt < maxAttemptsPerSkull; attempt++)
        {
            Vector2 rand = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = transform.position + new Vector3(rand.x, 0f, rand.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                continue;

            bool tooClose = false;
            foreach (var pos in existing)
            {
                if (Vector3.Distance(hit.position, pos) < minDistanceBetweenSkulls)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose) return hit.position;
        }

        return Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}

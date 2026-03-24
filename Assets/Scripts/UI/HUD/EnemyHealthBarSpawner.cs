using UnityEngine;

public class EnemyHealthBarSpawner : MonoBehaviour
{
    [SerializeField] private EnemyHealthBarUI barPrefab;

    public void SpawnBar(EnemyHealth enemy, Vector3 offset, Vector3 scale)
    {
        if (barPrefab == null) return;
        var bar = Instantiate(barPrefab);
        bar.Init(enemy, offset, scale);
    }
}
using UnityEngine;

public class EntityHealthBarSpawner : MonoBehaviour
{
    public static EntityHealthBarSpawner Instance { get; private set; }

    [SerializeField] private EntityHealthBarUI barPrefab;

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnBar(HealthBase entity, float height, Vector3 scale)
    {
        if (barPrefab == null) return;
        var bar = Instantiate(barPrefab);
        bar.Init(entity, height, scale);
    }

}
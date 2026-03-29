using System.Collections.Generic;
using UnityEngine;

public class MaterialStorage : MonoBehaviour
{
    public static MaterialStorage Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("MaterialStorage");
                instance = go.AddComponent<MaterialStorage>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    private static MaterialStorage instance;

    private readonly Dictionary<MaterialData, int> storage = new();

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Add(MaterialData data, int count)
    {
        if (storage.ContainsKey(data)) storage[data] += count;
        else storage[data] = count;
    }

    public Dictionary<MaterialData, int> GetAll() => new(storage);

    public void Clear() => storage.Clear();
}

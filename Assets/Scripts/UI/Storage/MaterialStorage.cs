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

    public void AddAll(MaterialRequirement[] reqs)
    {
        if (reqs == null) return;
        foreach (var r in reqs)
            if (r.material != null && r.count > 0) Add(r.material, r.count);
    }

    public bool HasEnough(MaterialData data, int count)
        => storage.TryGetValue(data, out int have) && have >= count;

    public bool HasEnoughAll(MaterialRequirement[] reqs)
    {
        if (reqs == null || reqs.Length == 0) return true;
        foreach (var r in reqs)
            if (!HasEnough(r.material, r.count)) return false;
        return true;
    }

    public bool TryRemove(MaterialData data, int count)
    {
        if (!HasEnough(data, count)) return false;
        storage[data] -= count;
        if (storage[data] == 0) storage.Remove(data);
        return true;
    }

    public void RemoveAll(MaterialRequirement[] reqs)
    {
        if (reqs == null) return;
        foreach (var r in reqs)
            if (r.material != null && r.count > 0) TryRemove(r.material, r.count);
    }

    public Dictionary<MaterialData, int> GetAll() => new(storage);

    public void Clear() => storage.Clear();
}

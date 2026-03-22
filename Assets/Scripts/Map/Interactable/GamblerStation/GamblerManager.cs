using UnityEngine;

public class GamblerManager : GenericTreeManager
{
    public static GamblerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
}

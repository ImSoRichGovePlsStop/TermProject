using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach this to every EventSystem in every scene.
/// If an EventSystem is already present when this one wakes up, this one destroys itself.
/// </summary>
[RequireComponent(typeof(EventSystem))]
public class SingletonEventSystem : MonoBehaviour
{
    void Awake()
    {
        var all = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        foreach (var es in all)
        {
            if (es.gameObject != gameObject)
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}

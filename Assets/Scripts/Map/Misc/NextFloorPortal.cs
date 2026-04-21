using UnityEngine;
using UnityEngine.SceneManagement;

public class NextFloorPortal : MonoBehaviour, IInteractable
{
    public void Interact(PlayerController playerController)
    {
        
        var player = GameObject.FindWithTag("Player");
        if (player != null) player.transform.position = new Vector3(-200f, -75f, -200f);

        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (RunManager.Instance != null)
            RunManager.Instance.StartFloorTransition(sceneIndex);
        else
            SceneManager.LoadScene(sceneIndex);
    }

    string IInteractable.GetPromptText() => $"[E] -> Go to {NextFloorLabel()}";
    InteractInfo IInteractable.GetInteractInfo() => new InteractInfo
    {
        name       = $"Floor {NextFloorLabel()}",
        actionText = "Enter",
        cost       = null
    };

    static string NextFloorLabel()
    {
        int next = (RunManager.Instance?.CurrentFloor ?? 1) + 1;
        var pool  = EnemyPoolManager.Instance;
        int fps   = pool != null ? pool.floorsPerSegment : 3;
        int seg         = (next - 1) / fps + 1;
        int floorInSeg  = (next - 1) % fps + 1;
        return $"{seg}-{floorInSeg}";
    }
}
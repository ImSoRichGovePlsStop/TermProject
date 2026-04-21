using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public class FloorTextUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI floorLabel;

    private void Update()
    {
        floorLabel.text = BuildFloorLabel();
    }

    public static string BuildFloorLabel()
    {
        int floor = RunManager.Instance?.CurrentFloor ?? 1;
        var pool = EnemyPoolManager.Instance;
        int fps = pool != null ? pool.floorsPerSegment : 3;
        int seg = (floor - 1) / fps + 1;
        int floorInSeg = (floor - 1) % fps + 1;
        return $"{seg}-{floorInSeg}";
    }
}

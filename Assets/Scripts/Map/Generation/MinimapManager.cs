using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapManager : MonoBehaviour
{
    [Header("UI")]
    public RectTransform minimapRoot;
    public float cellPixels = 2f;

    [Header("Colors")]
    public Color emptyColor = new Color(0, 0, 0, 0);
    public Color corridorColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    public Color unvisitedRoom = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    public Color currentColor = Color.white;
    public Color visitedBattle = new Color(0.8f, 0.2f, 0.2f, 1f);
    public Color spawnColor = Color.green;
    public Color bossColor = Color.magenta;
    public Color shopColor = Color.yellow;
    public Color healColor = new Color(0.4f, 1f, 0.4f, 1f);
    public Color upgradeColor = new Color(1f, 0.5f, 0f, 1f);
    public Color mergeColor = Color.cyan;

    [Header("Player Tracker")]
    public Transform player;
    public Color playerCorridorColor = Color.white;

    private Dictionary<Vector2Int, Image> _cellImages = new();
    private Dictionary<Vector2Int, RoomNode> _centerToNode = new();
    private HashSet<Vector2Int> _visited = new();
    private HashSet<Vector2Int> _revealed = new();
    private Vector2Int _currentRoomCell;
    private Vector2Int _lastCorridorCell = new Vector2Int(-1, -1);

    private byte[,] _matrix;
    private int _matrixSize;
    private int _minX, _minZ;   // map bounds offset used for UI positioning

    // ── Called by MapGenerator ────────────────────────────────────────────────

    public void BuildMinimapFromMatrix(byte[,] matrix, int size, List<RoomNode> rooms)
    {
        _matrix = matrix;
        _matrixSize = size;

        foreach (Transform child in minimapRoot)
            Destroy(child.gameObject);
        _cellImages.Clear();
        _centerToNode.Clear();
        _visited.Clear();
        _revealed.Clear();

        int maxX = 0, maxZ = 0;
        _minX = size; _minZ = size;
        for (int x = 0; x < size; x++)
            for (int z = 0; z < size; z++)
                if (matrix[x, z] != Cell.Empty)
                {
                    if (x < _minX) _minX = x; if (x > maxX) maxX = x;
                    if (z < _minZ) _minZ = z; if (z > maxZ) maxZ = z;
                }

        for (int x = _minX; x <= maxX; x++)
            for (int z = _minZ; z <= maxZ; z++)
            {
                byte v = matrix[x, z];
                if (v == Cell.Empty) continue;

                var img = MakeCell($"MC_{x}_{z}", minimapRoot);
                img.rectTransform.sizeDelta = Vector2.one * cellPixels;
                img.rectTransform.anchoredPosition = new Vector2(
                    (x - _minX) * cellPixels,
                    (z - _minZ) * cellPixels);

                img.color = v == Cell.Corridor ? corridorColor : unvisitedRoom;
                _cellImages[new Vector2Int(x, z)] = img;
            }

        foreach (var node in rooms)
            _centerToNode[node.MatrixCenter] = node;

        // Auto-size panel to fit the map
        minimapRoot.sizeDelta = new Vector2(
            (maxX - _minX + 1) * cellPixels,
            (maxZ - _minZ + 1) * cellPixels);
    }

    // ── Called by room scripts on player enter ────────────────────────────────

    public void OnPlayerEnterRoom(RoomNode node)
    {
        if (_visited.Contains(_currentRoomCell))
            PaintRoomCells(_currentRoomCell, VisitedColor(_currentRoomCell));

        _currentRoomCell = node.MatrixCenter;
        _visited.Add(_currentRoomCell);
        PaintRoomCells(_currentRoomCell, currentColor);

        foreach (var neighbor in node.Neighbors)
        {
            if (_visited.Contains(neighbor.MatrixCenter)) continue;
            _revealed.Add(neighbor.MatrixCenter);
            PaintRoomCells(neighbor.MatrixCenter, unvisitedRoom);
        }

        // Clear any highlighted corridor cell when entering a room
        RestoreCorridorCell(_lastCorridorCell);
        _lastCorridorCell = new Vector2Int(-1, -1);
    }

    // ── Update: track player position in corridors ────────────────────────────

    void LateUpdate()
    {
        if (player == null || _matrix == null) return;

        // Convert player world position to matrix cell
        int cx = Mathf.FloorToInt(player.position.x);
        int cz = Mathf.FloorToInt(player.position.z);
        var cell = new Vector2Int(cx, cz);

        if (cell == _lastCorridorCell) return;

        // Only highlight if this cell is a corridor cell
        if (cx < 0 || cz < 0 || cx >= _matrixSize || cz >= _matrixSize) return;
        if (_matrix[cx, cz] != Cell.Corridor)
        {
            // Player left corridor — restore previous corridor highlight
            RestoreCorridorCell(_lastCorridorCell);
            _lastCorridorCell = new Vector2Int(-1, -1);
            return;
        }

        // Restore previous corridor cell
        RestoreCorridorCell(_lastCorridorCell);

        // Player just stepped into a corridor for the first time —
        // revert the last room to its visited color immediately
        if (_lastCorridorCell.x < 0 && _visited.Contains(_currentRoomCell))
            PaintRoomCells(_currentRoomCell, VisitedColor(_currentRoomCell));

        // Highlight new corridor cell
        if (_cellImages.TryGetValue(cell, out var img))
            img.color = playerCorridorColor;

        _lastCorridorCell = cell;
    }

    void RestoreCorridorCell(Vector2Int cell)
    {
        if (cell.x < 0) return;
        if (_cellImages.TryGetValue(cell, out var img))
            img.color = corridorColor;
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    void PaintRoomCells(Vector2Int center, Color color)
    {
        if (!_centerToNode.TryGetValue(center, out var node)) return;
        int ox = node.MatrixOrigin.x;
        int oz = node.MatrixOrigin.y;
        for (int x = ox; x < ox + node.Size.x; x++)
            for (int z = oz; z < oz + node.Size.y; z++)
                if (_cellImages.TryGetValue(new Vector2Int(x, z), out var img))
                    img.color = color;
    }

    Color VisitedColor(Vector2Int center)
    {
        if (!_centerToNode.TryGetValue(center, out var node)) return corridorColor;
        return node.Type switch
        {
            RoomType.Spawn => spawnColor,
            RoomType.Boss => bossColor,
            RoomType.Shop => shopColor,
            RoomType.Heal => healColor,
            RoomType.Upgrade => upgradeColor,
            RoomType.Merge => mergeColor,
            _ => visitedBattle
        };
    }

    Image MakeCell(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }
}
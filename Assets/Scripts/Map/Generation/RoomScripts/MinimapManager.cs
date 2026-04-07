using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapManager : MonoBehaviour
{
    [Header("UI")]
    public RectTransform minimapRoot;
    public float cellPixels = 2f;

    [Header("Colors")]
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

    private Dictionary<(Vector2Int, Vector2Int), HashSet<Vector2Int>> _edgeCells = new();
    private HashSet<Vector2Int> _visibleCorridorCells = new();

    private Vector2Int _currentRoomCell;
    private Vector2Int _lastCorridorCell = new Vector2Int(-1, -1);

    private byte[,] _matrix;
    private int _matrixSize;


    public void BuildMinimapFromMatrix(byte[,] matrix, int size,
                                       List<RoomNode> rooms,
                                       IReadOnlyList<MapEdge> edges = null)
    {
        _matrix = matrix;
        _matrixSize = size;

        foreach (Transform child in minimapRoot) Destroy(child.gameObject);
        _cellImages.Clear();
        _centerToNode.Clear();
        _edgeCells.Clear();
        _visibleCorridorCells.Clear();
        _visited.Clear();
        _revealed.Clear();

        int minX = size, minZ = size, maxX = 0, maxZ = 0;
        for (int x = 0; x < size; x++)
            for (int z = 0; z < size; z++)
                if (matrix[x, z] != Cell.Empty && matrix[x, z] != Cell.Occupied)
                {
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (z < minZ) minZ = z; if (z > maxZ) maxZ = z;
                }

        minimapRoot.pivot = new Vector2(0.5f, 0.5f);
        int matCX = size / 2, matCZ = size / 2;

        for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
            {
                byte v = matrix[x, z];
                if (v == Cell.Empty || v == Cell.Occupied) continue;

                var img = MakeCell($"MC_{x}_{z}", minimapRoot);
                img.rectTransform.sizeDelta = Vector2.one * cellPixels;
                img.rectTransform.anchoredPosition = new Vector2(
                    (x - matCX) * cellPixels,
                    (z - matCZ) * cellPixels);

                img.color = Color.clear;
                _cellImages[new Vector2Int(x, z)] = img;
            }

        foreach (var node in rooms)
            _centerToNode[node.MatrixCenter] = node;

        if (edges != null)
        {
            foreach (var edge in edges)
            {
                var key = EdgeKey(edge.A.CenterX, edge.A.CenterZ,
                                  edge.B.CenterX, edge.B.CenterZ);
                if (!_edgeCells.TryGetValue(key, out var set))
                    _edgeCells[key] = set = new HashSet<Vector2Int>();

                foreach (var seg in edge.Segments)
                    foreach (var cell in seg)
                        set.Add(cell);
            }
        }
        else
        {
            var all = new HashSet<Vector2Int>();
            for (int x = 0; x < size; x++)
                for (int z = 0; z < size; z++)
                    if (matrix[x, z] == Cell.Corridor)
                        all.Add(new Vector2Int(x, z));
            _edgeCells[(new Vector2Int(-1, -1), new Vector2Int(-1, -1))] = all;
        }
    }


    public void OnPlayerEnterRoom(RoomNode node)
    {
        if (_currentRoomCell != default && _visited.Contains(_currentRoomCell))
            PaintRoomCells(_currentRoomCell, VisitedColor(_currentRoomCell));

        _currentRoomCell = node.MatrixCenter;
        _visited.Add(_currentRoomCell);

        PaintRoomCells(_currentRoomCell, currentColor);

        foreach (var neighbor in node.Neighbors)
        {
            Vector2Int nc = neighbor.MatrixCenter;

            bool neighborVisited = _visited.Contains(nc);

            if (!neighborVisited && !_revealed.Contains(nc))
            {
                _revealed.Add(nc);
                PaintRoomCells(nc, unvisitedRoom);
            }

            RevealEdgeCorridors(_currentRoomCell, nc);
        }

        foreach (var visited in _visited)
        {
            if (visited == _currentRoomCell) continue;
            RevealEdgeCorridors(_currentRoomCell, visited);
        }

        RestoreCorridorCell(_lastCorridorCell);
        _lastCorridorCell = new Vector2Int(-1, -1);
    }

    void RevealEdgeCorridors(Vector2Int centerA, Vector2Int centerB)
    {
        var key = EdgeKey(centerA.x, centerA.y, centerB.x, centerB.y);
        if (!_edgeCells.TryGetValue(key, out var cells)) return;

        foreach (var cell in cells)
        {
            if (_visibleCorridorCells.Add(cell))
                if (_cellImages.TryGetValue(cell, out var img))
                    img.color = corridorColor;
        }
    }


    void LateUpdate()
    {
        if (player == null || _matrix == null) return;

        int cx = Mathf.FloorToInt(player.position.x);
        int cz = Mathf.FloorToInt(player.position.z);
        var cell = new Vector2Int(cx, cz);

        if (cell == _lastCorridorCell) return;
        if (cx < 0 || cz < 0 || cx >= _matrixSize || cz >= _matrixSize) return;

        if (_matrix[cx, cz] != Cell.Corridor)
        {
            RestoreCorridorCell(_lastCorridorCell);
            _lastCorridorCell = new Vector2Int(-1, -1);
            return;
        }

        if (!_visibleCorridorCells.Contains(cell)) return;

        RestoreCorridorCell(_lastCorridorCell);

        if (_lastCorridorCell.x < 0 && _visited.Contains(_currentRoomCell))
            PaintRoomCells(_currentRoomCell, VisitedColor(_currentRoomCell));

        if (_cellImages.TryGetValue(cell, out var cImg))
            cImg.color = playerCorridorColor;

        _lastCorridorCell = cell;
    }

    void RestoreCorridorCell(Vector2Int cell)
    {
        if (cell.x < 0) return;
        if (_cellImages.TryGetValue(cell, out var img))
            img.color = _visibleCorridorCells.Contains(cell) ? corridorColor : Color.clear;
    }


    void PaintRoomCells(Vector2Int center, Color color)
    {
        if (!_centerToNode.TryGetValue(center, out var node)) return;
        int ox = node.MatrixOrigin.x, oz = node.MatrixOrigin.y;
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

    static (Vector2Int, Vector2Int) EdgeKey(int ax, int az, int bx, int bz)
    {
        var a = new Vector2Int(ax, az);
        var b = new Vector2Int(bx, bz);
        return a.x < b.x || (a.x == b.x && a.y < b.y) ? (a, b) : (b, a);
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
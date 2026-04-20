using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class MinimapManager : MonoBehaviour
{
    [Header("UI")]
    public RectTransform minimapRoot;

    [Header("Fog of War")]
    [Tooltip("Radius in matrix cells revealed around the player each frame.")]
    public int fogRadius = 6;
    [Tooltip("Pixels per matrix cell. Higher = more border detail.")]
    public int cellScale = 4;
    [Tooltip("Border thickness in pixels. Must be < cellScale.")]
    public int borderThickness = 1;

    [Header("Colors")]
    public Color colorUnmarked = new Color(0.45f, 0.45f, 0.45f, 1f);
    public Color colorEvent = new Color(0.75f, 0.70f, 0.35f, 1f);
    public Color colorBattleUnvisited = new Color(0.75f, 0.10f, 0.10f, 1f);
    public Color colorBattleVisited = new Color(0.45f, 0.45f, 0.45f, 1f);
    public Color colorBossUnvisited = new Color(0.55f, 0.10f, 0.65f, 1f);
    public Color colorBossDefeated = new Color(0.40f, 0.30f, 0.45f, 1f);
    public Color colorPlayer = new Color(0.10f, 0.90f, 0.20f, 1f);
    public Color colorHidden = new Color(0f, 0f, 0f, 0f);
    public Color colorWall = new Color(0.20f, 0.20f, 0.20f, 1f);

    [Header("Player")]
    public Transform player;


    byte[,] _matrix;
    MapNode[,] _roomMap;
    bool[,] _isDoor;
    int _matrixSize;
    int _minX, _minZ, _maxX, _maxZ;   


    Texture2D _tex;
    RawImage _rawImage;

    Color32[] _pixels;     
    bool[] _revealed;    


    HashSet<MapNode> _visitedRooms = new();
    HashSet<MapNode> _defeatedBoss = new();

    Vector2Int _playerCell = new(-1, -1);
    MapNode _currentRoom;

    static readonly RoomType[] EventTypes =
    {
        RoomType.Heal, RoomType.Shop, RoomType.RareLoot,
        RoomType.Merge, RoomType.Fountain
    };

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshVisibility(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshVisibility(scene.buildIndex);
    }

    private void RefreshVisibility(int sceneIndex)
    {
        if (minimapRoot != null)
            minimapRoot.gameObject.SetActive(sceneIndex != 1);
    }

    public void BuildMinimapFromMatrix(byte[,] matrix, int size,
                                       MapNode[,] roomMap,
                                       List<RoomNode> rooms = null,
                                       IReadOnlyList<MapEdge> edges = null)
    {
        _matrix = matrix;
        _matrixSize = size;
        _roomMap = roomMap;
        _isDoor = FindFirstObjectByType<BSPMapGeometry>()?.IsDoor;

        _minX = size; _minZ = size; _maxX = 0; _maxZ = 0;
        for (int x = 0; x < size; x++)
            for (int z = 0; z < size; z++)
                if (matrix[x, z] == Cell.Room || matrix[x, z] == Cell.Occupied)
                {
                    if (x < _minX) _minX = x; if (x > _maxX) _maxX = x;
                    if (z < _minZ) _minZ = z; if (z > _maxZ) _maxZ = z;
                }

        int w = _maxX - _minX + 1;
        int h = _maxZ - _minZ + 1;

        _revealed = new bool[size * size];
        _visitedRooms.Clear();
        _defeatedBoss.Clear();
        _playerCell = new(-1, -1);
        _currentRoom = null;


        if (_tex != null) Destroy(_tex);
        _tex = new Texture2D(w * cellScale, h * cellScale, TextureFormat.RGBA32, false);
        _tex.filterMode = FilterMode.Point;
        _pixels = new Color32[w * cellScale * h * cellScale];
        for (int i = 0; i < _pixels.Length; i++) _pixels[i] = Color.clear;
        _tex.SetPixels32(_pixels);
        _tex.Apply();


        foreach (Transform child in minimapRoot) Destroy(child.gameObject);
        var go = new GameObject("MinimapTexture", typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(minimapRoot, false);
        _rawImage = go.GetComponent<RawImage>();
        _rawImage.texture = _tex;
        _rawImage.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public void OnPlayerEnterRoom(RoomNode node)
    {
        if (_roomMap == null) return;


        var mapNode = GetMapNode(node.MatrixCenter);
        if (mapNode == null) return;

        _visitedRooms.Add(mapNode);
        _currentRoom = mapNode;

        RevealRoom(mapNode);
        ApplyTexture();
    }


    public void OnBossDefeated(RoomNode node)
    {
        var mapNode = GetMapNode(node.MatrixCenter);
        if (mapNode == null) return;
        _defeatedBoss.Add(mapNode);
        RepaintRoom(mapNode);
        ApplyTexture();
    }


    void LateUpdate()
    {
        if (player == null || _matrix == null || _tex == null) return;

        int cx = Mathf.FloorToInt(player.position.x);
        int cz = Mathf.FloorToInt(player.position.z);
        var cell = new Vector2Int(cx, cz);

        bool dirty = false;


        dirty |= RevealRadius(cx, cz);


        if (cell != _playerCell)
        {
            var oldCell = _playerCell;
            _playerCell = cell;


            if (oldCell.x >= 0)
            {
                RepaintCell(oldCell.x, oldCell.y);
                dirty = true;
            }

            if (IsInBounds(cx, cz) && _revealed[cx + cz * _matrixSize] && _matrix[cx, cz] == Cell.Room)
            {
                SetPixel(cx, cz, colorPlayer);
                dirty = true;
            }
        }

        if (dirty) ApplyTexture();
    }


    bool RevealRadius(int cx, int cz)
    {
        bool dirty = false;
        for (int dx = -fogRadius; dx <= fogRadius; dx++)
            for (int dz = -fogRadius; dz <= fogRadius; dz++)
            {
                if (dx * dx + dz * dz > fogRadius * fogRadius) continue;
                int nx = cx + dx, nz = cz + dz;
                if (!IsInBounds(nx, nz)) continue;
                byte v = _matrix[nx, nz];
                if (v != Cell.Room && v != Cell.Occupied) continue;
                int idx = nx + nz * _matrixSize;
                if (_revealed[idx]) continue;
                _revealed[idx] = true;
                RepaintCell(nx, nz);
                dirty = true;
            }
        return dirty;
    }

    void RevealRoom(MapNode node)
    {
        for (int x = node.MinX; x <= node.MaxX; x++)
            for (int z = node.MinZ; z <= node.MaxZ; z++)
            {
                if (!IsInBounds(x, z)) continue;
                byte v = _matrix[x, z];
                if (v != Cell.Room && v != Cell.Occupied) continue;
                int idx = x + z * _matrixSize;
                _revealed[idx] = true;
                PaintCellWithBorder(x, z, node);
            }
    }


    void PaintCellWithBorder(int x, int z, MapNode owner)
    {
        Color roomCol = CellColor(owner);
        SetPixel(x, z, roomCol);

        if (cellScale < 2 || borderThickness <= 0) return;

        int bx = (x - _minX) * cellScale;
        int bz = (z - _minZ) * cellScale;

        int t = Mathf.Clamp(borderThickness, 1, cellScale - 1);

        if (IsWallFacing(x, z, x + 1, z, owner))
            for (int p = 0; p < t; p++)
                for (int dz = 0; dz < cellScale; dz++) WritePixel(bx + cellScale - 1 - p, bz + dz, colorWall);

        if (IsWallFacing(x, z, x - 1, z, owner))
            for (int p = 0; p < t; p++)
                for (int dz = 0; dz < cellScale; dz++) WritePixel(bx + p, bz + dz, colorWall);

        if (IsWallFacing(x, z, x, z + 1, owner))
            for (int p = 0; p < t; p++)
                for (int dx = 0; dx < cellScale; dx++) WritePixel(bx + dx, bz + cellScale - 1 - p, colorWall);
        if (IsWallFacing(x, z, x, z - 1, owner))
            for (int p = 0; p < t; p++)
                for (int dx = 0; dx < cellScale; dx++) WritePixel(bx + dx, bz + p, colorWall);
    }

    bool IsWallFacing(int x, int z, int nx, int nz, MapNode owner)
    {
        if (_isDoor != null && (IsInBounds(x, z) && _isDoor[x, z])) return false;
        if (_isDoor != null && (IsInBounds(nx, nz) && _isDoor[nx, nz])) return false;
        if (!IsInBounds(nx, nz)) return true;
        byte nv = _matrix[nx, nz];
        if (nv != Cell.Room) return true;
        if (_roomMap != null && _roomMap[nx, nz] != owner) return true;
        return false;
    }

    void RepaintRoom(MapNode node)
    {
        for (int x = node.MinX; x <= node.MaxX; x++)
            for (int z = node.MinZ; z <= node.MaxZ; z++)
                if (_revealed[x + z * _matrixSize])
                    RepaintCell(x, z);
    }


    void RepaintCell(int x, int z)
    {
        int idx = x + z * _matrixSize;
        if (!_revealed[idx]) { SetPixel(x, z, colorHidden); return; }

        byte v = _matrix[x, z];
        if (v == Cell.Occupied) { SetPixel(x, z, colorWall); return; }
        if (v != Cell.Room) return;

        if (x == _playerCell.x && z == _playerCell.y) { SetPixel(x, z, colorPlayer); return; }

        var node = _roomMap?[x, z];
        if (node != null) PaintCellWithBorder(x, z, node);
        else SetPixel(x, z, colorUnmarked);
    }

    Color CellColor(MapNode node)
    {
        if (node == null) return colorUnmarked;

        switch (node.Type)
        {
            case RoomType.Spawn:
            case RoomType.Unmarked:
                return colorUnmarked;

            case RoomType.Battle:
                return _visitedRooms.Contains(node) ? colorBattleVisited : colorBattleUnvisited;

            case RoomType.Boss:
                return _defeatedBoss.Contains(node) ? colorBossDefeated : colorBossUnvisited;

            case RoomType.Heal:
            case RoomType.Shop:
            case RoomType.RareLoot:
            case RoomType.Merge:
            case RoomType.Fountain:
                return colorEvent;

            default:
                return colorUnmarked;
        }
    }

    void SetPixel(int x, int z, Color c)
    {
        int bx = (x - _minX) * cellScale;
        int bz = (z - _minZ) * cellScale;
        for (int dx = 0; dx < cellScale; dx++)
            for (int dz = 0; dz < cellScale; dz++)
                WritePixel(bx + dx, bz + dz, c);
    }

    void WritePixel(int px, int pz, Color c)
    {
        if (px < 0 || pz < 0 || px >= _tex.width || pz >= _tex.height) return;
        _pixels[px + pz * _tex.width] = c;
    }

    void ApplyTexture()
    {
        _tex.SetPixels32(_pixels);
        _tex.Apply();
    }


    MapNode GetMapNode(Vector2Int matrixCenter)
    {
        if (_roomMap == null) return null;
        int cx = matrixCenter.x, cz = matrixCenter.y;
        if (IsInBounds(cx, cz) && _roomMap[cx, cz] != null)
            return _roomMap[cx, cz];
        for (int r = 1; r <= 4; r++)
            for (int dx = -r; dx <= r; dx++)
                for (int dz = -r; dz <= r; dz++)
                {
                    int nx = cx + dx, nz = cz + dz;
                    if (!IsInBounds(nx, nz)) continue;
                    var n = _roomMap[nx, nz];
                    if (n != null) return n;
                }
        return null;
    }

    bool IsInBounds(int x, int z) =>
        x >= 0 && z >= 0 && x < _matrixSize && z < _matrixSize;

    public void Reset()
    {
        _matrix = null;
        _roomMap = null;
        _isDoor = null;
        _revealed = null;
        _visitedRooms.Clear();
        _defeatedBoss.Clear();
        _playerCell = new(-1, -1);
        _currentRoom = null;

        if (_tex != null) { Destroy(_tex); _tex = null; }
        _pixels = null;

        foreach (Transform child in minimapRoot) Destroy(child.gameObject);
        _rawImage = null;
    }
}
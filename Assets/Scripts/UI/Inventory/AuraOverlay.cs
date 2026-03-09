using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to the same GameObject as your weapon grid UI panel.
/// It spawns coloured overlay images on every aura cell whenever
/// the weapon grid contents change.
///
/// Setup:
///   1. Create a UI Image asset for the aura cell (a semi-transparent square).
///      Assign it to auraCellSprite.
///   2. Set cellSize to match your grid cell pixel size (e.g. 64).
///   3. Set gridOriginOffset to the pixel position of cell (0,0) inside the panel.
/// </summary>
public class AuraOverlay : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The RectTransform of the weapon grid panel cells sit on.")]
    public RectTransform gridPanel;

    [Header("Visuals")]
    [Tooltip("Sprite used for each aura cell (semi-transparent square recommended).")]
    public Sprite auraCellSprite;

    [Tooltip("Colour of the aura overlay cells.")]
    public Color auraColor = new Color(1f, 0.85f, 0.2f, 0.35f);

    [Header("Grid Layout")]
    [Tooltip("Pixel size of one inventory cell.")]
    public float cellSize = 64f;

    [Tooltip("Pixel offset from the panel's bottom-left to cell (0,0).")]
    public Vector2 gridOriginOffset = Vector2.zero;

    // Pool of overlay images
    private readonly List<Image> _pool    = new List<Image>();
    private readonly List<Image> _active  = new List<Image>();

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (gridPanel == null) gridPanel = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        InventoryManager.Instance.OnModuleEquipped   += OnGridChanged;
        InventoryManager.Instance.OnModuleUnequipped += OnGridChanged;
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance == null) return;
        InventoryManager.Instance.OnModuleEquipped   -= OnGridChanged;
        InventoryManager.Instance.OnModuleUnequipped -= OnGridChanged;
    }

    // ── Rebuild ───────────────────────────────────────────────────────────────

    private void OnGridChanged(ModuleInstance _) => Rebuild();

    /// <summary>Call this manually if you need to force a refresh.</summary>
    public void Rebuild()
    {
        ReturnAll();

        var weaponGrid = InventoryManager.Instance.WeaponGrid;

        foreach (var inst in weaponGrid.GetAllModules())
        {
            if (inst.Data.moduleEffect is not LevelUpBuffEffect) continue;

            // World-space aura cells = GridPosition + each local aura offset
            foreach (var localCell in inst.Data.GetAuraCells())
            {
                Vector2Int worldCell = inst.GridPosition + localCell;
                // Make sure the cell is inside the grid
                if (!weaponGrid.IsInBounds(worldCell)) continue;

                var img = Rent();
                img.color = GetAuraColor(inst.Data);
                img.rectTransform.sizeDelta        = new Vector2(cellSize, cellSize);
                img.rectTransform.anchoredPosition = CellToPixel(worldCell);
                img.gameObject.SetActive(true);
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Returns the pixel position (anchored) for a given grid cell.
    private Vector2 CellToPixel(Vector2Int cell)
    {
        return gridOriginOffset + new Vector2(cell.x * cellSize, cell.y * cellSize);
    }

    // Use the buff module's moduleColor if auraColor on this overlay is default.
    private Color GetAuraColor(ModuleData data)
    {
        // Tint the overlay with the module's own color, keeping our alpha
        Color c = data.moduleColor;
        c.a = auraColor.a;
        return c;
    }

    // ── Object pool ───────────────────────────────────────────────────────────

    private Image Rent()
    {
        Image img;
        if (_pool.Count > 0)
        {
            img = _pool[_pool.Count - 1];
            _pool.RemoveAt(_pool.Count - 1);
        }
        else
        {
            var go = new GameObject("AuraCell", typeof(Image));
            go.transform.SetParent(gridPanel, false);
            img = go.GetComponent<Image>();
            img.sprite = auraCellSprite;
            img.type   = Image.Type.Sliced;
            img.raycastTarget = false;

            // Sit behind module icons
            go.transform.SetAsFirstSibling();
        }
        _active.Add(img);
        return img;
    }

    private void ReturnAll()
    {
        foreach (var img in _active)
        {
            img.gameObject.SetActive(false);
            _pool.Add(img);
        }
        _active.Clear();
    }
}

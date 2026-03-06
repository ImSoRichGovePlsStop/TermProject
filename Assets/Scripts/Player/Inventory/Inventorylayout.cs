using UnityEngine;
using UnityEngine.UI;

public class InventoryLayout : MonoBehaviour
{
    [Header("Grid Objects")]
    [SerializeField] private RectTransform weaponGridRect;
    [SerializeField] private RectTransform bagGridRect;

    [Header("Layout Settings")]
    [SerializeField] private float cellSize    = 64f;
    [SerializeField] private float cellSpacing = 2f;
    [SerializeField] private float gapBetweenGrids = 24f;

    [Header("Weapon Grid Size")]
    [SerializeField] private int weaponCols = 5;
    [SerializeField] private int weaponRows = 5;

    [Header("Bag Grid Size")]
    [SerializeField] private int bagCols = 8;
    [SerializeField] private int bagRows = 8;

    private void Awake()
    {
        ApplyLayout();
    }

    [ContextMenu("Apply Layout")]
    public void ApplyLayout()
    {
        if (weaponGridRect == null || bagGridRect == null) return;

        Vector2 weaponSize = CalcGridSize(weaponCols, weaponRows);
        Vector2 bagSize    = CalcGridSize(bagCols,    bagRows);

        float totalHeight = weaponSize.y + gapBetweenGrids + bagSize.y;
        float topY        =  totalHeight * 0.5f; 

        SetRect(weaponGridRect, weaponSize,
            anchorX: 0.5f, anchorY: 0.5f,
            posX: 0f,
            posY: topY - weaponSize.y * 0.5f);

        SetRect(bagGridRect, bagSize,
            anchorX: 0.5f, anchorY: 0.5f,
            posX: 0f,
            posY: topY - weaponSize.y - gapBetweenGrids - bagSize.y * 0.5f);
    }

    private Vector2 CalcGridSize(int cols, int rows)
    {
        float w = cols * (cellSize + cellSpacing) - cellSpacing;
        float h = rows * (cellSize + cellSpacing) - cellSpacing;
        return new Vector2(w, h);
    }

    private static void SetRect(RectTransform rt, Vector2 size,
        float anchorX, float anchorY, float posX, float posY)
    {
        rt.anchorMin        = new Vector2(anchorX, anchorY);
        rt.anchorMax        = new Vector2(anchorX, anchorY);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = new Vector2(posX, posY);
    }
}
using UnityEngine;
using UnityEngine.UI;

public class InventoryLayout : MonoBehaviour
{
    [Header("Grid Objects")]
    [SerializeField] private RectTransform weaponGridRect;
    [SerializeField] private RectTransform bagGridRect;
    [SerializeField] private RectTransform envGridRect;

    [Header("Layout Settings")]
    [SerializeField] private float cellSize        = 64f;
    [SerializeField] private float cellSpacing     = 2f;
    [SerializeField] private float gapBetweenGrids = 24f;

    public float CellSize    => cellSize;
    public float CellSpacing => cellSpacing;

    public void ApplyLayout(int weaponCols, int weaponRows, int bagCols, int bagRows, int envCols, int envRows)
    {
        if (weaponGridRect == null || bagGridRect == null) return;

        Vector2 weaponSize = CalcGridSize(weaponCols, weaponRows);
        Vector2 bagSize    = CalcGridSize(bagCols,    bagRows);

        float totalHeight  = weaponSize.y + gapBetweenGrids + bagSize.y;
        float topY         = totalHeight * 0.5f;
        float centerY      = 0f;

        // weapon และ bag ยังอยู่กลางเหมือนเดิม (posX = 0)
        SetRect(weaponGridRect, weaponSize,
            anchorX: 0.5f, anchorY: 0.5f,
            posX: 0f,
            posY: topY - weaponSize.y * 0.5f);

        SetRect(bagGridRect, bagSize,
            anchorX: 0.5f, anchorY: 0.5f,
            posX: 0f,
            posY: topY - weaponSize.y - gapBetweenGrids - bagSize.y * 0.5f);

        // envGrid อยู่ซ้าย ชิดกับ weaponGrid โดยใช้ gap เดิม
        if (envGridRect != null)
        {
            Vector2 envSize = CalcGridSize(envCols, envRows);
            float envPosX   = -(bagSize.x * 0.5f + gapBetweenGrids + envSize.x * 0.5f);
            SetRect(envGridRect, envSize,
                anchorX: 0.5f, anchorY: 0.5f,
                posX: envPosX,
                posY: centerY);
        }
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
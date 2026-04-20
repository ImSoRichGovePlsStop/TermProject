using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundMaterial : MonoBehaviour, IInteractable
{
    [SerializeField] private Sprite fallbackSprite;
    [SerializeField] private float cellWorldSize = 0.15f;
    [SerializeField] private float borderSize = 4f;
    [SerializeField] private float inventoryCellSize = 63f;

    [Header("Float Animation")]
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatAmplitude = 0.15f;

    private MaterialData data;
    private InventoryUI inventoryUI;
    private float baseY;
    private bool baseYSet = false;

    public void Setup(MaterialData materialData)
    {
        data = materialData;
        Vector2Int bounds = data.GetBoundingSize();
        float width = bounds.x * cellWorldSize;
        float height = bounds.y * cellWorldSize;
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;
        if (data.icon != null)
        {
            sr.sprite = data.icon;
            sr.color = Color.white;
        }
        else
        {
            sr.sprite = fallbackSprite;
            sr.color = data.moduleColor;
        }
        float nativeW = sr.sprite != null ? sr.sprite.bounds.size.x : 1f;
        float nativeH = sr.sprite != null ? sr.sprite.bounds.size.y : 1f;
        transform.localScale = new Vector3(width / nativeW, height / nativeH, 1f);
        BuildRarityBorder(sr, bounds);
    }

    private void BuildRarityBorder(SpriteRenderer mainSr, Vector2Int bounds)
    {
        if (data.icon == null) return;
        var tex = data.icon.texture;
        float pixelsPerUnit = tex.width / (inventoryCellSize * bounds.x);
        int thickness = Mathf.Max(1, Mathf.RoundToInt(borderSize * pixelsPerUnit));
        var outlineTex = SpriteOutlineUtility.GetOrCreate(
            tex,
            SpriteOutlineUtility.RarityColor(data.rarity),
            thickness
        );
        if (outlineTex == null) return;
        float scaleRatio = Mathf.Min(
            (float)outlineTex.width / tex.width,
            (float)outlineTex.height / tex.height
        );
        float adjustedPPU = data.icon.pixelsPerUnit * scaleRatio;
        var outlineSprite = Sprite.Create(
            outlineTex,
            new Rect(0, 0, outlineTex.width, outlineTex.height),
            new Vector2(0.5f, 0.5f),
            adjustedPPU
        );
        var borderGo = new GameObject("RarityBorder");
        borderGo.transform.SetParent(transform, false);
        borderGo.transform.localPosition = Vector3.zero;
        borderGo.transform.localScale = Vector3.one;
        var borderSr = borderGo.AddComponent<SpriteRenderer>();
        borderSr.sprite = outlineSprite;
        borderSr.color = Color.white;
        borderSr.sortingOrder = mainSr.sortingOrder - 1;
    }

    private void Start()
    {
        inventoryUI = FindFirstObjectByType<InventoryUI>();
        StartCoroutine(InitBaseY());
    }

    private IEnumerator InitBaseY()
    {
        yield return null;
        yield return null;
        baseY = transform.position.y;
        baseYSet = true;
    }

    private void LateUpdate()
    {
        if (!baseYSet) return;
        Vector3 pos = transform.position;
        pos.y = baseY + (Mathf.Sin(Time.time * floatSpeed) + 1f) * floatAmplitude;
        transform.position = pos;
    }

    public string GetPromptText() => $"[ E ]  Pick up {data?.moduleName}";

    public InteractInfo GetInteractInfo() => new InteractInfo
    {
        name        = data?.moduleName ?? "Material",
        actionText  = "Pick Up",
        cost        = null
    };

    public void Interact(PlayerController playerController)
    {
        if (data == null) return;
        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        if (inventoryUI == null)
        {
            Debug.LogWarning("[GroundMaterial] InventoryUI not found!");
            return;
        }
        var result = inventoryUI.SpawnMaterial(data);
        Destroy(gameObject);
    }
}
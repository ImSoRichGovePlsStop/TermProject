using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GenericTreeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI treeNameText;
    [SerializeField] private Transform nodeContainer;
    [SerializeField] private GameObject nodeUIPrefab;
    [SerializeField] private Transform lineContainer;

    private Color lineColorDefault = new Color(0.4f, 0.4f, 0.4f, 0.3f);
    private float lineWidth = 3f;

    private GenericTreeData tree;
    private GenericTreeManager manager;
    private object owner;
    private bool tooltipAnchorLeft;

    private Dictionary<GenericTreeNode, GenericTreeNodeUI> nodeUIMap
        = new Dictionary<GenericTreeNode, GenericTreeNodeUI>();
    private Dictionary<GenericTreeNode, RectTransform> nodeRects
        = new Dictionary<GenericTreeNode, RectTransform>();
    private List<(Image line, GenericTreeNode from, GenericTreeNode to)> lineImages
        = new List<(Image, GenericTreeNode, GenericTreeNode)>();

    public void Setup(GenericTreeData tree, GenericTreeManager manager, IGenericTreeScreenUI screenUI, object owner, bool tooltipAnchorLeft = false)
    {
        this.tree = tree;
        this.manager = manager;
        this.owner = owner;
        this.tooltipAnchorLeft = tooltipAnchorLeft;

        treeNameText.text = tree.treeName;
        treeNameText.color = tree.treeColor;

        nodeUIMap.Clear();
        nodeRects.Clear();
        lineImages.Clear();

        Dictionary<GenericTreeNode, int> layerMap = ComputeLayers();
        Dictionary<int, List<GenericTreeNode>> layers = GroupByLayer(layerMap);

        int maxLayer = 0;
        foreach (var k in layers.Keys)
            if (k > maxLayer) maxLayer = k;

        for (int layer = 0; layer <= maxLayer; layer++)
        {
            if (!layers.ContainsKey(layer)) continue;

            var layerNodes = layers[layer];
            RectTransform prefabRect = nodeUIPrefab.GetComponent<RectTransform>();
            float nodeHeight = prefabRect.sizeDelta.y;

            GameObject row = new GameObject("Layer" + layer, typeof(RectTransform));
            row.transform.SetParent(nodeContainer, false);

            LayoutElement rowLE = row.AddComponent<LayoutElement>();
            rowLE.minHeight = nodeHeight;
            rowLE.preferredHeight = nodeHeight;

            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 50;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            foreach (var node in layerNodes)
            {
                GameObject obj = Instantiate(nodeUIPrefab, row.transform);
                var nodeUI = obj.GetComponent<GenericTreeNodeUI>();
                nodeUI.Setup(node, tree, manager, screenUI, owner, tooltipAnchorLeft);

                nodeUIMap[node] = nodeUI;
                nodeRects[node] = obj.GetComponent<RectTransform>();
            }
        }

        StartCoroutine(DrawLinesAfterLayout());
    }

    private Dictionary<GenericTreeNode, int> ComputeLayers()
    {
        var layerMap = new Dictionary<GenericTreeNode, int>();
        var visited = new HashSet<GenericTreeNode>();

        foreach (var node in tree.nodes)
            AssignLayer(node, layerMap, visited);

        return layerMap;
    }

    private int AssignLayer(GenericTreeNode node, Dictionary<GenericTreeNode, int> layerMap, HashSet<GenericTreeNode> visited)
    {
        if (layerMap.ContainsKey(node)) return layerMap[node];
        if (visited.Contains(node))
        {
            layerMap[node] = 0;
            return 0;
        }

        visited.Add(node);

        if (node.parents == null || node.parents.Length == 0)
        {
            layerMap[node] = 0;
            return 0;
        }

        int maxParentLayer = -1;
        foreach (var parent in node.parents)
        {
            if (parent == null) continue;
            int parentLayer = AssignLayer(parent, layerMap, visited);
            if (parentLayer > maxParentLayer) maxParentLayer = parentLayer;
        }

        layerMap[node] = maxParentLayer + 1;
        return maxParentLayer + 1;
    }

    private Dictionary<int, List<GenericTreeNode>> GroupByLayer(Dictionary<GenericTreeNode, int> layerMap)
    {
        var result = new Dictionary<int, List<GenericTreeNode>>();
        foreach (var kvp in layerMap)
        {
            if (!result.ContainsKey(kvp.Value))
                result[kvp.Value] = new List<GenericTreeNode>();
            result[kvp.Value].Add(kvp.Key);
        }
        return result;
    }

    private IEnumerator DrawLinesAfterLayout()
    {
        yield return null;
        yield return null;

        if (lineContainer == null) yield break;

        foreach (var node in tree.nodes)
        {
            if (node.parents == null) continue;
            foreach (var parent in node.parents)
            {
                if (parent == null) continue;
                if (!nodeRects.ContainsKey(parent) || !nodeRects.ContainsKey(node)) continue;
                DrawLine(nodeRects[parent], nodeRects[node], parent, node);
            }
        }

        RefreshLineColors();
    }

    private void DrawLine(RectTransform fromRect, RectTransform toRect, GenericTreeNode fromNode, GenericTreeNode toNode)
    {
        Vector2 fromPos = GetLocalPos(fromRect);
        Vector2 toPos = GetLocalPos(toRect);

        Vector2 mid1 = new Vector2(fromPos.x, (fromPos.y + toPos.y) / 2f);
        Vector2 mid2 = new Vector2(toPos.x, (fromPos.y + toPos.y) / 2f);

        Image seg1 = CreateLineSegment(fromPos, mid1);
        Image seg2 = CreateLineSegment(mid1, mid2);
        Image seg3 = CreateLineSegment(mid2, toPos);

        lineImages.Add((seg1, fromNode, toNode));
        lineImages.Add((seg2, fromNode, toNode));
        lineImages.Add((seg3, fromNode, toNode));
    }

    private Image CreateLineSegment(Vector2 from, Vector2 to)
    {
        Vector2 dir = to - from;
        float distance = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        GameObject lineObj = new GameObject("Line", typeof(RectTransform));
        lineObj.transform.SetParent(lineContainer, false);

        Image img = lineObj.AddComponent<Image>();
        img.color = lineColorDefault;

        RectTransform rt = lineObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(distance, lineWidth);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = from;
        rt.localRotation = Quaternion.Euler(0, 0, angle);

        return img;
    }

    private Vector2 GetLocalPos(RectTransform rect)
    {
        Vector3 worldPos = rect.TransformPoint(Vector3.zero);
        return lineContainer.InverseTransformPoint(worldPos);
    }

    public void RefreshAll()
    {
        foreach (var kvp in nodeUIMap)
            kvp.Value.Refresh();
        RefreshLineColors();
    }

    private void RefreshLineColors()
    {
        if (manager == null) return;
        int pts = manager.GetAvailablePoints(owner);
        var state = manager.GetState(owner, tree);
        foreach (var (img, fromNode, toNode) in lineImages)
        {
            if (fromNode == null || toNode == null) continue;
            bool fromActive = state.IsUnlocked(fromNode);
            bool toActive = state.IsUnlocked(toNode) || state.CanUnlock(toNode, pts);
            Color litColor = tree.treeColor;
            litColor.a = 0.5f;
            img.color = (fromActive && toActive) ? litColor : lineColorDefault;
        }
    }

    public void Clear()
    {
        foreach (Transform child in nodeContainer)
            Destroy(child.gameObject);
        if (lineContainer != null)
            foreach (Transform child in lineContainer)
                Destroy(child.gameObject);
        nodeUIMap.Clear();
        nodeRects.Clear();
        lineImages.Clear();
    }
}

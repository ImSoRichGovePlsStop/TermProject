using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PassiveTreeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI treeNameText;
    [SerializeField] private Transform nodeContainer;
    [SerializeField] private GameObject nodeUIPrefab;
    [SerializeField] private Transform lineContainer;

    private Color lineColor = new Color(0.4f, 0.4f, 0.4f, 0.3f);
    private float lineWidth = 3f;

    private PassiveTree tree;
    private WeaponPassiveManager manager;
    private PassiveNodeUI[] nodeUIs;
    private WeaponPassiveData passiveData;
    private bool tooltipAnchorLeft;

    private Dictionary<(int layer, int branch), RectTransform> nodeRects
        = new Dictionary<(int, int), RectTransform>();

    private List<(Image line, PassiveNode fromNode, PassiveNode toNode)> lineImages
    = new List<(Image, PassiveNode, PassiveNode)>();

    public void Setup(PassiveTree tree, WeaponPassiveManager manager, PassiveScreenUI screenUI, WeaponPassiveData data, bool tooltipAnchorLeft = false)
    {
        this.tree = tree;
        this.manager = manager;
        this.passiveData = data;
        this.tooltipAnchorLeft = tooltipAnchorLeft;

        treeNameText.text = tree.treeName;
        nodeUIs = new PassiveNodeUI[tree.nodes.Length];
        nodeRects.Clear();
        lineImages.Clear();

        for (int layer = 1; layer <= 6; layer++)
        {
            List<PassiveNode> layerNodes = new List<PassiveNode>();
            foreach (var node in tree.nodes)
                if (node.layer == layer) layerNodes.Add(node);

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

            if (layerNodes.Count == 0) continue;

            layerNodes.Sort((a, b) => a.branch.CompareTo(b.branch));

            foreach (var node in layerNodes)
            {
                GameObject obj = Instantiate(nodeUIPrefab, row.transform);
                PassiveNodeUI nodeUI = obj.GetComponent<PassiveNodeUI>();
                nodeUI.Setup(node, tree, manager, screenUI, data, tooltipAnchorLeft);

                nodeRects[(node.layer, node.branch)] = obj.GetComponent<RectTransform>();

                for (int i = 0; i < nodeUIs.Length; i++)
                {
                    if (nodeUIs[i] == null) { nodeUIs[i] = nodeUI; break; }
                }
            }
        }

        StartCoroutine(DrawLinesAfterLayout());
    }

    private IEnumerator DrawLinesAfterLayout()
    {
        yield return null;
        yield return null;

        if (lineContainer == null) yield break;

        var connections = new List<((int l, int b) from, (int l, int b) to)>
        {
            ((1,0),(2,0)),
            ((2,0),(3,1)),
            ((2,0),(3,2)),
            ((3,1),(4,0)),
            ((3,2),(4,0)),
            ((4,0),(5,1)),
            ((4,0),(5,2)),
            ((5,1),(6,0)),
            ((5,2),(6,0)),
        };

        foreach (var (from, to) in connections)
        {
            if (!nodeRects.ContainsKey(from) || !nodeRects.ContainsKey(to)) continue;

            PassiveNode fromNode = null, toNode = null;
            foreach (var node in tree.nodes)
            {
                if (node.layer == from.l && node.branch == from.b) fromNode = node;
                if (node.layer == to.l && node.branch == to.b) toNode = node;
            }
            DrawLine(nodeRects[from], nodeRects[to], fromNode, toNode);
        }

        RefreshLineColors();
    }

    private void DrawLine(RectTransform fromRect, RectTransform toRect, PassiveNode fromNode, PassiveNode toNode)
    {
        Vector2 fromPos = GetLocalPos(fromRect);
        Vector2 toPos = GetLocalPos(toRect);

        Vector2 midFrom = new Vector2(fromPos.x, (fromPos.y + toPos.y) / 2f);
        Vector2 midTo = new Vector2(toPos.x, (fromPos.y + toPos.y) / 2f);

        Image img1 = CreateLineSegment(fromPos, midFrom);
        Image img2 = CreateLineSegment(midFrom, midTo);
        Image img3 = CreateLineSegment(midTo, toPos);

        lineImages.Add((img1, fromNode, toNode));
        lineImages.Add((img2, fromNode, toNode));
        lineImages.Add((img3, fromNode, toNode));
    }

    private Image CreateLineSegment(Vector2 from, Vector2 to)
    {
        Vector2 dir = to - from;
        float distance = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        GameObject lineObj = new GameObject("Line", typeof(RectTransform));
        lineObj.transform.SetParent(lineContainer, false);

        Image img = lineObj.AddComponent<Image>();
        img.color = lineColor;

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

    private void RefreshLineColors()
    {
        if (manager == null || passiveData == null) return;
        var state = manager.GetState(passiveData);
        foreach (var (img, fromNode, toNode) in lineImages)
        {
            if (fromNode == null || toNode == null) continue;
            bool fromActive = state.IsUnlocked(fromNode);
            bool toActive = state.IsUnlocked(toNode) || state.CanUnlock(toNode, tree);
            Color litColor = tree.treeColor;
            litColor.a = 0.5f;
            img.color = (fromActive && toActive) ? litColor : lineColor;
        }
    }

    public void RefreshAll()
    {
        if (nodeUIs == null) return;
        foreach (var nodeUI in nodeUIs)
            nodeUI.Refresh();
        RefreshLineColors();
    }

    public void Clear()
    {
        foreach (Transform child in nodeContainer)
            Destroy(child.gameObject);
        if (lineContainer != null)
            foreach (Transform child in lineContainer)
                Destroy(child.gameObject);
        nodeUIs = null;
        nodeRects.Clear();
        lineImages.Clear();
    }
}
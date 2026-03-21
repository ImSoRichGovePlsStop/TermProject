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

    [Header("Layout")]
    [SerializeField] private float nodeSpacing = 50f;
    [SerializeField] private float layerSpacing = 80f;

    private Color lineColorDefault = new Color(0.4f, 0.4f, 0.4f, 0.3f);
    private float lineWidth = 3f;

    private GenericTreeData tree;
    private GenericTreeManager manager;
    private object owner;
    private bool tooltipAnchorLeft;

    private Dictionary<GenericTreeNode, GenericTreeNodeUI> nodeUIMap = new Dictionary<GenericTreeNode, GenericTreeNodeUI>();
    private Dictionary<GenericTreeNode, RectTransform> nodeRects = new Dictionary<GenericTreeNode, RectTransform>();
    private List<(Image line, GenericTreeNode from, GenericTreeNode to)> lineImages = new List<(Image, GenericTreeNode, GenericTreeNode)>();

    private Dictionary<GenericTreeNode, int> layerMap;
    private Dictionary<int, List<GenericTreeNode>> layers;

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

        layerMap = ComputeLayers();
        layers = GroupByLayer(layerMap);

        RectTransform prefabRect = nodeUIPrefab.GetComponent<RectTransform>();
        float nodeWidth = prefabRect.sizeDelta.x;
        float nodeHeight = prefabRect.sizeDelta.y;

        foreach (var node in tree.nodes)
        {
            GameObject obj = Instantiate(nodeUIPrefab, nodeContainer);
            var nodeUI = obj.GetComponent<GenericTreeNodeUI>();
            nodeUI.Setup(node, tree, manager, screenUI, owner, tooltipAnchorLeft);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            nodeUIMap[node] = nodeUI;
            nodeRects[node] = rt;
        }

        StartCoroutine(ApplyLayoutNextFrame(nodeWidth, nodeHeight));
    }

    private IEnumerator ApplyLayoutNextFrame(float nodeWidth, float nodeHeight)
    {
        yield return null;

        int maxLayer = 0;
        foreach (var k in layers.Keys)
            if (k > maxLayer) maxLayer = k;

        float step = nodeWidth + nodeSpacing;
        Dictionary<GenericTreeNode, float> xPositions = new Dictionary<GenericTreeNode, float>();

        // Step 1: assign initial X to leaf nodes bottom-up left to right
        float cursor = 0f;
        AssignLeafPositions(maxLayer, xPositions, ref cursor, step);

        // Step 2: bottom-up pass — parent X = median (odd) or average (even) of children X
        for (int layer = maxLayer - 1; layer >= 0; layer--)
        {
            if (!layers.ContainsKey(layer)) continue;
            foreach (var node in layers[layer])
            {
                List<GenericTreeNode> children = GetChildren(node);
                if (children.Count == 0) continue;

                List<float> childXList = new List<float>();
                foreach (var child in children)
                    if (xPositions.ContainsKey(child)) childXList.Add(xPositions[child]);
                childXList.Sort();

                if (childXList.Count == 0) continue;

                if (childXList.Count % 2 == 1)
                    xPositions[node] = childXList[childXList.Count / 2]; // odd → middle
                else
                {
                    float sum = 0f;
                    foreach (var x in childXList) sum += x;
                    xPositions[node] = sum / childXList.Count; // even → average
                }
            }

            EnsureMinSpacing(layers[layer], xPositions, step);
        }

        // Step 3: center multi-parent nodes between their parents
        for (int layer = 1; layer <= maxLayer; layer++)
        {
            if (!layers.ContainsKey(layer)) continue;
            foreach (var node in layers[layer])
            {
                if (node.parents == null || node.parents.Length <= 1) continue;

                List<float> parentXList = new List<float>();
                foreach (var parent in node.parents)
                    if (parent != null && xPositions.ContainsKey(parent))
                        parentXList.Add(xPositions[parent]);

                if (parentXList.Count == 0) continue;
                parentXList.Sort();

                float newX;
                if (parentXList.Count % 2 == 1)
                    newX = parentXList[parentXList.Count / 2];
                else
                {
                    float sum = 0f;
                    foreach (var x in parentXList) sum += x;
                    newX = sum / parentXList.Count;
                }

                if (xPositions.ContainsKey(node))
                {
                    float delta = newX - xPositions[node];
                    xPositions[node] = newX;
                    ShiftSubtree(node, delta, xPositions);
                }
            }

            EnsureMinSpacing(layers[layer], xPositions, step);
        }

        // Step 4: center based on root node
        GenericTreeNode rootNode = null;
        foreach (var node in tree.nodes)
            if (node.parents == null || node.parents.Length == 0) { rootNode = node; break; }

        float offset = rootNode != null && xPositions.ContainsKey(rootNode) ? xPositions[rootNode] : 0f;

        // Step 5: apply positions
        foreach (var node in tree.nodes)
        {
            if (!xPositions.ContainsKey(node) || !nodeRects.ContainsKey(node)) continue;
            int layer = layerMap[node];
            nodeRects[node].anchoredPosition = new Vector2(xPositions[node] - offset, -layer * (nodeHeight + layerSpacing));
        }

        yield return null;
        yield return null;

        foreach (var node in tree.nodes)
        {
            if (node.parents == null) continue;
            foreach (var parent in node.parents)
            {
                if (parent == null) continue;
                if (!nodeRects.ContainsKey(parent) || !nodeRects.ContainsKey(node)) continue;
                DrawLineFromPositions(parent, node);
            }
        }

        RefreshLineColors();
    }

    private void AssignLeafPositions(int maxLayer, Dictionary<GenericTreeNode, float> xPos, ref float cursor, float step)
    {
        var roots = new List<GenericTreeNode>();
        foreach (var node in tree.nodes)
            if (node.parents == null || node.parents.Length == 0) roots.Add(node);

        var visitedForTraversal = new HashSet<GenericTreeNode>();
        var assignedLeafs = new HashSet<GenericTreeNode>();

        foreach (var root in roots)
            AssignLeafDFS(root, xPos, ref cursor, step, visitedForTraversal, assignedLeafs);
    }

    private void AssignLeafDFS(GenericTreeNode node, Dictionary<GenericTreeNode, float> xPos, ref float cursor, float step, HashSet<GenericTreeNode> visitedForTraversal, HashSet<GenericTreeNode> assignedLeafs)
    {
        if (visitedForTraversal.Contains(node)) return;
        visitedForTraversal.Add(node);

        List<GenericTreeNode> children = GetChildren(node);
        if (children.Count == 0)
        {
            if (!assignedLeafs.Contains(node))
            {
                xPos[node] = cursor;
                int parentCount = (node.parents != null) ? node.parents.Length : 1;
                cursor += step * Mathf.Max(1, parentCount);
                assignedLeafs.Add(node);
            }
            return;
        }

        foreach (var child in children)
            AssignLeafDFS(child, xPos, ref cursor, step, visitedForTraversal, assignedLeafs);
    }

    private void ShiftSubtree(GenericTreeNode root, float delta, Dictionary<GenericTreeNode, float> xPos)
    {
        var queue = new Queue<GenericTreeNode>();
        queue.Enqueue(root);
        var visited = new HashSet<GenericTreeNode>();
        visited.Add(root);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            foreach (var child in GetChildren(node))
            {
                if (visited.Contains(child)) continue;
                visited.Add(child);
                if (xPos.ContainsKey(child)) xPos[child] += delta;
                queue.Enqueue(child);
            }
        }
    }

    private void EnsureMinSpacing(List<GenericTreeNode> nodes, Dictionary<GenericTreeNode, float> xPos, float minDist)
    {
        var positioned = new List<GenericTreeNode>();
        foreach (var n in nodes)
            if (xPos.ContainsKey(n)) positioned.Add(n);

        positioned.Sort((a, b) => xPos[a].CompareTo(xPos[b]));

        for (int i = 1; i < positioned.Count; i++)
        {
            float diff = xPos[positioned[i]] - xPos[positioned[i - 1]];
            if (diff < minDist)
            {
                float shift = minDist - diff;
                xPos[positioned[i]] += shift;
                ShiftSubtree(positioned[i], shift, xPos);
            }
        }
    }

    private List<GenericTreeNode> GetChildren(GenericTreeNode parent)
    {
        var children = new List<GenericTreeNode>();
        foreach (var node in tree.nodes)
        {
            if (node.parents == null) continue;
            foreach (var p in node.parents)
                if (p == parent) { children.Add(node); break; }
        }
        return children;
    }

    private void DrawLineFromPositions(GenericTreeNode fromNode, GenericTreeNode toNode)
    {
        float nodeH = nodeUIPrefab.GetComponent<RectTransform>().sizeDelta.y;

        Vector2 from = GetLocalPos(nodeRects[fromNode]);
        from.y -= nodeH / 2f;

        Vector2 to = GetLocalPos(nodeRects[toNode]);
        to.y += nodeH / 2f;

        float midY = to.y + layerSpacing / 2f;

        Vector2 mid1 = new Vector2(from.x, midY);
        Vector2 mid2 = new Vector2(to.x, midY);

        lineImages.Add((CreateLineSegment(from, mid1), fromNode, toNode));
        lineImages.Add((CreateLineSegment(mid1, mid2), fromNode, toNode));
        lineImages.Add((CreateLineSegment(mid2, to), fromNode, toNode));
    }

    private Vector2 GetLocalPos(RectTransform rect)
    {
        Vector3 worldPos = rect.TransformPoint(Vector3.zero);
        return lineContainer.InverseTransformPoint(worldPos);
    }

    private Image CreateLineSegment(Vector2 from, Vector2 to)
    {
        Vector2 dir = to - from;
        float distance = dir.magnitude;
        if (distance < 0.01f) return null;
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

    private Dictionary<GenericTreeNode, int> ComputeLayers()
    {
        var result = new Dictionary<GenericTreeNode, int>();
        var visited = new HashSet<GenericTreeNode>();
        foreach (var node in tree.nodes)
            AssignLayer(node, result, visited);
        return result;
    }

    private int AssignLayer(GenericTreeNode node, Dictionary<GenericTreeNode, int> layerMap, HashSet<GenericTreeNode> visited)
    {
        if (layerMap.ContainsKey(node)) return layerMap[node];
        if (visited.Contains(node)) { layerMap[node] = 0; return 0; }
        visited.Add(node);

        if (node.parents == null || node.parents.Length == 0)
        {
            visited.Remove(node);
            layerMap[node] = 0;
            return 0;
        }

        int maxParentLayer = -1;
        foreach (var parent in node.parents)
        {
            if (parent == null) continue;
            int pl = AssignLayer(parent, layerMap, visited);
            if (pl > maxParentLayer) maxParentLayer = pl;
        }

        visited.Remove(node);
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
            if (img == null || fromNode == null || toNode == null) continue;
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
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

    private PassiveTree tree;
    private WeaponPassiveManager manager;
    private PassiveNodeUI[] nodeUIs;
    private WeaponPassiveData passiveData;

    public void Setup(PassiveTree tree, WeaponPassiveManager manager, PassiveScreenUI screenUI, WeaponPassiveData data)
    {
        this.tree = tree;
        this.manager = manager;
        this.passiveData = data;

        treeNameText.text = tree.treeName;

        nodeUIs = new PassiveNodeUI[tree.nodes.Length];

        // group nodes by layer
        for (int layer = 1; layer <= 6; layer++)
        {
            List<PassiveNode> layerNodes = new List<PassiveNode>();
            foreach (var node in tree.nodes)
            {
                if (node.layer == layer)
                    layerNodes.Add(node);
            }

            RectTransform prefabRect = nodeUIPrefab.GetComponent<RectTransform>();
            float nodeHeight = prefabRect.sizeDelta.y;

            GameObject row = new GameObject("Layer" + layer, typeof(RectTransform));
            row.transform.SetParent(nodeContainer, false);

            LayoutElement rowLE = row.AddComponent<LayoutElement>();
            rowLE.minHeight = nodeHeight;
            rowLE.preferredHeight = nodeHeight;

            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 10;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            if (layerNodes.Count == 0) continue;

            foreach (var node in layerNodes)
            {
                GameObject obj = Instantiate(nodeUIPrefab, row.transform);
                PassiveNodeUI nodeUI = obj.GetComponent<PassiveNodeUI>();
                nodeUI.Setup(node, tree, manager, screenUI, data);

                for (int i = 0; i < nodeUIs.Length; i++)
                {
                    if (nodeUIs[i] == null) { nodeUIs[i] = nodeUI; break; }
                }
            }
        }
    }

    public void RefreshAll()
    {
        if (nodeUIs == null) return;
        foreach (var nodeUI in nodeUIs)
            nodeUI.Refresh();
    }

    public void Clear()
    {
        foreach (Transform child in nodeContainer)
            Destroy(child.gameObject);
        nodeUIs = null;
    }
}
using UnityEngine;

public class GamblerTreeHandler : GenericTreeHandlerBase
{
    [Header("References")]
    [SerializeField] private CardPhaseUI cardPhaseUI;

    public override void Init(GenericTreeData tree, GenericTreeManager manager, object owner)
    {
        base.Init(tree, manager, owner);

        if (cardPhaseUI == null)
            cardPhaseUI = FindFirstObjectByType<CardPhaseUI>(FindObjectsInactive.Include);
    }

    public override void Apply()
    {
        if (cardPhaseUI == null)
            cardPhaseUI = FindFirstObjectByType<CardPhaseUI>(FindObjectsInactive.Include);

        if (tree == null || manager == null || treeOwner == null) return;

        cardPhaseUI?.Configure(BuildConfig());
    }

    private GamblerCardPhaseConfig BuildConfig()
    {
        var cfg = new GamblerCardPhaseConfig();

        cfg.cardPhaseEnabled = IsUnlocked(1, 0);
        cfg.givePermanentBuff = IsUnlocked(2, 0);

        if (IsUnlocked(2, 1))
        {
            cfg.loadedDeckVariant = true;
            cfg.initialPositiveCount = 4;
            cfg.initialNegativeCount = 2;
        }

        cfg.hasReroll = IsUnlocked(3, 0);
        cfg.showAura = IsUnlocked(4, 0);
        cfg.hasDevilsBet = IsUnlocked(4, 1);
        cfg.useExtendedPool = IsUnlocked(4, 2);

        if (IsUnlocked(5, 0))
        {
            cfg.hasPeek = true;
            cfg.peekCount = 1;
        }

        if (IsUnlocked(5, 2))
        {
            cfg.highRollerMode = true;
            cfg.extremeCardWeight = 3f;
        }

        return cfg;
    }

    private bool IsUnlocked(int layer, int branch)
    {
        if (tree?.nodes == null) return false;
        var state = manager.GetState(treeOwner, tree);
        foreach (var node in tree.nodes)
        {
            var gNode = node as GamblerNode;
            if (gNode != null && gNode.layer == layer && gNode.branch == branch)
                return state.IsUnlocked(gNode);
        }
        return false;
    }
}
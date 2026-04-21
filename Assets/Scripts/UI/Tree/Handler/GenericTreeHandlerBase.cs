using UnityEngine;

public abstract class GenericTreeHandlerBase : MonoBehaviour
{
    protected GenericTreeData tree;
    protected GenericTreeManager manager;
    protected object treeOwner;

    public virtual void Init(GenericTreeData tree, GenericTreeManager manager, object owner)
    {
        this.tree = tree;
        this.manager = manager;
        this.treeOwner = owner;
        Apply();
    }

    public abstract void Apply();

    protected bool IsUnlocked(GenericTreeNode node)
    {
        return manager.GetState(treeOwner, tree).IsUnlocked(node);
    }

    public virtual void Cleanup() { }
}

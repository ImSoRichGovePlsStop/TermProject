public class MaterialInstance : ModuleInstance
{
    public MaterialData MaterialData => (MaterialData)Data;
    public int MaxStack  => MaterialData.maxStack;
    public int Cost => MaterialData.cost[0];
    public int StackCount { get; private set; } = 1;

    public event System.Action OnStackChanged;

    public MaterialInstance(MaterialData data)
        : base(data, data.rarity) { }

    // Can this instance stack onto `target`?
    public bool CanStackOnto(MaterialInstance target) =>
        target != this &&
        target.MaterialData == MaterialData &&
        target.StackCount < target.MaxStack;

    public void AddStack()
    {
        if (StackCount < MaxStack)
        {
            StackCount++;
            OnStackChanged?.Invoke();
        }
    }

    public void RemoveStack()
    {
        if (StackCount > 0)
        {
            StackCount--;
            OnStackChanged?.Invoke();
        }
    }
}

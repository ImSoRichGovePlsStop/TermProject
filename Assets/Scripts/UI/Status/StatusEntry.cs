using UnityEngine;

public enum StatusBorderType { Default, Gold, Red }

public class StatusEntry
{
    public string id;
    public Sprite icon;
    public bool isActive;
    public int stackCount;

    public float innerBorderFill;
    public bool isInnerBorderVisible;
    public StatusBorderType outerBorderType;

    public float stackExpireFill;

    public StatusEntry(string id, Sprite icon)
    {
        this.id = id;
        this.icon = icon;
        this.isActive = true;
    }
}
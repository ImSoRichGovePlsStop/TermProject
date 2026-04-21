using UnityEngine;

public enum StatusBorderType { Default, Gold, Red, Blue }

public class StatusEntry
{
    public string id;
    public Sprite icon;
    public bool isActive;
    public int count;

    public float innerFill;
    public bool showInnerBorder;
    public Color innerBorderColor = Color.white;
    public StatusBorderType outerBorderType;

    public float sweepFill;
    public bool sweepClockwise;
    public bool innerFillClockwise;

    public StatusEntry(string id, Sprite icon)
    {
        this.id = id;
        this.icon = icon;
        this.isActive = true;
    }
}
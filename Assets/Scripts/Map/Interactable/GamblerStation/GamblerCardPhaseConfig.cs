public class GamblerCardPhaseConfig
{
    // L1
    public bool cardPhaseEnabled = false;

    // L2A
    public bool givePermanentBuff = false;

    // L2B
    public bool loadedDeckVariant = false;

    public int initialPositiveCount = 3;
    public int initialNegativeCount = 3;

    // L3
    public bool hasReroll = false;

    // L4A
    public bool showAura = false;

    // L4B
    public bool hasDevilsBet = false;

    // L4C
    public bool useExtendedPool = false;

    // L5A
    public bool hasPeek = false;
    public int peekCount = 1;

    // L5C
    public bool highRollerMode = false;
    public float extremeCardWeight = 1f;
}

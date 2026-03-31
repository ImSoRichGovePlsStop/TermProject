[System.Serializable]
public class TargetPriority
{
    public float playerPriority = 1f;
    public float summonerPriority = 1f;
    public float totemPriority = 1f;

    public float GetPriority(HealthBase target)
    {
        if (target is SummonerHealth) return summonerPriority;
        if (target is Totem) return totemPriority;
        return playerPriority;
    }
}
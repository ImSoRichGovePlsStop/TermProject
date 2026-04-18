
public interface IGroupSpawner
{

    int GetSpawnCount();


    void SetGroupStatScale(StatScale scale);


    void SetMissCallback(System.Action<int> onMissed);
}

using UnityEngine;

public class TilePlacer : MonoBehaviour
{
    public GameObject tilePrefab;
    public int width = 5;
    public int height = 5;
    public float y = 0f;
    public float tileSize = 1f;

    [ContextMenu("Place Tiles")]
    void PlaceTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos = new Vector3(x * tileSize, y, z * tileSize);
                Instantiate(tilePrefab, pos, Quaternion.identity, transform);
            }
        }
    }
}
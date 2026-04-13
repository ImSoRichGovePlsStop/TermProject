using UnityEngine;

public class WallPlacer : MonoBehaviour
{
    public GameObject wallPrefab;
    public int width = 5;
    public int height = 5;
    public float tileSize = 1f;
    public float wallTileSize = 2f;
    public float wallY = 0f;
    public float wallOffset = 0f;

    [ContextMenu("Place Walls")]
    void PlaceWalls()
    {
        float totalWidth = (width - 1) * tileSize;
        float totalHeight = (height - 1) * tileSize;

        // Bottom edge
        for (float x = 0; x < totalWidth; x += wallTileSize)
        {
            Vector3 pos = new Vector3(x+0.5f, wallY, -wallOffset);
            Instantiate(wallPrefab, pos, Quaternion.identity, transform);
        }

        // Top edge
        for (float x = 0; x < totalWidth; x += wallTileSize)
        {
            Vector3 pos = new Vector3(x+0.5f, wallY, totalHeight + wallOffset);
            Instantiate(wallPrefab, pos, Quaternion.identity, transform);
        }

        // Left edge
        for (float z = 0; z <= totalHeight; z += wallTileSize)
        {
            Vector3 pos = new Vector3(-wallOffset, wallY, z+0.5f);
            Quaternion rot = Quaternion.Euler(0, 90, 0);
            Instantiate(wallPrefab, pos, rot, transform);
        }

        // Right edge
        for (float z = 0; z <= totalHeight; z += wallTileSize)
        {
            Vector3 pos = new Vector3(totalWidth + wallOffset, wallY, z + 0.5f);
            Quaternion rot = Quaternion.Euler(0, 90, 0);
            Instantiate(wallPrefab, pos, rot, transform);
        }
    }
}
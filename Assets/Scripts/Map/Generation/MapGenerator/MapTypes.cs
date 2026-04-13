using System;
using System.Collections.Generic;
using UnityEngine;

public enum RoomType { Unmarked, Spawn, Battle, Boss, Shop, Merge, Heal, RareLoot, Fountain }

public static class Cell
{
    public const byte Empty    = 0;
    public const byte Corridor = 1;
    public const byte Room     = 2;
    public const byte Occupied = 3;
}

[Serializable]
public class MapNode
{
    public RoomType   Type;
    public int        MinX, MinZ, MaxX, MaxZ;
    public Vector3    WorldCenter;
    public GameObject ChosenPrefab;
    public GameObject RoomObject;

    [NonSerialized] public List<MapEdge>  Edges      = new();
    [NonSerialized] public RoomNode       LegacyNode;
    [NonSerialized] public BSPRoomPreset  Preset;

    public int CenterX => (MinX + MaxX) / 2;
    public int CenterZ => (MinZ + MaxZ) / 2;
    public int Width   => MaxX - MinX + 1;
    public int Depth   => MaxZ - MinZ + 1;
}

public class MapEdge
{
    public MapNode              A;
    public MapNode              B;
    public List<List<Vector2Int>> Segments = new();
}

[Serializable]
public class RoomNode
{
    public RoomType   Type;
    public Vector2Int MatrixOrigin;
    public Vector2Int MatrixCenter;
    public Vector2Int Size;
    public Vector3    WorldPosition;
    public GameObject ChosenPrefab;
    public GameObject RoomObject;
    [NonSerialized] public List<RoomNode> Neighbors = new();
}

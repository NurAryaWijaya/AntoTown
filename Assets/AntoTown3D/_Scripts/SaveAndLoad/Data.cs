using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TileData
{
    public Vector2Int gridPosition;
    public bool isOccupied;
    public string objectType; // "Building" / "Road"
    public string prefabName; // nama prefab
}

[Serializable]
public class BuildingData
{
    public string buildingName;
    public string prefabName;
    public Vector2Int anchorTile;
    public Vector3 size;
    public BuildingType buildingType;
    public BuildingLevel level;
    public bool canUpgrade;
    public ZoneTier zoneTier;
    public float upgradeTimer;
    public int currentPopulation;
    public float incomePerTick;
    public bool connectedToRoad;
    public bool connectedToPower;
    public bool connectedToWater;
    // bisa tambah field lain sesuai kebutuhan
}

[Serializable]
public class RoadData
{
    public string prefabName;
    public Vector3 position;
    // arah koneksi dsb jika perlu
}

[Serializable]
public class GameData
{
    public int money;
    public List<BuildingData> buildings = new();
    public List<RoadData> roads = new();
    // bisa tambahkan statistik lain, misal population, happiness, jobBalance
}

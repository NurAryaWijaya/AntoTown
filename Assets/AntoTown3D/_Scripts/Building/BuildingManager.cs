using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Header("Prefabs & Grid")]
    public List<Building> buildingPrefabs;
    public List<Building> placedBuildings = new();
    public Tile[,] gridTiles;

    [Header("Upgrade")]
    public float upgradeInterval = 300f; // 5 menit default

    [HideInInspector]
    public GameObject currentObject = null;


    public GridManager gridManager;

    void Awake()
    {
        if (gridManager != null)
            gridTiles = gridManager.tiles;
    }

    void Start()
    {
        if (gridManager != null)
            gridTiles = gridManager.tiles;
    }

    public Tile[] GetNeighborTiles(Tile tile)
    {
        List<Tile> neighbors = new();
        Vector2Int pos = tile.gridPosition;

        int width = gridTiles.GetLength(0);
        int height = gridTiles.GetLength(1);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0) continue;

                int nx = pos.x + dx;
                int nz = pos.y + dz;

                if (nx >= 0 && nx < width && nz >= 0 && nz < height)
                {
                    neighbors.Add(gridTiles[nx, nz]);
                }
            }
        }

        return neighbors.ToArray();
    }

    public bool CanPlaceBuilding(Tile tile, Building prefab)
    {
        if (tile == null || tile.isOccupied) return false;

        // contoh: Park/Industry tidak boleh ditempatkan di tepi grid //  Error disini

        if (prefab.hasArea)
        {
            if (tile.gridPosition.x + prefab.size.x > gridTiles.GetLength(0) ||
                tile.gridPosition.y + prefab.size.z > gridTiles.GetLength(1))
                return false;
        }

        return true;
    }

    public Building PlaceBuilding(Building prefab, Tile tile)
    {
        if (!CanPlaceBuilding(tile, prefab)) return null;

        GameObject obj = Instantiate(prefab.gameObject, tile.transform.position, Quaternion.identity);
        Building building = obj.GetComponent<Building>();

        building.OnPlace(tile, this);
        tile.isOccupied = true;
        tile.currentObject = obj;

        placedBuildings.Add(building);
        return building;
    }

    public void CheckAllConnections()
    {
        foreach (var b in placedBuildings)
        {
            b.CheckRoadConnection();
            b.CheckUtilitiesConnection();
            b.UpdateWarningMark();
        }
    }

    public List<Building> GetBuildingsInArea(Vector3 center, float radius)
    {
        List<Building> nearby = new();
        foreach (var b in placedBuildings)
        {
            if (Vector3.Distance(center, b.transform.position) <= radius)
                nearby.Add(b);
        }
        return nearby;
    }

    public float CalculateHappinessForBuilding(Building building)
    {
        float happiness = 0f;
        var nearby = GetBuildingsInArea(building.transform.position, 10f);

        foreach (var b in nearby)
        {
            happiness += b.happinessEffect;
        }

        return happiness;
    }

    public void UpgradeBuildings(float deltaTime)
    {
        foreach (var b in placedBuildings)
        {
            b.Upgrade(deltaTime);
        }
    }

    public void UpdateHappinessAreas()
    {
        foreach (var b in placedBuildings)
        {
            b.ApplyHappinessEffect();
        }
    }

    public void NotifyNeighbors(Tile tile)
    {
        foreach (var neighbor in GetNeighborTiles(tile))
        {
            // pastikan neighbor dan currentObject valid
            if (neighbor != null && neighbor.currentObject != null)
            {
                var bld = neighbor.currentObject.GetComponent<Building>();
                if (bld != null)
                {
                    bld.CheckRoadConnection();
                    bld.CheckUtilitiesConnection();
                    bld.UpdateWarningMark();
                }
            }
        }
    }
}

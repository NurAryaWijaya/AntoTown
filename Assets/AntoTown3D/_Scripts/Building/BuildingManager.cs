using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Header("Prefabs & Grid")]
    public List<Building> buildingPrefabs;
    public List<Building> placedBuildings = new();
    public Tile[,] gridTiles;

    [HideInInspector]
    public GameObject currentObject = null;

    [Header("Zone Spawn")]
    public float zoneSpawnInterval = 10f;

    Queue<(Building prefab, Tile tile)> zoneSpawnQueue = new();
    float zoneSpawnTimer;

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
        StartCoroutine(UpgradeRoutine());
    }
    void Update()
{
    ProcessZoneSpawn(Time.deltaTime);
}

    IEnumerator UpgradeRoutine()
    {
        while (true)
        {
            UpgradeBuildings(Time.deltaTime);
            yield return null;
        }
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

        int gridWidth = gridTiles.GetLength(0);
        int gridHeight = gridTiles.GetLength(1);

        // cek semua tile yang akan ditempati berdasarkan size
        for (int x = 0; x < prefab.size.x; x++)
        {
            for (int z = 0; z < prefab.size.z; z++)
            {
                int nx = tile.gridPosition.x + x;
                int nz = tile.gridPosition.y + z;

                // out of bounds
                if (nx >= gridWidth || nz >= gridHeight) return false;

                // sudah occupied
                if (gridTiles[nx, nz].isOccupied) return false;
            }
        }

        return true;
    }

    public Building PlaceBuilding(Building prefab, Tile tile)
    {
        if (!CanPlaceBuilding(tile, prefab)) return null;

        GameObject obj = Instantiate(prefab.gameObject);

        // hitung posisi agar prefab muncul **center dari semua tile** untuk visual
        float offsetX = (prefab.size.x - 1) * 0.5f * gridManager.tileSize;
        float offsetZ = (prefab.size.z - 1) * 0.5f * gridManager.tileSize;
        Vector3 basePos = gridManager.SnapToTile(tile.transform.position);
        Vector3 worldPos = basePos + new Vector3(offsetX, 0, offsetZ);
        obj.transform.position = worldPos;


        // scale sesuai size
        obj.transform.localScale = prefab.size;

        Quaternion rot = GetRotationFacingRoad(prefab, tile, worldPos);
        obj.transform.rotation = rot;

        Building building = obj.GetComponent<Building>();

        for (int x = 0; x < prefab.size.x; x++)
        {
            for (int z = 0; z < prefab.size.z; z++)
            {
                int nx = tile.gridPosition.x + x;
                int nz = tile.gridPosition.y + z;

                Tile t = gridTiles[nx, nz];
                t.isOccupied = true;
                t.currentObject = obj;

                building.occupiedTiles.Add(t);
            }
        }

        building.OnPlace(tile, this);
        placedBuildings.Add(building);
        return building;
    }

    public void RecheckAllBuildings()
    {
        foreach (var b in placedBuildings)
        {
            b.CheckRoadConnection();
            b.CheckUtilitiesConnection();
            b.UpdateWarningMark();
        }
    }

    public List<Building> GetBuildingsInRadius(Building source)
    {
        List<Building> result = new();
        if (!source.hasArea || source.areaRadiusInTiles <= 0) return result;

        Vector2Int centerPos = source.placedTile.gridPosition;
        int r = source.areaRadiusInTiles;

        foreach (var b in placedBuildings)
        {
            if (b == source || b.placedTile == null) continue;

            Vector2Int p = b.placedTile.gridPosition;

            int dx = p.x - centerPos.x;
            int dz = p.y - centerPos.y;

            if (dx * dx + dz * dz <= r * r)
            {
                result.Add(b);
            }
        }

        return result;
    }

    public void UpgradeBuildings(float deltaTime)
    {
        foreach (var b in placedBuildings)
        {
            b.Upgrade(deltaTime);
        }
    }

    // Area
    public bool HasUtilityForTile(Tile tile)
    {
        foreach (var b in placedBuildings)
        {
            if (!b.hasArea) continue;

            Vector2Int center = b.placedTile.gridPosition;
            Vector2Int p = tile.gridPosition;

            int dx = p.x - center.x;
            int dz = p.y - center.y;

            if (dx * dx + dz * dz <= b.areaRadiusInTiles * b.areaRadiusInTiles)
            {
                if (b.buildingType == BuildingType.PowerPlant ||
                    b.buildingType == BuildingType.WaterSource)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void EnqueueZoneBuilding(Building prefab, Tile tile)
    {
        zoneSpawnQueue.Enqueue((prefab, tile));
    }

    void ProcessZoneSpawn(float deltaTime)
    {
        if (zoneSpawnQueue.Count == 0) return;

        zoneSpawnTimer += deltaTime;
        if (zoneSpawnTimer < zoneSpawnInterval) return;

        var (prefab, tile) = zoneSpawnQueue.Dequeue();
        if (tile == null || tile.isOccupied) return;

        // Hitung posisi center bangunan
        float offsetX = (prefab.size.x - 1) * 0.5f * gridManager.tileSize;
        float offsetZ = (prefab.size.z - 1) * 0.5f * gridManager.tileSize;
        Vector3 basePos = gridManager.SnapToTile(tile.transform.position);
        Vector3 centerPos = basePos + new Vector3(offsetX, 0, offsetZ);

        // Pasang bangunan
        Building building = PlaceBuilding(prefab, tile);

        // **Update rotasi ke jalan setelah bangunan terpasang**
        building.transform.rotation = GetRotationFacingRoad(prefab, tile, centerPos);

        // Update semua building agar neighbor road dikenali
        RecheckAllBuildings();

        // Reset timer
        zoneSpawnTimer = Random.Range(0f, 1f);
    }


    Quaternion GetRotationFacingRoad(Building prefab, Tile originTile, Vector3 centerPosition)
    {
        // Loop semua tile bangunan
        for (int x = 0; x < prefab.size.x; x++)
        {
            for (int z = 0; z < prefab.size.z; z++)
            {
                int bx = originTile.gridPosition.x + x;
                int bz = originTile.gridPosition.y + z;

                Tile buildingTile = gridTiles[bx, bz];

                // Cek 4 arah cardinal SAJA
                TryRotate(buildingTile, bx, bz, Vector2Int.up, Vector3.forward, out Quaternion rot);
                if (rot != Quaternion.identity) return rot;

                TryRotate(buildingTile, bx, bz, Vector2Int.down, Vector3.back, out rot);
                if (rot != Quaternion.identity) return rot;

                TryRotate(buildingTile, bx, bz, Vector2Int.right, Vector3.right, out rot);
                if (rot != Quaternion.identity) return rot;

                TryRotate(buildingTile, bx, bz, Vector2Int.left, Vector3.left, out rot);
                if (rot != Quaternion.identity) return rot;
            }
        }

        return Quaternion.identity;
    }

    bool TryRotate(
        Tile buildingTile,
        int bx,
        int bz,
        Vector2Int offset,
        Vector3 lookDir,
        out Quaternion rotation)
    {
        rotation = Quaternion.identity;

        int nx = bx + offset.x;
        int nz = bz + offset.y;

        if (nx < 0 || nz < 0 ||
            nx >= gridTiles.GetLength(0) ||
            nz >= gridTiles.GetLength(1))
            return false;

        Tile neighbor = gridTiles[nx, nz];

        if (neighbor.currentObject != null &&
            neighbor.currentObject.GetComponent<RoadTile>() != null)
        {
            rotation = Quaternion.LookRotation(lookDir);
            return true;
        }

        return false;
    }
}

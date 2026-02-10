using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string saveFilePath => Path.Combine(Application.persistentDataPath, "save.json");
    static bool IsUtility(BuildingType type)
    {
        return type == BuildingType.PowerPlant
            || type == BuildingType.WaterSource;
    }

    static bool IsFacility(BuildingType type)
    {
        return type == BuildingType.School
            || type == BuildingType.Hospital
            || type == BuildingType.FireStation
            || type == BuildingType.PoliceStation
            || type == BuildingType.Park;
    }

    static bool IsZoned(BuildingType type)
    {
        return type == BuildingType.Residential
            || type == BuildingType.Commercial
            || type == BuildingType.Industry;
    }

    public static void SaveGame()
    {
        GameData data = new GameData();

        // 1. Save money
        data.money = GameManager.Instance.money;

        // 2. Save buildings
        foreach (var b in BuildingManager.Instance.placedBuildings)
        {
            BuildingData bd = new BuildingData
            {
                buildingName = b.buildingName,
                prefabName = b.name.Replace("(Clone)", ""),
                anchorTile = b.anchorTile,
                size = b.size,
                buildingType = b.buildingType,
                level = b.level,
                canUpgrade = b.canUpgrade,
                zoneTier = b.zoneTier,
                upgradeTimer = b.upgradeTimer,
                currentPopulation = b.currentPopulation,
                incomePerTick = b.incomePerTick,
                connectedToRoad = b.connectedToRoad,
                connectedToPower = b.connectedToPower,
                connectedToWater = b.connectedToWater
            };
            data.buildings.Add(bd);
        }

        foreach (var r in Object.FindObjectsByType<RoadTile>(FindObjectsSortMode.None))
        {
            if (r.OwnerTile == null) continue;

            RoadData rd = new RoadData
            {
                position = r.OwnerTile.transform.position
            };
            data.roads.Add(rd);
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game Saved to " + saveFilePath);
    }

    public static void LoadGame(BuildingManager buildingManager, GridManager gridManager)
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("Save file not found!");
            return;
        }

        string json = File.ReadAllText(saveFilePath);
        GameData data = JsonUtility.FromJson<GameData>(json);

        if (gridManager.tiles == null)
        {
            Debug.LogError("Grid NOT READY when loading game!");
            return;
        }

        // 1. Reset current scene/buildings
        foreach (var b in Object.FindObjectsByType<Building>(FindObjectsSortMode.None))
            GameObject.Destroy(b.gameObject);

        foreach (var r in Object.FindObjectsByType<RoadTile>(FindObjectsSortMode.None))
            GameObject.Destroy(r.gameObject);


        // 4. Restore roads
        if (buildingManager.roadLogicPrefab == null)
        {
            Debug.LogError("RoadLogic prefab NOT assigned in BuildingManager!");
            return;
        }

        foreach (var rd in data.roads)
        {
            Tile tile = gridManager.GetTileAtPosition(rd.position);
            if (tile == null || tile.isOccupied) continue;

            Vector3 spawnPos = tile.transform.position + new Vector3(0, 0.05f, 0);

            RoadTile road = GameObject.Instantiate(
                buildingManager.roadLogicPrefab,
                spawnPos,
                Quaternion.identity
            );

            road.isLoading = true; // 🔴 PENTING
            road.OnPlaced(tile, gridManager);

            tile.currentObject = road.gameObject;
            tile.isOccupied = true;
        }

        foreach (var road in Object.FindObjectsByType<RoadTile>(FindObjectsSortMode.None))
        {
            road.isLoading = false;
            road.RefreshAfterLoad(gridManager);
        }

        // 3. Restore buildings
        // 1️⃣ Utility
        foreach (var bd in data.buildings)
        {
            if (IsUtility(bd.buildingType))
                SpawnBuilding(bd, buildingManager, gridManager);
        }

        // 2️⃣ Facility
        foreach (var bd in data.buildings)
        {
            if (IsFacility(bd.buildingType))
                SpawnBuilding(bd, buildingManager, gridManager);
        }

        // 3️⃣ Zoned (PALING AKHIR)
        foreach (var bd in data.buildings)
        {
            if (IsZoned(bd.buildingType))
                SpawnBuilding(bd, buildingManager, gridManager);
        }

        // 2. Restore money
        GameManager.Instance.money = data.money;
        GameManager.Instance.UpdateUI();

        Debug.Log("Game Loaded!");
    }

    static void SpawnBuilding( BuildingData bd, BuildingManager buildingManager, GridManager gridManager )
    {
        Building prefab = buildingManager.buildingPrefabs.Find(p =>
            p.buildingType == bd.buildingType &&
            p.zoneTier == bd.zoneTier &&
            p.level == bd.level
        );

        if (prefab == null)
        {
            Debug.LogWarning($"Prefab tidak ditemukan: {bd.buildingType}");
            return;
        }

        Vector2Int pos = bd.anchorTile;
        if (pos.x < 0 || pos.x >= gridManager.width || pos.y < 0 || pos.y >= gridManager.height)
            return;

        Tile anchorTile = gridManager.tiles[
            bd.anchorTile.x,
            bd.anchorTile.y
        ];

        if (anchorTile.isOccupied)
            return;

        Building b = buildingManager.PlaceBuilding(
            prefab,
            anchorTile,
            isLoading: true
        );

        if (b == null) return;

        b.level = bd.level;
        b.currentPopulation = bd.currentPopulation;
        b.upgradeTimer = bd.upgradeTimer;
    }

}


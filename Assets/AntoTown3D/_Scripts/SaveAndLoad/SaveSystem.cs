using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string saveFilePath => Path.Combine(Application.persistentDataPath, "save.json");

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
                position = b.transform.position,
                size = b.size,
                buildingType = b.buildingType,
                level = b.level,
                canUpgrade = b.canUpgrade,
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
        foreach (var bd in data.buildings)
        {
            Building prefab = buildingManager.buildingPrefabs.Find(p => p.name == bd.prefabName);
            if (prefab != null)
            {
                Building b = buildingManager.PlaceBuilding(prefab, gridManager.GetTileAtPosition(bd.position), isLoading: true);
                b.level = bd.level;
                b.currentPopulation = bd.currentPopulation;
                b.upgradeTimer = bd.upgradeTimer;
                // restore other fields kalau perlu
            }
        }

        // 2. Restore money
        GameManager.Instance.money = data.money;
        GameManager.Instance.UpdateUI();

        Debug.Log("Game Loaded!");
    }
}

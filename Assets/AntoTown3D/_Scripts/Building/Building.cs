using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BuildingType
{
    Residential, Commercial, Industry, PowerPlant, WaterSource, Park,
    School, Hospital, FireStation, PoliceStation
}

public enum BuildingLevel
{
    Poor, Middle, Rich
}

public class Building : MonoBehaviour, IPlaceable
{
    [Header("Basic Info")]
    public string buildingName;
    public int cost;
    public int population;
    public Vector3 size;
    public BuildingType buildingType;
    public BuildingLevel level = BuildingLevel.Poor;

    [Header("Area & Connection")]
    public bool hasArea = false;
    public int areaRadiusInTiles = 0;
    public bool connectedToRoad = false;
    public bool connectedToPower = false;
    public bool connectedToWater = false;
    public GameObject warningMark;

    [Header("Upgrade Settings")]
    public bool canUpgrade = false;
    public float upgradeTimer = 0f; // seconds
    public float poorToMiddleInterval = 300f; // 5 menit
    public float middleToRichInterval = 600f; // 10 menit
    MeshScaleAnimator meshAnimator;

    public float nextUpgradeTime = -1f;
    public bool facilityUnlocked = false;

    [Tooltip("Aktif jika canUpgrade = true")]
    public GameObject poorMesh;
    public GameObject middleMesh;
    public GameObject richMesh;

    [Header("Happiness")]
    public float happinessEffect = 0f;
    [Range(0, 100)]
    public float baseHappiness = 70f;
    [Header("Workforce")]
    public int jobCapacity = 0; // hanya dipakai untuk Commercial & Industry

    [Header("Happiness Penalty")]
    [HideInInspector] public bool happinessPenaltyApplied = false;
    [HideInInspector] public float happinessPenaltyValue = 30f;
    [Header("Facility Happiness Penalty")]
    public float facilityPenalty = -30f;

    [HideInInspector]
    public float currentHappiness = 70f;
    public float efficiency = 1f; // 0–1


    [Header("Economy & Population")]
    public int buildPrice = 100;        // Harga membangun
    public int populationCapacity = 0;  // Kapasitas penduduk (untuk residential)
    public int currentPopulation = 0;   // Populasi saat ini (misal bisa berkembang)
    public float incomePerTick = 0f;    // Pendapatan / pajak per tick atau per detik

    [HideInInspector] public Tile placedTile;
    [HideInInspector] public BuildingManager manager;
    [HideInInspector] public List<Tile> occupiedTiles = new();

    bool isUpgrading = false;


    public void OnPlace(Tile tile, BuildingManager buildingManager)
    {
        placedTile = tile;
        manager = buildingManager;

        GameManager.Instance.RegisterBuilding(this);
        CheckRoadConnection();
        CheckUtilitiesConnection();

        if (buildingType == BuildingType.Residential)
        {
            currentPopulation = populationCapacity;
        }

        meshAnimator = GetComponent<MeshScaleAnimator>();

        if (level != BuildingLevel.Middle && level != BuildingLevel.Rich)
        {
            level = BuildingLevel.Poor;
        }

        upgradeTimer = 0f;

        UpdateUpgradeVisual();

        if (meshAnimator != null)
        {
            StartCoroutine(meshAnimator.Show(GetCurrentMesh()));
        }

        UpdateWarningMark();
    }
    GameObject GetCurrentMesh()
    {
        return level switch
        {
            BuildingLevel.Poor => poorMesh,
            BuildingLevel.Middle => middleMesh,
            BuildingLevel.Rich => richMesh,
            _ => null
        };
    }


    public void CheckRoadConnection()
    {
        if (manager == null) return;

        connectedToRoad = false;

        foreach (var tile in occupiedTiles)
        {
            var neighbors = manager.GetNeighborTiles(tile);
            foreach (var neighbor in neighbors)
            {
                if (neighbor.currentObject != null &&
                    neighbor.currentObject.GetComponent<RoadTile>() != null)
                {
                    connectedToRoad = true;
                    return;
                }
            }
        }
    }

    public void CheckUtilitiesConnection()
    {
        // Non-residential tidak butuh utility
        if (buildingType != BuildingType.Residential &&
            buildingType != BuildingType.Commercial &&
            buildingType != BuildingType.Industry)
        {
            connectedToPower = true;
            connectedToWater = true;
            return;
        }

        connectedToPower = false;
        connectedToWater = false;

        foreach (var utility in manager.placedBuildings)
        {
            if (!utility.hasArea) continue;

            bool isPower = utility.buildingType == BuildingType.PowerPlant;
            bool isWater = utility.buildingType == BuildingType.WaterSource;

            if (!isPower && !isWater) continue;

            int r = utility.areaRadiusInTiles;
            Vector2 center = utility.GetCenterGridPosition();

            foreach (var tile in occupiedTiles)
            {
                Vector2Int p = tile.gridPosition;
                float dx = p.x - center.x;
                float dz = p.y - center.y;

                if (dx * dx + dz * dz <= r * r)
                {
                    if (isPower) connectedToPower = true;
                    if (isWater) connectedToWater = true;
                }
            }
        }
    }

    public Vector2 GetCenterGridPosition()
    {
        if (occupiedTiles == null || occupiedTiles.Count == 0)
            return new Vector2(
                placedTile.gridPosition.x,
                placedTile.gridPosition.y
            );

        int minX = int.MaxValue, minZ = int.MaxValue;
        int maxX = int.MinValue, maxZ = int.MinValue;

        foreach (var t in occupiedTiles)
        {
            minX = Mathf.Min(minX, t.gridPosition.x);
            minZ = Mathf.Min(minZ, t.gridPosition.y);
            maxX = Mathf.Max(maxX, t.gridPosition.x);
            maxZ = Mathf.Max(maxZ, t.gridPosition.y);
        }

        return new Vector2(
            (minX + maxX) * 0.5f,
            (minZ + maxZ) * 0.5f
        );
    }

    public void UpdateWarningMark()
    {
        if (warningMark == null) return;

        bool shouldShow = false;

        if (buildingType == BuildingType.Residential ||
            buildingType == BuildingType.Commercial ||
            buildingType == BuildingType.Industry)
        {
            shouldShow = !(connectedToRoad && connectedToPower && connectedToWater);
        }

        warningMark.SetActive(shouldShow);

        Debug.Log(
            $"{buildingName} | Road:{connectedToRoad} Power:{connectedToPower} Water:{connectedToWater} Warning:{shouldShow}"
        );
    }

    public void Upgrade()
    {
        if (!canUpgrade) return;
        if (isUpgrading) return;
        if (level == BuildingLevel.Rich) return;

        StartCoroutine(UpgradeSequence());
    }


    IEnumerator UpgradeSequence()
    {
        isUpgrading = true;
        upgradeTimer = 0f;

        GameObject oldMesh = GetCurrentMesh();

        // Fade OUT mesh lama
        if (meshAnimator != null && oldMesh != null)
            yield return meshAnimator.Hide(oldMesh);

        // Naik level
        if (level == BuildingLevel.Poor)
            level = BuildingLevel.Middle;
        else if (level == BuildingLevel.Middle)
            level = BuildingLevel.Rich;

        UpdateUpgradeVisual();

        GameObject newMesh = GetCurrentMesh();

        // Fade IN mesh baru
        if (meshAnimator != null && newMesh != null)
            yield return meshAnimator.Show(newMesh);

        //ApplyHappinessEffect();

        isUpgrading = false;

        Debug.Log($"{buildingName} upgraded to {level}");
    }

    public float CalculateHappiness(BuildingManager manager)
    {
        if (buildingType != BuildingType.Residential)
            return 0f;

        float result = baseHappiness;

        var nearby = manager.GetBuildingsInRadius(this);

        foreach (var b in nearby)
        {
            if (!b.hasArea) continue;

            result += b.buildingType switch
            {
                BuildingType.Park => 20f,
                BuildingType.Industry => -20f,
                _ => 0f
            };
        }

        return Mathf.Clamp(result, 0, 100);
    }


    public bool IsOperational()
    {
        if (buildingType == BuildingType.Residential ||
            buildingType == BuildingType.Commercial ||
            buildingType == BuildingType.Industry)
        {
            return connectedToRoad && connectedToPower && connectedToWater;
        }

        if (buildingType == BuildingType.School ||
            buildingType == BuildingType.Hospital ||
            buildingType == BuildingType.FireStation ||
            buildingType == BuildingType.PoliceStation)
        {
            return connectedToRoad;
        }

        return true;
    }

    public void OnPlaced(Tile tile, GridManager gridManager)
    {
        //var buildingManager = gridManager.buildingManager;
        //buildingManager.PlaceBuilding(this, tile);
    }

    public void OnRemoved(Tile tile, GridManager gridManager)
    {
        // hapus dari manager dll
    }

    void UpdateUpgradeVisual()
    {
        if (!canUpgrade) return;

        if (poorMesh != null) poorMesh.SetActive(level == BuildingLevel.Poor);
        if (middleMesh != null) middleMesh.SetActive(level == BuildingLevel.Middle);
        if (richMesh != null) richMesh.SetActive(level == BuildingLevel.Rich);
    }

    void OnValidate()
    {
        if (canUpgrade)
        {
            if (poorMesh == null || middleMesh == null || richMesh == null)
            {
                Debug.LogWarning($"{name} canUpgrade aktif tapi mesh belum lengkap!");
            }
        }
    }

    public void DestroyBuilding()
    {
        // 1. Unregister dari GameManager
        GameManager.Instance.UnregisterBuilding(this);

        // 2. Lepaskan tile
        foreach (var t in occupiedTiles)
        {
            t.isOccupied = false;
            t.currentObject = null;
        }
        occupiedTiles.Clear();

        // 3. Remove dari BuildingManager
        if (manager != null)
            manager.placedBuildings.Remove(this);

        // 4. Recheck neighbor
        if (manager != null)
            manager.RecheckAllBuildings();

        // 5. Visual destroy
        StartCoroutine(DestroySequence());
    }

    IEnumerator DestroySequence()
    {
        var mesh = GetCurrentMesh();
        if (meshAnimator != null && mesh != null)
            yield return meshAnimator.Hide(GetCurrentMesh());

        foreach (var t in occupiedTiles)
        {
            t.isOccupied = false;
            t.currentObject = null;
        }
        occupiedTiles.Clear();

        Destroy(gameObject);
    }

    public float GetCurrentUpgradeInterval()
    {
        return level switch
        {
            BuildingLevel.Poor => poorToMiddleInterval,
            BuildingLevel.Middle => middleToRichInterval,
            BuildingLevel.Rich => 0f, // sudah maksimal
            _ => 0f
        };
    }

    public bool HasRequiredFacilities(BuildingManager manager)
    {
        if (buildingType == BuildingType.Residential)
        {
            if (level == BuildingLevel.Poor) return true;

            return manager.IsInFacilityArea(this, BuildingType.School) &&
                   manager.IsInFacilityArea(this, BuildingType.Hospital);
        }

        if (buildingType == BuildingType.Commercial ||
            buildingType == BuildingType.Industry)
        {
            if (level == BuildingLevel.Poor) return true;

            return manager.IsInFacilityArea(this, BuildingType.PoliceStation) &&
                   manager.IsInFacilityArea(this, BuildingType.FireStation);
        }

        return true;
    }

}

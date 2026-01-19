using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BuildingType
{
    Residential, Commercial, Industry, PowerPlant, WaterSource, Park
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
    public bool connectedToRoad = false;
    public bool connectedToUtilities = false;
    public GameObject warningMark;

    [Header("Happiness & Upgrade")]
    public float happinessEffect = 0f;
    public float upgradeTimer = 0f; // seconds
    public float upgradeInterval = 300f; // default 5 minutes

    [HideInInspector] public Tile placedTile;
    [HideInInspector] public BuildingManager manager;

    void Start()
    {
        //if (warningMark != null) warningMark.SetActive(false);
    }

    public void OnPlace(Tile tile, BuildingManager buildingManager)
    {
        placedTile = tile;
        manager = buildingManager;

        CheckRoadConnection();
        CheckUtilitiesConnection();
        ApplyHappinessEffect();
        upgradeTimer = 0f;
        UpdateWarningMark();
    }

    public void CheckRoadConnection()
    {
        if (placedTile == null || manager == null) return;

        connectedToRoad = false;

        var neighbors = manager.GetNeighborTiles(placedTile);
        foreach (var neighbor in neighbors)
        {
            if (neighbor.currentObject != null)
            {
                var road = neighbor.currentObject.GetComponent<RoadTile>();
                if (road != null)
                {
                    connectedToRoad = true;
                    break;
                }
            }
        }

        //UpdateWarningMark();

#if UNITY_EDITOR
        Debug.Log($"{buildingName} connectedToRoad: {connectedToRoad}");
#endif
    }


    public void CheckUtilitiesConnection()
    {
        if (buildingType == BuildingType.Residential ||
            buildingType == BuildingType.Commercial ||
            buildingType == BuildingType.Industry)
        {
            connectedToUtilities = false;

            foreach (var neighbor in manager.GetNeighborTiles(placedTile))
            {
                if (neighbor.currentObject != null)
                {
                    var bld = neighbor.currentObject.GetComponent<Building>();
                    if (bld != null && (bld.buildingType == BuildingType.PowerPlant || bld.buildingType == BuildingType.WaterSource))
                    {
                        connectedToUtilities = true;
                        break;
                    }
                }
            }

            //UpdateWarningMark();
        }
        else
        {
            connectedToUtilities = true; // park, power, water tidak perlu koneksi
        }
    }

    public void UpdateWarningMark()
    {
        if (warningMark != null)
        {
            bool shouldShow = false;

            if ((buildingType == BuildingType.Residential ||
                 buildingType == BuildingType.Commercial ||
                 buildingType == BuildingType.Industry))
            {
                shouldShow = !(connectedToRoad && connectedToUtilities);
            }

            warningMark.SetActive(shouldShow);

            Debug.Log($"{buildingName} - connectedToRoad: {connectedToRoad}, connectedToUtilities: {connectedToUtilities}, shouldShowMarker: {shouldShow}");
        }
        if (warningMark != null)
        {
            bool shouldShow = !(connectedToRoad && connectedToUtilities);
            warningMark.SetActive(shouldShow);

            Debug.Log($"{buildingName} Marker active: {warningMark.activeSelf}, shouldShow: {shouldShow}, position: {warningMark.transform.position}");
        }

    }

    public void Upgrade(float deltaTime)
    {
        if (!IsOperational()) return;

        upgradeTimer += deltaTime;

        // pengaruh kebahagiaan area mempercepat upgrade
        float happinessMultiplier = 1f + (manager.CalculateHappinessForBuilding(this) / 100f);
        if (upgradeTimer >= upgradeInterval / happinessMultiplier)
        {
            if (level == BuildingLevel.Poor) level = BuildingLevel.Middle;
            else if (level == BuildingLevel.Middle) level = BuildingLevel.Rich;

            upgradeTimer = 0f;
            ApplyHappinessEffect();
        }
    }

    public void ApplyHappinessEffect()
    {
        if (!hasArea) return;

        var nearbyBuildings = manager.GetBuildingsInArea(transform.position, size.x * 2f);

        foreach (var bld in nearbyBuildings)
        {
            if (bld.buildingType == BuildingType.Residential || bld.buildingType == BuildingType.Commercial)
            {
                float effect = 0f;

                switch (buildingType)
                {
                    case BuildingType.Park: effect = 10f; break;
                    case BuildingType.Industry: effect = -5f; break;
                    default: effect = 0f; break;
                }

                bld.happinessEffect += effect;
            }
        }
    }

    public bool IsOperational()
    {
        if ((buildingType == BuildingType.Residential ||
            buildingType == BuildingType.Commercial ||
            buildingType == BuildingType.Industry))
        {
            return connectedToRoad && connectedToUtilities;
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
}

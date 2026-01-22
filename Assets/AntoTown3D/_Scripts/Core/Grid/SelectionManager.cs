using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlacementMode
{
    None,
    Road,
    ZoneResidential,
    ZoneCommercial,
    ZoneIndustry
}

public class SelectionManager : MonoBehaviour
{
    public GridManager gridManager;
    public BuildingManager buildingManager;
    public GameObject buildingPrefab;
    public GameObject roadPrefab;

    // Area
    public PlacementMode currentMode;
    bool isDraggingZone;
    Tile zoneStartTile;
    List<Tile> zonePreviewTiles = new();

    [Header("Building Buttons")]
    public UnityEngine.UI.Button residentialButton;
    public UnityEngine.UI.Button commercialButton;
    public UnityEngine.UI.Button industryButton;
    public UnityEngine.UI.Button parkButton;
    public UnityEngine.UI.Button powerPlantButton;
    public UnityEngine.UI.Button waterSourceButton;

    // Prefab references
    public Building residentialPrefab;
    public Building commercialPrefab;
    public Building industryPrefab;
    public Building parkPrefab;
    public Building powerPlantPrefab;
    public Building waterSourcePrefab;

    bool isDraggingRoad;
    Tile startTile;
    Tile lastPlacedTile;
    AxisLock lockedAxis = AxisLock.None;

    HashSet<Tile> placedTiles = new();

    // Button toggle road
    public UnityEngine.UI.Button roadButton;
    public Color roadActiveColor = Color.green;
    public Color roadInactiveColor = Color.white;


    void Update()
    {
        bool isZoneMode =
    currentMode == PlacementMode.ZoneResidential ||
    currentMode == PlacementMode.ZoneCommercial ||
    currentMode == PlacementMode.ZoneIndustry;

        if (!isZoneMode)
        {
            if (!gridManager.isPlacing || gridManager.previewObject == null ||
                (gridManager.currentPlacementType == PlacementType.Road && !gridManager.roadModeActive))
                return;
        }

        if (!isZoneMode)
        {
            gridManager.MovePreviewToMouse();
        }

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, LayerMask.GetMask("Tile")))
            return;

        Tile tile = hit.collider.GetComponent<Tile>();
        if (tile == null) return;

        // Ini menyembunyikan area bangunan
        if (currentMode == PlacementMode.None &&
    Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (hit.collider.GetComponent<Building>() == null)
            {
                gridManager.ClearArea();
            }
        }


        if (!isZoneMode && gridManager.previewObject != null)
        {
            gridManager.previewObject.transform.position =
                gridManager.SnapToTile(tile.transform.position);
        }

        // ROAD MODE
        if (gridManager.currentPlacementType == PlacementType.Road)
        {
            // FORCE STOP (ANTI BUG)
            if (isDraggingRoad && !Mouse.current.leftButton.isPressed)
            {
                isDraggingRoad = false;
                lockedAxis = AxisLock.None;
                return;
            }

            // START DRAG
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                isDraggingRoad = true;
                startTile = tile;
                lastPlacedTile = tile;
                lockedAxis = AxisLock.None;
                placedTiles.Clear();

                TryPlaceRoad(tile);
            }

            // DRAGGING
            if (isDraggingRoad)
            {
                Tile axisLockedTile = GetAxisLockedTile(startTile, tile);

                if (axisLockedTile != null &&
                    axisLockedTile != lastPlacedTile &&
                    !placedTiles.Contains(axisLockedTile))
                {
                    TryPlaceRoad(axisLockedTile);
                    lastPlacedTile = axisLockedTile;
                }
            }
        }

        // ZONE MODE
        if (currentMode == PlacementMode.ZoneResidential ||
            currentMode == PlacementMode.ZoneCommercial ||
            currentMode == PlacementMode.ZoneIndustry)
        {
            HandleZonePlacement(tile);
            return;
        }

        // BUILDING MODE
        if (gridManager.currentPlacementType == PlacementType.Building && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Building buildingPrefab = gridManager.currentPrefab.GetComponent<Building>();
            if (buildingPrefab != null)
            {
                buildingManager.PlaceBuilding(buildingPrefab, tile);
                gridManager.ClearHighlightedTiles();
                // Panggil notifikasi jika bangunan adalah PowerPlant / WaterSource
                if (buildingPrefab.buildingType == BuildingType.PowerPlant || buildingPrefab.buildingType == BuildingType.WaterSource)
                {
                    buildingManager.RecheckAllBuildings();
                }

                // Hentikan placing mode
                gridManager.isPlacing = false;
                if (gridManager.previewObject != null)
                    Destroy(gridManager.previewObject);
            }
        }

    }

    // Road 
    public void ToggleRoadMode()
    {
        currentMode = PlacementMode.None;
        ClearZonePreview();

        gridManager.roadModeActive = !gridManager.roadModeActive;

        var colors = roadButton.colors;

        if (gridManager.roadModeActive)
        {
            gridManager.currentPlacementType = PlacementType.Road;
            gridManager.ShowPreview(roadPrefab);

            colors.normalColor = roadActiveColor;
            colors.highlightedColor = roadActiveColor;
            colors.selectedColor = roadActiveColor;
        }
        else
        {
            gridManager.currentPlacementType = PlacementType.None;
            gridManager.isPlacing = false;

            if (gridManager.previewObject != null)
                Destroy(gridManager.previewObject);

            colors.normalColor = roadInactiveColor;
            colors.highlightedColor = roadInactiveColor;
            colors.selectedColor = roadInactiveColor;

            // SAFETY STOP
            isDraggingRoad = false;
            lockedAxis = AxisLock.None;
            placedTiles.Clear();
        }

        roadButton.colors = colors;
    }


    void TryPlaceRoad(Tile tile)
    {
        if (gridManager.CanPlaceObject(tile))
        {
            gridManager.PlaceObject(tile);
            placedTiles.Add(tile);

            buildingManager.RecheckAllBuildings();
        }
    }

    Tile GetAxisLockedTile(Tile start, Tile current)
    {
        Vector2Int s = start.gridPosition;
        Vector2Int c = current.gridPosition;

        // Tentukan axis sekali
        if (lockedAxis == AxisLock.None)
        {
            int dx = Mathf.Abs(c.x - s.x);
            int dz = Mathf.Abs(c.y - s.y);

            if (dx > dz)
                lockedAxis = AxisLock.Horizontal;
            else if (dz > dx)
                lockedAxis = AxisLock.Vertical;
            else
                return start;
        }

        Vector2Int lockedPos = s;

        if (lockedAxis == AxisLock.Horizontal)
            lockedPos.x = c.x;
        else if (lockedAxis == AxisLock.Vertical)
            lockedPos.y = c.y;

        return gridManager.tiles[lockedPos.x, lockedPos.y];
    }

    public void SelectBuilding(Building prefab)
    {
        // ❗ KELUAR DARI ZONE MODE
        currentMode = PlacementMode.None;
        ClearZonePreview();
        // Turn off road mode
        gridManager.roadModeActive = false;

        // Reset warna tombol road ke default/nonaktif
        var colors = roadButton.colors;
        colors.normalColor = roadInactiveColor;
        colors.highlightedColor = roadInactiveColor;
        colors.selectedColor = roadInactiveColor;
        roadButton.colors = colors;

        // Safety reset
        isDraggingRoad = false;
        lockedAxis = AxisLock.None;

        gridManager.currentPlacementType = PlacementType.Building;
        gridManager.ShowPreview(prefab.gameObject);
    }

    public void SelectResidential() => SelectBuilding(residentialPrefab);
    public void SelectCommercial() => SelectBuilding(commercialPrefab);
    public void SelectIndustry() => SelectBuilding(industryPrefab);
    public void SelectPark() => SelectBuilding(parkPrefab);
    public void SelectPowerPlant() => SelectBuilding(powerPlantPrefab);
    public void SelectWaterSource() => SelectBuilding(waterSourcePrefab);

    // Zone Area
    class ZoneSelection
    {
        public Tile start;
        public Tile end;
        public List<Tile> tiles = new();
    }

    void HandleZonePlacement(Tile tile)
    {
        // START DRAG
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isDraggingZone = true;
            zoneStartTile = tile;
            zonePreviewTiles.Clear();
        }

        // DRAG
        if (isDraggingZone && Mouse.current.leftButton.isPressed)
        {
            UpdateZonePreview(zoneStartTile, tile);
        }

        // RELEASE
        if (isDraggingZone && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            ConfirmZonePlacement();
            ClearZonePreview();
            isDraggingZone = false;
        }
    }

    void UpdateZonePreview(Tile start, Tile current)
    {
        ClearZonePreview();

        int minX = Mathf.Min(start.gridPosition.x, current.gridPosition.x);
        int maxX = Mathf.Max(start.gridPosition.x, current.gridPosition.x);
        int minZ = Mathf.Min(start.gridPosition.y, current.gridPosition.y);
        int maxZ = Mathf.Max(start.gridPosition.y, current.gridPosition.y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Tile t = gridManager.tiles[x, z];
                zonePreviewTiles.Add(t);

                bool valid = CanZoneTileBuild(t);
                t.SetPreview(valid); // hijau / merah
            }
        }
    }

    bool CanZoneTileBuild(Tile tile)
    {
        if (tile.isOccupied) return false;
        if (!HasRoadAccess(tile)) return false;
        if (!HasUtilityCoverage(tile)) return false;

        Building prefab = GetZonePrefab();
        return buildingManager.CanPlaceBuilding(tile, prefab);
    }

    bool HasRoadAccess(Tile tile)
    {
        foreach (var n in gridManager.GetNeighbors(tile))
        {
            if (n.currentObject == null) continue;

            if (n.currentObject.GetComponent<RoadTile>() != null)
                return true;
        }
        return false;
    }

    bool HasUtilityCoverage(Tile tile)
    {
        return buildingManager.HasUtilityForTile(tile);
    }

    void ConfirmZonePlacement()
    {
        Building prefab = GetZonePrefab();

        foreach (var tile in zonePreviewTiles)
        {
            if (!CanZoneTileBuild(tile)) continue;

            buildingManager.EnqueueZoneBuilding(prefab, tile);
        }

        currentMode = PlacementMode.None;
    }


    Building GetZonePrefab()
    {
        return currentMode switch
        {
            PlacementMode.ZoneResidential => residentialPrefab,
            PlacementMode.ZoneCommercial => commercialPrefab,
            PlacementMode.ZoneIndustry => industryPrefab,
            _ => null
        };
    }

    void ClearZonePreview()
    {
        foreach (var t in zonePreviewTiles)
            t.ClearPreview();

        zonePreviewTiles.Clear();
    }

    public void SelectResidentialZone()
    {
        currentMode = PlacementMode.ZoneResidential;

        gridManager.roadModeActive = false;
        gridManager.currentPlacementType = PlacementType.None;

        if (gridManager.previewObject != null)
            Destroy(gridManager.previewObject);
    }


    public void ExitZoneMode()
    {
        currentMode = PlacementMode.None;
        ClearZonePreview();
    }

    public void SelectCommercialZone()
    {
        currentMode = PlacementMode.ZoneCommercial;
        gridManager.roadModeActive = false;
        gridManager.currentPlacementType = PlacementType.None;

        if (gridManager.previewObject != null)
            Destroy(gridManager.previewObject);
    }

    public void SelectIndustryZone()
    {
        currentMode = PlacementMode.ZoneIndustry;
        gridManager.roadModeActive = false;
        gridManager.currentPlacementType = PlacementType.None;

        if (gridManager.previewObject != null)
            Destroy(gridManager.previewObject);
    }
}

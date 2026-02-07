using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public enum PlacementMode
{
    None,
    Road,
    LowResidentialZone,
    HighResidentialZone,
    LowCommercialZone,
    HighCommercialZone,
}


public class SelectionManager : MonoBehaviour
{
    public GridManager gridManager;
    public BuildingManager buildingManager;
    public GameObject buildingPrefab;
    public GameObject roadPrefab;
    public UIManager uiManager;


    // Area
    public PlacementMode currentMode;
    bool isDraggingZone;
    Tile zoneStartTile;
    List<Tile> zonePreviewTiles = new();


    [Header("Area")]
    public Building lowResidential;
    public Building highResidential;
    public Building lowCommercial;
    public Building highCommercial;

    [Header("=== MANUAL : LOW RESIDENTIAL ===")]
    public Building mediumHouse1;
    public Building mediumHouse2;
    public Building mediumHouse3;
    public Building mediumHouse4;

    public Building poorHouse1;
    public Building poorHouse2;

    public Building richHouse1;
    public Building richHouse2;
    public Building richHouse3;
    public Building richHouse4;


    [Header("=== MANUAL : HIGH RESIDENTIAL ===")]
    public Building highResidential1;
    public Building highResidential2;
    public Building highResidential3;

    public Building lowResidential1;
    public Building mediumResidential1;
    public Building mediumResidential2;
    public Building mediumResidential3;


    [Header("=== MANUAL : LOW COMMERCIAL ===")]
    public Building mediumCommercial1;
    public Building mediumCommercial2;
    public Building mediumCommercial3;

    public Building poorCommercial1;
    public Building poorCommercial2;
    public Building poorCommercial3;

    public Building richCommercial1;
    public Building richCommercial2;
    public Building richCommercial3;


    [Header("=== MANUAL : HIGH COMMERCIAL ===")]
    public Building highCommercial1;
    public Building highCommercial2;
    public Building highCommercial3;

    public Building highMediumCommercial1;
    public Building highMediumCommercial2;
    public Building highMediumCommercial3;

    public Building lowCommercial1;
    public Building lowCommercial2;
    public Building lowCommercial3;


    [Header("=== MANUAL : ENERGY ===")]
    public Building waterSource;
    public Building substation;
    public Building plta;
    public Building pltu;


    [Header("=== MANUAL : INDUSTRY ===")]
    public Building industry1;
    public Building industry2;
    public Building industry3;
    public Building industry4;

    public Building bigIndustry1;
    public Building bigIndustry2;
    public Building bigIndustry3;


    [Header("=== MANUAL : PARK ===")]
    public Building park1;
    public Building park2;
    public Building park3;
    public Building park4;
    public Building park5;
    public Building park6;

    [Header("=== MANUAL : FACILITIES ===")]
    public Building school;
    public Building hospital;
    public Building fireStation;
    public Building policeStation;

    bool isDraggingRoad;
    Tile startTile;
    Tile lastPlacedTile;
    AxisLock lockedAxis = AxisLock.None;

    HashSet<Tile> placedTiles = new();

    // Button toggle road
    public UnityEngine.UI.Button roadButton;
    public Color roadActiveColor = Color.green;
    public Color roadInactiveColor = Color.white;

    [Header("Bulldozer Button")]
    public UnityEngine.UI.Button bulldozerButton;
    public Color bulldozerActiveColor = Color.red;
    public Color bulldozerInactiveColor = Color.white;


    void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        bool isZoneMode =
    currentMode == PlacementMode.LowResidentialZone ||
    currentMode == PlacementMode.HighResidentialZone ||
    currentMode == PlacementMode.LowCommercialZone || 
    currentMode == PlacementMode.HighCommercialZone;

        bool isDeleteMode = gridManager.currentPlacementType == PlacementType.Delete;

        if (!isZoneMode && !isDeleteMode)
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
        if (tile == null)
            tile = hit.collider.GetComponentInParent<Tile>();

        if (tile == null) return;


        // ================= DELETE MODE =================
        if (gridManager.currentPlacementType == PlacementType.Delete)
        {
            HandleDeleteMode(tile);
            return; // ⛔ stop logic lain
        }

        // Ini menyembunyikan area bangunan
        if (currentMode == PlacementMode.None &&
    gridManager.currentPlacementType != PlacementType.Delete &&
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
        if (currentMode == PlacementMode.LowResidentialZone ||
    currentMode == PlacementMode.HighResidentialZone ||
    currentMode == PlacementMode.LowCommercialZone ||
    currentMode == PlacementMode.HighCommercialZone)
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
        bool newState = !gridManager.roadModeActive;
        SetRoadMode(newState);
    }

    public void SetRoadMode(bool active)
    {
        currentMode = PlacementMode.None;
        ClearZonePreview();

        gridManager.roadModeActive = active;

        // MATIKAN BULLDOSER kalau Road aktif
        gridManager.currentPlacementType = active ? PlacementType.Road : PlacementType.None;
        gridManager.isPlacing = false;
        currentMode = PlacementMode.None;
        isDraggingRoad = false;
        lockedAxis = AxisLock.None;

        if (gridManager.previewObject != null)
            Destroy(gridManager.previewObject);

        if (active)
            gridManager.ShowPreview(roadPrefab);

        // Update tombol Road
        var colors = roadButton.colors;
        colors.normalColor = active ? roadActiveColor : roadInactiveColor;
        colors.highlightedColor = active ? roadActiveColor : roadInactiveColor;
        colors.selectedColor = active ? roadActiveColor : roadInactiveColor;
        roadButton.colors = colors;

        // Update tombol Bulldozer
        colors = bulldozerButton.colors;
        colors.normalColor = gridManager.currentPlacementType == PlacementType.Delete ? bulldozerActiveColor : bulldozerInactiveColor;
        colors.highlightedColor = gridManager.currentPlacementType == PlacementType.Delete ? bulldozerActiveColor : bulldozerInactiveColor;
        colors.selectedColor = gridManager.currentPlacementType == PlacementType.Delete ? bulldozerActiveColor : bulldozerInactiveColor;
        bulldozerButton.colors = colors;
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

        if (uiManager != null)
            uiManager.CloseBuildPanel();

        gridManager.currentPlacementType = PlacementType.Building;
        gridManager.ShowPreview(prefab.gameObject);
    }

    // Delete
    public void ToggleBulldozerMode()
    {
        bool newState = gridManager.currentPlacementType != PlacementType.Delete;
        SetBulldozerMode(newState);
    }

    public void SetBulldozerMode(bool active)
    {
        gridManager.currentPlacementType = active ? PlacementType.Delete : PlacementType.None;

        gridManager.isPlacing = false;
        currentMode = PlacementMode.None;
        isDraggingRoad = false;
        lockedAxis = AxisLock.None;

        if (gridManager.previewObject != null)
        {
            Destroy(gridManager.previewObject);
            gridManager.previewObject = null;
        }

        // MATIKAN ROAD kalau Bulldozer aktif
        gridManager.roadModeActive = !active ? gridManager.roadModeActive : false;

        // Update tombol
        var colors = bulldozerButton.colors;
        colors.normalColor = active ? bulldozerActiveColor : bulldozerInactiveColor;
        colors.highlightedColor = active ? bulldozerActiveColor : bulldozerInactiveColor;
        colors.selectedColor = active ? bulldozerActiveColor : bulldozerInactiveColor;
        bulldozerButton.colors = colors;

        // Update road button warna
        colors = roadButton.colors;
        colors.normalColor = gridManager.roadModeActive ? roadActiveColor : roadInactiveColor;
        colors.highlightedColor = gridManager.roadModeActive ? roadActiveColor : roadInactiveColor;
        colors.selectedColor = gridManager.roadModeActive ? roadActiveColor : roadInactiveColor;
        roadButton.colors = colors;
    }

    public void OnDeleteModeClicked()
    {
        gridManager.currentPlacementType = PlacementType.Delete;
        gridManager.isPlacing = false;

        gridManager.ClearHighlightedTiles();
        gridManager.ClearArea();

        if (gridManager.previewObject != null)
            Destroy(gridManager.previewObject);

        Debug.Log("DELETE MODE ACTIVE");
    }

    void HandleDeleteMode(Tile tile)
    {
        gridManager.ClearHighlightedTiles();

        if (tile == null || tile.currentObject == null)
            return;

        // 🔴 highlight merah
        tile.SetColor(Color.red);
        gridManager.RegisterHighlightedTile(tile);

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        GameObject target = tile.currentObject;

        // ===== BUILDING =====
        Building building = target.GetComponent<Building>();
        if (building != null)
        {
            building.DestroyBuilding();
            return;
        }

        // ===== ROAD =====
        RoadTile road = target.GetComponent<RoadTile>();
        if (road != null)
        {
            road.OnRemoved(tile, gridManager); // update neighbors
            Destroy(road.gameObject);          // 🔥 HANCURKAN ROAD
            tile.currentObject = null;
            tile.isOccupied = false;
            return;
        }

        // ===== PLACEABLE LAIN =====
        IPlaceable placeable = target.GetComponent<IPlaceable>();
        if (placeable != null)
        {
            placeable.OnRemoved(tile, gridManager);
        }

        Destroy(target);
        tile.currentObject = null;
        tile.isOccupied = false;
    }

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
            PlacementMode.LowResidentialZone => lowResidential,
            PlacementMode.HighResidentialZone => highResidential,
            PlacementMode.LowCommercialZone => lowCommercial,
            PlacementMode.HighCommercialZone => highCommercial,
            _ => null
        };
    }

    void ClearZonePreview()
    {
        foreach (var t in zonePreviewTiles)
            t.ClearPreview();

        zonePreviewTiles.Clear();
    }

    public void SelectLowResidentialZone()
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

        if (uiManager != null)
            uiManager.CloseBuildPanel();

        currentMode = PlacementMode.LowResidentialZone;

        gridManager.roadModeActive = false;
        gridManager.currentPlacementType = PlacementType.None;

        if (gridManager.previewObject != null)
            Destroy(gridManager.previewObject);
    }

    public void SelectHighResidentialZone()
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

        if (uiManager != null)
            uiManager.CloseBuildPanel();

        currentMode = PlacementMode.HighResidentialZone;

        gridManager.roadModeActive = false;
        gridManager.currentPlacementType = PlacementType.None;

        if (gridManager.previewObject != null)
            Destroy(gridManager.previewObject);
    }
    public void SelectLowCommercial()
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

        if (uiManager != null)
            uiManager.CloseBuildPanel();

        currentMode = PlacementMode.LowCommercialZone;

        gridManager.roadModeActive = false;
        gridManager.currentPlacementType = PlacementType.None;

        if (gridManager.previewObject != null)
            Destroy(gridManager.previewObject);
    }
    public void SelectHighCommercial()
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

        if (uiManager != null)
            uiManager.CloseBuildPanel();

        currentMode = PlacementMode.HighCommercialZone;

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

    // Button
    public void SelectMediumHouse1() => SelectBuilding(mediumHouse1);
    public void SelectMediumHouse2() => SelectBuilding(mediumHouse2);
    public void SelectMediumHouse3() => SelectBuilding(mediumHouse3);
    public void SelectMediumHouse4() => SelectBuilding(mediumHouse4);

    public void SelectPoorHouse1() => SelectBuilding(poorHouse1);
    public void SelectPoorHouse2() => SelectBuilding(poorHouse2);

    public void SelectRichHouse1() => SelectBuilding(richHouse1);
    public void SelectRichHouse2() => SelectBuilding(richHouse2);
    public void SelectRichHouse3() => SelectBuilding(richHouse3);
    public void SelectRichHouse4() => SelectBuilding(richHouse4);


    public void SelectHighResidential1() => SelectBuilding(highResidential1);
    public void SelectHighResidential2() => SelectBuilding(highResidential2);
    public void SelectHighResidential3() => SelectBuilding(highResidential3);

    public void SelectLowResidential1() => SelectBuilding(lowResidential1);

    public void SelectMediumResidential1() => SelectBuilding(mediumResidential1);
    public void SelectMediumResidential2() => SelectBuilding(mediumResidential2);
    public void SelectMediumResidential3() => SelectBuilding(mediumResidential3);


    public void SelectMediumCommercial1() => SelectBuilding(mediumCommercial1);
    public void SelectMediumCommercial2() => SelectBuilding(mediumCommercial2);
    public void SelectMediumCommercial3() => SelectBuilding(mediumCommercial3);

    public void SelectPoorCommercial1() => SelectBuilding(poorCommercial1);
    public void SelectPoorCommercial2() => SelectBuilding(poorCommercial2);
    public void SelectPoorCommercial3() => SelectBuilding(poorCommercial3);

    public void SelectRichCommercial1() => SelectBuilding(richCommercial1);
    public void SelectRichCommercial2() => SelectBuilding(richCommercial2);
    public void SelectRichCommercial3() => SelectBuilding(richCommercial3);


    public void SelectHighCommercial1() => SelectBuilding(highCommercial1);
    public void SelectHighCommercial2() => SelectBuilding(highCommercial2);
    public void SelectHighCommercial3() => SelectBuilding(highCommercial3);

    public void SelectHighMediumCommercial1() => SelectBuilding(highMediumCommercial1);
    public void SelectHighMediumCommercial2() => SelectBuilding(highMediumCommercial2);
    public void SelectHighMediumCommercial3() => SelectBuilding(highMediumCommercial3);

    public void SelectLowCommercial1() => SelectBuilding(lowCommercial1);
    public void SelectLowCommercial2() => SelectBuilding(lowCommercial2);
    public void SelectLowCommercial3() => SelectBuilding(lowCommercial3);


    public void SelectWaterSource() => SelectBuilding(waterSource);
    public void SelectSubstation() => SelectBuilding(substation);
    public void SelectPLTA() => SelectBuilding(plta);
    public void SelectPLTU() => SelectBuilding(pltu);


    public void SelectIndustry1() => SelectBuilding(industry1);
    public void SelectIndustry2() => SelectBuilding(industry2);
    public void SelectIndustry3() => SelectBuilding(industry3);
    public void SelectIndustry4() => SelectBuilding(industry4);

    public void SelectBigIndustry1() => SelectBuilding(bigIndustry1);
    public void SelectBigIndustry2() => SelectBuilding(bigIndustry2);
    public void SelectBigIndustry3() => SelectBuilding(bigIndustry3);


    public void SelectPark1() => SelectBuilding(park1);
    public void SelectPark2() => SelectBuilding(park2);
    public void SelectPark3() => SelectBuilding(park3);
    public void SelectPark4() => SelectBuilding(park4);
    public void SelectPark5() => SelectBuilding(park5);
    public void SelectPark6() => SelectBuilding(park6);

    // Facilities Buttons
    public void SelectSchool() => SelectBuilding(school);
    public void SelectHospital() => SelectBuilding(hospital);
    public void SelectFireStation() => SelectBuilding(fireStation);
    public void SelectPoliceStation() => SelectBuilding(policeStation);

}

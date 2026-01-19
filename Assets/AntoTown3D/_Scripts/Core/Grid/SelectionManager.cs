using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionManager : MonoBehaviour
{
    public GridManager gridManager;
    public BuildingManager buildingManager;
    public GameObject buildingPrefab;
    public GameObject roadPrefab;

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
        if (!gridManager.isPlacing || gridManager.previewObject == null || 
            (gridManager.currentPlacementType == PlacementType.Road && !gridManager.roadModeActive))
            return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, LayerMask.GetMask("Ground")))
            return;

        Tile tile = gridManager.GetTileAtPosition(hit.point);
        if (tile == null) return;

        gridManager.previewObject.transform.position =
            gridManager.SnapToTile(tile.transform.position);

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

        // BUILDING MODE
        if (gridManager.currentPlacementType == PlacementType.Building && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Building buildingPrefab = gridManager.currentPrefab.GetComponent<Building>();
            if (buildingPrefab != null)
            {
                buildingManager.PlaceBuilding(buildingPrefab, tile);

                // Panggil notifikasi jika bangunan adalah PowerPlant / WaterSource
                if (buildingPrefab.buildingType == BuildingType.PowerPlant ||
                    buildingPrefab.buildingType == BuildingType.WaterSource)
                {
                    buildingManager.NotifyNeighbors(tile);
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
            gridManager.buildingManager.NotifyNeighbors(tile);
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
        // Turn off road mode
        gridManager.roadModeActive = false;
        roadButton.image.color = roadInactiveColor;

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

}

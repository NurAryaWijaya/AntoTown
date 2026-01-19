using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 20;
    public int height = 20;
    public float tileSize = 1f;
    public GameObject tilePrefab;

    [Header("References")]
    public BuildingManager buildingManager;
    public RoadTile roadManager;
    public UIManager uiManager;
    public CameraController cameraController;
    public PlacementType currentPlacementType;


    [Header("Placement")]
    public GameObject previewObject;

    public GameObject currentPrefab;
    public bool isPlacing = false;
    public Vector3 placementOffset = new Vector3(0, 0.05f, 0); // sedikit di atas tile

    [HideInInspector]
    public Tile[,] tiles;
    public bool roadModeActive = false;

    void Start()
    {
        GenerateGrid();

        // Pastikan BuildingManager tahu tiles
        if (buildingManager != null)
            buildingManager.gridTiles = tiles;
    }

    #region Grid Generation
    public void GenerateGrid()
    {
        tiles = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos = new Vector3(x * tileSize, 0, z * tileSize);
                GameObject tileObj = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                Tile tile = tileObj.GetComponent<Tile>();
                tile.gridPosition = new Vector2Int(x, z);
                tiles[x, z] = tile;
            }
        }
    }
    #endregion

    #region Tile Utility
    public Tile GetTileAtPosition(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x + tileSize * 0.5f) / tileSize);
        int z = Mathf.FloorToInt((worldPos.z + tileSize * 0.5f) / tileSize);

        if (x >= 0 && x < width && z >= 0 && z < height)
            return tiles[x, z];

        return null;
    }

    public Vector3 SnapToTile(Vector3 tileWorldPos)
    {
        return tileWorldPos + placementOffset;
    }

    public void MovePreviewToMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Tile")))
        {
            Tile tile = hit.collider.GetComponent<Tile>();
            if (tile != null)
                previewObject.transform.position = SnapToTile(tile.transform.position);
        }
    }

    public void HighlightTile(Tile tile, bool highlight)
    {
        if (tile != null)
            tile.Highlight(highlight);
    }

    public Tile[] GetNeighbors(Tile tile)
    {
        Vector2Int pos = tile.gridPosition;
        System.Collections.Generic.List<Tile> neighbors = new System.Collections.Generic.List<Tile>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0) continue;
                int nx = pos.x + dx;
                int nz = pos.y + dz;

                if (nx >= 0 && nx < width && nz >= 0 && nz < height)
                {
                    neighbors.Add(tiles[nx, nz]);
                }
            }
        }
        return neighbors.ToArray();
    }
    #endregion

    #region Placement
    public void ShowPreview(GameObject prefab)
    {
        if (previewObject != null)
            Destroy(previewObject);

        currentPrefab = prefab;
        previewObject = Instantiate(prefab);

        foreach (var r in previewObject.GetComponentsInChildren<Renderer>())
            r.material = new Material(r.material);

        isPlacing = true;
    }

    public void UpdatePreviewPosition(Vector3 worldPos)
    {
        if (!isPlacing || previewObject == null) return;
        previewObject.transform.position = SnapToTile(worldPos);
    }

    public bool CanPlaceObject(Tile tile)
    {
        if (tile == null) return false;
        if (tile.isOccupied) return false;

        // Contoh: bangunan harus terhubung jalan/utilities
        // Ini nanti integrasi BuildingManager / RoadManager
        return true;
    }

    public void PlaceObject(Tile tile)
    {
        if (!isPlacing || tile == null) return;
        if (!CanPlaceObject(tile)) return;

        GameObject obj = Instantiate(
            currentPrefab,
            SnapToTile(tile.transform.position),
            Quaternion.identity
        );

        tile.isOccupied = true;
        tile.currentObject = obj;

        var placeable = obj.GetComponent<IPlaceable>();
        if (placeable != null)
            placeable.OnPlaced(tile, this);

        // 🔥 HANYA BUILDING YANG KELUAR DARI PLACING MODE
        if (currentPlacementType == PlacementType.Building)
        {
            Destroy(previewObject);
            previewObject = null;
            isPlacing = false;
        }
    }

    public void RemoveObject(Tile tile)
    {
        if (tile == null || tile.currentObject == null) return;

        var placeable = tile.currentObject.GetComponent<IPlaceable>();
        if (placeable != null)
            placeable.OnRemoved(tile, this);

        Destroy(tile.currentObject);
        tile.isOccupied = false;
        tile.currentObject = null;
    }


    public void UpdateTileStatus(Tile tile)
    {
        if (tile == null) return;
        // Update koneksi, warning mark, dll
    }
    #endregion
}


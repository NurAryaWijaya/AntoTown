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
    public Vector3 placementOffset = new Vector3(0, 0.25f, 0); // sedikit di atas tile

    [HideInInspector]
    public Tile[,] tiles;
    public bool roadModeActive = false;

    List<Tile> highlightedTiles = new();
    List<Tile> areaHighlightedTiles = new();


    void Start()
    {
        GenerateGrid();

        if (buildingManager != null)
            buildingManager.gridTiles = tiles;
    }


    void Update()
    {
        if (isPlacing)
        {
            MovePreviewToMouse();
            return;
        }

        // mode normal (klik bangunan untuk lihat area)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Building building = hit.collider.GetComponentInParent<Building>();

                if (building != null && building.hasArea)
                    ShowArea(building);
                else
                    ClearArea();
            }
            else
            {
                ClearArea();
            }
        }
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

    public Vector3 SnapToTile(Vector3 tileWorldPos, Building building = null)
    {
        Vector3 pos = tileWorldPos + placementOffset;

        return pos;
    }

    public void MovePreviewToMouse()
    {
        if (!isPlacing || previewObject == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Tile")))
        {
            ClearHighlightedTiles();
            return;
        }

        Tile originTile = hit.collider.GetComponent<Tile>();
        if (originTile == null) return;

        var building = previewObject.GetComponentInChildren<Building>();
        Vector2Int size = building != null ? new Vector2Int(
            Mathf.RoundToInt(building.size.x),
            Mathf.RoundToInt(building.size.z) 
        ) : Vector2Int.one;

        var tilesToCheck = GetTilesForBuilding(originTile, size);

        ClearHighlightedTiles();

        if (tilesToCheck == null)
        {
            HighlightTiles(tilesToCheck, Color.red);
            return;
        }

        bool canPlace = true;
        foreach (var t in tilesToCheck)
        {
            if (t.isOccupied)
            {
                canPlace = false;
                break;
            }
        }

        HighlightTiles(tilesToCheck, canPlace ? Color.green : Color.red);

        // posisi preview tetap snap ke tile origin
        previewObject.transform.position = SnapToTile(originTile.transform.position);
    }

    public List<Tile> GetTilesForBuilding(Tile origin, Vector2Int size)
    {
        List<Tile> result = new();
        if (origin == null) return result;

        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                int nx = origin.gridPosition.x + x;
                int nz = origin.gridPosition.y + z;

                if (nx < 0 || nx >= width || nz < 0 || nz >= height)
                    return null; // out of bounds

                result.Add(tiles[nx, nz]);
            }
        }
        return result;
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
        if (previewObject != null) Destroy(previewObject);

        currentPrefab = prefab;

        // buat container kosong untuk preview pivot
        previewObject = new GameObject("Preview_" + prefab.name);
        previewObject.transform.position = Vector3.zero; // nanti akan diupdate oleh MovePreview
        previewObject.transform.rotation = prefab.transform.rotation;

        // spawn prefab sebagai child
        GameObject mesh = Instantiate(prefab, previewObject.transform);

        // scale sesuai size
        var building = mesh.GetComponent<Building>();
        if (building != null)
        {
            mesh.transform.localScale = building.size;

            // geser mesh agar center prefab berada di tengah semua tile
            float offsetX = (building.size.x - 1) * 0.5f * tileSize;
            float offsetZ = (building.size.z - 1) * 0.5f * tileSize;

            mesh.transform.localPosition = new Vector3(offsetX, 0, offsetZ);
        }

        // buat material transparan
        foreach (var r in mesh.GetComponentsInChildren<Renderer>())
        {
            r.material = new Material(r.material);
            Color c = r.material.color;
            c.a = 0.5f;
            r.material.color = c;
        }

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

        ClearHighlightedTiles();
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

    // Area
    void HighlightTiles(List<Tile> tiles, Color color)
    {
        if (tiles == null) return;

        foreach (var t in tiles)
        {
            t.SetColor(color);
            highlightedTiles.Add(t);
        }
    }

    public void ClearHighlightedTiles()
    {
        foreach (var t in highlightedTiles)
            t.ResetColor();

        highlightedTiles.Clear();
    }

    public void ShowArea(Building building)
    {
        ClearArea();

        if (!building.hasArea) return;

        Vector2 center = building.GetCenterGridPosition();
        int r = building.areaRadiusInTiles;

        foreach (var tile in tiles)
        {
            Vector2Int p = tile.gridPosition;
            float dx = p.x - center.x;
            float dz = p.y - center.y;

            if (dx * dx + dz * dz <= r * r)
            {
                tile.SetColor(new Color(0.2f, 0.4f, 1f, 0.6f));
                areaHighlightedTiles.Add(tile);
            }
        }
    }

    public void ClearArea()
    {
        foreach (var t in areaHighlightedTiles)
            t.ResetColor();

        areaHighlightedTiles.Clear();
    }

    public void RegisterHighlightedTile(Tile tile)
    {
        if (!highlightedTiles.Contains(tile))
            highlightedTiles.Add(tile);
    }

    #endregion
}


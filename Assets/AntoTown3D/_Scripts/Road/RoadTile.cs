using UnityEngine;
using System.Collections.Generic;

public class RoadTile : MonoBehaviour, IPlaceable
{
    public RoadType roadType;
    public JunctionType junctionType;

    [Header("Visual")]
    public RoadVisualSet visualSet;

    Transform visualRoot;
    GameObject currentVisual;

    public bool connectNorth;
    public bool connectEast;
    public bool connectSouth;
    public bool connectWest;

    public List<RoadTile> connectedRoads = new List<RoadTile>();

    private Tile ownerTile;
    void Awake()
    {
        visualRoot = transform.Find("VisualRoot");
    }

    // PLACEMENT CALLBACK
    public void OnPlaced(Tile tile, GridManager grid)
    {
        ownerTile = tile;
        DetectNeighbors(grid);
        UpdateJunctionType();
    }

    public void OnRemoved(Tile tile, GridManager grid)
    {
        foreach (var road in connectedRoads)
        {
            road.RemoveConnection(this);
            road.UpdateJunctionType();
        }
    }

    // NEIGHBOR DETECTION
    void DetectNeighbors(GridManager grid)
    {
        Vector2Int pos = ownerTile.gridPosition;

        TryConnect(grid, pos + Vector2Int.up, Direction.North);
        TryConnect(grid, pos + Vector2Int.right, Direction.East);
        TryConnect(grid, pos + Vector2Int.down, Direction.South);
        TryConnect(grid, pos + Vector2Int.left, Direction.West);
    }

    void TryConnect(GridManager grid, Vector2Int pos, Direction dir)
    {
        if (pos.x < 0 || pos.y < 0 ||
            pos.x >= grid.width || pos.y >= grid.height)
            return;

        Tile neighborTile = grid.tiles[pos.x, pos.y];
        if (neighborTile == null || neighborTile.currentObject == null)
            return;

        RoadTile neighbor = neighborTile.currentObject.GetComponent<RoadTile>();
        if (neighbor == null) return;

        AddConnection(neighbor, dir);
        neighbor.AddConnection(this, Opposite(dir));

        neighbor.UpdateJunctionType();
    }

    // CONNECTION LOGIC
    public void AddConnection(RoadTile road, Direction dir)
    {
        if (!connectedRoads.Contains(road))
            connectedRoads.Add(road);

        SetDirection(dir, true);
    }

    public void RemoveConnection(RoadTile road)
    {
        connectedRoads.Remove(road);

        // Direction flag akan dihitung ulang
        ResetConnections();
    }

    void ResetConnections()
    {
        connectNorth = connectEast = connectSouth = connectWest = false;

        foreach (var r in connectedRoads)
        {
            Vector3 d = r.transform.position - transform.position;
            if (d.z > 0) connectNorth = true;
            if (d.z < 0) connectSouth = true;
            if (d.x > 0) connectEast = true;
            if (d.x < 0) connectWest = true;
        }
    }

    void SetDirection(Direction dir, bool value)
    {
        if (dir == Direction.North) connectNorth = value;
        if (dir == Direction.East) connectEast = value;
        if (dir == Direction.South) connectSouth = value;
        if (dir == Direction.West) connectWest = value;
    }

    Direction Opposite(Direction d)
    {
        if (d == Direction.North) return Direction.South;
        if (d == Direction.East) return Direction.West;
        if (d == Direction.South) return Direction.North;
        return Direction.East;
    }

    // JUNCTION
    public void UpdateJunctionType()
    {
        int count =
            (connectNorth ? 1 : 0) +
            (connectEast ? 1 : 0) +
            (connectSouth ? 1 : 0) +
            (connectWest ? 1 : 0);

        if (count <= 1) junctionType = JunctionType.End;
        else if (count == 2)
        {
            if ((connectNorth && connectSouth) ||
                (connectEast && connectWest))
                junctionType = JunctionType.Straight;
            else
                junctionType = JunctionType.Curve;
        }
        else if (count == 3) junctionType = JunctionType.TJunction;
        else junctionType = JunctionType.Crossroad;

        UpdateVisual();
    }
    void UpdateVisual()
    {
        if (currentVisual != null)
            Destroy(currentVisual);

        GameObject prefab = junctionType switch
        {
            JunctionType.End => visualSet.end,
            JunctionType.Straight => visualSet.straight,
            JunctionType.Curve => visualSet.curve,
            JunctionType.TJunction => visualSet.tJunction,
            JunctionType.Crossroad => visualSet.crossroad,
            _ => null
        };

        if (prefab == null) return;

        currentVisual = Instantiate(prefab, visualRoot);
        currentVisual.transform.localPosition = Vector3.zero;

        UpdateRotation();
    }

    void UpdateRotation()
    {
        if (currentVisual == null) return;

        float rotY = 0f;

        if (junctionType == JunctionType.End)
        {
            if (connectNorth) rotY = 0;
            else if (connectEast) rotY = 90;
            else if (connectSouth) rotY = 180;
            else if (connectWest) rotY = 270;
        }

        if (junctionType == JunctionType.Straight)
        {
            if (connectNorth && connectSouth) rotY = 0;
            else rotY = 90;
        }

        if (junctionType == JunctionType.Curve)
        {
            if (connectNorth && connectEast) rotY = 0;
            else if (connectEast && connectSouth) rotY = 90;
            else if (connectSouth && connectWest) rotY = 180;
            else if (connectWest && connectNorth) rotY = 270;
        }

        if (junctionType == JunctionType.TJunction)
        {
            if (!connectNorth) rotY = 180;
            else if (!connectEast) rotY = 270;
            else if (!connectSouth) rotY = 0;
            else if (!connectWest) rotY = 90;
        }

        currentVisual.transform.localRotation = Quaternion.Euler(0, rotY, 0);
    }

}

public enum Direction
{
    North, East, South, West
}

using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    public GameObject vehiclePrefab;
    public GridManager gridManager;

    public void SpawnAtRandomRoad()
    {
        foreach (var tile in gridManager.tiles)
        {
            if (tile != null && tile.currentObject != null)
            {
                RoadTile road = tile.currentObject.GetComponent<RoadTile>();
                if (road != null)
                {
                    SpawnAtRoad(road);
                    return;
                }
            }
        }

        Debug.LogWarning("No road found to spawn vehicle");
    }

    public void SpawnAtRoad(RoadTile road)
    {
        GameObject v = Instantiate(vehiclePrefab);
        v.GetComponent<VehicleController>().SetStartTile(road);
    }
}

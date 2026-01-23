using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleSpawner : MonoBehaviour
{
    public GameObject[] vehiclePrefabs;
    public GridManager gridManager;

    void Update()
    {
        // 🔑 Tekan V → spawn vehicle
        if (Keyboard.current.vKey.wasPressedThisFrame)
        {
            SpawnAtRandomRoad();
        }
    }

    public void SpawnAtRandomRoad()
    {
        foreach (var tile in gridManager.tiles)
        {
            if (tile == null || tile.currentObject == null)
                continue;

            RoadTile road = tile.currentObject.GetComponent<RoadTile>();
            if (road != null)
            {
                SpawnAtRoad(road);
                return;
            }
        }

        Debug.LogWarning("No road found to spawn vehicle");
    }

    void SpawnAtRoad(RoadTile road)
    {
        if (vehiclePrefabs == null || vehiclePrefabs.Length == 0)
            return;

        GameObject prefab =
            vehiclePrefabs[Random.Range(0, vehiclePrefabs.Length)];

        GameObject v = Instantiate(prefab);
        v.GetComponent<VehicleController>().SetStartTile(road);
    }
}

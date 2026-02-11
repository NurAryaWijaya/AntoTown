using UnityEngine;
using System.Collections;

public class VehicleSpawner : MonoBehaviour
{
    [Header("Vehicle Prefabs")]
    public GameObject[] vehiclePrefabs;
    public GridManager gridManager;

    [Header("Vehicle Lifetime (Seconds)")]
    public float minVehicleLifeTime = 300f; // 5 menit
    public float maxVehicleLifeTime = 600f; // 10 menit

    [Header("Spawn Interval")]
    public float spawnInterval = 600f;

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }
    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            SpawnAtRandomRoad();
        }
    }

    void OnEnable()
    {
        // Subscribe ke event saat bangunan muncul
        BuildingManager.OnBuildingPlaced += OnBuildingPlaced;
    }

    void OnDisable()
    {
        BuildingManager.OnBuildingPlaced -= OnBuildingPlaced;
    }

    // Event handler
    void OnBuildingPlaced(Building building)
    {
        // Opsional: spawn kendaraan hanya untuk residential atau commercial
        if (building.buildingType != BuildingType.Residential &&
            building.buildingType != BuildingType.Commercial)
            return;

        SpawnAtRandomRoad();
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

        GameObject prefab = vehiclePrefabs[Random.Range(0, vehiclePrefabs.Length)];

        GameObject vehicle = Instantiate(prefab);
        var controller = vehicle.GetComponent<VehicleController>();
        if (controller != null)
        {
            controller.SetStartTile(road);
        }

        // 🔥 Auto destroy setelah vehicleLifeTime detik
        ScheduleVehicleDestroy(vehicle);
    }
    void ScheduleVehicleDestroy(GameObject vehicle)
    {
        float lifeTime = Random.Range(minVehicleLifeTime, maxVehicleLifeTime);
        Destroy(vehicle, lifeTime);
    }

}

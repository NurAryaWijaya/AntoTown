using System.Collections.Generic;
using UnityEngine;

public class VehicleController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 4f;
    public float arriveDistance = 0.05f;

    [Header("Runtime")]
    public RoadTile currentTile;
    public RoadTile previousTile;

    [Header("Visual")]
    public Transform vehicleVisual; // assign di inspector
    public float rotationSpeed = 20f; // kecepatan rotasi visual child

    private Vector3 targetPosition;
    private bool isMoving;

    void Update()
    {
        if (!isMoving) return;

        MoveAlongPath();
        RotateVisualTowardsMovement();
    }

    // PUBLIC API
    public void SetStartTile(RoadTile startTile)
    {
        currentTile = startTile;
        previousTile = null;

        transform.position = startTile.transform.position;
        ChooseNextTile();
    }

    public void Stop()
    {
        isMoving = false;
    }

    // CORE LOGIC
    void MoveAlongPath()
    {
        // Pindah ke target tile tanpa slow-down
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) <= arriveDistance)
        {
            OnArriveAtTile();
        }
    }

    void RotateVisualTowardsMovement()
    {
        if (vehicleVisual == null || previousTile == null) return;

        Vector3 moveDir = (targetPosition - transform.position).normalized;
        if (moveDir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
        vehicleVisual.rotation = Quaternion.Slerp(vehicleVisual.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    void OnArriveAtTile()
    {
        currentTile = GetTileFromPosition();
        CheckJunction();
    }

    void ChooseNextTile()
    {
        if (currentTile == null) return;

        List<RoadTile> options = new List<RoadTile>();

        foreach (var road in currentTile.connectedRoads)
        {
            if (road != previousTile)
                options.Add(road);
        }

        if (options.Count == 0 && previousTile != null)
        {
            options.Add(previousTile);
        }

        RoadTile nextTile = options[Random.Range(0, options.Count)];

        previousTile = currentTile;
        currentTile = nextTile;

        // Tetap offset lane untuk child visual
        targetPosition = nextTile.transform.position; // root bergerak ke tile
        vehicleVisual.localPosition = GetLaneOffset(previousTile, nextTile);
        isMoving = true;
    }

    Vector3 GetLaneOffset(RoadTile from, RoadTile to)
    {
        Vector3 dir = (to.transform.position - from.transform.position).normalized;
        float laneOffset = 0.25f; // geser ke tepi tile

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z)) // horizontal
            return new Vector3(0, 0, dir.x > 0 ? laneOffset : -laneOffset);
        else // vertical
            return new Vector3(dir.z > 0 ? -laneOffset : laneOffset, 0, 0);
    }

    void CheckJunction()
    {
        ChooseNextTile();
    }

    RoadTile GetTileFromPosition()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.1f);

        foreach (var hit in hits)
        {
            RoadTile road = hit.GetComponent<RoadTile>();
            if (road != null)
                return road;
        }

        return currentTile;
    }
}

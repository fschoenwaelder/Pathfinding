using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Node = GridNode.Node;
using Unity.VisualScripting;
using JetBrains.Annotations;
using UnityEngine.Serialization;
// using System;

public class NPCController : MonoBehaviour
{
    [Header("Properties")]
    public GridGenerator gridGenerator;
    public float speed = 5f;
    public Material outlineMaterial;
    public LayerMask npcLayerMask;
    [Space(10)]

    [Header("Steering")]
    public float steeringStrength = 10f;
    public float searchRadius = 3f;         // Radius für zufällige Ziele
    [FormerlySerializedAs("wanderRadius")]
    public float steeringRadius = 1.5f;       // Radius für Wanderkreis
    [FormerlySerializedAs("wanderDistance")]
    public float steeringDistance = 2f;       // Abstand von NPC zum Wanderkreis
    [FormerlySerializedAs("wanderJitter")]
    public float steeringJitter = 0.5f;       // Zufällige Jitter-Amount
    [Space(10)]

    [Header("Group behaviour")]
    public float neighborRadius = 5f;
    public float separationDistance = 1f;
    public float cohesionWeight = 1f;
    public float alignmentWeight = 1f;
    public float separationWeight = 3f;
    public int groupThreshold = 3;
    public float avoidDistance = 1f;
    public float avoidStrength = 10f;

    private List<Node> path;
    private int targetIndex;
    private static NPCController selectedNPC;
    private Renderer npcRenderer;
    private Material originalMaterial;
    private Vector3 velocity;
    private Vector3 origin;
    private Vector3 targetPosition;

    void Start()
    {
        npcRenderer = GetComponent<Renderer>();
        originalMaterial = npcRenderer.material;
        velocity = transform.forward * speed;
        origin = transform.position;

        if (gridGenerator == null)
        {
            Debug.LogError("GridGenerator is not assigned.");
            enabled = false;
            return;
        }

        SetRandomTargetPosition();
    }

    void Update()
    {
        HandleSelection();

        if (selectedNPC != this)
        {
            WanderAndMoveAlongPath();
        }
    }

    private void HandleSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            Debug.DrawRay(ray.origin, ray.direction * 100, Color.blue, 1f);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, npcLayerMask))
            {
                Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green, 1f);

                if (hit.transform.gameObject != gameObject)
                {
                    return;
                }

                if (selectedNPC != null)
                {
                    selectedNPC.Deselect();
                }

                Select();
            }
            else
            {
                Debug.DrawRay(ray.origin, ray.direction * 100, Color.yellow, 1f);

                if (selectedNPC == this)
                {
                    Deselect();
                }
            }
        }
    }

    private void Select()
    {
        selectedNPC = this;
        npcRenderer.material = outlineMaterial;
    }

    private void Deselect()
    {
        npcRenderer.material = originalMaterial;
        selectedNPC = null;
    }

    private void WanderAndMoveAlongPath()
    {
        if (path != null && targetIndex < path.Count)
        {
            MoveAlongPath();
        }
        else
        {
            SetRandomTargetPosition();
        }
    }

    private void MoveAlongPath()
    {
        if (targetIndex >= path.Count)
        {
            return;
        }

        targetPosition = path[targetIndex].Position;
        targetPosition.y = transform.position.y;

        Vector3 desiredVelocity = (targetPosition - transform.position).normalized * speed;
        Vector3 steering = desiredVelocity - velocity;
        steering = Vector3.ClampMagnitude(steering, steeringStrength);

        velocity = Vector3.ClampMagnitude(velocity + steering, speed);
        ApplyGroupBehavior();
        AvoidObstacles();
        transform.position += velocity * Time.deltaTime;

        // Rotate NPCs in movement direction 
        if (velocity != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(velocity), Time.deltaTime * speed);
        }

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            targetIndex++;
        }
    }

    private void SetRandomTargetPosition()
    {
        GameObject[] waypoints = GameObject.FindGameObjectsWithTag("Waypoint");

        if (waypoints == null)
        {
            return;
        }

        int randomWaypointIndex = Random.Range(0, waypoints.Length - 1);

        GameObject randomWaypoint = waypoints[randomWaypointIndex];

        try
        {
            FindPath(randomWaypoint.transform.position);
        }
        catch (System.Exception)
        {
            // Ignore errors if path to random Waypoint not found
            return;
        }
    }

    private void FindPath(Vector3 targetPosition)
    {
        if (gridGenerator == null)
        {
            return;
        }

        path = gridGenerator.FindPath(transform.position, targetPosition);

        targetIndex = 0;

        if (gridGenerator.IsDebug && (path == null || path.Count == 0))
        {
            Debug.LogError($"No path found for {gameObject.name}.");
        }
    }

    private void AvoidObstacles()
    {
        RaycastHit hit;
        Vector3 forward = transform.forward;
        Vector3 rayOrigin = transform.position;

        if (Physics.Raycast(rayOrigin, forward, out hit, avoidDistance, npcLayerMask))
        {
            Vector3 avoidDirection = Vector3.Reflect(forward, hit.normal);
            Vector3 desiredVelocity = avoidDirection.normalized * speed;
            Vector3 steering = desiredVelocity - velocity;
            steering = Vector3.ClampMagnitude(steering, avoidStrength);

            velocity = Vector3.ClampMagnitude(velocity + steering, speed);

            // Debugging: draw the raycast and avoidance direction
            Debug.DrawRay(rayOrigin, forward * hit.distance, Color.red);
            Debug.DrawRay(hit.point, avoidDirection, Color.green);
        }
    }

    private void ApplyGroupBehavior()
    {
        Collider[] neighbors = Physics.OverlapSphere(transform.position, neighborRadius, npcLayerMask);

        Vector3 separation = Vector3.zero;
        int neighborCount = 0;

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.gameObject != gameObject)
            {
                NPCController neighborController = neighbor.GetComponent<NPCController>();
                if (neighborController == null) continue;

                Vector3 toNeighbor = neighbor.transform.position - transform.position;

                // Separation
                if (toNeighbor.magnitude < separationDistance)
                {
                    separation -= (toNeighbor.normalized / toNeighbor.magnitude);
                }

                neighborCount++;
            }
        }

        if (neighborCount == 0)
        {
            return;
        }

        // Apply separation force
        Vector3 separationForce = (separation / neighborCount) * separationWeight;
        velocity = Vector3.ClampMagnitude(velocity + separationForce, speed);
    }
}

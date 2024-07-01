using System.Collections.Generic;
using UnityEngine;
using Node = GridNode.Node;

[ExecuteAlways]
public class GridGenerator : MonoBehaviour
{
    [Header("Properties")]
    public int gridWidth = 100;
    public int gridHeight = 100;
    public float nodeSize = 1f;
    public Transform startingPoint;
    [Space(10)]

    [Header("Layers")]
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;
    [Space(10)]

    [Header("Colors")]
    public Color walkableColor = Color.green;
    public Color nonWalkableColor = Color.red;
    public Color noGroundColor = Color.blue;
    [Space(10)]

    [Header("Debug")]
    public bool IsDebug = false;
    public bool ShowRays, ShowPaths = false;

    [HideInInspector]
    public List<Node> Path { get; private set; }


    private static Node[,] nodes = null;
    private static List<Node> obstacles = new List<Node>();

    private void OnDrawGizmos()
    {
        if (nodes == null || nodes.Length == 0)
        {
            GenerateGrid();
        }

        Time.timeScale = 1;
        DrawGrid();
    }

    private void Awake()
    {
        if (!Application.isPlaying) return;

        if (nodes == null || nodes.Length == 0)
        {
            GenerateGrid();
        }
    }


    private void GenerateGrid()
    {
        Node.nodeSize = nodeSize;
        Node.GridWidth = gridWidth;
        Node.GridHeight = gridHeight;
        nodes = new Node[gridWidth, gridHeight];
        obstacles.Clear();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 nodePosition = new Vector3(startingPoint.position.x + x * nodeSize, 20, startingPoint.position.z + z * nodeSize);
                Node node;
                CheckNodeWalkability(nodePosition, out node);
                node.gridPosition = new Vector3Int(x, 0, z);
                nodes[x, z] = node;
            }
        }
    }

    private void DrawGrid()
    {
        foreach (Node node in nodes)
        {
            if (node == null) continue;

            Gizmos.color = node.HasGround ? (node.IsWalkable ? walkableColor : nonWalkableColor) : noGroundColor;

            if (node.HasGround && ShowRays)
            {
                Gizmos.DrawLine(node.Position - Vector3.up * 10, node.HitPoint);
            }
        }
    }

    private void CheckNodeWalkability(Vector3 nodePosition, out Node node)
    {
        Ray ray = new Ray(nodePosition, Vector3.down);
        RaycastHit hit;

        bool hasGround = Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer);

        float additionalCost = 40;

        if (hit.collider != null && hit.collider.gameObject.GetComponent<GridNodeProperty>() != null)
        {
            additionalCost = hit.collider.gameObject.GetComponent<GridNodeProperty>().additionalCost;
        }

        if (hasGround)
        {
            bool isWalkable = !Physics.Raycast(ray, Mathf.Infinity, obstacleLayer);

            node = new Node { Position = nodePosition, HasGround = true, IsWalkable = isWalkable, HitPoint = hit.point, additionalCost = additionalCost };

            if (!isWalkable)
            {
                obstacles.Add(node);
            }
        }
        else
        {
            node = new Node { Position = nodePosition, HasGround = false, IsWalkable = false, additionalCost = additionalCost };

            obstacles.Add(node);
        }
    }



    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = GetNodeFromWorldPoint(startPos);
        Node targetNode = GetNodeFromWorldPoint(targetPos);

        if (startNode == null || targetNode == null)
        {
            Debug.LogError("Start or Target node is null");
            return null;
        }

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];

            // Get node from openSet
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            // Skip non-walkable fields
            if (!targetNode.IsWalkable)
            {
                return null;
            }

            // Path found
            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            // Loop through neighbors
            foreach (Node neighbor in currentNode.GetNeighbors(nodes))
            {
                if (!neighbor.IsWalkable || closedSet.Contains(neighbor))
                {
                    continue;
                }

                float newCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor) + neighbor.additionalCost;

                // Check costs and check for duplicates 
                if (newCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }


                    if (ShowPaths)
                    {
                        Debug.DrawLine(currentNode.Position, neighbor.Position, Color.cyan, 200f);
                    }
                }
            }
        }

        // No path found
        return null;
    }

    private List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();

        return path;
    }

    private float GetDistance(Node nodeA, Node nodeB)
    {
        float dstX = Mathf.Abs(nodeA.Position.x - nodeB.Position.x);
        float dstZ = Mathf.Abs(nodeA.Position.z - nodeB.Position.z);

        if (dstX > dstZ)
            return 1.4f * dstZ + (dstX - dstZ);
        return 1.4f * dstX + (dstZ - dstX);
    }


    public Node GetNodeFromWorldPoint(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - startingPoint.position.x) / nodeSize);
        int z = Mathf.RoundToInt((worldPosition.z - startingPoint.position.z) / nodeSize);

        return nodes[x, z];
    }

}

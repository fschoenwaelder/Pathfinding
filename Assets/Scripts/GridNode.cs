using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GridNode : MonoBehaviour
{
    public class Node
    {
        [HideInInspector]
        public Vector3Int gridPosition;
        [HideInInspector]
        public Vector3 Position { get; set; }
        [HideInInspector]
        public bool HasGround { get; set; }
        [HideInInspector]
        public bool IsWalkable { get; set; }
        [HideInInspector]
        public Vector3 HitPoint { get; set; }
        [HideInInspector]
        public float gCost;
        public float hCost;
        public float additionalCost;
        [HideInInspector]
        public float fCost => gCost + hCost + additionalCost;

        [HideInInspector]
        public Node parent;
        [HideInInspector]
        public static float nodeSize;
        [HideInInspector]
        public static int GridWidth;
        [HideInInspector]
        public static int GridHeight;

        public List<Node> GetNeighbors(Node[,] grid)
        {
            List<Node> neighbors = new List<Node>();

            int x = gridPosition.x;
            int z = gridPosition.z;

            /* 
             * Check every neighbor of the given node
            */

            // Left
            if (x > 0) neighbors.Add(grid[x - 1, z]);
            // Left bottom
            if (x > 0 && z > 0) neighbors.Add(grid[x - 1, z - 1]);
            // Left top
            if (x > 0 && z < GridHeight - 1) neighbors.Add(grid[x - 1, z + 1]);
            // Right
            if (x < GridWidth - 1) neighbors.Add(grid[x + 1, z]);
            // Right bottom
            if (x < GridWidth - 1 && z > 0) neighbors.Add(grid[x + 1, z - 1]);
            // Right top
            if (x < GridWidth - 1 && z < GridHeight - 1) neighbors.Add(grid[x + 1, z + 1]);
            // Bottom
            if (z > 0) neighbors.Add(grid[x, z - 1]);
            // Top
            if (z < GridHeight - 1) neighbors.Add(grid[x, z + 1]);

            return neighbors;
        }
    }

}

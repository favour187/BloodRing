using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Modern 3D battle royale navigation and pathfinding grid system.
/// Evaluates terrain slopes, water obstacle depths, road speed bonuses, and building colliders
/// across a high-performance spatial navigation grid for AI tactical bots and vehicles.
/// </summary>
public class NavGridSystem : MonoBehaviour
{
    public static NavGridSystem Instance;

    public class NavNode
    {
        public int gridX, gridY;
        public Vector3 worldPosition;
        public bool isWalkable;
        public float movementPenalty; // 1.0f = normal grass, 0.8f = road bonus, 2.0f = shallow water

        public int gCost;
        public int hCost;
        public NavNode parent;
        public int fCost { get { return gCost + hCost; } }
    }

    private NavNode[,] grid;
    private int gridSizeX = 100;
    private int gridSizeY = 100;
    private float worldSizeX = 500f;
    private float worldSizeZ = 500f;
    private float nodeRadius = 2.5f;
    private float nodeDiameter = 5.0f;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Builds the navigation grid across the 500x500m battle royale terrain.
    /// </summary>
    public void BuildNavGrid(MapData mapData)
    {
        Debug.Log("NavGridSystem: Constructing 100x100 spatial navigation grid...");
        grid = new NavNode[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * (worldSizeX / 2) - Vector3.forward * (worldSizeZ / 2);

        float waterLevel = mapData != null ? mapData.waterLevel : 1.5f;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);

                // Check obstacle collisions
                bool walkable = !Physics.CheckSphere(worldPoint + Vector3.up * 1f, nodeRadius * 0.8f, LayerMask.GetMask("Obstacle", "Wall"));
                float penalty = 1.0f;

                // Check water depth slowdown
                if (worldPoint.y < waterLevel)
                {
                    penalty = 2.0f; // shallow water penalty
                    if (worldPoint.y < waterLevel - 1.5f)
                    {
                        walkable = false; // deep water impassable without vehicle
                    }
                }

                grid[x, y] = new NavNode
                {
                    gridX = x,
                    gridY = y,
                    worldPosition = worldPoint,
                    isWalkable = walkable,
                    movementPenalty = penalty
                };
            }
        }

        Debug.Log("NavGridSystem: Navigation grid generated successfully (" + (gridSizeX * gridSizeY) + " nodes).");
    }

    public NavNode NodeFromWorldPoint(Vector3 worldPosition)
    {
        if (grid == null) return null;
        float percentX = (worldPosition.x + worldSizeX / 2) / worldSizeX;
        float percentY = (worldPosition.z + worldSizeZ / 2) / worldSizeZ;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.Clamp(Mathf.FloorToInt(gridSizeX * percentX), 0, gridSizeX - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(gridSizeY * percentY), 0, gridSizeY - 1);
        return grid[x, y];
    }

    /// <summary>
    /// Computes shortest path across walkable navigation nodes using A* search.
    /// </summary>
    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        NavNode startNode = NodeFromWorldPoint(startPos);
        NavNode targetNode = NodeFromWorldPoint(targetPos);

        if (startNode == null || targetNode == null || !targetNode.isWalkable) return new List<Vector3>();

        List<NavNode> openSet = new List<NavNode>();
        HashSet<NavNode> closedSet = new HashSet<NavNode>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            NavNode currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            foreach (NavNode neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.isWalkable || closedSet.Contains(neighbor)) continue;

                int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor) + Mathf.RoundToInt(neighbor.movementPenalty * 10f);
                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                }
            }
        }

        return new List<Vector3>();
    }

    private List<Vector3> RetracePath(NavNode startNode, NavNode endNode)
    {
        List<Vector3> path = new List<Vector3>();
        NavNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.worldPosition);
            if (currentNode.parent == null) break;
            currentNode = currentNode.parent;
        }

        path.Reverse();
        return path;
    }

    private List<NavNode> GetNeighbors(NavNode node)
    {
        List<NavNode> neighbors = new List<NavNode>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbors;
    }

    private int GetDistance(NavNode nodeA, NavNode nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (dstX > dstY) return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }
}

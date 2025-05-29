using UnityEngine;
using System.Collections.Generic;
public class SpatialGrid
{
    private Dictionary<Vector2Int, List<Particle>> grid;
    private float cellSize;
    private Vector2Int gridMin, gridMax;

    public SpatialGrid(float cellSize)
    {
        this.cellSize = cellSize;
        this.grid = new Dictionary<Vector2Int, List<Particle>>();
    }

    public void Clear()
    {
        foreach (var cell in grid.Values)
        {
            cell.Clear();
        }
    }

    public void AddParticle(Particle particle)
    {
        Vector2Int cellCoord = WorldToGrid(particle.position);

        if (!grid.ContainsKey(cellCoord))
        {
            grid[cellCoord] = new List<Particle>();
        }

        grid[cellCoord].Add(particle);
    }

    public List<Particle> GetNearbyParticles(Vector3 position, float radius)
    {
        List<Particle> nearby = new List<Particle>();

        // Check cells in a radius around the position
        int cellRadius = Mathf.CeilToInt(radius / cellSize);
        Vector2Int centerCell = WorldToGrid(position);

        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int z = -cellRadius; z <= cellRadius; z++)
            {
                Vector2Int checkCell = centerCell + new Vector2Int(x, z);

                if (grid.ContainsKey(checkCell))
                {
                    nearby.AddRange(grid[checkCell]);
                }
            }
        }

        return nearby;
    }

    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / cellSize),
            Mathf.FloorToInt(worldPos.z / cellSize)
        );
    }
}
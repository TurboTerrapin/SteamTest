
/*
    SpawnPointGenerator.cs
    - Uses the poisson disk sampling algorithm to generate a randomized even distribution of points within the volume of a cylinder
    - No 2 points generated will be closer than the minDistance from each other
    - Supports pre-placed obstacles with their own exclusion radii that random points will avoid
    Contributor(s): Henryk Musial
    Last Updated: 4/23/2026
*/

using System.Collections.Generic;
using UnityEngine;

public static class SpawnPointGenerator
{
    public struct Obstacle
    {
        public Vector3 position;
        public float radius;

        public Obstacle(Vector3 position, float radius)
        {
            this.position = position;
            this.radius = radius;
        }
    }

    public static List<Vector3> GenerateSpawnLocations(float radius, float height, float minDistance, int spawnPoints, List<Obstacle> obstacles = null, int maxAttempts = 30)
    {
        List<Vector3> points = new List<Vector3>(spawnPoints);

        if (spawnPoints <= 0)
        {
            //Debug.Log("ERROR - Invalid num of spawn points ");
            return points;
        }

        float cellSize = minDistance / Mathf.Sqrt(3.0f); // Grid Cell size = minDistance / sqrt(3)

        // any two points in adjacent cells are within minDistance
        int gridWidth = Mathf.CeilToInt((2.0f * radius) / cellSize);
        int gridHeight = Mathf.CeilToInt(height / cellSize);

        // Initialize 3D grid array to store indices of points and populate (-1 for empty cell)
        int[,,] grid = new int[gridWidth, gridHeight, gridWidth];
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                for (int k = 0; k < gridWidth; k++)
                {
                    grid[i, j, k] = -1;
                }
            }
        }

        Vector3Int GetGridCoordinate(Vector3 pt) // Helper to get grid cell coordinates for a point
        {
            // Convert from world coordinates (centered at 0) to grid indices
            float x = pt.x + radius; // shift so min is 0
            float y = pt.y + height / 2.0f;
            float z = pt.z + radius;

            int xi = Mathf.FloorToInt(x / cellSize);
            int yi = Mathf.FloorToInt(y / cellSize);
            int zi = Mathf.FloorToInt(z / cellSize);
            return new Vector3Int(Mathf.Clamp(xi, 0, gridWidth - 1), Mathf.Clamp(yi, 0, gridHeight - 1), Mathf.Clamp(zi, 0, gridWidth - 1));
        }

        bool IsValid(Vector3 pt) // Helper to test if point is too close to any existing point
        {
            Vector3Int coord = GetGridCoordinate(pt);

            // Check cells in a 3 x 3 x3 neighborhood
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int nx = coord.x + dx;
                        int ny = coord.y + dy;
                        int nz = coord.z + dz;

                        if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight || nz < 0 || nz >= gridWidth) // If neighbor index is outside of grid bounds
                        {
                            continue; // skip
                        }

                        int idx = grid[nx, ny, nz];

                        if (idx != -1 && Vector3.Distance(pt, points[idx]) < minDistance) // If the cell is occupied and fails euclidean distance check
                        {
                            return false; // Point is invalid
                        }
                    }
                }
            }

            if (obstacles != null)
            {
                for (int i = 0; i < obstacles.Count; i++)
                {
                    float requiredDistance = obstacles[i].radius + minDistance;
                    if (Vector3.Distance(pt, obstacles[i].position) < requiredDistance)
                    {
                        return false; // Point is invalid
                    }
                }
            }

            // No neighbors are within the minDistance
            return true; // Valid point
        }

        // Generate spawn points
        for (int i = 0; i < spawnPoints; i++)
        {
            bool found = false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Calculate a random point within the volume of a cylinder 
                float angle = Random.Range(0f, 2.0f * Mathf.PI); // random angle between 0 & 360 deg
                float r = radius * Mathf.Sqrt(Random.Range(0f, 1.0f)); // random radius sqrt for uniform distribution on circle 

                // Convert polar xz coords to cartesian
                float x = r * Mathf.Cos(angle);
                float z = r * Mathf.Sin(angle);

                float y = Random.Range(-height / 2.0f, height / 2.0f); // random height 

                Vector3 candidatePoint = new Vector3(x, y, z);

                if (IsValid(candidatePoint))
                {
                    points.Add(candidatePoint);
                    Vector3Int coord = GetGridCoordinate(candidatePoint);
                    grid[coord.x, coord.y, coord.z] = points.Count - 1; // store point index
                    found = true;
                    break;
                }
            }

            if (!found) // FALLBACK
            {
                /* This case should only be triggered if we use some ridiculous quantity of objects to spawn such that
                 * the volume of the cylinder is saturated, or if we use a super large minDistance that we cant fit all of the 
                 * points inside the cylinder volume. It theoretically should never be triggered when generating points for 
                 * our scenarios but just in case
                 */

                // Generates a point without a min distance check
                float angle = Random.Range(0f, 2.0f * Mathf.PI);
                float r = radius * Mathf.Sqrt(Random.Range(0f, 1.0f));
                float x = r * Mathf.Cos(angle);
                float z = r * Mathf.Sin(angle);
                float y = Random.Range(-height / 2.0f, height / 2.0f);
                points.Add(new Vector3(x, y, z));
            }
        }
        return points;
    }
}
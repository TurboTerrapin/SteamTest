/*
    EffectsHandler.cs
    - Handles various game effects
    Contributor(s): Henryk Musial, Jake Schott
    Last Updated: 2/24/2026
*/

using UnityEngine;

public class EffectsHandler : MonoBehaviour
{
    //CLASS CONSTANTS
    private static int CONE_SEGMENTS = 32;

    //precomputed angles for mesh generation
    private float[] sin_angles;
    private float[] cos_angles;
    private float half_angle_rad;

    //preallocated arrays for vertex and triangle data
    private Vector3[] vertices;
    private Vector2[] uvs;
    private int[] triangles;

    public void drawConeMesh(Mesh cone_mesh, float range)
    {
        cone_mesh.Clear();

        float radius = range * Mathf.Tan(half_angle_rad);
        int total_vertices = 1 + CONE_SEGMENTS + 1;

        vertices[0] = Vector3.zero;

        // Build base
        for (int i = 0; i < CONE_SEGMENTS; i++)
        {
            vertices[1 + i] = new Vector3(cos_angles[i] * radius, sin_angles[i] * radius, range);
        }

        vertices[total_vertices - 1] = new Vector3(0.0f, 0.0f, range);

        cone_mesh.vertices = vertices;
        cone_mesh.uv = uvs;
        cone_mesh.triangles = triangles;
        cone_mesh.RecalculateNormals();
        cone_mesh.RecalculateBounds();
    }
}
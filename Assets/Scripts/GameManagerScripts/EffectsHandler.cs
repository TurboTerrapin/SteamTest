/*
    EffectsHandler.cs
    - Handles various game effects
    Contributor(s): Henryk Musial, Jake Schott
    Last Updated: 3/21/2026
*/

using Unity.Netcode;
using UnityEngine;

public class EffectsHandler : MonoBehaviour
{
    //CLASS CONSTANTS
    private static int CONE_SEGMENTS = 32;

    public GameObject explosion_prefab;

    //precomputed angles for mesh generation
    private float[] sin_angles;
    private float[] cos_angles;

    //preallocated arrays for vertex and triangle data
    private Vector3[] vertices;
    private Vector2[] uvs;
    private int[] triangles;

    private void Awake()
    {
        initializeConeValues();
    }

    private void initializeConeValues()
    {
        //apex + base circle vertices + base center
        int totalVertices = 1 + CONE_SEGMENTS + 1;

        //vertex data
        vertices = new Vector3[totalVertices]; //3D positions
        uvs = new Vector2[totalVertices]; //tex uvs
        triangles = new int[CONE_SEGMENTS * 2 * 3];

        //sin/cos angles
        sin_angles = new float[CONE_SEGMENTS];
        cos_angles = new float[CONE_SEGMENTS];

        uvs[0] = new Vector2(0.5f, 0f); //apex at origin
        uvs[totalVertices - 1] = new Vector2(0.5f, 1f); //base center vertex

        //base circle UVs
        for (int i = 0; i < CONE_SEGMENTS; i++)
        {
            float angle = (float)i / CONE_SEGMENTS * Mathf.PI * 2f;

            //cache trig values
            sin_angles[i] = Mathf.Sin(angle);
            cos_angles[i] = Mathf.Cos(angle);
            uvs[1 + i] = new Vector2((cos_angles[i] + 1f) * 0.5f, 1f);
        }

        //cone body tris
        for (int i = 0; i < CONE_SEGMENTS; i++)
        {
            int baseIndex = i * 3; //3 vertices per triangle, 1 triangle per cone seg
            triangles[baseIndex] = 0; //v1
            triangles[baseIndex + 1] = 1 + i; //v2
            triangles[baseIndex + 2] = 1 + (i + 1) % CONE_SEGMENTS; //v3
        }

        //base circle tris
        int baseTriStart = CONE_SEGMENTS * 3; //3 vertices per triangle, 1 triangle per cone seg
        for (int i = 0; i < CONE_SEGMENTS; i++)
        {
            int baseIndex = baseTriStart + i * 3;
            triangles[baseIndex] = totalVertices - 1; //v1
            triangles[baseIndex + 1] = 1 + (i + 1) % CONE_SEGMENTS; //v2
            triangles[baseIndex + 2] = 1 + i; //v3
        }

        //flip normals
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int temp = triangles[i + 1];
            triangles[i + 1] = triangles[i + 2];
            triangles[i + 2] = temp;
        }
    }

    public void drawConeMesh(Mesh cone_mesh, float range, float half_angle)
    {
        cone_mesh.Clear();

        float radius = range * Mathf.Tan(half_angle * Mathf.Deg2Rad);
        int total_vertices = 1 + CONE_SEGMENTS + 1;

        vertices[0] = Vector3.zero;

        //build base
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

    public void initializeConeGameObject(Transform cone_origin, Material cone_material)
    {
        //add mesh components to the empty beam object
        MeshFilter mesh_filter = cone_origin.gameObject.AddComponent<MeshFilter>();
        MeshRenderer mesh_renderer = cone_origin.gameObject.AddComponent<MeshRenderer>();
        
        //set mesh
        Mesh cone_mesh = new Mesh();
        cone_mesh.name = name;
        mesh_filter.mesh = cone_mesh;

        //set material and info
        mesh_renderer.material = cone_material;
        mesh_renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mesh_renderer.receiveShadows = false;
    }

    private GameObject spawnExplosion(Vector3 location)
    {
        GameObject world_root = GameObject.FindGameObjectWithTag("WorldRoot");
        if (world_root == null)
        {
            return null;
        }
        GameObject e = GameObject.Instantiate(explosion_prefab);
        e.transform.position = location;
        e.GetComponent<NetworkObject>().SpawnWithOwnership(0, true);
        e.GetComponent<NetworkObject>().TrySetParent(world_root);
        e.GetComponent<Collider>().excludeLayers = LayerMask.GetMask("None");
        return e;
    }

    public void createExplosion(Vector3 location)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        GameObject e = spawnExplosion(location);
        if (e != null)
        {
            e.GetComponent<Explosion>().transmitExplosionRPC();
        }
    }

    public void createExplosion(Vector3 location, float size)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        GameObject e = spawnExplosion(location);
        if (e != null)
        {
            e.GetComponent<Explosion>().transmitExplosionRPC(size);
        }
    }

    public void createExplosion(Vector3 location, float size, Color explosion_color)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        GameObject e = spawnExplosion(location);
        if (e != null)
        {
            e.GetComponent<Explosion>().transmitExplosionRPC(size, explosion_color);
        }
    }

    public void createExplosion(Vector3 location, float size, Color base_color, Color accent_color)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        GameObject e = spawnExplosion(location);
        if (e != null)
        {
            e.GetComponent<Explosion>().transmitExplosionRPC(size, base_color, accent_color);
        }
    }
}
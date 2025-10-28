using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class DrillMeshFractureDestroyFixed : MonoBehaviour
{
    [Header("Deformation")]
    [SerializeField] float deformRadius = 2.5f;
    [SerializeField] float deformStrength = 0.1f;
    [SerializeField] float noiseScale = 0f;
    [SerializeField] bool updateCollider = false;
    [SerializeField] float colliderUpdateDelay = 0.5f;

    [Header("Fracture")]
    [SerializeField] float fractureThreshold = 0.4f;
    [SerializeField] float chunkSize = 0.04f;
    [SerializeField] float chunkForce = 6f;
    [SerializeField] int maxChunksPerHit = 3;
    [SerializeField] GameObject gibPrefab;
    [SerializeField] float destroyPercent = 0.8f;

    MeshFilter filter;
    MeshCollider col;
    Mesh mesh;
    Vector3[] verts;
    Vector3[] normals;
    Vector3[] originalVerts;
    int[] tris;

    HashSet<int> fracturedVerts = new HashSet<int>();
    float colliderTimer;

    void Start()
    {
        filter = GetComponent<MeshFilter>();
        col = GetComponent<MeshCollider>();
        mesh = filter.mesh;

        verts = mesh.vertices;
        normals = mesh.normals;
        originalVerts = mesh.vertices;
        tris = mesh.triangles;
    }

    void OnCollisionStay(Collision collision)
    {
        foreach (var contact in collision.contacts)
            Deform(contact.point, contact.normal);
    }

    void Deform(Vector3 worldPoint, Vector3 normal)
    {
        Vector3 local = transform.InverseTransformPoint(worldPoint);
        float rSqr = deformRadius * deformRadius;
        Bounds bounds = new Bounds(local, Vector3.one * deformRadius * 2);

        int chunksSpawned = 0;

        for (int i = 0; i < verts.Length; i++)
        {
            if (fracturedVerts.Contains(i)) continue;
            Vector3 v = verts[i];
            if (!bounds.Contains(v)) continue;

            float distSqr = (v - local).sqrMagnitude;
            if (distSqr > rSqr) continue;

            float dist = Mathf.Sqrt(distSqr);
            float t = dist / deformRadius;
            float influence = 1f - (t * t * (3f - 2f * t));
            float noise = noiseScale > 0 ? Mathf.PerlinNoise(v.x * noiseScale, v.y * noiseScale) : 1f;

            Vector3 dir = -normals[i];
            verts[i] += dir * deformStrength * influence * noise;

            if ((verts[i] - originalVerts[i]).sqrMagnitude > fractureThreshold * fractureThreshold
                && chunksSpawned < maxChunksPerHit)
            {
                fracturedVerts.Add(i);
                SpawnChunk(i, worldPoint, normal);
                chunksSpawned++;
            }
        }

        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (fracturedVerts.Count > 0)
            RemoveBrokenTriangles();

        colliderTimer += Time.deltaTime;
        if (updateCollider && colliderTimer >= colliderUpdateDelay)
        {
            colliderTimer = 0;
            col.sharedMesh = null;
            col.sharedMesh = mesh;
        }

        if ((float)fracturedVerts.Count / verts.Length > destroyPercent)
            DestroyCompletely();
    }

    void RemoveBrokenTriangles()
    {
        List<int> newTris = new List<int>(tris.Length);

        for (int i = 0; i < tris.Length; i += 3)
        {
            int a = tris[i];
            int b = tris[i + 1];
            int c = tris[i + 2];

            if (fracturedVerts.Contains(a) || fracturedVerts.Contains(b) || fracturedVerts.Contains(c))
                continue; // skip triangle that uses a broken vertex

            newTris.Add(a);
            newTris.Add(b);
            newTris.Add(c);
        }

        mesh.triangles = newTris.ToArray();
        mesh.RecalculateBounds();
    }

    void SpawnChunk(int i, Vector3 point, Vector3 normal)
    {
        if (gibPrefab == null || Random.value < 0.9f) return;

        GameObject c = Instantiate(gibPrefab);
        c.transform.position = transform.TransformPoint(verts[i]);
        c.transform.localScale = gibPrefab.transform.localScale * chunkSize;

        Rigidbody rb = c.AddComponent<Rigidbody>();
        rb.AddForce((verts[i].normalized + normal) * chunkForce, ForceMode.Impulse);

        Destroy(c.GetComponent<Collider>(), 5f);
        Destroy(c, 10f);
    }

    void DestroyCompletely()
    {
        for (int i = 0; i < 10; i++)
        {
            int randomIndex = Random.Range(0, verts.Length);
            if (!fracturedVerts.Contains(randomIndex))
                SpawnChunk(randomIndex, transform.position, Vector3.up);
        }

        Destroy(gameObject, 0.1f);
    }
}

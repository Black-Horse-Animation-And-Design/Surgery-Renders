using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class DrillMeshFractureFast : MonoBehaviour
{
    [Header("Deformation")]
    [SerializeField] float deformRadius = 0.2f;
    [SerializeField] float deformStrength = 0.8f;
    [SerializeField] float noiseScale = 5f;
    [SerializeField] bool updateCollider = false;
    [SerializeField] float colliderUpdateDelay = 0.5f;

    [Header("Fracture")]
    [SerializeField] float fractureThreshold = 0.4f;
    [SerializeField] float chunkSize = 0.04f;
    [SerializeField] float chunkForce = 6f;
    [SerializeField] int maxChunksPerHit = 3;

    MeshFilter filter;
    MeshCollider col;
    Mesh mesh;
    Vector3[] verts;
    Vector3[] normals;
    Vector3[] originalVerts;
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

        // quick spatial filter
        Bounds bounds = new Bounds(local, Vector3.one * deformRadius * 2);

        int chunksSpawned = 0;

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 v = verts[i];
            if (!bounds.Contains(v)) continue;

            float distSqr = (v - local).sqrMagnitude;
            if (distSqr > rSqr) continue;

            float dist = Mathf.Sqrt(distSqr);
            float t = dist / deformRadius;
            float influence = 1f - (t * t * (3f - 2f * t));
            float noise = Mathf.PerlinNoise(v.x * noiseScale, v.y * noiseScale);

            Vector3 dir = -normals[i];
            verts[i] += dir * deformStrength * influence * noise;

            if ((verts[i] - originalVerts[i]).sqrMagnitude > fractureThreshold * fractureThreshold
                && !fracturedVerts.Contains(i) && chunksSpawned < maxChunksPerHit)
            {
                SpawnChunk(i, worldPoint, normal);
                fracturedVerts.Add(i);
                chunksSpawned++;
            }
        }

        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // only update collider occasionally
        colliderTimer += Time.deltaTime;
        if (Random.Range(0f, 1f) > 0.8f)
        {
            if (updateCollider && colliderTimer >= colliderUpdateDelay)
            {
                colliderTimer = 0;
                col.sharedMesh = null;
                col.sharedMesh = mesh;
            }
        }
    }

    void SpawnChunk(int i, Vector3 point, Vector3 normal)
    {

        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        c.transform.position = transform.TransformPoint(verts[i]);
        c.transform.localScale = Vector3.one * chunkSize;
        Rigidbody rb = c.AddComponent<Rigidbody>();
        rb.AddForce((verts[i].normalized + normal) * chunkForce, ForceMode.Impulse);
        Destroy(c.GetComponent<Collider>(), 5f);
        Destroy(c, 10f);
    }
}

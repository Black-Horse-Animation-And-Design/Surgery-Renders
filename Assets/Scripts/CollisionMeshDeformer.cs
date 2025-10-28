using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class DrillMeshFractureFast : MonoBehaviour
{
    [Header("Deformation")]
    [SerializeField] float deformRadius = 2.5f;
    [SerializeField] float deformStrength = 0.2f;
    [SerializeField] float noiseScale = 0.2f;
    [SerializeField] bool updateCollider = false;
    [SerializeField] float colliderUpdateDelay = 0.5f;

    [Header("Fracture")]
    [SerializeField] float fractureThreshold = 0.2f;
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
    [SerializeField] GameObject gib;

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

        int chunksSpawned = 0;

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 v = verts[i];
            float distSqr = (v - local).sqrMagnitude;
            if (distSqr > rSqr) continue;

            // instant displacement instead of smooth falloff
            float noise = noiseScale > 0 ? Mathf.PerlinNoise(v.x * noiseScale, v.y * noiseScale) : 1f;
            Vector3 dir = -normals[i];

            // make it sharp and strong
            verts[i] += dir * deformStrength * (0.75f + Random.value * 0.5f) * noise;

            // fracture check
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

        colliderTimer += Time.deltaTime;
        if (updateCollider && colliderTimer >= colliderUpdateDelay)
        {
            colliderTimer = 0;
            col.sharedMesh = null;
            col.sharedMesh = mesh;
        }
    }

    void SpawnChunk(int i, Vector3 point, Vector3 normal)
    {
        if (gib == null) return;

        GameObject c = Instantiate(gib);
        c.transform.position = transform.TransformPoint(verts[i]);
        c.transform.localScale = gib.transform.localScale * chunkSize;

        Rigidbody rb = c.AddComponent<Rigidbody>();
        Vector3 dir = (verts[i].normalized + normal).normalized;
        rb.AddForce(dir * chunkForce, ForceMode.Impulse);

        Destroy(c.GetComponent<Collider>(), 5f);
        Destroy(c, 10f);
    }
}

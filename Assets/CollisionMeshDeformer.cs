using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class DrillMeshFracture : MonoBehaviour
{
    [Header("Deformation Settings")]
    [SerializeField] float deformRadius = 1f;
    [SerializeField] float deformStrength = 10f;
    [SerializeField] float noiseScale = 10f;
    [SerializeField] bool autoRecalculateNormals = true;

    [Header("Fracture Settings")]
    [SerializeField] float fractureThreshold = 1.5f;
    [SerializeField] float chunkSize = 0.05f;
    [SerializeField] float chunkForce = 5f;

    [Header("Drill Reference")]
    [SerializeField] Transform drill;
    [SerializeField] Collider drillCollider;

    Mesh _mesh;
    Vector3[] _originalVerts;
    Vector3[] _deformedVerts;
    Vector3[] _originalNormals;
    MeshCollider _meshCollider;

    // Track which vertices already fractured
    HashSet<int> fracturedVerts = new HashSet<int>();

    void Awake()
    {
        _mesh = GetComponent<MeshFilter>().mesh;
        _originalVerts = _mesh.vertices;
        _originalNormals = _mesh.normals;
        _deformedVerts = new Vector3[_originalVerts.Length];
        _originalVerts.CopyTo(_deformedVerts, 0);

        _meshCollider = GetComponent<MeshCollider>();
        _meshCollider.sharedMesh = _mesh;

        if (!drillCollider && drill != null)
            drillCollider = drill.GetComponent<Collider>();
    }

    void Update()
    {
        if (drill == null || drillCollider == null) return;

        Collider[] hits = Physics.OverlapSphere(drill.position, deformRadius);
        bool touching = false;
        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject)
            {
                touching = true;
                break;
            }
        }

        if (touching)
        {
            DeformAroundPoint(drill.position);
        }
    }

    void DeformAroundPoint(Vector3 worldPoint)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

        for (int i = 0; i < _deformedVerts.Length; i++)
        {
            Vector3 v = _deformedVerts[i];
            float dist = Vector3.Distance(v, localPoint);
            if (dist > deformRadius) continue;

            float t = dist / deformRadius;
            float influence = 1f - (t * t * (3f - 2f * t));
            float noise = Mathf.PerlinNoise(v.x * noiseScale, v.y * noiseScale) * 0.5f + 0.5f;

            Vector3 dir = -_originalNormals[i];
            _deformedVerts[i] += dir * deformStrength * influence * noise;

            // Check for fracture
            if ((_deformedVerts[i] - _originalVerts[i]).magnitude > fractureThreshold && !fracturedVerts.Contains(i))
            {
                SpawnChunk(i, worldPoint);
                fracturedVerts.Add(i); // mark vertex as fractured
            }
        }

        _mesh.vertices = _deformedVerts;
        if (autoRecalculateNormals)
            _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _meshCollider.sharedMesh = null;
        _meshCollider.sharedMesh = _mesh;
    }

    void SpawnChunk(int vertexIndex, Vector3 drillWorldPos)
    {
        GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chunk.transform.position = transform.TransformPoint(_deformedVerts[vertexIndex]);
        chunk.transform.localScale = Vector3.one * chunkSize;

        Rigidbody rb = chunk.AddComponent<Rigidbody>();

        Vector3 forceDir = (chunk.transform.position - drillWorldPos).normalized;
        rb.AddForce(forceDir * chunkForce, ForceMode.Impulse);

        Destroy(chunk.GetComponent<Collider>(), 5f);
    }
}

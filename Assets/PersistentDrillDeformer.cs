using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class PersistentDrillDeformer : MonoBehaviour
{
    public Transform drill;
    public float drillRadius = 0.1f;
    public float drillDepth = 0.02f;
    public float noiseScale = 25f;

    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] displacedVertices;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        baseVertices = mesh.vertices;
        displacedVertices = new Vector3[baseVertices.Length];
        baseVertices.CopyTo(displacedVertices, 0);
    }

    void Update()
    {
        if (!drill) return;

        Vector3 drillLocal = transform.InverseTransformPoint(drill.position);

        for (int i = 0; i < displacedVertices.Length; i++)
        {
            Vector3 vertex = displacedVertices[i];

            float dist = Vector3.Distance(vertex, drillLocal);
            if (dist < drillRadius)
            {
                float influence = 1f - (dist / drillRadius);

                // Add a little pseudo-random noise per vertex
                float noise = Mathf.PerlinNoise(vertex.x * noiseScale, vertex.y * noiseScale);

                displacedVertices[i] -= mesh.normals[i] * drillDepth * influence * noise;
            }
        }

        mesh.vertices = displacedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}

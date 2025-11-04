using System.Linq;
using UnityEngine;

public class BurrSpin : MonoBehaviour
{
    [SerializeField] float spinStrength = 5f;
    [SerializeField] float timeToFullSpeed = 1f;
    [SerializeField] MeshFilter targetMesh;
    [SerializeField] float triggerDistance = 2f;
    [SerializeField] int nearestVerticesCount = 1000;

    float currentSpeed;
    bool spinning;
    Rigidbody rb;
    Vector3[] nearestVertices;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = 5000;

        if (targetMesh != null)
        {
            Transform t = targetMesh.transform;
            Vector3[] vertices = targetMesh.sharedMesh.vertices;

            nearestVertices = vertices
                .OrderBy(v => Vector3.Distance(t.TransformPoint(v), transform.position))
                .Take(nearestVerticesCount)
                .ToArray();

            Mesh fakeMesh = new Mesh();
            fakeMesh.vertices = nearestVertices;

            int[] tris = new int[(nearestVertices.Length - 2) * 3];
            for (int i = 0; i < nearestVertices.Length - 2; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }

            fakeMesh.triangles = tris;
            fakeMesh.RecalculateNormals();

            MeshCollider targetCollider = targetMesh.gameObject.GetComponent<MeshCollider>();
            if (targetCollider == null)
                targetCollider = targetMesh.gameObject.AddComponent<MeshCollider>();

            targetCollider.sharedMesh = fakeMesh;
            targetCollider.convex = false; // non-convex
        }
    }

    void FixedUpdate()
    {

        spinning = false;
        Transform t = targetMesh.transform;
        foreach (Vector3 v in nearestVertices)
        {
            if (Vector3.Distance(transform.position, t.TransformPoint(v)) <= triggerDistance)
            {
                spinning = true;
                break;
            }
        }


        currentSpeed = Mathf.MoveTowards(currentSpeed, spinning ? spinStrength : 0f, spinStrength / timeToFullSpeed * Time.deltaTime);
        rb.angularVelocity = new Vector3(0, currentSpeed, 0);
    }
}

using UnityEngine;

public class Rotate : MonoBehaviour
{
    [SerializeField] float rotateSpeed = 1f;
    [SerializeField] float maxRotations = 3;
    float currentRotation;
    void FixedUpdate()
    {
        if (currentRotation / 360 > maxRotations) return;
        transform.Rotate(Vector3.up, rotateSpeed * Time.fixedDeltaTime);
        currentRotation += rotateSpeed * Time.fixedDeltaTime;

    }
}

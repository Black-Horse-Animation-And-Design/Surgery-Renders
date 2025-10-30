using UnityEngine;

public class BurrSpin : MonoBehaviour
{
    [SerializeField] float spinStrength = 5;
    private void Start()
    {
        GetComponent<Rigidbody>().AddTorque(Vector3.up * spinStrength);
    }
}

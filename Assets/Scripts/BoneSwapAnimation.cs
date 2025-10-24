using UnityEngine;

public class VisiblitySwapAnimation : MonoBehaviour
{
    [SerializeField] GameObject[] objectsToToggleOn, objectsToToggleOff;
    public void ToggleObjects(int index)
    {
        objectsToToggleOn[index].SetActive(true);
        objectsToToggleOff[index].SetActive(false);
    }
}

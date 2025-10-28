using UnityEngine;

public class VisiblitySwapAnimation : MonoBehaviour
{
    [SerializeField] GameObject[] objectsToToggle;
    int index = 0;

    [SerializeField] bool stops;

    public void ToggleObjects()
    {
        if (index >= objectsToToggle.Length) return;
        objectsToToggle[index].SetActive(false);
        index++;
        objectsToToggle[index].SetActive(true);
    }
}

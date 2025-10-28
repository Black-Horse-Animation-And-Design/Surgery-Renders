using UnityEngine;

public class VisiblitySwapAnimation : MonoBehaviour
{
    [SerializeField] GameObject[] objectsToToggle;
    int index;
    public void ToggleObjects()
    {
        if (index >= objectsToToggle.Length - 1) return;
        objectsToToggle[index].SetActive(false);
        index++;
        objectsToToggle[index].SetActive(true);
    }
}

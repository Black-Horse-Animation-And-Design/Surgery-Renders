using UnityEngine;

public class VisiblitySwapAnimation : MonoBehaviour
{
    [SerializeField] GameObject[] objectsToToggle;
    int index;

    [SerializeField] bool stops;

    public void ToggleObjects()
    {

        objectsToToggle[index].SetActive(false);
        index++;
        objectsToToggle[index].SetActive(true);
    }
}

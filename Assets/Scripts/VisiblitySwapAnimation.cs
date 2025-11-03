using UnityEngine;

public class VisiblitySwapAnimation : MonoBehaviour
{
    [SerializeField] GameObject[] objectsToToggle;
    int index = 0;

    [SerializeField] bool stops;

    private void Start()
    {
        objectsToToggle[0].SetActive(true);
        for (int i = 1; i < objectsToToggle.Length; i++)
        {
            objectsToToggle[i].SetActive(false);
        }
    }

    public void ToggleObjects()
    {
        if (index >= objectsToToggle.Length) return;
        objectsToToggle[index].SetActive(false);
        index++;
        objectsToToggle[index].SetActive(true);
    }
}

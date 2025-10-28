using UnityEditor;
using UnityEngine;

public class VisiblitySwapAnimation : MonoBehaviour
{
    [SerializeField] GameObject[] objectsToToggle;
    int index = -1;

    [SerializeField] bool stops;

    public void ToggleObjects()
    {
        if (index >= objectsToToggle.Length - 1 && stops) EditorApplication.isPlaying = false;
        if (index > 0) objectsToToggle[index].SetActive(false);
        index++;
        objectsToToggle[index].SetActive(true);
    }
}

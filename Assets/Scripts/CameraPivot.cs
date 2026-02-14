using UnityEngine;

public class CameraPivot : MonoBehaviour
{
    [SerializeField] float speed = 1;
    private void Awake()
    {
        Time.timeScale = speed;
    }

    public void OnApplicationQuit()
    {
        Application.Quit();
    }
}

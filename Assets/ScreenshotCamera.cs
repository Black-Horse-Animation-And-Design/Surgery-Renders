using System.IO;
using UnityEngine;

public class ScreenshotCamera : MonoBehaviour
{
    Camera cam;

    [Header("Screenshot Settings")]
    [SerializeField] Vector2Int screenshotSize = new Vector2Int(1920, 1080);
    [SerializeField] string screenshotName = "Screenshot";

    [Header("Movement Settings")]
    [SerializeField] Transform pivot;
    [SerializeField] bool rotates, moves;
    [SerializeField] float rotateSpeed = .2f, moveSpeed = .5f;
    [SerializeField] float mouseSensitivity = 100f;
    [SerializeField] bool lockCursor = true;

    Vector3 input;
    Vector3 direction;

    float yaw;
    float pitch;

    private void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    private void Update()
    {
        HandleMovementInput();
        HandleMouseLook();
    }

    private void FixedUpdate()
    {
        if (rotates && pivot != null)
            pivot.Rotate(Vector3.up * rotateSpeed);

        if (moves)
            transform.position += input * Time.deltaTime * moveSpeed;
    }

    void HandleMovementInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        input = move.normalized;
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    public void TakeScreenshot()
    {
        cam = GetComponent<Camera>();
        if (cam == null) return;

        RenderTexture rt = new RenderTexture(screenshotSize.x, screenshotSize.y, 24);
        cam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(screenshotSize.x, screenshotSize.y, TextureFormat.RGB24, false);
        cam.Render();

        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, screenshotSize.x, screenshotSize.y), 0, 0);
        screenShot.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        byte[] bytes = screenShot.EncodeToPNG();
        string filename = "Assets/" + screenshotName;

        if (File.Exists(filename + ".png"))
        {
            for (int i = 1; i < 1000; i++)
            {
                if (!File.Exists(filename + i + ".png"))
                {
                    filename += i;
                    break;
                }
            }
        }

        filename += ".png";
        File.WriteAllBytes(filename, bytes);
        Debug.Log("Saved screenshot: " + filename);
    }
}

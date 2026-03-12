using System.Collections;
using System.IO;
using UnityEngine;

public class OfflineRenderer : MonoBehaviour
{
    public int framesToRender = 300;
    public float secondsToConverge = 1f;
    public int targetFPS = 30;
    public int width = 1920;
    public int height = 1080;
    public string folder = "OfflineFrames";

    int frameIndex;
    Texture2D tex;

    IEnumerator Start()
    {
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(secondsToConverge);
        while (frameIndex < framesToRender)
        {
            Debug.Log("Converging frame " + frameIndex);

            yield return new WaitForSecondsRealtime(secondsToConverge);
            yield return new WaitForEndOfFrame();

            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            string path = folder + "/frame_" + frameIndex.ToString("D4") + ".png";
            File.WriteAllBytes(path, bytes);

            frameIndex++;

            yield return StartCoroutine(AdvanceOneFrame());
        }

        Debug.Log("Render complete");
    }

    IEnumerator AdvanceOneFrame()
    {
        Time.timeScale = 1f;

        yield return new WaitForSeconds(1f / targetFPS);

        Time.timeScale = 0f;
    }
}
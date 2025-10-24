using UnityEngine;

public class DrillDamagePainter : MonoBehaviour
{
    public Material targetMaterial;
    public Transform drill;
    public float brushSize = 0.05f;
    public float drawStrength = 0.5f;
    public int textureSize = 512;

    private RenderTexture damageMap;
    private Material drawMat;

    void Start()
    {
        damageMap = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.R8);
        damageMap.Create();

        targetMaterial.SetTexture("_DamageMap", damageMap);

        drawMat = new Material(Shader.Find("Hidden/DamageDraw"));
    }

    void Update()
    {
        if (!drill || !targetMaterial) return;


        Vector3 localPos = transform.InverseTransformPoint(drill.position);
        Vector2 uv = new Vector2(localPos.x + 0.5f, localPos.z + 0.5f);

        drawMat.SetVector("_BrushPos", new Vector4(uv.x, uv.y, brushSize, drawStrength));

        RenderTexture temp = RenderTexture.GetTemporary(damageMap.width, damageMap.height, 0, RenderTextureFormat.R8);
        Graphics.Blit(damageMap, temp);
        Graphics.Blit(temp, damageMap, drawMat);
        RenderTexture.ReleaseTemporary(temp);
    }
}

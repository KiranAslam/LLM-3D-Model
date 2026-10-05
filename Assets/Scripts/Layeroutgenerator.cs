using UnityEngine;
using TMPro;

[ExecuteAlways]
public class LayerOutGenerator : MonoBehaviour
{
    [Header("Mirror these — ConcatGenerator ki Inspector values")]
    public Vector3 concatPosition = new Vector3(220f, 18f, 0f);
    public float concatSheetWidth = 14f;

    [Header("W_O sheet")]
    public Vector3 woPosition = new Vector3(245f, 18f, 0f);
    public float woWidth = 12f;
    public float woHeight = 12f;
    public float woThickness = 0.3f;

    [Header("W_O texture (grey shades)")]
    public int textureSize = 256;
    public int gridColumns = 16;
    public int gridRows = 20;
    public float grainStrength = 0.12f;
    public float gridLineDarken = 0.4f;
    public Color colorA = new Color(0.55f, 0.55f, 0.55f);
    public Color colorB = new Color(0.25f, 0.25f, 0.25f);
    public Color colorC = new Color(0.85f, 0.85f, 0.85f);

    [Header("Layer Out sheet (transparent, QK^T jaisa)")]
    public Vector3 layerOutPosition = new Vector3(270f, 18f, 0f);
    public Vector3 layerOutSize = new Vector3(10f, 8f, 1f);
    public Color layerOutColor = new Color(0.6f, 0.9f, 0.85f, 0.35f);

    [Header("Lines")]
    public Material lineMaterial;
    public Color lineColor = Color.white;
    public float lineWidth = 0.2f;

    [Header("Labels — sirf sheet ke upar, texture ke andar nahi")]
    public string woLabel = "W_O   [960x960]";
    public string layerOutLabel = "Layer Out   [5x960]";
    public float labelFontSize = 10f;
    public Color labelColor = Color.black;
    public Vector3 woLabelOffset = new Vector3(0f, 7f, 0f);
    public Vector3 layerOutLabelOffset = new Vector3(0f, 5f, 0f);

    [Header("Text thickness")]
    public float textThickness = 0.4f;

    private void Start()
    {
        if (Application.isPlaying)
            Rebuild();
    }

    public void Rebuild()
    {
        CancelInvoke(nameof(Rebuild));
        Clear();

        BuildWO();
        BuildLayerOut();

        Vector3 concatExit =
            concatPosition +
            Vector3.right * (concatSheetWidth / 2f);

        Vector3 woLeftEdge =
            woPosition -
            Vector3.right * (woWidth / 2f);

        Vector3 woRightEdge =
            woPosition +
            Vector3.right * (woWidth / 2f);

        Vector3 layerOutLeftEdge =
            layerOutPosition -
            Vector3.right * (layerOutSize.x / 2f);

        DrawLine(concatExit, woLeftEdge);
        DrawLine(woRightEdge, layerOutLeftEdge);

        CreateLabel(
            woLabel,
            woPosition + woLabelOffset
        );

        CreateLabel(
            layerOutLabel,
            layerOutPosition + layerOutLabelOffset
        );
    }

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        CancelInvoke(nameof(Rebuild));
        Invoke(nameof(Rebuild), 0.05f);
    }

    private void BuildWO()
    {
        GameObject slab = GameObject.CreatePrimitive(
            PrimitiveType.Cube
        );

        slab.name = "WO_Sheet";
        slab.transform.SetParent(transform, false);
        slab.transform.localPosition = woPosition;
        slab.transform.localScale = new Vector3(
            woWidth,
            woHeight,
            woThickness
        );

        Material mat = new Material(GetCompatibleShader());
        mat.mainTexture = GenerateGreyGridTexture();

        Renderer renderer = slab.GetComponent<Renderer>();

        if (renderer != null)
            renderer.material = mat;
    }

    private void BuildLayerOut()
    {
        GameObject slab = GameObject.CreatePrimitive(
            PrimitiveType.Cube
        );

        slab.name = "LayerOut_Sheet";
        slab.transform.SetParent(transform, false);
        slab.transform.localPosition = layerOutPosition;
        slab.transform.localScale = layerOutSize;

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
            return;

        Material mat = new Material(shader);
        mat.color = layerOutColor;

        Renderer renderer = slab.GetComponent<Renderer>();

        if (renderer != null)
            renderer.material = mat;
    }

    private Shader GetCompatibleShader()
    {
        Shader shader = Shader.Find(
            "Universal Render Pipeline/Unlit"
        );

        if (shader != null)
            return shader;

        shader = Shader.Find("Unlit/Texture");

        if (shader != null)
            return shader;

        shader = Shader.Find("Standard");

        if (shader != null)
            return shader;

        return Shader.Find("Sprites/Default");
    }

    private Texture2D GenerateGreyGridTexture()
    {
        Texture2D tex = new Texture2D(
            textureSize,
            textureSize
        );

        tex.filterMode = FilterMode.Point;

        int cellWidth = Mathf.Max(
            1,
            textureSize / gridColumns
        );

        int cellHeight = Mathf.Max(
            1,
            textureSize / gridRows
        );

        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                int cellX = x / cellWidth;
                int cellY = y / cellHeight;

                float cellNoise = Mathf.PerlinNoise(
                    cellX * 0.35f,
                    cellY * 0.35f
                );

                Color baseColor;

                if (cellNoise > 0.66f)
                    baseColor = colorA;
                else if (cellNoise > 0.33f)
                    baseColor = colorB;
                else
                    baseColor = colorC;

                float grain = (
                    Mathf.PerlinNoise(
                        x * 0.6f,
                        y * 0.6f
                    ) - 0.5f
                ) * grainStrength;

                Color c = baseColor + new Color(
                    grain,
                    grain,
                    grain,
                    0f
                );

                bool isGridLine =
                    (x % cellWidth == 0) ||
                    (y % cellHeight == 0);

                if (isGridLine)
                {
                    c = Color.Lerp(
                        c,
                        Color.black,
                        gridLineDarken
                    );
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return tex;
    }

    private void DrawLine(Vector3 from, Vector3 to)
    {
        GameObject lineObj = new GameObject(
            "LayerOut_Line"
        );

        lineObj.transform.SetParent(
            transform,
            false
        );

        EnsureLineMaterial();

        LineRenderer lr =
            lineObj.AddComponent<LineRenderer>();

        lr.material = lineMaterial;
        lr.positionCount = 2;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        lr.SetPosition(
            0,
            transform.TransformPoint(from)
        );

        lr.SetPosition(
            1,
            transform.TransformPoint(to)
        );

        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.useWorldSpace = true;
    }

    private void EnsureLineMaterial()
    {
        if (lineMaterial != null)
            return;

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
            return;

        lineMaterial = new Material(shader);
    }

    private void CreateLabel(string text, Vector3 localPos)
    {
        GameObject labelObj = new GameObject(
            $"Label_{text}"
        );

        labelObj.transform.SetParent(
            transform,
            false
        );

        labelObj.transform.localPosition = localPos;

        TextMeshPro tmp =
            labelObj.AddComponent<TextMeshPro>();

        tmp.text = text;
        tmp.fontSize = labelFontSize;
        tmp.color = labelColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Midline;

        ApplyThickness(tmp);
    }

    private void ApplyThickness(TextMeshPro tmp)
    {
        if (tmp == null || tmp.font == null)
            return;

        Material material = tmp.fontMaterial;

        if (material == null)
            return;

        if (material.HasProperty("_FaceDilate"))
            material.SetFloat(
                "_FaceDilate",
                textThickness
            );
    }
}
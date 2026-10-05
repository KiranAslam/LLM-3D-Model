using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[ExecuteAlways]
public class TransposeGenerator : MonoBehaviour
{
    private readonly List<GameObject> slabObjects = new();
    private readonly List<LineRenderer> slabLines = new();
    private bool revealRequested;

    [Header("Mirror these — KeyStack (source) ki Inspector values")]
    public Vector3 keyStackPosition = new Vector3(75f, 3f, 0f);
    public int slabCount = 10;
    public float keySlabGapZ = 0.6f;
    public float keySlabStaggerX = 0.1f;
    public float keySlabStaggerY = 0.15f;
    public float keySheetWidth = 4f;

    [Header("Transpose stack (target — jaan boojh kar different look)")]
    public Vector3 transposeStackPosition = new Vector3(95f, 3f, 0f);
    public float sheetWidth = 3f;
    public float sheetHeight = 5f;
    public float sheetThickness = 0.3f;
    public float slabGapZ = 0.5f;
    public float slabStaggerX = 0.08f;
    public float slabStaggerY = 0.12f;

    [Header("Texture (rows/columns swap + color order different)")]
    public int textureSize = 256;
    public int gridColumns = 20;
    public int gridRows = 16;
    public float grainStrength = 0.12f;
    public float gridLineDarken = 0.4f;
    public Color colorA = new Color(0.13f, 0.85f, 0.63f);
    public Color colorB = new Color(0.11f, 0.45f, 0.4f);
    public Color colorC = Color.white;

    [Header("Line settings")]
    public Material lineMaterial;
    public Color lineColor = new Color(0.15f, 0.7f, 0.55f);
    public float lineWidth = 0.05f;

    [Header("Label")]
    public string titleLabel = "Transpose";
    public string shapeLabel = "K^T";
    public float labelFontSize = 6f;
    public Color labelColor = Color.white;
    public Vector3 labelOffset = new Vector3(0f, 2.5f, 0f);

    [Header("Text thickness")]
    public float textThickness = 0.15f;

    private void Start()
    {
        if (Application.isPlaying && !revealRequested)
            Rebuild();
    }

    public void Rebuild()
    {
        CancelInvoke(nameof(Rebuild));
        Clear();
        BuildTransposeStack();
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

    private void BuildTransposeStack()
    {
        slabObjects.Clear();
        slabLines.Clear();

        Vector3 slabSize = new Vector3(
            sheetWidth,
            sheetHeight,
            sheetThickness
        );

        for (int i = 0; i < slabCount; i++)
        {
            Vector3 sourcePos = keyStackPosition + new Vector3(
                i * keySlabStaggerX,
                i * keySlabStaggerY,
                i * keySlabGapZ
            );

            Vector3 sourceRightEdge = sourcePos + new Vector3(
                keySheetWidth / 2f,
                0f,
                0f
            );

            Vector3 targetPos = transposeStackPosition + new Vector3(
                i * slabStaggerX,
                i * slabStaggerY,
                i * slabGapZ
            );

            Vector3 targetLeftEdge = targetPos - new Vector3(
                sheetWidth / 2f,
                0f,
                0f
            );

            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = $"TransposeSlab_{i}";
            slab.transform.SetParent(transform, false);
            slab.transform.localPosition = targetPos;
            slab.transform.localScale = slabSize;

            Material mat = new Material(GetCompatibleShader());
            mat.mainTexture = GenerateNoiseTextureTransposed();

            Renderer renderer = slab.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = mat;

            LineRenderer line = DrawLine(sourceRightEdge, targetLeftEdge);

            slabObjects.Add(slab);
            slabLines.Add(line);
        }

        GameObject labelObj = new GameObject("Label_Transpose");
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition = transposeStackPosition + labelOffset;

        TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = $"{titleLabel}   {shapeLabel}";
        tmp.fontSize = labelFontSize;
        tmp.color = labelColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Midline;

        ApplyThickness(tmp);
    }

    public IEnumerator AnimateReveal(float slabDelay)
    {
        revealRequested = true;
        CancelInvoke(nameof(Rebuild));
        Clear();
        BuildTransposeStack();

        foreach (GameObject slab in slabObjects)
            slab.SetActive(false);
        foreach (LineRenderer line in slabLines)
            line.enabled = false;

        for (int i = 0; i < slabObjects.Count; i++)
        {
            yield return new WaitForSeconds(slabDelay);
            slabObjects[i].SetActive(true);
            slabLines[i].enabled = true;
        }
    }

    private Shader GetCompatibleShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
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

    private Texture2D GenerateNoiseTextureTransposed()
    {
        Texture2D tex = new Texture2D(textureSize, textureSize);
        tex.filterMode = FilterMode.Point;

        int cellWidth = Mathf.Max(1, textureSize / gridColumns);
        int cellHeight = Mathf.Max(1, textureSize / gridRows);

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
                    baseColor = colorB;
                else if (cellNoise > 0.33f)
                    baseColor = colorA;
                else
                    baseColor = colorC;

                float grain = (
                    Mathf.PerlinNoise(x * 0.6f, y * 0.6f) - 0.5f
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
                    c = Color.Lerp(
                        c,
                        Color.black,
                        gridLineDarken
                    );

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return tex;
    }

    private LineRenderer DrawLine(Vector3 from, Vector3 to)
    {
        GameObject lineObj = new GameObject("Transpose_Line");
        lineObj.transform.SetParent(transform, false);

        EnsureLineMaterial();

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.positionCount = 2;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.SetPosition(0, transform.TransformPoint(from));
        lr.SetPosition(1, transform.TransformPoint(to));
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.useWorldSpace = true;
        return lr;
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

    private void ApplyThickness(TextMeshPro tmp)
    {
        if (tmp == null || tmp.font == null)
            return;

        Material material = tmp.fontMaterial;

        if (material == null)
            return;

        if (material.HasProperty("_FaceDilate"))
            material.SetFloat("_FaceDilate", textThickness);
    }
}
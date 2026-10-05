using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[ExecuteAlways]
public class ConcatGenerator : MonoBehaviour
{
    private readonly List<LineRenderer> concatLines = new();
    private GameObject concatSheet;
    private bool revealRequested;

    [Header("Mirror these — OAVGenerator (O stack) ki Inspector values")]
    public Vector3 oStackPosition = new Vector3(190f, 18f, 0f);
    public int slabCount = 10;
    public float oSlabGapZ = 9f;
    public float oSlabStaggerX = 0.1f;
    public float oSlabStaggerY = 0.1f;
    public float oSheetWidth = 18f;

    [Header("Concat sheet (target — ek hi sheet)")]
    public Vector3 concatPosition = new Vector3(220f, 18f, 0f);
    public float sheetWidth = 14f;
    public float sheetHeight = 10f;
    public float sheetThickness = 0.3f;

    [Header("Texture (vertical stripes, gap ke saath — horizontal lines nahi)")]
    public int textureSize = 256;
    public int stripeCount = 40;
    public float lineThicknessRatio = 0.5f;
    public Color baseColor = new Color(0.65f, 0.55f, 0.88f);
    public Color stripeColor = new Color(0.45f, 0.35f, 0.7f);

    [Header("Lines")]
    public Material lineMaterial;
    public Color lineColor = new Color(0.7f, 0.7f, 0.9f);
    public float lineWidth = 0.06f;

    [Header("Label")]
    public string sectionLabel = "Concat";
    public string shapeLabel = "[5x960]";
    public float labelFontSize = 10f;
    public Color labelColor = Color.white;
    public Vector3 labelOffset = new Vector3(0f, 6f, 0f);

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
        BuildConcatSheet();
        DrawLinesFromOStack();
        CreateLabel();
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

    private void BuildConcatSheet()
    {
        concatSheet = GameObject.CreatePrimitive(PrimitiveType.Cube);
        concatSheet.name = "Concat_Sheet";
        concatSheet.transform.SetParent(transform, false);
        concatSheet.transform.localPosition = concatPosition;
        concatSheet.transform.localScale = new Vector3(
            sheetWidth,
            sheetHeight,
            sheetThickness
        );

        Material mat = new Material(GetCompatibleShader());
        mat.mainTexture = GenerateVerticalStripeTexture();

        Renderer renderer = concatSheet.GetComponent<Renderer>();

        if (renderer != null)
            renderer.material = mat;
    }

    public IEnumerator AnimateReveal(float slabDelay)
    {
        revealRequested = true;
        CancelInvoke(nameof(Rebuild));
        Clear();
        concatLines.Clear();
        BuildConcatSheet();
        DrawLinesFromOStack();
        CreateLabel();

        concatSheet.SetActive(false);
        foreach (LineRenderer line in concatLines)
            line.enabled = false;

        yield return new WaitForSeconds(slabDelay);
        concatSheet.SetActive(true);
        foreach (LineRenderer line in concatLines)
            line.enabled = true;
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

    private Texture2D GenerateVerticalStripeTexture()
    {
        Texture2D tex = new Texture2D(
            textureSize,
            textureSize
        );

        tex.filterMode = FilterMode.Point;

        int cellWidth = Mathf.Max(
            1,
            textureSize / stripeCount
        );

        int lineWidthPixels = Mathf.RoundToInt(
            cellWidth * lineThicknessRatio
        );

        for (int x = 0; x < textureSize; x++)
        {
            int localX = x % cellWidth;

            Color colorForColumn =
                localX < lineWidthPixels
                    ? stripeColor
                    : baseColor;

            for (int y = 0; y < textureSize; y++)
                tex.SetPixel(x, y, colorForColumn);
        }

        tex.Apply();
        return tex;
    }

    private void DrawLinesFromOStack()
    {
        concatLines.Clear();

        Vector3 concatLeftEdge = concatPosition - new Vector3(
            sheetWidth / 2f,
            0f,
            0f
        );

        for (int i = 0; i < slabCount; i++)
        {
            Vector3 oPos = oStackPosition + new Vector3(
                i * oSlabStaggerX,
                i * oSlabStaggerY,
                i * oSlabGapZ
            );

            Vector3 oRightEdge = oPos + new Vector3(
                oSheetWidth / 2f,
                0f,
                0f
            );

            concatLines.Add(DrawLine(oRightEdge, concatLeftEdge));
        }
    }

    private LineRenderer DrawLine(Vector3 from, Vector3 to)
    {
        GameObject lineObj = new GameObject("Concat_Line");
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

    private void CreateLabel()
    {
        GameObject labelObj = new GameObject("Label_Concat");
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition = concatPosition + labelOffset;

        TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = $"{sectionLabel}   {shapeLabel}";
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
            material.SetFloat("_FaceDilate", textThickness);
    }
}
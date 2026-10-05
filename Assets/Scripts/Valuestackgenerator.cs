using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[ExecuteAlways]
public class ValueStackGenerator : MonoBehaviour
{
    private readonly List<GameObject> slabObjects = new();
    private readonly List<LineRenderer> slabLines = new();
    private bool revealRequested;

    [Header("Mirror values")]
    public Vector3 outputMatrixPosition = new Vector3(40f, 5f, 0f);
    public int displayColumns = 5;
    public float colSpacing = 2.2f;
    public float matrixBracketPadding = 1.5f;
    public float boxesX = 65f;
    public float boxSpacingY = 5f;
    public int boxIndex = 0;

    [Header("Line color")]
    public Color lineColor = Color.white;
    public Vector3 lineStartOffset = new Vector3(-1.5f, 0f, 0f);

    [Header("Stack")]
    public Vector3 stackPosition = new Vector3(75f, 8f, 0f);
    public int slabCount = 10;
    public float sheetWidth = 4f;
    public float sheetHeight = 3f;
    public float sheetThickness = 0.3f;
    public float slabGapZ = 0.6f;
    public float slabStaggerY = 0.15f;
    public float slabStaggerX = 0.1f;

    [Header("Texture grid")]
    public int textureSize = 256;
    public int gridColumns = 16;
    public int gridRows = 20;
    public float grainStrength = 0.12f;
    public float gridLineDarken = 0.4f;
    public Color colorA = new Color(0.13f, 0.85f, 0.63f);
    public Color colorB = new Color(0.11f, 0.45f, 0.4f);
    public Color colorC = Color.white;

    [Header("Line settings")]
    public Material lineMaterial;
    public float lineWidth = 0.05f;

    [Header("Label")]
    public string sectionLabel = "VALUES";
    public string formulaLabel = "V = XW_V";
    public string shapeLabel = "5x64";
    public float labelFontSize = 6f;
    public Color labelColor = Color.white;
    public Vector3 labelOffset = new Vector3(0f, 2f, 0f);

    [Header("Text thickness")]
    public float textThickness = 0.15f;

    public void Rebuild()
    {
        CancelInvoke(nameof(Rebuild));
        Clear();
        BuildStack();
    }

    public void Clear()
    {
        CancelInvoke(nameof(Rebuild));

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child =
                transform.GetChild(i).gameObject;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private void Start()
    {
        if (Application.isPlaying && !revealRequested)
            Rebuild();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        CancelInvoke(nameof(Rebuild));
        Invoke(nameof(Rebuild), 0.05f);
    }

    private void BuildStack()
    {
        slabObjects.Clear();
        slabLines.Clear();

        Vector3 sourceBoxPosition = ComputeSourceBoxPosition();
        Vector3 pullbackSourcePoint = sourceBoxPosition + lineStartOffset;
        Vector3 slabSize = new Vector3(sheetWidth, sheetHeight, sheetThickness);

        for (int i = 0; i < slabCount; i++)
        {
            Vector3 position = stackPosition + new Vector3(
                i * slabStaggerX,
                i * slabStaggerY,
                i * slabGapZ
            );

            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = $"ValueSlab_{i}";
            slab.transform.SetParent(transform, false);
            slab.transform.localPosition = position;
            slab.transform.localScale = slabSize;

            Shader shader = GetCompatibleShader();

            if (shader != null)
            {
                Material material = new Material(shader);
                material.mainTexture = GenerateNoiseTexture();
                slab.GetComponent<Renderer>().material = material;
            }

            Vector3 targetPoint = position - new Vector3(slabSize.x / 2f, 0f, 0f);
            LineRenderer line = DrawLine(pullbackSourcePoint, targetPoint);

            slabObjects.Add(slab);
            slabLines.Add(line);
        }

        GameObject labelObj = new GameObject("Label_Values");
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition = stackPosition + labelOffset;

        TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = $"{sectionLabel}   {formulaLabel}   {shapeLabel}";
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
        BuildStack();

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

    public void CompleteImmediately()
    {
        StopAllCoroutines();
        foreach (GameObject slab in slabObjects)
            if (slab != null)
                slab.SetActive(true);
        foreach (LineRenderer line in slabLines)
            if (line != null)
                line.enabled = true;
    }

    private Vector3 ComputeSourceBoxPosition()
    {
        float centerY =
            outputMatrixPosition.y;

        float boxY =
            centerY +
            boxSpacingY -
            boxSpacingY * boxIndex;

        return new Vector3(
            boxesX,
            boxY,
            outputMatrixPosition.z
        );
    }

    private Shader GetCompatibleShader()
    {
        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Unlit"
            );

        if (shader != null)
            return shader;

        shader =
            Shader.Find("Unlit/Texture");

        if (shader != null)
            return shader;

        shader =
            Shader.Find("Standard");

        if (shader != null)
            return shader;

        return Shader.Find("Sprites/Default");
    }

    private Texture2D GenerateNoiseTexture()
    {
        Texture2D texture =
            new Texture2D(
                textureSize,
                textureSize
            );

        texture.filterMode =
            FilterMode.Point;

        int cellWidth =
            Mathf.Max(
                1,
                textureSize / gridColumns
            );

        int cellHeight =
            Mathf.Max(
                1,
                textureSize / gridRows
            );

        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                int cellX =
                    x / cellWidth;

                int cellY =
                    y / cellHeight;

                float cellNoise =
                    Mathf.PerlinNoise(
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

                float grain =
                    (
                        Mathf.PerlinNoise(
                            x * 0.6f,
                            y * 0.6f
                        ) - 0.5f
                    ) * grainStrength;

                Color color =
                    baseColor +
                    new Color(
                        grain,
                        grain,
                        grain,
                        0f
                    );

                bool isGridLine =
                    x % cellWidth == 0 ||
                    y % cellHeight == 0;

                if (isGridLine)
                {
                    color =
                        Color.Lerp(
                            color,
                            Color.black,
                            gridLineDarken
                        );
                }

                texture.SetPixel(
                    x,
                    y,
                    color
                );
            }
        }

        texture.Apply();

        return texture;
    }

    private LineRenderer DrawLine(
        Vector3 from,
        Vector3 to
    )
    {
        GameObject lineObj =
            new GameObject(
                "Value_Source_Line"
            );

        lineObj.transform.SetParent(
            transform
        );

        EnsureLineMaterial();

        LineRenderer lineRenderer =
            lineObj.AddComponent<LineRenderer>();

        lineRenderer.material =
            lineMaterial;

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth =
            lineWidth;

        lineRenderer.endWidth =
            lineWidth;

        lineRenderer.SetPosition(
            0,
            transform.TransformPoint(from)
        );

        lineRenderer.SetPosition(
            1,
            transform.TransformPoint(to)
        );

        lineRenderer.startColor =
            lineColor;

        lineRenderer.endColor =
            lineColor;

        lineRenderer.useWorldSpace = true;
        return lineRenderer;
    }

    private void EnsureLineMaterial()
    {
        if (lineMaterial != null)
            return;

        Shader shader =
            Shader.Find("Sprites/Default");

        if (shader != null)
            lineMaterial =
                new Material(shader);
    }

    private void ApplyThickness(
        TextMeshPro tmp
    )
    {
        if (tmp == null || tmp.font == null)
            return;

        Material material =
            tmp.fontMaterial;

        if (material == null)
            return;

        if (material.HasProperty("_FaceDilate"))
        {
            material.SetFloat(
                "_FaceDilate",
                textThickness
            );
        }
    }
}
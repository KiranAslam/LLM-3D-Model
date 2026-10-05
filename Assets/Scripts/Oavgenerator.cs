using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[ExecuteAlways]
public class OAVGenerator : MonoBehaviour
{
    private readonly List<GameObject> slabObjects = new();
    private readonly List<LineRenderer> slabLines = new();
    private bool revealRequested;

    [Header("Mirror")]
    public Vector3 avStackPosition = new Vector3(165f, 18f, 0f);
    public int slabCount = 10;
    public float avSlabGapZ = 9f;
    public float avSlabStaggerX = 0.1f;
    public float avSlabStaggerY = 0.1f;
    public Vector3 avBoxSize = new Vector3(5f, 5f, 1f);

    [Header("O stack (target)")]
    public Vector3 oStackPosition = new Vector3(190f, 18f, 0f);
    public float sheetWidth = 10f;
    public float sheetHeight = 4f;
    public float sheetThickness = 0.3f;
    public float slabGapZ = 9f;
    public float slabStaggerX = 0.1f;
    public float slabStaggerY = 0.1f;

    [Header("Texture grid (V/K/Q jaisa hi)")]
    public int textureSize = 256;
    public int gridColumns = 16;
    public int gridRows = 20;
    public float grainStrength = 0.12f;
    public float gridLineDarken = 0.4f;
    public Color colorA = new Color(0.13f, 0.85f, 0.63f);
    public Color colorB = new Color(0.11f, 0.45f, 0.4f);
    public Color colorC = Color.white;

    [Header("Lines")]
    public Material lineMaterial;
    public Color lineColor = new Color(0.7f, 0.7f, 0.7f);
    public float lineWidth = 0.06f;

    [Header("Label")]
    public string sectionLabel = "O = AV";
    public string shapeLabel = "[5x64]";
    public float labelFontSize = 8f;
    public Color labelColor = Color.white;
    public Vector3 labelOffset = new Vector3(0f, 3f, 0f);

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
        BuildStackAndLines();
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

    private void BuildStackAndLines()
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
            Vector3 avPos = avStackPosition + new Vector3(
                i * avSlabStaggerX,
                i * avSlabStaggerY,
                i * avSlabGapZ
            );

            Vector3 avRightEdge = avPos + new Vector3(
                avBoxSize.x / 2f,
                0f,
                0f
            );

            Vector3 oPos = oStackPosition + new Vector3(
                i * slabStaggerX,
                i * slabStaggerY,
                i * slabGapZ
            );

            Vector3 oLeftEdge = oPos - new Vector3(
                sheetWidth / 2f,
                0f,
                0f
            );

            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = $"OSlab_{i}";
            slab.transform.SetParent(transform, false);
            slab.transform.localPosition = oPos;
            slab.transform.localScale = slabSize;

            Material mat = new Material(GetCompatibleShader());
            mat.mainTexture = GenerateNoiseTexture();

            Renderer renderer = slab.GetComponent<Renderer>();

            if (renderer != null)
                renderer.material = mat;

            LineRenderer line = DrawLine(avRightEdge, oLeftEdge);
            slabObjects.Add(slab);
            slabLines.Add(line);
        }
    }

    public IEnumerator AnimateReveal(float slabDelay)
    {
        revealRequested = true;
        CancelInvoke(nameof(Rebuild));
        Clear();
        BuildStackAndLines();
        CreateLabel();

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

    private Texture2D GenerateNoiseTexture()
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

    private LineRenderer DrawLine(Vector3 from, Vector3 to)
    {
        GameObject lineObj = new GameObject("O_Line");
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
        GameObject labelObj = new GameObject("Label_O");
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition = oStackPosition + labelOffset;

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
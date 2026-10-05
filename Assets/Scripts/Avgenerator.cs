using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[ExecuteAlways]
public class AVGenerator : MonoBehaviour
{
    private readonly List<GameObject> avBoxes = new();
    private readonly List<List<LineRenderer>> avLines = new();
    private bool revealRequested;

    [Header("Mirror these — ValueStack (V, Box Index 0) ki Inspector values")]
    public Vector3 valueStackPosition = new Vector3(75f, 18f, 0f);
    public int slabCount = 10;
    public float valueSlabGapZ = 9f;
    public float valueSlabStaggerX = 0.1f;
    public float valueSlabStaggerY = 0.1f;
    public float valueSheetWidth = 7f;

    [Header("Mirror these — SoftmaxGenerator (A) ki Inspector values")]
    public Vector3 softmaxStackPosition = new Vector3(140f, -7f, 0f);
    public float softmaxSlabGapZ = 9f;
    public float softmaxSlabStaggerX = 0.1f;
    public float softmaxSlabStaggerY = 0.1f;
    public float softmaxSheetWidth = 13f;
    public float softmaxSheetHeight = 13f;

    [Header("AV boxes (10 chhoti grey boxes)")]
    public Vector3 avStackPosition = new Vector3(165f, 5f, 0f);
    public Vector3 avBoxSize = new Vector3(1.5f, 1.5f, 1.5f);
    public Color avBoxColor = new Color(0.5f, 0.5f, 0.5f, 0.55f);
    public float avSlabGapZ = 9f;
    public float avSlabStaggerX = 0.1f;
    public float avSlabStaggerY = 0.1f;

    [Header("Lines")]
    public Material lineMaterial;
    public Color vLineColor = Color.white;
    public Color aLineColor = new Color(0.7f, 0.7f, 0.7f);
    public float lineWidth = 0.06f;

    [Header("Label")]
    public string labelText = "AV";
    public float labelFontSize = 10f;
    public Color labelColor = Color.white;
    public Vector3 labelOffset = new Vector3(0f, 4f, 0f);

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
        BuildBoxesAndLines();
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

    private void BuildBoxesAndLines()
    {
        avBoxes.Clear();
        avLines.Clear();

        for (int i = 0; i < slabCount; i++)
        {
            Vector3 avBoxPos = avStackPosition + new Vector3(
                i * avSlabStaggerX,
                i * avSlabStaggerY,
                i * avSlabGapZ
            );

            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = $"AV_Box_{i}";
            box.transform.SetParent(transform, false);
            box.transform.localPosition = avBoxPos;
            box.transform.localScale = avBoxSize;

            Shader shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                Material mat = new Material(shader);
                mat.color = avBoxColor;

                Renderer renderer = box.GetComponent<Renderer>();

                if (renderer != null)
                    renderer.material = mat;
            }

            Vector3 vPos = valueStackPosition + new Vector3(
                i * valueSlabStaggerX,
                i * valueSlabStaggerY,
                i * valueSlabGapZ
            );

            Vector3 vRightEdge = vPos + new Vector3(
                valueSheetWidth / 2f,
                0f,
                0f
            );

            List<LineRenderer> lines = new();
            lines.Add(DrawLine(
                new[] { vRightEdge, avBoxPos },
                vLineColor
            ));

            Vector3 aPos = softmaxStackPosition + new Vector3(
                i * softmaxSlabStaggerX,
                i * softmaxSlabStaggerY,
                i * softmaxSlabGapZ
            );

            Vector3 aTopRight = aPos + new Vector3(
                softmaxSheetWidth / 2f,
                softmaxSheetHeight / 2f,
                0f
            );

            Vector3 elbowPoint = new Vector3(
                avBoxPos.x,
                aTopRight.y,
                aTopRight.z
            );

            lines.Add(DrawLine(
                new[] { aTopRight, elbowPoint, avBoxPos },
                aLineColor
            ));

            avBoxes.Add(box);
            avLines.Add(lines);
        }
    }

    public IEnumerator AnimateReveal(float slabDelay)
    {
        revealRequested = true;
        CancelInvoke(nameof(Rebuild));
        Clear();
        BuildBoxesAndLines();
        CreateLabel();

        foreach (GameObject box in avBoxes)
            box.SetActive(false);
        foreach (List<LineRenderer> lines in avLines)
            foreach (LineRenderer line in lines)
                line.enabled = false;

        for (int i = 0; i < avBoxes.Count; i++)
        {
            yield return new WaitForSeconds(slabDelay);
            avBoxes[i].SetActive(true);
            foreach (LineRenderer line in avLines[i])
                line.enabled = true;
        }
    }

    private LineRenderer DrawLine(Vector3[] points, Color color)
    {
        GameObject lineObj = new GameObject("AV_Line");
        lineObj.transform.SetParent(transform, false);

        EnsureLineMaterial();

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.positionCount = points.Length;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        Vector3[] worldPoints = new Vector3[points.Length];

        for (int i = 0; i < points.Length; i++)
            worldPoints[i] = transform.TransformPoint(points[i]);

        lr.SetPositions(worldPoints);
        lr.startColor = color;
        lr.endColor = color;
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
        GameObject labelObj = new GameObject("Label_AV");
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition = avStackPosition + labelOffset;

        TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = labelText;
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
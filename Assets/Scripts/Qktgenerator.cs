using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[ExecuteAlways]
public class QKTGenerator : MonoBehaviour
{
    private readonly List<GameObject> qktBoxes = new();
    private readonly List<List<LineRenderer>> qktLines = new();
    private bool revealRequested;

    [Header("Mirror these — QueryStack ki Inspector values")]
    public Vector3 queryStackPosition = new Vector3(75f, -8f, 0f);
    public int slabCount = 10;
    public float querySlabGapZ = 9f;
    public float querySlabStaggerX = 0.1f;
    public float querySlabStaggerY = 0.1f;
    public float querySheetWidth = 7f;

    [Header("Mirror these — TransposeGenerator ki Inspector values")]
    public Vector3 transposeStackPosition = new Vector3(95f, 5f, 0f);
    public float transposeSlabGapZ = 9f;
    public float transposeSlabStaggerX = 0.1f;
    public float transposeSlabStaggerY = 0.12f;
    public float transposeSheetWidth = 5f;

    [Header("QK^T boxes (10, ek har index ke liye)")]
    public Vector3 qktStackPosition = new Vector3(115f, -1.5f, 0f);
    public Vector3 qktBoxSize = new Vector3(1.2f, 1.2f, 1.2f);
    public Color qktBoxColor = new Color(0.6f, 0.9f, 0.85f, 0.35f);
    public float qktSlabGapZ = 0.6f;
    public float qktSlabStaggerX = 0.1f;
    public float qktSlabStaggerY = 0.1f;

    [Header("Lines")]
    public Material lineMaterial;
    public Color queryLineColor = new Color(0.75f, 0.3f, 0.3f);
    public Color transposeLineColor = new Color(0.15f, 0.7f, 0.55f);
    public float lineWidth = 0.05f;

    [Header("Label")]
    public string labelText = "QK^T";
    public float labelFontSize = 8f;
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
        qktBoxes.Clear();
        qktLines.Clear();

        for (int i = 0; i < slabCount; i++)
        {
            Vector3 boxPos = qktStackPosition + new Vector3(
                i * qktSlabStaggerX,
                i * qktSlabStaggerY,
                i * qktSlabGapZ
            );

            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = $"QKT_Box_{i}";
            box.transform.SetParent(transform, false);
            box.transform.localPosition = boxPos;
            box.transform.localScale = qktBoxSize;

            Shader shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                Material mat = new Material(shader);
                mat.color = qktBoxColor;

                Renderer renderer = box.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material = mat;
            }

            List<LineRenderer> lines = new();
            Vector3 boxLeftFace = boxPos - new Vector3(
                qktBoxSize.x / 2f,
                0f,
                0f
            );

            Vector3 queryPos = queryStackPosition + new Vector3(
                i * querySlabStaggerX,
                i * querySlabStaggerY,
                i * querySlabGapZ
            );

            Vector3 queryRightEdge = queryPos + new Vector3(
                querySheetWidth / 2f,
                0f,
                0f
            );

            lines.Add(DrawLine(
                queryRightEdge,
                boxLeftFace,
                queryLineColor
            ));

            Vector3 transposePos = transposeStackPosition + new Vector3(
                i * transposeSlabStaggerX,
                i * transposeSlabStaggerY,
                i * transposeSlabGapZ
            );

            Vector3 transposeRightEdge = transposePos + new Vector3(
                transposeSheetWidth / 2f,
                0f,
                0f
            );

            lines.Add(DrawLine(
                transposeRightEdge,
                boxLeftFace,
                transposeLineColor
            ));

            qktBoxes.Add(box);
            qktLines.Add(lines);
        }
    }

    public IEnumerator AnimateReveal(float slabDelay)
    {
        revealRequested = true;
        CancelInvoke(nameof(Rebuild));
        Clear();
        BuildBoxesAndLines();
        CreateLabel();

        foreach (GameObject box in qktBoxes)
            box.SetActive(false);
        foreach (List<LineRenderer> lines in qktLines)
            foreach (LineRenderer line in lines)
                line.enabled = false;

        for (int i = 0; i < qktBoxes.Count; i++)
        {
            yield return new WaitForSeconds(slabDelay);
            qktBoxes[i].SetActive(true);
            foreach (LineRenderer line in qktLines[i])
                line.enabled = true;
        }
    }

    private LineRenderer DrawLine(Vector3 from, Vector3 to, Color color)
    {
        GameObject lineObj = new GameObject("QKT_Line");
        lineObj.transform.SetParent(transform, false);

        EnsureLineMaterial();

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.positionCount = 2;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.SetPosition(0, transform.TransformPoint(from));
        lr.SetPosition(1, transform.TransformPoint(to));
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
        GameObject labelObj = new GameObject("Label_QKT");
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition = qktStackPosition + labelOffset;

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
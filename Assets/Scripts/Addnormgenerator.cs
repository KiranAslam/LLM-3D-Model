using UnityEngine;
using TMPro;

[ExecuteAlways]
public class AddNormGenerator : MonoBehaviour
{
    [Header("Mirror these — LayerOutGenerator ke Inspector values")]
    public Vector3 layerOutPosition = new Vector3(275f, 18f, 0f);
    public Vector3 layerOutSize = new Vector3(18f, 10f, 1f);

    [Header("Mirror these — AttentionHeadGenerator ke Inspector values")]
    public Vector3 outputMatrixPosition = new Vector3(40f, 5f, 0f);
    public int displayColumns = 5;
    public float colSpacing = 2.2f;
    public float matrixBracketPadding = 1.5f;

    [Header("Add+Norm block (naya — yahin tune karo)")]
    public Vector3 addNormPosition = new Vector3(300f, 8f, 0f);
    public Vector3 addNormSize = new Vector3(6f, 6f, 1f);
    public Color addNormColor = new Color(0.15f, 0.32f, 0.55f);
    public string addNormLabel = "Add+Norm";
    public float addNormLabelFontSize = 8f;
    public Color addNormLabelColor = Color.white;
    public Vector3 addNormLabelOffset = new Vector3(0f, 3f, 0f);

    [Header("Residual line path")]
    public float residualStartXOffset = 0f;
    public float residualDropY = -25f;
    public Color lineColor = new Color(0.549f, 0.851f, 0.8f);
    public float lineWidth = 0.2f;
    public Material lineMaterial;

    [Header("Section labels — bottom, x-axis ke alag distance par")]
    public string attentionHeadSectionLabel = "ATTENTION HEAD (Layer 0)";
    public Vector3 attentionHeadLabelPos = new Vector3(150f, -32f, 0f);
    public string residualSectionLabel = "Residual";
    public Vector3 residualLabelPos = new Vector3(280f, -32f, 0f);
    public float sectionLabelFontSize = 8f;
    public Color sectionLabelColor = Color.white;

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

        BuildAddNormBox();

        CreateLabel(
            addNormLabel,
            addNormPosition + addNormLabelOffset,
            addNormLabelFontSize,
            addNormLabelColor
        );

        Vector3 layerOutExit =
            layerOutPosition +
            Vector3.right * (layerOutSize.x / 2f);

        Vector3 addNormTopEdge =
            addNormPosition +
            Vector3.up * (addNormSize.y / 2f);

        Vector3 layerOutTurn = new Vector3(
            addNormTopEdge.x,
            layerOutExit.y,
            layerOutExit.z
        );

        DrawLine(
            new[]
            {
                layerOutExit,
                layerOutTurn,
                addNormTopEdge
            }
        );

        Vector3 residualSource =
            ComputeResidualSourcePoint();

        residualSource.x += residualStartXOffset;

        Vector3 addNormBottomEdge =
            addNormPosition -
            Vector3.up * (addNormSize.y / 2f);

        Vector3 down = new Vector3(
            residualSource.x,
            residualDropY,
            residualSource.z
        );

        Vector3 across = new Vector3(
            addNormPosition.x,
            residualDropY,
            residualSource.z
        );

        Vector3 up = new Vector3(
            addNormPosition.x,
            addNormBottomEdge.y,
            residualSource.z
        );

        DrawLine(
            new[]
            {
                residualSource,
                down,
                across,
                up
            }
        );

        CreateLabel(
            attentionHeadSectionLabel,
            attentionHeadLabelPos,
            sectionLabelFontSize,
            sectionLabelColor
        );

        CreateLabel(
            residualSectionLabel,
            residualLabelPos,
            sectionLabelFontSize,
            sectionLabelColor
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

    private Vector3 ComputeResidualSourcePoint()
    {
        float gridTotalWidth =
            (displayColumns - 1) * colSpacing;

        float rightEdgeX =
            outputMatrixPosition.x +
            (gridTotalWidth / 2f) +
            matrixBracketPadding;

        return new Vector3(
            rightEdgeX,
            outputMatrixPosition.y,
            outputMatrixPosition.z
        );
    }

    private void BuildAddNormBox()
    {
        GameObject block = GameObject.CreatePrimitive(
            PrimitiveType.Cube
        );

        block.name = "AddNorm_Block";
        block.transform.SetParent(transform, false);
        block.transform.localPosition = addNormPosition;
        block.transform.localScale = addNormSize;

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
            return;

        Material mat = new Material(shader);
        mat.color = addNormColor;

        Renderer renderer =
            block.GetComponent<Renderer>();

        if (renderer != null)
            renderer.material = mat;
    }

    private void DrawLine(Vector3[] localPoints)
    {
        GameObject lineObj = new GameObject(
            "AddNorm_Line"
        );

        lineObj.transform.SetParent(
            transform,
            false
        );

        EnsureLineMaterial();

        LineRenderer lr =
            lineObj.AddComponent<LineRenderer>();

        lr.material = lineMaterial;
        lr.positionCount = localPoints.Length;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        Vector3[] worldPoints =
            new Vector3[localPoints.Length];

        for (int i = 0; i < localPoints.Length; i++)
        {
            worldPoints[i] =
                transform.TransformPoint(
                    localPoints[i]
                );
        }

        lr.SetPositions(worldPoints);
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

    private void CreateLabel(
        string text,
        Vector3 localPos,
        float fontSize,
        Color color
    )
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
        tmp.fontSize = fontSize;
        tmp.color = color;
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
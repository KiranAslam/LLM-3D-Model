using UnityEngine;
using TMPro;

[ExecuteAlways]
public class AttentionHeadGenerator : MonoBehaviour
{
    [Header("Mirror values")]
    public Vector3 outputMatrixPosition = new Vector3(40f, 5f, 0f);
    public int displayColumns = 5;
    public float colSpacing = 2.2f;
    public float matrixBracketPadding = 1.5f;

    [Header("QKV boxes")]
    public float boxSize = 1.2f;
    public float boxDepth = 1.2f;
    public float boxSpacingY = 5f;
    public float boxesX = 65f;
    public float busDistanceFromBoxes = 3f;
    public string dimensionLabel = "960x64";
    public float labelFontSize = 6f;
    public Vector3 labelOffset = new Vector3(0f, 1.2f, 0f);

    public Color boxColorTop = Color.white;
    public Color boxColorMid = Color.black;
    public Color boxColorBottom = new Color(0.549f, 0.851f, 0.8f);

    [Header("Lines")]
    public Material lineMaterial;
    public Color lineColor = Color.white;
    public float lineWidth = 0.06f;

    [Header("Text thickness")]
    public float textThickness = 0.15f;

    public void Rebuild()
    {
        CancelInvoke(nameof(Rebuild));
        Clear();

        Vector3 sourcePoint =
            ComputeOutputMatrixExitPoint();

        Vector3 topBoxPosition =
            new Vector3(
                boxesX,
                sourcePoint.y + boxSpacingY,
                sourcePoint.z
            );

        Vector3[] boxPositions = new Vector3[3];

        Color[] colors =
        {
            boxColorTop,
            boxColorMid,
            boxColorBottom
        };

        for (int i = 0; i < 3; i++)
        {
            Vector3 position =
                topBoxPosition -
                new Vector3(
                    0f,
                    boxSpacingY * i,
                    0f
                );

            boxPositions[i] = position;

            CreateBox(
                position,
                colors[i],
                i
            );

            CreateLabelAbove(
                position,
                dimensionLabel
            );
        }

        DrawBusAndConnections(
            sourcePoint,
            boxPositions
        );
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
        if (Application.isPlaying)
            Rebuild();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        CancelInvoke(nameof(Rebuild));
        Invoke(nameof(Rebuild), 0.05f);
    }

    private Vector3 ComputeOutputMatrixExitPoint()
    {
        float gridTotalWidth =
            (displayColumns - 1) *
            colSpacing;

        float rightEdgeX =
            outputMatrixPosition.x +
            gridTotalWidth / 2f +
            matrixBracketPadding;

        return new Vector3(
            rightEdgeX,
            outputMatrixPosition.y,
            outputMatrixPosition.z
        );
    }

    private void CreateBox(
        Vector3 localPosition,
        Color color,
        int index
    )
    {
        GameObject box =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        box.name =
            $"QKV_Box_{index}";

        box.transform.SetParent(
            transform,
            false
        );

        box.transform.localPosition =
            localPosition;

        box.transform.localScale =
            new Vector3(
                boxSize,
                boxSize,
                boxDepth
            );

        Shader shader =
            GetCompatibleShader();

        if (shader != null)
        {
            Material material =
                new Material(shader);

            material.color = color;

            box.GetComponent<Renderer>()
                .material = material;
        }
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
            Shader.Find("Unlit/Color");

        if (shader != null)
            return shader;

        return Shader.Find("Sprites/Default");
    }

    private void CreateLabelAbove(
        Vector3 boxLocalPosition,
        string text
    )
    {
        GameObject labelObj =
            new GameObject(
                "Label_Dimension"
            );

        labelObj.transform.SetParent(
            transform,
            false
        );

        labelObj.transform.localPosition =
            boxLocalPosition +
            labelOffset;

        TextMeshPro tmp =
            labelObj.AddComponent<TextMeshPro>();

        tmp.text = text;
        tmp.fontSize = labelFontSize;
        tmp.color = Color.black;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment =
            TextAlignmentOptions.Midline;

        ApplyThickness(tmp);
    }

    private void DrawBusAndConnections(
        Vector3 sourcePoint,
        Vector3[] boxPositions
    )
    {
        float busX =
            boxesX -
            busDistanceFromBoxes;

        Vector3 midBoxHeight =
            new Vector3(
                busX,
                boxPositions[1].y,
                boxPositions[1].z
            );

        DrawLine(
            new[]
            {
                transform.TransformPoint(sourcePoint),
                transform.TransformPoint(midBoxHeight)
            }
        );

        Vector3 busTop =
            new Vector3(
                busX,
                boxPositions[0].y,
                boxPositions[0].z
            );

        Vector3 busBottom =
            new Vector3(
                busX,
                boxPositions[2].y,
                boxPositions[2].z
            );

        DrawLine(
            new[]
            {
                transform.TransformPoint(busTop),
                transform.TransformPoint(busBottom)
            }
        );

        foreach (Vector3 position in boxPositions)
        {
            Vector3 stubStart =
                new Vector3(
                    busX,
                    position.y,
                    position.z
                );

            Vector3 stubEnd =
                new Vector3(
                    position.x - boxSize / 2f,
                    position.y,
                    position.z
                );

            DrawLine(
                new[]
                {
                    transform.TransformPoint(stubStart),
                    transform.TransformPoint(stubEnd)
                }
            );
        }
    }

    private void DrawLine(Vector3[] points)
    {
        GameObject lineObj =
            new GameObject("Attention_Line");

        lineObj.transform.SetParent(
            transform
        );

        EnsureLineMaterial();

        LineRenderer lineRenderer =
            lineObj.AddComponent<LineRenderer>();

        lineRenderer.material =
            lineMaterial;

        lineRenderer.positionCount =
            points.Length;

        lineRenderer.startWidth =
            lineWidth;

        lineRenderer.endWidth =
            lineWidth;

        lineRenderer.SetPositions(points);
        lineRenderer.startColor =
            lineColor;

        lineRenderer.endColor =
            lineColor;

        lineRenderer.useWorldSpace = true;
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
using UnityEngine;
using TMPro;
using System.Collections;

[ExecuteAlways]
public class OutputMatrixGenerator : MonoBehaviour, ISentenceAnimatable
{
    [Header("Mirror values")]
    public int tokenCount = 5;
    public Vector3 embeddingSlabPosition = new Vector3(12f, 3f, 0f);
    public Vector3 embeddingSlabScale = new Vector3(1.2f, 8f, 3f);
    public float outputSpreadY = 9f;
    public float lineCrossDistance = 5f;
    public float vectorGapX = 1.2f;
    public int vectorDimensions = 6;
    public float vectorRowSpacing = 0.4f;
    public float bracketOffsetX = 0.9f;

    [Header("Exit lines")]
    public Material lineMaterial;
    public Color lineColor = new Color(0.549f, 0.851f, 0.8f);
    public float lineWidth = 0.05f;
    public float vectorExitGap = 0.4f;
    public float convergeDistanceX = 4f;
    public float exitLineDuration = 0.35f;
    public float finalLineDuration = 0.3f;
    public float rowRevealDelay = 0.12f;

    [Header("Output Matrix box")]
    public Vector3 outputMatrixPosition = new Vector3(35f, 3f, 0f);
    public int displayColumns = 5;
    public int fullDimensions = 960;
    public float rowSpacing = 1f;
    public float colSpacing = 2.2f;
    public float cellFontSize = 3f;
    public Color positiveColor = new Color(0.35f, 0.55f, 1f);
    public Color negativeColor = new Color(1f, 0.35f, 0.35f);
    public Color dashColor = Color.white;
    public float minValue = -0.2f;
    public float maxValue = 0.2f;

    [Header("Output matrix brackets")]
    public float matrixBracketTickLength = 0.4f;
    public float matrixBracketLineWidth = 0.05f;
    public Color matrixBracketColor = Color.white;
    public float matrixBracketPadding = 1f;

    [Header("Label")]
    public float labelFontSize = 6f;
    public Color labelColor = Color.white;
    public float labelYOffset = 1.5f;

    [Header("Text thickness")]
    public float textThickness = 0.15f;

    [Header("Section title")]
    public string outputTitleLabel = "Embed Output Matrix";
    public float outputTitleYOffset = 2f;
    public float titleFontSize = 10f;
    public Color titleColor = Color.white;

    private float gridTopY;
    private float gridLeftX;
    private float gridTotalHeight;
    private float gridTotalWidth;

    public void Animate(string sentence)
    {
        if (!string.IsNullOrEmpty(sentence))
            tokenCount = sentence.Split(' ').Length;
        CancelInvoke(nameof(Rebuild));
        Clear();
        if (Application.isPlaying)
            StartCoroutine(AnimateBuild());
        else
            Rebuild();
    }

    public void CompleteImmediately()
    {
        StopAllCoroutines();
        Rebuild();
    }

    public void Rebuild()
    {
        CancelInvoke(nameof(Rebuild));
        Clear();

        Vector3 convergePoint = DrawExitLinesToConverge();

        DrawOutputMatrixGrid();
        DrawFinalConnectingLine(convergePoint);
        DrawMatrixLabel();
        CreateOutputTitle();
    }

    private IEnumerator AnimateBuild()
    {
        gridTotalHeight = (tokenCount - 1) * rowSpacing;
        gridTopY = outputMatrixPosition.y + gridTotalHeight / 2f;
        gridTotalWidth = (displayColumns - 1) * colSpacing;
        gridLeftX = outputMatrixPosition.x - gridTotalWidth / 2f;

        GameObject matrixRoot = new GameObject("OutputMatrix_Grid");
        matrixRoot.transform.SetParent(transform, false);

        float rightFaceX = embeddingSlabPosition.x + embeddingSlabScale.x / 2f;
        float convergeLocalX = rightFaceX + lineCrossDistance + vectorGapX + bracketOffsetX + convergeDistanceX;
        float convergeLocalY = embeddingSlabPosition.y;
        Vector3 convergePointWorld = transform.TransformPoint(new Vector3(convergeLocalX, convergeLocalY, embeddingSlabPosition.z));

        System.Random random = new System.Random(999);

        for (int i = 0; i < tokenCount; i++)
        {
            float t = tokenCount > 1 ? (float)i / (tokenCount - 1) : 0.5f;
            float vectorCenterY = Mathf.Lerp(outputSpreadY / 2f, -outputSpreadY / 2f, t) + embeddingSlabPosition.y;
            float vectorCenterX = rightFaceX + lineCrossDistance + vectorGapX;
            float exitLocalX = vectorCenterX + bracketOffsetX + vectorExitGap;
            Vector3 exitPointWorld = transform.TransformPoint(new Vector3(exitLocalX, vectorCenterY, embeddingSlabPosition.z));

            yield return StartCoroutine(AnimateLine(exitPointWorld, convergePointWorld, exitLineDuration));
            BuildRow(matrixRoot, i, random);
            yield return new WaitForSeconds(rowRevealDelay);
        }

        float leftBracketX = outputMatrixPosition.x - gridTotalWidth / 2f - matrixBracketPadding;
        Vector3 matrixEntryWorld = transform.TransformPoint(new Vector3(leftBracketX, outputMatrixPosition.y, outputMatrixPosition.z));
        yield return StartCoroutine(AnimateLine(convergePointWorld, matrixEntryWorld, finalLineDuration));

        Vector3 topEdgeLocal = new Vector3(outputMatrixPosition.x, gridTopY, outputMatrixPosition.z);
        Vector3 bottomEdgeLocal = new Vector3(outputMatrixPosition.x, gridTopY - gridTotalHeight, outputMatrixPosition.z);
        float halfWidthWithPadding = gridTotalWidth / 2f + matrixBracketPadding;
        DrawMatrixBracket(topEdgeLocal, bottomEdgeLocal, halfWidthWithPadding, true);
        DrawMatrixBracket(topEdgeLocal, bottomEdgeLocal, halfWidthWithPadding, false);

        DrawMatrixLabel();
        CreateOutputTitle();
    }

    private void BuildRow(GameObject matrixRoot, int row, System.Random random)
    {
        float rowY = gridTopY - row * rowSpacing;

        for (int column = 0; column < displayColumns; column++)
        {
            float columnX = gridLeftX + column * colSpacing;

            GameObject cellObj = new GameObject($"Cell_{row}_{column}");
            cellObj.transform.SetParent(matrixRoot.transform, false);
            cellObj.transform.localPosition = new Vector3(columnX, rowY, outputMatrixPosition.z);

            TextMeshPro tmp = cellObj.AddComponent<TextMeshPro>();
            tmp.fontSize = cellFontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Midline;

            if (column == displayColumns - 2)
            {
                tmp.text = "—";
                tmp.color = dashColor;
            }
            else
            {
                float value = (float)(random.NextDouble() * (maxValue - minValue) + minValue);
                tmp.text = value.ToString("+0.00;-0.00");
                tmp.color = value >= 0 ? positiveColor : negativeColor;
            }

            ApplyThickness(tmp);
        }
    }

    private IEnumerator AnimateLine(Vector3 from, Vector3 to, float duration)
    {
        EnsureLineMaterial();
        GameObject lineObj = new GameObject("Vector_To_OutputMatrix_Line");
        lineObj.transform.SetParent(transform, false);
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.positionCount = 2;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.useWorldSpace = true;
        lr.SetPosition(0, from);
        lr.SetPosition(1, from);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            lr.SetPosition(1, Vector3.Lerp(from, to, t));
            yield return null;
        }
    }

    public void Clear()
    {
        CancelInvoke(nameof(Rebuild));

        gridTopY = 0f;
        gridLeftX = 0f;
        gridTotalHeight = 0f;
        gridTotalWidth = 0f;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;

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

    private Vector3 DrawExitLinesToConverge()
    {
        float rightFaceX =
            embeddingSlabPosition.x +
            embeddingSlabScale.x / 2f;

        float convergeLocalX =
            rightFaceX +
            lineCrossDistance +
            vectorGapX +
            bracketOffsetX +
            convergeDistanceX;

        float convergeLocalY =
            embeddingSlabPosition.y;

        Vector3 convergePointWorld =
            transform.TransformPoint(
                new Vector3(
                    convergeLocalX,
                    convergeLocalY,
                    embeddingSlabPosition.z
                )
            );

        for (int i = 0; i < tokenCount; i++)
        {
            float t =
                tokenCount > 1
                    ? (float)i / (tokenCount - 1)
                    : 0.5f;

            float vectorCenterY =
                Mathf.Lerp(
                    outputSpreadY / 2f,
                    -outputSpreadY / 2f,
                    t
                ) + embeddingSlabPosition.y;

            float vectorCenterX =
                rightFaceX +
                lineCrossDistance +
                vectorGapX;

            float exitLocalX =
                vectorCenterX +
                bracketOffsetX +
                vectorExitGap;

            Vector3 exitPointWorld =
                transform.TransformPoint(
                    new Vector3(
                        exitLocalX,
                        vectorCenterY,
                        embeddingSlabPosition.z
                    )
                );

            DrawWorldLine(
                new[]
                {
                    exitPointWorld,
                    convergePointWorld
                }
            );
        }

        return convergePointWorld;
    }

    private void DrawOutputMatrixGrid()
    {
        System.Random random =
            new System.Random(999);

        gridTotalHeight =
            (tokenCount - 1) * rowSpacing;

        gridTopY =
            outputMatrixPosition.y +
            gridTotalHeight / 2f;

        gridTotalWidth =
            (displayColumns - 1) * colSpacing;

        gridLeftX =
            outputMatrixPosition.x -
            gridTotalWidth / 2f;

        GameObject matrixRoot =
            new GameObject("OutputMatrix_Grid");

        matrixRoot.transform.SetParent(
            transform,
            false
        );

        for (int row = 0; row < tokenCount; row++)
        {
            float rowY =
                gridTopY -
                row * rowSpacing;

            for (int column = 0;
                 column < displayColumns;
                 column++)
            {
                float columnX =
                    gridLeftX +
                    column * colSpacing;

                GameObject cellObj =
                    new GameObject(
                        $"Cell_{row}_{column}"
                    );

                cellObj.transform.SetParent(
                    matrixRoot.transform,
                    false
                );

                cellObj.transform.localPosition =
                    new Vector3(
                        columnX,
                        rowY,
                        outputMatrixPosition.z
                    );

                TextMeshPro tmp =
                    cellObj.AddComponent<TextMeshPro>();

                tmp.fontSize = cellFontSize;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment =
                    TextAlignmentOptions.Midline;

                if (column == displayColumns - 2)
                {
                    tmp.text = "—";
                    tmp.color = dashColor;
                }
                else
                {
                    float value =
                        (float)(
                            random.NextDouble() *
                            (maxValue - minValue) +
                            minValue
                        );

                    tmp.text =
                        value.ToString("+0.00;-0.00");

                    tmp.color =
                        value >= 0
                            ? positiveColor
                            : negativeColor;
                }

                ApplyThickness(tmp);
            }
        }

        Vector3 topEdgeLocal =
            new Vector3(
                outputMatrixPosition.x,
                gridTopY,
                outputMatrixPosition.z
            );

        Vector3 bottomEdgeLocal =
            new Vector3(
                outputMatrixPosition.x,
                gridTopY - gridTotalHeight,
                outputMatrixPosition.z
            );

        float halfWidthWithPadding =
            gridTotalWidth / 2f +
            matrixBracketPadding;

        DrawMatrixBracket(
            topEdgeLocal,
            bottomEdgeLocal,
            halfWidthWithPadding,
            true
        );

        DrawMatrixBracket(
            topEdgeLocal,
            bottomEdgeLocal,
            halfWidthWithPadding,
            false
        );
    }

    private void DrawFinalConnectingLine(
        Vector3 convergePointWorld
    )
    {
        float leftBracketX =
            outputMatrixPosition.x -
            gridTotalWidth / 2f -
            matrixBracketPadding;

        Vector3 matrixEntryLocal =
            new Vector3(
                leftBracketX,
                outputMatrixPosition.y,
                outputMatrixPosition.z
            );

        Vector3 matrixEntryWorld =
            transform.TransformPoint(
                matrixEntryLocal
            );

        DrawWorldLine(
            new[]
            {
                convergePointWorld,
                matrixEntryWorld
            }
        );
    }

    private void DrawMatrixLabel()
    {
        GameObject labelObj =
            new GameObject(
                "Label_OutputMatrixShape"
            );

        labelObj.transform.SetParent(
            transform,
            false
        );

        labelObj.transform.localPosition =
            new Vector3(
                outputMatrixPosition.x,
                gridTopY + labelYOffset,
                outputMatrixPosition.z
            );

        TextMeshPro labelTmp =
            labelObj.AddComponent<TextMeshPro>();

        labelTmp.text =
            $"[{tokenCount} x {fullDimensions}]";

        labelTmp.fontSize =
            labelFontSize;

        labelTmp.color =
            labelColor;

        labelTmp.fontStyle =
            FontStyles.Bold;

        labelTmp.alignment =
            TextAlignmentOptions.Midline;

        ApplyThickness(labelTmp);
    }

    private void CreateOutputTitle()
    {
        GameObject titleObj =
            new GameObject(
                "Title_EmbedOutputMatrix"
            );

        titleObj.transform.SetParent(
            transform,
            false
        );

        titleObj.transform.localPosition =
            new Vector3(
                outputMatrixPosition.x,
                gridTopY +
                labelYOffset +
                outputTitleYOffset,
                outputMatrixPosition.z
            );

        TextMeshPro tmp =
            titleObj.AddComponent<TextMeshPro>();

        tmp.text = outputTitleLabel;
        tmp.fontSize = titleFontSize;
        tmp.color = titleColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment =
            TextAlignmentOptions.Midline;

        ApplyThickness(tmp);
    }

    private void DrawMatrixBracket(
        Vector3 topPointLocal,
        Vector3 bottomPointLocal,
        float halfWidth,
        bool isLeft
    )
    {
        float side =
            isLeft ? -1f : 1f;

        Vector3 offset =
            Vector3.right *
            halfWidth *
            side;

        Vector3 topLocal =
            topPointLocal + offset;

        Vector3 bottomLocal =
            bottomPointLocal + offset;

        Vector3 tickDirection =
            Vector3.right *
            matrixBracketTickLength *
            -side;

        Vector3 p0Local =
            topLocal + tickDirection;

        Vector3 p1Local =
            topLocal;

        Vector3 p2Local =
            bottomLocal;

        Vector3 p3Local =
            bottomLocal + tickDirection;

        Vector3[] worldPoints =
        {
            transform.TransformPoint(p0Local),
            transform.TransformPoint(p1Local),
            transform.TransformPoint(p2Local),
            transform.TransformPoint(p3Local)
        };

        GameObject bracketObj =
            new GameObject(
                isLeft
                    ? "OutputMatrix_Bracket_Left"
                    : "OutputMatrix_Bracket_Right"
            );

        bracketObj.transform.SetParent(
            transform,
            false
        );

        EnsureLineMaterial();

        LineRenderer lineRenderer =
            bracketObj.AddComponent<LineRenderer>();

        lineRenderer.material =
            lineMaterial;

        lineRenderer.positionCount = 4;
        lineRenderer.startWidth =
            matrixBracketLineWidth;

        lineRenderer.endWidth =
            matrixBracketLineWidth;

        lineRenderer.SetPositions(
            worldPoints
        );

        lineRenderer.startColor =
            matrixBracketColor;

        lineRenderer.endColor =
            matrixBracketColor;

        lineRenderer.useWorldSpace = true;
    }

    private void DrawWorldLine(
        Vector3[] worldPoints
    )
    {
        GameObject lineObj =
            new GameObject(
                "Vector_To_OutputMatrix_Line"
            );

        lineObj.transform.SetParent(
            transform,
            false
        );

        EnsureLineMaterial();

        LineRenderer lineRenderer =
            lineObj.AddComponent<LineRenderer>();

        lineRenderer.material =
            lineMaterial;

        lineRenderer.positionCount =
            worldPoints.Length;

        lineRenderer.startWidth =
            lineWidth;

        lineRenderer.endWidth =
            lineWidth;

        lineRenderer.SetPositions(
            worldPoints
        );

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
            lineMaterial = new Material(shader);
    }

    private void ApplyThickness(TextMeshPro tmp)
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
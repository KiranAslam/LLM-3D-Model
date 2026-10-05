using UnityEngine;
using TMPro;
using System.Collections;

[ExecuteAlways]
public class EmbeddingMatrixGenerator : MonoBehaviour, ISentenceAnimatable
{
    [Header("ID layout")]
    public TokenGenerator tokenGenerator;
    public float chipTravelDuration = 0.4f;
    public Color[] lineColors = new Color[] {
        new Color(0.365f, 0.792f, 0.647f),
        Color.white,
        new Color(0.561f, 0.831f, 0.761f),
        new Color(0.114f, 0.62f, 0.459f)
    };
    public int tokenCount = 5;
    public float idStartX = -3f;
    public float idStartY = 9f;
    public float idYStep = 2f;

    [Header("Slab shape")]
    public Vector3 slabPosition = new Vector3(12f, 3f, 0f);
    public Vector3 slabScale = new Vector3(1.2f, 8f, 3f);

    [Header("Texture (grid pattern)")]
    public int textureSize = 256;
    public int gridColumns = 24;
    public int gridRows = 36;
    public float grainStrength = 0.12f;
    public float gridLineDarken = 0.45f;
    public Color colorA = new Color(0.15f, 0.85f, 0.75f);
    public Color colorB = new Color(0.9f, 0.92f, 0.9f);
    public Color colorC = new Color(0.05f, 0.05f, 0.06f);

    [Header("Label")]
    public string labelText = "W_e";
    public float labelFontSize = 6f;
    public Color labelColor = Color.black;
    public Vector3 labelPosition = new Vector3(8f, 12f, 0f);

    [Header("Line settings")]
    public Material lineMaterial;
    public Color lineColor = new Color(0.549f, 0.851f, 0.8f);
    public float lineWidth = 0.06f;
    public float lineGap = 0.9f;
    public float lineCrossDistance = 5f;

    [Header("Output vector spacing")]
    public float outputSpreadY = 9f;
    public bool convergeAtCenter = true;

    [Header("Output vector numbers")]
    public int vectorDimensions = 6;
    public float vectorRowSpacing = 0.4f;
    public float vectorNumberFontSize = 3f;
    public Color positiveColor = new Color(0.35f, 0.55f, 1f);
    public Color negativeColor = new Color(1f, 0.35f, 0.35f);
    public float minValue = -0.2f;
    public float maxValue = 0.2f;

    [Header("Matrix brackets")]
    public float bracketOffsetX = 0.9f;
    public float bracketTickLength = 0.3f;
    public float bracketLineWidth = 0.04f;
    public Color bracketColor = Color.white;
    public float vectorGapX = 1.2f;

    [Header("Text thickness")]
    public float textThickness = 0.15f;

    [Header("Section titles")]
    public string matrixTitleLabel = "Embedding Matrix";
    public Vector3 matrixTitleOffset = new Vector3(-2f, 3f, 0f);
    public string vectorGroupTitleLabel = "Token Embedding";
    public Vector3 vectorGroupTitleOffset = new Vector3(0f, 6f, 0f);
    public float titleFontSize = 10f;
    public Color titleColor = Color.white;

    public void Animate(string sentence)
    {
        if (!string.IsNullOrEmpty(sentence))
            tokenCount = sentence.Split(' ').Length;
        CancelInvoke(nameof(Rebuild));
        Clear();
        GameObject slab = CreateSlab();
        CreateLabel(slab.transform);
        CreateSectionTitle(matrixTitleLabel, slabPosition + matrixTitleOffset);
        CreateVectorGroupTitle();
        if (Application.isPlaying)
            StartCoroutine(AnimateLookups(slab));
        else
            ConnectIdsToSlab(slab);
    }

    public void CompleteImmediately()
    {
        StopAllCoroutines();
        Clear();
        GameObject slab = CreateSlab();
        CreateLabel(slab.transform);
        CreateSectionTitle(matrixTitleLabel, slabPosition + matrixTitleOffset);
        CreateVectorGroupTitle();
        ConnectIdsToSlab(slab);

        if (tokenGenerator != null)
        {
            tokenGenerator.SetColumnLabelsVisible(false);
            for (int i = 0; i < tokenCount; i++)
            {
                GameObject tokenChip = tokenGenerator.GetTokenObject(i);
                GameObject idChip = tokenGenerator.GetIdObject(i);
                if (tokenChip != null)
                    tokenChip.SetActive(false);
                if (idChip != null)
                    idChip.SetActive(false);
                LineRenderer tokenLine = tokenGenerator.GetConnectionLine(i);
                if (tokenLine != null)
                    tokenLine.enabled = false;
            }
        }
    }

    private IEnumerator AnimateLookups(GameObject slab)
    {
        float rightFaceX = slab.transform.position.x + slabScale.x / 2f;
        Vector3 matrixCenter = slab.transform.position;

        if (tokenGenerator != null)
            tokenGenerator.SetColumnLabelsVisible(false);

        for (int i = 0; i < tokenCount; i++)
        {
            GameObject tokenChip = tokenGenerator != null ? tokenGenerator.GetTokenObject(i) : null;
            GameObject idChip = tokenGenerator != null ? tokenGenerator.GetIdObject(i) : null;

            LineRenderer tokenLine = tokenGenerator != null ? tokenGenerator.GetConnectionLine(i) : null;
            if (tokenLine != null)
                tokenLine.enabled = false;

            yield return StartCoroutine(MoveAndShrinkPair(tokenChip, idChip, matrixCenter, chipTravelDuration));

            float t = tokenCount > 1 ? (float)i / (tokenCount - 1) : 0.5f;
            float outputEndY = Mathf.Lerp(outputSpreadY / 2f, -outputSpreadY / 2f, t) + slab.transform.position.y;
            Vector3 end = new Vector3(rightFaceX + lineCrossDistance, outputEndY, slab.transform.position.z);

            Color randomColor = lineColors != null && lineColors.Length > 0 ? lineColors[Random.Range(0, lineColors.Length)] : lineColor;
            yield return StartCoroutine(AnimateLine(matrixCenter, end, randomColor));

            CreateVectorAtPoint(end + Vector3.right * vectorGapX, i);
        }
    }

    private IEnumerator MoveAndShrinkPair(GameObject a, GameObject b, Vector3 target, float duration)
    {
        Vector3 aStart = a != null ? a.transform.position : target;
        Vector3 bStart = b != null ? b.transform.position : target;
        Vector3 aScaleStart = a != null ? a.transform.localScale : Vector3.one;
        Vector3 bScaleStart = b != null ? b.transform.localScale : Vector3.one;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (a != null)
            {
                a.transform.position = Vector3.Lerp(aStart, target, t);
                a.transform.localScale = Vector3.Lerp(aScaleStart, Vector3.zero, t);
            }
            if (b != null)
            {
                b.transform.position = Vector3.Lerp(bStart, target, t);
                b.transform.localScale = Vector3.Lerp(bScaleStart, Vector3.zero, t);
            }
            yield return null;
        }
        if (a != null) a.SetActive(false);
        if (b != null) b.SetActive(false);
    }

    private IEnumerator AnimateLine(Vector3 from, Vector3 to, Color color, float duration = 0.35f)
    {
        GameObject lineObj = new GameObject("Embedding_Line");
        lineObj.transform.SetParent(transform);
        EnsureLineMaterial();
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.positionCount = 2;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.startColor = color;
        lr.endColor = color;
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

    public void Rebuild()
    {
        CancelInvoke(nameof(Rebuild));
        Clear();

        GameObject slab = CreateSlab();
        CreateLabel(slab.transform);
        ConnectIdsToSlab(slab);
        CreateSectionTitle(
            matrixTitleLabel,
            slabPosition + matrixTitleOffset
        );
        CreateVectorGroupTitle();
    }

    public void Clear()
    {
        CancelInvoke(nameof(Rebuild));

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

    private GameObject CreateSlab()
    {
        GameObject slab =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        slab.name = "EmbeddingMatrix_We";
        slab.transform.SetParent(transform, false);
        slab.transform.localPosition = slabPosition;
        slab.transform.localScale = slabScale;

        Shader shader = GetCompatibleShader();

        if (shader != null)
        {
            Material material = new Material(shader);
            material.mainTexture = GenerateNoiseTexture();
            slab.GetComponent<Renderer>().material = material;
        }

        return slab;
    }

    private Shader GetCompatibleShader()
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit");

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
        Texture2D texture =
            new Texture2D(textureSize, textureSize);

        texture.filterMode = FilterMode.Point;

        int cellWidth =
            Mathf.Max(1, textureSize / gridColumns);

        int cellHeight =
            Mathf.Max(1, textureSize / gridRows);

        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                int cellX = x / cellWidth;
                int cellY = y / cellHeight;

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
                    (Mathf.PerlinNoise(
                        x * 0.6f,
                        y * 0.6f
                    ) - 0.5f) * grainStrength;

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
                    color = Color.Lerp(
                        color,
                        Color.black,
                        gridLineDarken
                    );

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();

        return texture;
    }

    private void CreateLabel(Transform slabTransform)
    {
        GameObject labelObj =
            new GameObject("Label_We");

        labelObj.transform.SetParent(
            slabTransform.parent,
            false
        );

        labelObj.transform.localPosition =
            labelPosition;

        TextMeshPro tmp =
            labelObj.AddComponent<TextMeshPro>();

        tmp.text = labelText;
        tmp.fontSize = labelFontSize;
        tmp.color = labelColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Midline;

        ApplyThickness(tmp);
    }

    private void CreateSectionTitle(
        string text,
        Vector3 localPosition
    )
    {
        GameObject titleObj =
            new GameObject(
                $"Title_{text.Replace(" ", "")}"
            );

        titleObj.transform.SetParent(
            transform,
            false
        );

        titleObj.transform.localPosition =
            localPosition;

        TextMeshPro tmp =
            titleObj.AddComponent<TextMeshPro>();

        tmp.text = text;
        tmp.fontSize = titleFontSize;
        tmp.color = titleColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Midline;

        ApplyThickness(tmp);
    }

    private void CreateVectorGroupTitle()
    {
        float rightFaceX =
            slabPosition.x +
            slabScale.x / 2f;

        float vectorGroupX =
            rightFaceX +
            lineCrossDistance +
            vectorGapX;

        Vector3 titlePosition =
            new Vector3(
                vectorGroupX,
                slabPosition.y,
                slabPosition.z
            ) + vectorGroupTitleOffset;

        CreateSectionTitle(
            vectorGroupTitleLabel,
            titlePosition
        );
    }

    private void ConnectIdsToSlab(GameObject slab)
    {
        float rightFaceX =
            slab.transform.position.x +
            slabScale.x / 2f;

        Vector3 centerPoint =
            slab.transform.position;

        for (int i = 0; i < tokenCount; i++)
        {
            float idPosY =
                idStartY -
                i * idYStep;

            Vector3 idWorldPos =
                transform.TransformPoint(
                    new Vector3(
                        idStartX,
                        idPosY,
                        0f
                    )
                );

            float t =
                tokenCount > 1
                    ? (float)i / (tokenCount - 1)
                    : 0.5f;

            float outputEndY =
                Mathf.Lerp(
                    outputSpreadY / 2f,
                    -outputSpreadY / 2f,
                    t
                ) + slab.transform.position.y;

            Vector3 start =
                idWorldPos +
                Vector3.right * lineGap;

            Vector3 end =
                new Vector3(
                    rightFaceX + lineCrossDistance,
                    outputEndY,
                    slab.transform.position.z
                );

            if (convergeAtCenter)
                DrawLine(start, centerPoint, end);
            else
                DrawLine(start, end);

            CreateVectorAtPoint(
                end + Vector3.right * vectorGapX,
                i
            );
        }
    }

    private void DrawLine(
        Vector3 from,
        Vector3 to
    )
    {
        DrawLine(new[] { from, to });
    }

    private void DrawLine(
        Vector3 from,
        Vector3 mid,
        Vector3 to
    )
    {
        DrawLine(new[] { from, mid, to });
    }

    private void DrawLine(Vector3[] points)
    {
        GameObject lineObj =
            new GameObject("Id_Matrix_Line");

        lineObj.transform.SetParent(transform);

        EnsureLineMaterial();

        LineRenderer lineRenderer =
            lineObj.AddComponent<LineRenderer>();

        lineRenderer.material = lineMaterial;
        lineRenderer.positionCount = points.Length;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.SetPositions(points);
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.useWorldSpace = true;
    }

    private void CreateVectorAtPoint(
        Vector3 centerPosition,
        int seedIndex
    )
    {
        System.Random random =
            new System.Random(seedIndex * 1000 + 7);

        float totalHeight =
            (vectorDimensions - 1) *
            vectorRowSpacing;

        float topY =
            centerPosition.y +
            totalHeight / 2f;

        GameObject vectorRoot =
            new GameObject(
                $"OutputVector_{seedIndex}"
            );

        vectorRoot.transform.SetParent(
            transform,
            false
        );

        for (int row = 0; row < vectorDimensions; row++)
        {
            float value =
                (float)(
                    random.NextDouble() *
                    (maxValue - minValue) +
                    minValue
                );

            Vector3 rowPosition =
                new Vector3(
                    centerPosition.x,
                    topY - row * vectorRowSpacing,
                    centerPosition.z
                );

            GameObject numberObj =
                new GameObject($"Num_{row}");

            numberObj.transform.SetParent(
                vectorRoot.transform,
                false
            );

            numberObj.transform.position =
                rowPosition;

            TextMeshPro tmp =
                numberObj.AddComponent<TextMeshPro>();

            tmp.text =
                value.ToString("+0.000;-0.000");

            tmp.fontSize =
                vectorNumberFontSize;

            tmp.fontStyle =
                FontStyles.Bold;

            tmp.alignment =
                TextAlignmentOptions.Midline;

            tmp.color =
                value >= 0
                    ? positiveColor
                    : negativeColor;

            ApplyThickness(tmp);
        }

        Vector3 topEdge =
            new Vector3(
                centerPosition.x,
                topY,
                centerPosition.z
            );

        Vector3 bottomEdge =
            new Vector3(
                centerPosition.x,
                topY - totalHeight,
                centerPosition.z
            );

        DrawBracket(
            topEdge,
            bottomEdge,
            true
        );

        DrawBracket(
            topEdge,
            bottomEdge,
            false
        );
    }

    private void DrawBracket(
        Vector3 topPoint,
        Vector3 bottomPoint,
        bool isLeft
    )
    {
        float side =
            isLeft ? -1f : 1f;

        Vector3 offset =
            Vector3.right *
            bracketOffsetX *
            side;

        Vector3 top =
            topPoint + offset;

        Vector3 bottom =
            bottomPoint + offset;

        Vector3 tickDirection =
            Vector3.right *
            bracketTickLength *
            -side;

        Vector3 p0 =
            top + tickDirection;

        Vector3 p1 = top;
        Vector3 p2 = bottom;

        Vector3 p3 =
            bottom + tickDirection;

        GameObject bracketObj =
            new GameObject(
                isLeft
                    ? "Bracket_Left"
                    : "Bracket_Right"
            );

        bracketObj.transform.SetParent(
            transform
        );

        EnsureLineMaterial();

        LineRenderer lineRenderer =
            bracketObj.AddComponent<LineRenderer>();

        lineRenderer.material =
            lineMaterial;

        lineRenderer.positionCount = 4;
        lineRenderer.startWidth =
            bracketLineWidth;

        lineRenderer.endWidth =
            bracketLineWidth;

        lineRenderer.SetPositions(
            new[]
            {
                p0,
                p1,
                p2,
                p3
            }
        );

        lineRenderer.startColor =
            bracketColor;

        lineRenderer.endColor =
            bracketColor;

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
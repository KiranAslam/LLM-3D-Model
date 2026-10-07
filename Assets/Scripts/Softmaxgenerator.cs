using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[ExecuteAlways]
public class SoftmaxGenerator : MonoBehaviour, ISentenceAnimatable
{
    private readonly List<GameObject> slabObjects = new();
    private readonly List<LineRenderer> slabLines = new();
    private bool revealRequested;

    [Header("Mirror these — QKTGenerator ki Inspector values")]
    public Vector3 qktStackPosition = new Vector3(115f, -7f, 0f);
    public int slabCount = 10;
    public float qktSlabGapZ = 9f;
    public float qktSlabStaggerX = 0.1f;
    public float qktSlabStaggerY = 0.1f;
    public Vector3 qktBoxSize = new Vector3(10f, 10f, 1f);

    [Header("Softmax stack (target — QK^T se bara)")]
    public Vector3 softmaxStackPosition = new Vector3(140f, -7f, 0f);
    public float sheetWidth = 13f;
    public float sheetHeight = 13f;
    public float sheetThickness = 1f;
    public float slabGapZ = 9f;
    public float slabStaggerX = 0.1f;
    public float slabStaggerY = 0.1f;

    public enum GridOrientation
    {
        Normal,
        FlipX,
        FlipY,
        FlipBoth
    }

    [Header("Attention grid pattern (5x5 blocks, region split + random shading)")]
    public int gridSize = 5;
    public Color highlightColor = new Color(0.05f, 0.35f, 0.3f);
    public Color maskedColor = Color.white;
    public Color lightShadeMin = new Color(0.55f, 0.85f, 0.8f);
    public Color lightShadeMax = new Color(0.8f, 0.95f, 0.9f);
    public Vector2Int highlightCell = new Vector2Int(0, 4);
    public GridOrientation orientation = GridOrientation.FlipBoth;
    public bool invertShadeRegions = false;
    public bool flipPatternVertically = false;
    public int randomSeed = 42;

    [Header("Lines")]
    public Material lineMaterial;
    public Color lineColor = new Color(0.8f, 0.8f, 0.8f);
    public float lineWidth = 0.1f;

    [Header("Label")]
    public string labelText = "A = Softmax(QK^T/√d)";
    public float labelFontSize = 8f;
    public Color labelColor = Color.white;
    public Vector3 labelOffset = new Vector3(0f, 8f, 0f);

    [Header("Text thickness")]
    public float textThickness = 0.15f;

    [Header("Token labels (front sheet ke around)")]
    public List<string> tokenLabels = new List<string>
    {
        "The",
        "Cat",
        "Sat",
        "On",
        "The"
    };

    public float tokenLabelFontSize = 4f;
    public Color tokenLabelColor = Color.white;
    public float columnLabelYOffset = 1f;
    public float columnLabelHorizontalGap = 0.8f;
    public float rowLabelXOffset = 1f;

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

    public void Animate(string sentence)
    {
        if (!string.IsNullOrWhiteSpace(sentence))
            tokenLabels = new List<string>(sentence.Split(' '));

        Rebuild();
    }

    public void CompleteImmediately()
    {
        StopAllCoroutines();
        Rebuild();
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
            Vector3 qktPos = qktStackPosition + new Vector3(
                i * qktSlabStaggerX,
                i * qktSlabStaggerY,
                i * qktSlabGapZ
            );

            Vector3 qktRightEdge = qktPos + new Vector3(
                qktBoxSize.x / 2f,
                0f,
                0f
            );

            Vector3 softmaxPos = softmaxStackPosition + new Vector3(
                i * slabStaggerX,
                i * slabStaggerY,
                i * slabGapZ
            );

            Vector3 softmaxLeftEdge = softmaxPos - new Vector3(
                sheetWidth / 2f,
                0f,
                0f
            );

            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = $"SoftmaxSlab_{i}";
            slab.transform.SetParent(transform, false);
            slab.transform.localPosition = softmaxPos;
            slab.transform.localScale = slabSize;

            Material mat = new Material(GetCompatibleShader());
            mat.mainTexture = GenerateAttentionGridTexture();

            Renderer renderer = slab.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = mat;

            LineRenderer line = DrawLine(qktRightEdge, softmaxLeftEdge);
            slabObjects.Add(slab);
            slabLines.Add(line);

            if (i == 0)
                CreateGridLabels(softmaxPos);
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

    private Texture2D GenerateAttentionGridTexture()
    {
        int textureSize = 256;

        Texture2D tex = new Texture2D(
            textureSize,
            textureSize
        );

        tex.filterMode = FilterMode.Point;

        int cellSize = Mathf.Max(
            1,
            textureSize / gridSize
        );

        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                int rawCol = x / cellSize;
                int rawRow = y / cellSize;

                bool flipX =
                    orientation == GridOrientation.FlipX ||
                    orientation == GridOrientation.FlipBoth;

                bool flipY =
                    orientation == GridOrientation.FlipY ||
                    orientation == GridOrientation.FlipBoth;

                int col = flipX
                    ? gridSize - 1 - rawCol
                    : rawCol;

                int row = flipY
                    ? gridSize - 1 - rawRow
                    : rawRow;

                Color cellColor;

                if (col == highlightCell.x && row == highlightCell.y)
                {
                    cellColor = highlightColor;
                }
                else
                {
                    int patternRow = flipPatternVertically
                        ? gridSize - 1 - row
                        : row;

                    bool isMaskedRegion = col > patternRow;

                    if (invertShadeRegions)
                        isMaskedRegion = !isMaskedRegion;

                    if (isMaskedRegion)
                    {
                        cellColor = maskedColor;
                    }
                    else
                    {
                        System.Random cellRng = new System.Random(
                            (row * gridSize + col) * 97 + randomSeed
                        );

                        float t = (float)cellRng.NextDouble();

                        cellColor = Color.Lerp(
                            lightShadeMin,
                            lightShadeMax,
                            t
                        );
                    }
                }

                tex.SetPixel(x, y, cellColor);
            }
        }

        tex.Apply();
        return tex;
    }

    private LineRenderer DrawLine(Vector3 from, Vector3 to)
    {
        GameObject lineObj = new GameObject("Softmax_Line");
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
        GameObject labelObj = new GameObject("Label_Softmax");
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition =
            softmaxStackPosition + labelOffset;

        TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
        if (!EnsureFontAsset(tmp))
        {
            DestroyGeneratedObject(labelObj);
            return;
        }

        tmp.text = labelText;
        tmp.fontSize = labelFontSize;
        tmp.color = labelColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Midline;

        ApplyThickness(tmp);
    }

    private void CreateGridLabels(Vector3 frontSheetPos)
    {
        float cellW = sheetWidth / gridSize;
        float cellH = sheetHeight / gridSize;

        float leftX = frontSheetPos.x - sheetWidth / 2f;
        float topY = frontSheetPos.y + sheetHeight / 2f;
        float bottomY = frontSheetPos.y - sheetHeight / 2f;

        int columnCount = Mathf.Min(gridSize, tokenLabels.Count);
        List<TextMeshPro> columnLabels = new List<TextMeshPro>(columnCount);
        List<float> columnLabelWidths = new List<float>(columnCount);
        float totalColumnLabelWidth = Mathf.Max(0, columnCount - 1) * columnLabelHorizontalGap;

        for (int col = 0; col < columnCount; col++)
        {
            TextMeshPro label = CreateTokenLabel(tokenLabels[col]);
            if (label == null)
                return;

            float labelWidth = Mathf.Max(0.1f, label.GetPreferredValues(tokenLabels[col]).x);
            columnLabels.Add(label);
            columnLabelWidths.Add(labelWidth);
            totalColumnLabelWidth += labelWidth;
        }

        float nextColumnLabelX = frontSheetPos.x - totalColumnLabelWidth / 2f;
        for (int col = 0; col < columnCount; col++)
        {
            float labelWidth = columnLabelWidths[col];
            TextMeshPro label = columnLabels[col];
            label.rectTransform.sizeDelta = new Vector2(labelWidth, cellH * 0.8f);
            label.transform.localPosition = new Vector3(
                nextColumnLabelX + labelWidth / 2f,
                topY + columnLabelYOffset,
                frontSheetPos.z
            );
            nextColumnLabelX += labelWidth + columnLabelHorizontalGap;
        }

        for (
            int row = 0;
            row < gridSize && row < tokenLabels.Count;
            row++
        )
        {
            float y = bottomY + (row + 0.5f) * cellH;

            Vector3 pos = new Vector3(
                leftX - rowLabelXOffset,
                y,
                frontSheetPos.z
            );

            TextMeshPro label = CreateTokenLabel(tokenLabels[row]);
            if (label == null)
                return;

            label.rectTransform.sizeDelta = new Vector2(cellW * 0.9f, cellH * 0.8f);
            label.transform.localPosition = pos;
        }
    }

    private TextMeshPro CreateTokenLabel(string text)
    {
        GameObject labelObj = new GameObject(
            $"TokenLabel_{text}"
        );

        labelObj.transform.SetParent(transform, false);

        TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
        if (!EnsureFontAsset(tmp))
        {
            DestroyGeneratedObject(labelObj);
            return null;
        }

        tmp.text = text;
        tmp.fontSize = tokenLabelFontSize;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.color = tokenLabelColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.ForceMeshUpdate();

        ApplyThickness(tmp);
        return tmp;
    }

    private bool EnsureFontAsset(TextMeshPro textComponent)
    {
        if (textComponent.font == null)
            textComponent.font = TMP_Settings.defaultFontAsset;

        if (textComponent.font == null)
            textComponent.font = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/LiberationSans SDF"
            );

        if (textComponent.font != null)
            return true;

        Debug.LogError(
            "TextMeshPro font asset is missing. Import TMP Essential Resources or assign a default TMP font asset.",
            this
        );
        return false;
    }

    private void DestroyGeneratedObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
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
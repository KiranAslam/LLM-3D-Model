using System.Collections;
using System.Globalization;
using UnityEngine;
using TMPro;

[ExecuteAlways]
public class LogitsProbabilitiesGenerator : MonoBehaviour, ISentenceAnimatable
{
    public Transform[] anchors;
    public IntegratedCameraController cameraController;
    public float stepDelay = 0.6f;
    public float pairRevealDelay = 0.3f;

    [Header("Mirror this — Softmax block ka right edge, world position")]
    public Vector3 sourcePoint = new Vector3(530f, 8f, 0f);

    [Header("Pair layout — highest probability back se start hoti hai")]
    public Vector3 logitBasePosition = new Vector3(570f, 8f, 0f);
    public Vector3 probBasePosition = new Vector3(600f, 8f, 0f);
    public float pairZGap = 2f;
    public float pairVerticalSpacing = 3f;

    [Header("Box shape")]
    public Vector3 boxSize = new Vector3(1.2f, 3f, 1.2f);
    public Color logitColor = Color.black;
    public Color probColor = new Color(0f, 0.35f, 0.32f);

    [Header("Values — har box ki apni value/label")]
    public string[] logitValues = { "19.6", "18.9", "18.2", "18.0", "17.0" };
    public string[] probabilityValues = { "33.1%", "16.6%", "8.2%", "4.6%", "2.5%" };
    public string[] probabilityWordLabels = { "", "", "(empty)", "and", "?" };

    [Header("Group titles")]
    public string logitsGroupTitle = "Raw Logits";
    public Color logitsTitleColor = new Color(0.9f, 0.9f, 0.9f);
    public string probsGroupTitle = "Probabilities";
    public Color probsTitleColor = new Color(0.2f, 0.8f, 0.7f);
    public float groupTitleFontSize = 9f;
    public Vector3 titleOffset = new Vector3(0f, 4.5f, 0f);

    [Header("Value labels")]
    public float valueLabelFontSize = 5f;
    public Color valueLabelColor = Color.white;
    public Vector3 valueLabelOffset = new Vector3(0f, 2f, 0f);
    public Vector3 wordLabelOffset = new Vector3(0f, 2.5f, 0f);
    public float chipPaddingX = 2f;
    public float chipPaddingY = 1.3f;
    public float chipOutlineThickness = 0.08f;
    public float chipCornerRadius = 0.35f;
    public Color chipOutlineColor = new Color(0.55f, 0.9f, 0.82f);

    [Header("Lines")]
    public Color sourceLineColor = new Color(0.549f, 0.851f, 0.8f);
    public float sourceLineWidth = 0.6f;
    public Color pairLineColor = new Color(1f, 1f, 1f, 0.6f);
    public float pairLineWidth = 0.15f;
    public Material lineMaterial;

    [Header("Text thickness")]
    public float textThickness = 0.4f;

    private float[] probabilityPercentages;

    public void Animate(string sentence)
    {
        ApplyDummyData(sentence);
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

        int pairCount = GetPairCount();

        if (pairCount == 0)
            return;

        int middleIndex = pairCount / 2;
        int[] displayOrder = GetDisplayOrder(pairCount);

        CreateLabel(
            logitsGroupTitle,
            logitBasePosition + titleOffset,
            groupTitleFontSize,
            logitsTitleColor
        );

        CreateLabel(
            probsGroupTitle,
            probBasePosition + titleOffset,
            groupTitleFontSize,
            probsTitleColor
        );

        Vector3[] logitPositions = new Vector3[pairCount];
        Vector3[] probabilityPositions = new Vector3[pairCount];

        for (int i = 0; i < pairCount; i++)
        {
            Vector3 offset = GetPairOffset(i, middleIndex);
            logitPositions[i] = logitBasePosition + offset;
            probabilityPositions[i] = probBasePosition + offset;
        }

        Vector3 sourceEntry = logitPositions[middleIndex] - Vector3.right * (boxSize.x / 2f);
        DrawLine(sourcePoint, sourceEntry, sourceLineColor, sourceLineWidth);

        for (int slot = 0; slot < pairCount; slot++)
        {
            int dataIndex = displayOrder[slot];
            BuildBox(logitPositions[slot], boxSize, logitColor, $"Logit_{dataIndex}");
            CreateValueChip(logitValues[dataIndex], logitPositions[slot] + valueLabelOffset, $"LogitValue_{dataIndex}");
        }

        for (int slot = 0; slot < pairCount; slot++)
        {
            int dataIndex = displayOrder[slot];
            float barHeight = GetBarHeight(dataIndex);
            Vector3 barPosition = GetProbabilityBarPosition(probabilityPositions[slot], barHeight);
            Vector3 logitExit = logitPositions[slot] + Vector3.right * (boxSize.x / 2f);
            Vector3 probEntry = barPosition - Vector3.right * (boxSize.x / 2f);
            DrawLine(logitExit, probEntry, pairLineColor, pairLineWidth);
        }

        for (int slot = 0; slot < pairCount; slot++)
        {
            int dataIndex = displayOrder[slot];
            float barHeight = GetBarHeight(dataIndex);
            Vector3 barPosition = GetProbabilityBarPosition(probabilityPositions[slot], barHeight);
            BuildBox(barPosition, new Vector3(boxSize.x, barHeight, boxSize.z), probColor, $"Prob_{dataIndex}");
            CreateValueChip(
                GetProbabilityChipText(dataIndex),
                GetProbabilityChipPosition(barPosition, dataIndex),
                $"Probability_{dataIndex}"
            );
        }
    }

    private IEnumerator AnimateBuild()
    {
        int pairCount = GetPairCount();

        if (pairCount == 0)
            yield break;

        int middleIndex = pairCount / 2;
        int[] displayOrder = GetDisplayOrder(pairCount);

        if (cameraController != null && anchors != null && anchors.Length > 0 && anchors[0] != null)
            cameraController.FocusOnStage(anchors[0], 3f);

        CreateLabel(logitsGroupTitle, logitBasePosition + titleOffset, groupTitleFontSize, logitsTitleColor);
        CreateLabel(probsGroupTitle, probBasePosition + titleOffset, groupTitleFontSize, probsTitleColor);

        Vector3[] logitPositions = new Vector3[pairCount];
        Vector3[] probabilityPositions = new Vector3[pairCount];
        for (int i = 0; i < pairCount; i++)
        {
            Vector3 offset = GetPairOffset(i, middleIndex);
            logitPositions[i] = logitBasePosition + offset;
            probabilityPositions[i] = probBasePosition + offset;
        }

        Vector3 sourceEntry = logitPositions[middleIndex] - Vector3.right * (boxSize.x / 2f);
        DrawLine(sourcePoint, sourceEntry, sourceLineColor, sourceLineWidth);
        yield return new WaitForSeconds(stepDelay);

        for (int slot = 0; slot < pairCount; slot++)
        {
            int dataIndex = displayOrder[slot];
            BuildBox(logitPositions[slot], boxSize, logitColor, $"Logit_{dataIndex}");
            CreateValueChip(logitValues[dataIndex], logitPositions[slot] + valueLabelOffset, $"LogitValue_{dataIndex}");
            yield return new WaitForSeconds(pairRevealDelay);
        }

        if (cameraController != null && anchors != null && anchors.Length > 1 && anchors[1] != null)
            cameraController.FocusOnStage(anchors[1], 3f);

        for (int slot = 0; slot < pairCount; slot++)
        {
            int dataIndex = displayOrder[slot];
            float barHeight = GetBarHeight(dataIndex);
            Vector3 barPosition = GetProbabilityBarPosition(probabilityPositions[slot], barHeight);
            Vector3 logitExit = logitPositions[slot] + Vector3.right * (boxSize.x / 2f);
            Vector3 probEntry = barPosition - Vector3.right * (boxSize.x / 2f);
            DrawLine(logitExit, probEntry, pairLineColor, pairLineWidth);
            yield return new WaitForSeconds(pairRevealDelay);
        }

        for (int slot = 0; slot < pairCount; slot++)
        {
            int dataIndex = displayOrder[slot];
            float barHeight = GetBarHeight(dataIndex);
            float bottomY = probabilityPositions[slot].y - boxSize.y / 2f;
            GameObject bar = BuildBox(
                new Vector3(probabilityPositions[slot].x, bottomY + 0.0005f, probabilityPositions[slot].z),
                new Vector3(boxSize.x, 0.001f, boxSize.z),
                probColor,
                $"Prob_{dataIndex}"
            );

            if (bar != null)
                yield return StartCoroutine(AnimateProbabilityBar(bar, bottomY, barHeight));

            Vector3 barPosition = GetProbabilityBarPosition(probabilityPositions[slot], barHeight);
            CreateValueChip(
                GetProbabilityChipText(dataIndex),
                GetProbabilityChipPosition(barPosition, dataIndex),
                $"Probability_{dataIndex}"
            );

            yield return new WaitForSeconds(pairRevealDelay);
        }
    }

    private void ApplyDummyData(string sentence)
    {
        if (DummyDataProvider.Instance == null || string.IsNullOrEmpty(sentence))
            return;

        DummyDataProvider.SentenceEntry entry = DummyDataProvider.Instance.GetEntry(sentence);
        if (entry == null || entry.predictedProbabilities == null || entry.predictedWords == null)
            return;

        int count = Mathf.Min(entry.predictedProbabilities.Length, entry.predictedWords.Length);
        if (count == 0)
            return;

        float total = 0f;
        for (int i = 0; i < count; i++)
            total += Mathf.Max(0f, entry.predictedProbabilities[i]);

        if (total <= 0f)
            return;

        logitValues = new string[count];
        probabilityValues = new string[count];
        probabilityWordLabels = new string[count];
        probabilityPercentages = new float[count];

        for (int i = 0; i < count; i++)
        {
            float percentage = Mathf.Max(0f, entry.predictedProbabilities[i]);
            float normalizedProbability = percentage / total;
            probabilityPercentages[i] = percentage;
            logitValues[i] = Mathf.Log(Mathf.Max(normalizedProbability, 0.000001f))
                .ToString("0.00", CultureInfo.InvariantCulture);
            probabilityValues[i] = percentage.ToString("0.####", CultureInfo.InvariantCulture) + "%";
            probabilityWordLabels[i] = entry.predictedWords[i];
        }
    }

    private int GetPairCount()
    {
        return Mathf.Min(
            logitValues != null ? logitValues.Length : 0,
            probabilityValues != null ? probabilityValues.Length : 0
        );
    }

    private int[] GetDisplayOrder(int pairCount)
    {
        int[] order = new int[pairCount];
        for (int i = 0; i < pairCount; i++)
            order[i] = i;

        System.Array.Sort(order, (left, right) =>
            GetBarHeight(right).CompareTo(GetBarHeight(left)));
        return order;
    }

    private Vector3 GetPairOffset(int slot, int middleIndex)
    {
        return new Vector3(
            0f,
            -slot * pairVerticalSpacing,
            (middleIndex - slot) * pairZGap
        );
    }

    private Vector3 GetWordLabelPosition(Vector3 barPosition, float barHeight)
    {
        return new Vector3(
            barPosition.x + wordLabelOffset.x,
            barPosition.y + barHeight / 2f + wordLabelOffset.y,
            barPosition.z + wordLabelOffset.z
        );
    }

    private string GetProbabilityChipText(int index)
    {
        string probability = probabilityValues[index];
        if (probabilityWordLabels == null ||
            index >= probabilityWordLabels.Length ||
            string.IsNullOrEmpty(probabilityWordLabels[index]))
            return probability;

        return $"{probabilityWordLabels[index]}: {probability}";
    }

    private Vector3 GetProbabilityChipPosition(Vector3 barPosition, int index)
    {
        float barHeight = GetBarHeight(index);

        if (probabilityWordLabels != null &&
            index < probabilityWordLabels.Length &&
            !string.IsNullOrEmpty(probabilityWordLabels[index]))
            return GetWordLabelPosition(barPosition, barHeight);

        return new Vector3(
            barPosition.x + valueLabelOffset.x,
            barPosition.y + barHeight / 2f + valueLabelOffset.y,
            barPosition.z + valueLabelOffset.z
        );
    }

    private float GetBarHeight(int index)
    {
        float percentage = probabilityPercentages != null && index < probabilityPercentages.Length
            ? probabilityPercentages[index]
            : ParsePercentage(probabilityValues[index]);
        return boxSize.y * Mathf.Clamp01(percentage / 100f);
    }

    private static float ParsePercentage(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            string numericValue = value.Replace("%", "").Trim();
            if (float.TryParse(numericValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float percentage))
                return Mathf.Max(0f, percentage);
        }
        return 0f;
    }

    private Vector3 GetProbabilityBarPosition(Vector3 basePosition, float barHeight)
    {
        float bottomY = basePosition.y - boxSize.y / 2f;
        return new Vector3(basePosition.x, bottomY + barHeight / 2f, basePosition.z);
    }

    private IEnumerator AnimateProbabilityBar(GameObject bar, float bottomY, float targetHeight)
    {
        float duration = pairRevealDelay;
        float elapsed = 0f;
        float initialHeight = 0.001f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float height = Mathf.Lerp(initialHeight, targetHeight, t);
            bar.transform.localScale = new Vector3(boxSize.x, height, boxSize.z);
            bar.transform.localPosition = new Vector3(
                bar.transform.localPosition.x,
                bottomY + height / 2f,
                bar.transform.localPosition.z
            );
            yield return null;
        }

        bar.transform.localScale = new Vector3(boxSize.x, targetHeight, boxSize.z);
        bar.transform.localPosition = new Vector3(
            bar.transform.localPosition.x,
            bottomY + targetHeight / 2f,
            bar.transform.localPosition.z
        );
    }

    private GameObject CreateValueChip(string text, Vector3 localPosition, string objectName)
    {
        GameObject chipObject = new GameObject(objectName);
        chipObject.transform.SetParent(transform, false);
        chipObject.name = objectName;
        chipObject.transform.localPosition = localPosition;

        TextMeshPro textComponent = chipObject.AddComponent<TextMeshPro>();
        if (!EnsureFontAsset(textComponent))
        {
            DestroyGeneratedObject(chipObject);
            return null;
        }

        textComponent.text = text;
        textComponent.fontSize = valueLabelFontSize;
        textComponent.color = valueLabelColor;
        textComponent.fontStyle = FontStyles.Normal;
        textComponent.alignment = TextAlignmentOptions.Midline;
        textComponent.ForceMeshUpdate();
        ApplyThickness(textComponent);

        float width = textComponent.bounds.size.x + chipPaddingX;
        float height = textComponent.bounds.size.y + chipPaddingY;
        CreateRoundedChipBackground(chipObject.transform, width, height);

        return chipObject;
    }

    private void CreateRoundedChipBackground(
        Transform parent,
        float width,
        float height)
    {
        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "Chip";
        background.transform.SetParent(parent, false);
        background.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        background.transform.localScale = new Vector3(width, height, 1f);

        Collider backgroundCollider = background.GetComponent<Collider>();
        if (backgroundCollider != null)
            DestroyGeneratedObject(backgroundCollider);

        Renderer layerRenderer = background.GetComponent<Renderer>();
        Shader chipShader = Shader.Find("Sprites/Default");
        if (layerRenderer == null || chipShader == null)
            return;

        Material chipMaterial = new Material(chipShader);
        chipMaterial.mainTexture = CreateRoundedChipTexture(width, height);
        chipMaterial.renderQueue = 2999;
        layerRenderer.material = chipMaterial;
        layerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        layerRenderer.receiveShadows = false;
    }

    private Texture2D CreateRoundedChipTexture(float width, float height)
    {
        const int textureWidth = 256;
        const int textureHeight = 128;
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;
        float radius = Mathf.Min(chipCornerRadius, halfWidth, halfHeight);
        float outline = Mathf.Max(0f, chipOutlineThickness);
        float innerRadius = Mathf.Max(0f, radius - outline);
        Color fillColor = new Color(0.06f, 0.06f, 0.06f);

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float localX = ((x + 0.5f) / textureWidth * 2f - 1f) * halfWidth;
                float localY = ((y + 0.5f) / textureHeight * 2f - 1f) * halfHeight;

                if (!IsInsideRoundedRectangle(localX, localY, halfWidth, halfHeight, radius))
                    continue;

                bool insideFill = IsInsideRoundedRectangle(
                    localX,
                    localY,
                    Mathf.Max(0f, halfWidth - outline),
                    Mathf.Max(0f, halfHeight - outline),
                    innerRadius
                );
                texture.SetPixel(x, y, insideFill ? fillColor : chipOutlineColor);
            }
        }

        texture.Apply();
        return texture;
    }

    private static bool IsInsideRoundedRectangle(
        float x,
        float y,
        float halfWidth,
        float halfHeight,
        float radius)
    {
        float cornerX = Mathf.Max(Mathf.Abs(x) - (halfWidth - radius), 0f);
        float cornerY = Mathf.Max(Mathf.Abs(y) - (halfHeight - radius), 0f);
        return cornerX * cornerX + cornerY * cornerY <= radius * radius;
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

    private GameObject BuildBox(
        Vector3 localPosition,
        Vector3 size,
        Color color,
        string objName)
    {
        GameObject box =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        box.name = objName;
        box.transform.SetParent(transform, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = size;

        Shader shader = GetCompatibleShader();

        if (shader == null)
            return box;

        Material mat = new Material(shader);
        mat.color = color;

        Renderer renderer = box.GetComponent<Renderer>();

        if (renderer != null)
            renderer.material = mat;

        return box;
    }

    private Shader GetCompatibleShader()
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit");

        if (shader != null)
            return shader;

        shader = Shader.Find("Standard");

        if (shader != null)
            return shader;

        return Shader.Find("Sprites/Default");
    }

    private void DrawLine(
        Vector3 fromLocal,
        Vector3 toLocal,
        Color color,
        float width)
    {
        EnsureLineMaterial();

        if (lineMaterial == null)
            return;

        GameObject lineObj =
            new GameObject("LogitsProb_Line");

        lineObj.transform.SetParent(transform, false);

        LineRenderer lineRenderer =
            lineObj.AddComponent<LineRenderer>();

        lineRenderer.material = lineMaterial;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;

        lineRenderer.SetPosition(
            0,
            transform.TransformPoint(fromLocal)
        );

        lineRenderer.SetPosition(
            1,
            transform.TransformPoint(toLocal)
        );

        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.useWorldSpace = true;
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
        Color color)
    {
        GameObject labelObj =
            new GameObject($"Label_{text}");

        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition = localPos;

        TextMeshPro tmp =
            labelObj.AddComponent<TextMeshPro>();

        if (!EnsureFontAsset(tmp))
        {
            DestroyGeneratedObject(labelObj);
            return;
        }

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Midline;

        ApplyThickness(tmp);
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
            "TextMeshPro font asset is missing. Assign a default TMP font or restore the LiberationSans SDF resource.",
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
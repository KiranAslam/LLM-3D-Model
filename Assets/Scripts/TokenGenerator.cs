using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[ExecuteAlways]
public class TokenGenerator : MonoBehaviour, ISentenceAnimatable
{
    [Header("Prefabs")]
    public GameObject tokenPrefab;
    public GameObject idPrefab;

    [Header("Line settings")]
    public Material lineMaterial;
    public Color lineColor = Color.white;
    public float lineWidth = 0.3f;
    public float lineGap = 0.3f;

    [Header("Token data")]
    public List<string> tokens = new()
    {
        "The",
        " cat",
        " sat",
        " on",
        " the"
    };

    public List<int> tokenIds = new()
    {
        504,
        2644,
        3134,
        260,
        9444
    };

    [Header("Fixed layout")]
    public float startX = -9f;
    public float startY = 9f;
    public float yStep = 3f;
    public float idOffsetX = 3f;

    [Header("Token movement")]
    public float tokenLeftRight = 0f;
    public float tokenForwardBack = 0f;
    public float idLeftRight = 0f;
    public float idForwardBack = 0f;

    [Header("Row animation")]
    public bool enableRowAnimation = false;
    public float rowAnimationSpeed = 1f;
    public float rowAnimationAmplitude = 0.6f;
    public float rowAnimationOffset = 0.8f;

    [Header("Font control")]
    public float tokenFontSize = 8f;
    public float idFontSize = 5f;
    public Color tokenTextColor = Color.black;
    public Color idTextColor = Color.black;

    [Header("Fine alignment")]
    public float idYOffset = 0f;

    [Header("Text boldness")]
    public bool useBoldText = true;

    [Header("Column labels")]
    public bool useRevealAnimation = true;
    public float revealStaggerDelay = 0.4f;
    public string tokenColumnLabel = "Token";
    public string idColumnLabel = "ID";
    public float columnLabelYOffset = 1.8f;
    public float columnLabelXOffset = 0.5f;
    public float columnLabelFontSize = 8f;
    public Color columnLabelColor = Color.black;

    public int chipCornerRadius = 18;
    public int chipBorderThickness = 6;
    public Color chipFillColor = new Color(0.06f, 0.06f, 0.06f);
    public Color chipBorderColor = new Color(0.114f, 0.62f, 0.459f);
    public float chipPaddingX = 0.7f;
    public float chipPaddingY = 0.5f;
    public float tokenChipExtraPaddingX = 0f;
    public float tokenChipExtraPaddingY = 0f;
    public float idChipExtraPaddingX = 0f;
    public float idChipExtraPaddingY = 0f;
    public float chipPixelsPerUnit = 100f;
    public bool alignChipsLeft = true;
    public float columnGap = 1.5f;

    [Header("Text thickness")]
    public float textThickness = 0.15f;

    private readonly List<GameObject> tokenObjects = new();
    private readonly List<GameObject> idObjects = new();
    private readonly List<LineRenderer> connectionLines = new();
    private TextMeshPro tokenColumnLabelText;
    private TextMeshPro idColumnLabelText;
    private bool suppressRevealAnimation;

    public GameObject GetTokenObject(int index)
    {
        return index >= 0 && index < tokenObjects.Count ? tokenObjects[index] : null;
    }

    public GameObject GetIdObject(int index)
    {
        return index >= 0 && index < idObjects.Count ? idObjects[index] : null;
    }

    public LineRenderer GetConnectionLine(int index)
    {
        return index >= 0 && index < connectionLines.Count ? connectionLines[index] : null;
    }

    public void SetColumnLabelsVisible(bool visible)
    {
        if (tokenColumnLabelText != null)
            tokenColumnLabelText.gameObject.SetActive(visible);
        if (idColumnLabelText != null)
            idColumnLabelText.gameObject.SetActive(visible);
    }

    private static readonly Dictionary<string, Material> chipMaterialCache = new();

    public void Animate(string sentence)
    {
        suppressRevealAnimation = false;

        if (!string.IsNullOrEmpty(sentence))
        {
            string[] words = sentence.Split(' ');
            tokens.Clear();
            for (int i = 0; i < words.Length; i++)
                tokens.Add(i == 0 ? words[i] : " " + words[i]);
            tokenIds.Clear();
            tokenIds.AddRange(DummyDataProvider.GenerateTokenIds(sentence));
        }
        Rebuild();
    }

    public void ShowDefaultView()
    {
        StopAllCoroutines();
        suppressRevealAnimation = true;
        Rebuild();
    }

    public void Rebuild()
    {
        CancelInvoke(nameof(Rebuild));
        Clear();
        GenerateTokens();
        CreateColumnLabels();
        if (suppressRevealAnimation)
            SetColumnLabelsVisible(false);
        if (useRevealAnimation && Application.isPlaying && !suppressRevealAnimation)
            StartCoroutine(AnimateReveal());
    }

    public void CompleteImmediately()
    {
        StopAllCoroutines();
        bool wasReveal = useRevealAnimation;
        useRevealAnimation = false;
        Rebuild();
        useRevealAnimation = wasReveal;
    }

    private IEnumerator AnimateReveal()
    {
        int count = Mathf.Min(tokenObjects.Count, idObjects.Count);

        for (int i = 0; i < count; i++)
        {
            tokenObjects[i].SetActive(false);
            idObjects[i].SetActive(false);
            if (i < connectionLines.Count && connectionLines[i] != null)
                connectionLines[i].enabled = false;
        }

        for (int i = 0; i < count; i++)
        {
            yield return new WaitForSeconds(revealStaggerDelay);
            tokenObjects[i].SetActive(true);
        }

        for (int i = 0; i < count; i++)
        {
            yield return new WaitForSeconds(revealStaggerDelay);
            if (i < connectionLines.Count && connectionLines[i] != null)
                connectionLines[i].enabled = true;
        }

        for (int i = 0; i < count; i++)
        {
            yield return new WaitForSeconds(revealStaggerDelay);
            idObjects[i].SetActive(true);
        }
    }

    public void Clear()
    {
        CancelInvoke(nameof(Rebuild));

        tokenObjects.Clear();
        idObjects.Clear();
        connectionLines.Clear();
        tokenColumnLabelText = null;
        idColumnLabelText = null;

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

    private void Update()
    {
        if (enableRowAnimation)
            AnimateRows();
        else if (alignChipsLeft)
            AlignChipsToObjects();

        UpdateColumnLabels();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        CancelInvoke(nameof(Rebuild));
        Invoke(nameof(Rebuild), 0.05f);
    }

    public Material GetChipMaterial(float worldWidth, float worldHeight)
    {
        int width = Mathf.Clamp(Mathf.RoundToInt(worldWidth * chipPixelsPerUnit / 4f) * 4, 16, 1024);
        int height = Mathf.Clamp(Mathf.RoundToInt(worldHeight * chipPixelsPerUnit / 4f) * 4, 16, 1024);

        string key =
            $"{width}x{height}|{chipCornerRadius}|{chipBorderThickness}|" +
            $"{ColorUtility.ToHtmlStringRGBA(chipFillColor)}|{ColorUtility.ToHtmlStringRGBA(chipBorderColor)}";

        if (chipMaterialCache.TryGetValue(key, out Material cached) && cached != null)
            return cached;

        Texture2D tex = GenerateChipTexture(width, height, chipCornerRadius, chipBorderThickness, chipFillColor, chipBorderColor);
        Shader shader = Shader.Find("Sprites/Default");
        Material mat = new Material(shader);
        mat.mainTexture = tex;
        // Draw before the TextMeshPro text (queue 3000) so the chip never covers the text when rotated.
        mat.renderQueue = 2999;
        chipMaterialCache[key] = mat;
        return mat;
    }

    private Texture2D GenerateChipTexture(int width, int height, int cornerRadius, int borderThickness, Color fillColor, Color borderColor)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float halfW = width / 2f;
        float halfH = height / 2f;
        float radius = Mathf.Min(cornerRadius, Mathf.Min(halfW, halfH));
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float px = x - halfW + 0.5f;
                float py = y - halfH + 0.5f;
                float dx = Mathf.Max(Mathf.Abs(px) - (halfW - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(py) - (halfH - radius), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;

                float outerAlpha = Mathf.Clamp01(0.5f - dist);
                float borderBlend = Mathf.Clamp01(dist + borderThickness + 0.5f);
                Color c = Color.Lerp(fillColor, borderColor, borderBlend);
                c.a *= outerAlpha;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    private void CenterChipOnText(Transform chip, Transform textChild, TextMeshPro tmp)
    {
        Vector3 worldCenter = textChild.TransformPoint(tmp.bounds.center);
        Vector3 local = chip.parent.InverseTransformPoint(worldCenter);
        chip.localPosition = new Vector3(local.x, local.y, chip.localPosition.z);
    }

    private void AlignChipLeft(Transform obj, Transform chip)
    {
        Renderer chipRenderer = chip.GetComponent<Renderer>();
        if (chipRenderer == null)
            return;

        float dx = obj.position.x - chipRenderer.bounds.min.x;
        for (int i = 0; i < obj.childCount; i++)
            obj.GetChild(i).position += Vector3.right * dx;
    }

    private Bounds GetChipBounds(Transform obj)
    {
        Transform chip = obj.Find("Chip");
        Renderer chipRenderer = chip != null ? chip.GetComponent<Renderer>() : null;
        return chipRenderer != null ? chipRenderer.bounds : new Bounds(obj.position, Vector3.zero);
    }

    private void AlignChipsToObjects()
    {
        for (int i = 0; i < tokenObjects.Count; i++)
        {
            Transform chip = tokenObjects[i] != null ? tokenObjects[i].transform.Find("Chip") : null;
            if (chip != null)
                AlignChipLeft(tokenObjects[i].transform, chip);
        }

        for (int i = 0; i < idObjects.Count; i++)
        {
            Transform chip = idObjects[i] != null ? idObjects[i].transform.Find("Chip") : null;
            if (chip != null)
                AlignChipLeft(idObjects[i].transform, chip);
        }
    }

    private void GenerateTokens()
    {
        if (tokenPrefab == null || idPrefab == null)
        {
            Debug.LogWarning(
                "TokenGenerator requires both tokenPrefab and idPrefab.",
                this
            );
            return;
        }

        int count = Mathf.Min(tokens.Count, tokenIds.Count);

        for (int i = 0; i < count; i++)
        {
            GameObject tokenObj = Instantiate(tokenPrefab, transform);
            tokenObjects.Add(tokenObj);
            tokenObj.transform.localPosition = GetTokenPosition(i);

            Transform tokenTextChild = tokenObj.transform.Find("TokenText");

            if (tokenTextChild == null)
            {
                Debug.LogError(
                    $"TokenPrefab is missing the 'TokenText' child at index {i}.",
                    this
                );
                continue;
            }

            TextMeshPro tokenTMP =
                tokenTextChild.GetComponent<TextMeshPro>();

            if (tokenTMP == null)
            {
                Debug.LogError(
                    $"TokenText is missing a TextMeshPro component at index {i}.",
                    this
                );
                continue;
            }

            ConfigureText(
                tokenTMP,
                tokens[i],
                tokenFontSize,
                tokenTextColor
            );

            GameObject idObj = Instantiate(idPrefab, transform);
            idObjects.Add(idObj);
            idObj.transform.localPosition = GetIdPosition(i);

            Transform idTextChild = idObj.transform.Find("Text (TMP)");

            if (idTextChild == null)
            {
                Debug.LogError(
                    $"IdPrefab is missing the 'Text (TMP)' child at index {i}.",
                    this
                );
                continue;
            }

            TextMeshPro idTMP =
                idTextChild.GetComponent<TextMeshPro>();

            if (idTMP == null)
            {
                Debug.LogError(
                    $"Text (TMP) is missing a TextMeshPro component at index {i}.",
                    this
                );
                continue;
            }

            ConfigureText(
                idTMP,
                tokenIds[i].ToString(),
                idFontSize,
                idTextColor
            );

            tokenTMP.ForceMeshUpdate();
            idTMP.ForceMeshUpdate();

            Transform tokenBorder = tokenObj.transform.Find("Border");
            Transform tokenBackground = tokenObj.transform.Find("Background");
            if (tokenBorder != null && tokenBackground != null)
            {
                Vector3 tokenSize = tokenTMP.bounds.size;
                tokenBackground.localScale = new Vector3(tokenSize.x + 0.6f, tokenSize.y + 0.4f, 1f);
                tokenBorder.localScale = new Vector3(tokenSize.x + 0.9f, tokenSize.y + 0.6f, 1f);
            }

            Transform idBorder = idObj.transform.Find("Border");
            Transform idBackground = idObj.transform.Find("Background");
            if (idBorder != null && idBackground != null)
            {
                Vector3 idSize = idTMP.bounds.size;
                idBackground.localScale = new Vector3(idSize.x + 0.6f, idSize.y + 0.4f, 1f);
                idBorder.localScale = new Vector3(idSize.x + 0.9f, idSize.y + 0.6f, 1f);
            }

            Transform tokenChip = tokenObj.transform.Find("Chip");
            if (tokenChip != null)
            {
                float tokenChipW = tokenTMP.bounds.size.x + chipPaddingX + tokenChipExtraPaddingX;
                float tokenChipH = tokenTMP.bounds.size.y + chipPaddingY + tokenChipExtraPaddingY;
                tokenChip.localScale = new Vector3(tokenChipW, tokenChipH, 1f);
                CenterChipOnText(tokenChip, tokenTextChild, tokenTMP);
                if (alignChipsLeft)
                    AlignChipLeft(tokenObj.transform, tokenChip);
                tokenChip.GetComponent<Renderer>().material = GetChipMaterial(tokenChipW, tokenChipH);
            }

            Transform idChip = idObj.transform.Find("Chip");
            if (idChip != null)
            {
                float idChipW = idTMP.bounds.size.x + chipPaddingX + idChipExtraPaddingX;
                float idChipH = idTMP.bounds.size.y + chipPaddingY + idChipExtraPaddingY;
                idChip.localScale = new Vector3(idChipW, idChipH, 1f);
                CenterChipOnText(idChip, idTextChild, idTMP);
                if (alignChipsLeft)
                    AlignChipLeft(idObj.transform, idChip);
                idChip.GetComponent<Renderer>().material = GetChipMaterial(idChipW, idChipH);
            }

            Bounds tokenChipBounds = GetChipBounds(tokenObj.transform);
            Bounds idChipBounds = GetChipBounds(idObj.transform);
            Vector3 lineStart = new Vector3(tokenChipBounds.max.x + lineGap, tokenChipBounds.center.y, tokenChipBounds.center.z);
            Vector3 lineEnd = new Vector3(idChipBounds.min.x - lineGap, idChipBounds.center.y, idChipBounds.center.z);

            connectionLines.Add(
                DrawConnectionLine(lineStart, lineEnd)
            );
        }
    }

    private void ConfigureText(
        TextMeshPro textComponent,
        string value,
        float fontSize,
        Color color
    )
    {
        textComponent.text = value;
        textComponent.fontSize = fontSize;
        textComponent.color = color;
        textComponent.fontStyle =
            useBoldText ? FontStyles.Bold : FontStyles.Normal;
        textComponent.alignment = TextAlignmentOptions.Midline;

        ApplyThickness(textComponent);
    }

    private void AnimateRows()
    {
        int count = Mathf.Min(
            tokenObjects.Count,
            idObjects.Count
        );

        for (int i = 0; i < count; i++)
        {
            if (tokenObjects[i] != null)
                tokenObjects[i].transform.localPosition =
                    GetTokenPosition(i);

            if (idObjects[i] != null)
                idObjects[i].transform.localPosition =
                    GetIdPosition(i);

            if (alignChipsLeft)
            {
                Transform tokenChip = tokenObjects[i] != null ? tokenObjects[i].transform.Find("Chip") : null;
                Transform idChip = idObjects[i] != null ? idObjects[i].transform.Find("Chip") : null;
                if (tokenChip != null)
                    AlignChipLeft(tokenObjects[i].transform, tokenChip);
                if (idChip != null)
                    AlignChipLeft(idObjects[i].transform, idChip);
            }

            if (i >= connectionLines.Count ||
                connectionLines[i] == null)
                continue;

            Transform tokenTextChild =
                tokenObjects[i].transform.Find("TokenText");

            Transform idTextChild =
                idObjects[i].transform.Find("Text (TMP)");

            if (tokenTextChild == null ||
                idTextChild == null)
                continue;

            TextMeshPro tokenTMP =
                tokenTextChild.GetComponent<TextMeshPro>();

            TextMeshPro idTMP =
                idTextChild.GetComponent<TextMeshPro>();

            if (tokenTMP == null || idTMP == null)
                continue;

            tokenTMP.ForceMeshUpdate();
            idTMP.ForceMeshUpdate();

            Bounds tokenChipBounds = GetChipBounds(tokenObjects[i].transform);
            Bounds idChipBounds = GetChipBounds(idObjects[i].transform);
            Vector3 lineStart = new Vector3(tokenChipBounds.max.x + lineGap, tokenChipBounds.center.y, tokenChipBounds.center.z);
            Vector3 lineEnd = new Vector3(idChipBounds.min.x - lineGap, idChipBounds.center.y, idChipBounds.center.z);

            connectionLines[i].SetPosition(0, lineStart);
            connectionLines[i].SetPosition(1, lineEnd);
        }
    }

    private Vector3 GetTokenPosition(int index)
    {
        float posY = startY - index * yStep;

        float waveOffsetX = enableRowAnimation
            ? Mathf.Sin(
                Time.time * rowAnimationSpeed +
                index * rowAnimationOffset
            ) * rowAnimationAmplitude
            : 0f;

        float waveOffsetZ = enableRowAnimation
            ? Mathf.Cos(
                Time.time * rowAnimationSpeed +
                index * rowAnimationOffset
            ) * rowAnimationAmplitude * 0.4f
            : 0f;

        return new Vector3(
            startX + tokenLeftRight - columnGap / 2f + waveOffsetX,
            posY,
            tokenForwardBack + waveOffsetZ
        );
    }

    private Vector3 GetIdPosition(int index)
    {
        float posY =
            startY -
            index * yStep +
            idYOffset;

        float waveOffsetX = enableRowAnimation
            ? Mathf.Sin(
                Time.time * rowAnimationSpeed +
                index * rowAnimationOffset
            ) * rowAnimationAmplitude
            : 0f;

        float waveOffsetZ = enableRowAnimation
            ? Mathf.Cos(
                Time.time * rowAnimationSpeed +
                index * rowAnimationOffset
            ) * rowAnimationAmplitude * 0.4f
            : 0f;

        return new Vector3(
            startX +
            idOffsetX +
            idLeftRight +
            columnGap / 2f +
            waveOffsetX,
            posY,
            idForwardBack + waveOffsetZ
        );
    }

    private LineRenderer DrawConnectionLine(
        Vector3 from,
        Vector3 to
    )
    {
        GameObject lineObj =
            new GameObject("Token_ID_Line");

        lineObj.transform.SetParent(transform);

        if (lineMaterial == null)
        {
            Shader shader =
                Shader.Find("Sprites/Default");

            if (shader != null)
                lineMaterial = new Material(shader);
        }

        LineRenderer lineRenderer =
            lineObj.AddComponent<LineRenderer>();

        lineRenderer.material = lineMaterial;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.SetPosition(0, from);
        lineRenderer.SetPosition(1, to);
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.useWorldSpace = true;

        return lineRenderer;
    }

    private void CreateColumnLabels()
    {
        tokenColumnLabelText = CreateSingleLabel(
            tokenColumnLabel,
            new Vector3(
                startX + tokenLeftRight - columnGap / 2f,
                startY + columnLabelYOffset,
                tokenForwardBack
            )
        );

        idColumnLabelText = CreateSingleLabel(
            idColumnLabel,
            new Vector3(
                startX +
                idOffsetX +
                idLeftRight +
                columnGap / 2f,
                startY + columnLabelYOffset,
                idForwardBack
            )
        );

        UpdateColumnLabels();
    }

    private TextMeshPro CreateSingleLabel(
        string text,
        Vector3 localPosition
    )
    {
        GameObject labelObj =
            new GameObject($"Label_{text}");

        labelObj.transform.SetParent(
            transform,
            false
        );

        labelObj.transform.localPosition =
            localPosition;

        TextMeshPro tmp =
            labelObj.AddComponent<TextMeshPro>();

        tmp.text = text;
        tmp.fontSize = columnLabelFontSize;
        tmp.color = columnLabelColor;
        tmp.fontStyle =
            useBoldText
                ? FontStyles.Bold
                : FontStyles.Normal;
        tmp.alignment =
            alignChipsLeft
                ? TextAlignmentOptions.MidlineLeft
                : TextAlignmentOptions.Midline;

        ApplyThickness(tmp);
        tmp.ForceMeshUpdate();
        return tmp;
    }

    private void UpdateColumnLabels()
    {
        if (tokenColumnLabelText != null)
        {
            Vector3 tokenPosition = alignChipsLeft
                ? GetTokenPosition(0)
                : new Vector3(startX + tokenLeftRight - columnGap / 2f, startY, tokenForwardBack);
            tokenPosition.y = startY + columnLabelYOffset;
            SetColumnLabelPosition(tokenColumnLabelText, tokenPosition);
        }

        if (idColumnLabelText != null)
        {
            Vector3 idPosition = alignChipsLeft
                ? GetIdPosition(0)
                : new Vector3(startX + idOffsetX + idLeftRight + columnGap / 2f, startY, idForwardBack);
            idPosition.y = startY + columnLabelYOffset;
            SetColumnLabelPosition(idColumnLabelText, idPosition);
        }
    }

    private void SetColumnLabelPosition(TextMeshPro label, Vector3 localPosition)
    {
        localPosition.x += columnLabelXOffset;
        label.transform.localPosition = localPosition;
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
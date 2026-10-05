using System.Collections;
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

    [Header("5 pairs — Z-axis mein arrange: 2 front, 1 middle, 2 back")]
    public Vector3 logitBasePosition = new Vector3(570f, 8f, 0f);
    public Vector3 probBasePosition = new Vector3(600f, 8f, 0f);
    public float pairZGap = 2f;

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
    public Vector3 wordLabelOffset = new Vector3(2f, 0f, 0f);

    [Header("Lines")]
    public Color sourceLineColor = new Color(0.549f, 0.851f, 0.8f);
    public float sourceLineWidth = 0.6f;
    public Color pairLineColor = new Color(1f, 1f, 1f, 0.6f);
    public float pairLineWidth = 0.15f;
    public Material lineMaterial;

    [Header("Text thickness")]
    public float textThickness = 0.4f;

    public void Animate(string sentence)
    {
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

        int pairCount = Mathf.Min(
            logitValues != null ? logitValues.Length : 0,
            probabilityValues != null ? probabilityValues.Length : 0
        );

        if (pairCount == 0)
            return;

        int middleIndex = pairCount / 2;

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

        for (int i = 0; i < pairCount; i++)
        {
            float zOffset = (i - middleIndex) * pairZGap;

            Vector3 offset = new Vector3(0f, 0f, zOffset);

            Vector3 logitPos = logitBasePosition + offset;
            Vector3 probPos = probBasePosition + offset;

            BuildBox(
                logitPos,
                boxSize,
                logitColor,
                $"Logit_{i}"
            );

            BuildBox(
                probPos,
                boxSize,
                probColor,
                $"Prob_{i}"
            );

            CreateLabel(
                logitValues[i],
                logitPos + valueLabelOffset,
                valueLabelFontSize,
                valueLabelColor
            );

            CreateLabel(
                probabilityValues[i],
                probPos + valueLabelOffset,
                valueLabelFontSize,
                valueLabelColor
            );

            if (probabilityWordLabels != null &&
                i < probabilityWordLabels.Length &&
                !string.IsNullOrEmpty(probabilityWordLabels[i]))
            {
                CreateLabel(
                    probabilityWordLabels[i],
                    probPos + wordLabelOffset,
                    valueLabelFontSize,
                    valueLabelColor
                );
            }

            Vector3 logitExit =
                logitPos +
                Vector3.right * (boxSize.x / 2f);

            Vector3 probEntry =
                probPos -
                Vector3.right * (boxSize.x / 2f);

            DrawLine(
                logitExit,
                probEntry,
                pairLineColor,
                pairLineWidth
            );

            if (i == middleIndex)
            {
                Vector3 middleEntry =
                    logitPos -
                    Vector3.right * (boxSize.x / 2f);

                DrawLine(
                    sourcePoint,
                    middleEntry,
                    sourceLineColor,
                    sourceLineWidth
                );
            }
        }
    }

    private IEnumerator AnimateBuild()
    {
        int pairCount = Mathf.Min(
            logitValues != null ? logitValues.Length : 0,
            probabilityValues != null ? probabilityValues.Length : 0
        );

        if (pairCount == 0)
            yield break;

        int middleIndex = pairCount / 2;

        if (cameraController != null && anchors.Length > 0 && anchors[0] != null)
            cameraController.FocusOnStage(anchors[0]);

        CreateLabel(logitsGroupTitle, logitBasePosition + titleOffset, groupTitleFontSize, logitsTitleColor);
        CreateLabel(probsGroupTitle, probBasePosition + titleOffset, groupTitleFontSize, probsTitleColor);

        yield return new WaitForSeconds(stepDelay);

        if (cameraController != null && anchors.Length > 1 && anchors[1] != null)
            cameraController.FocusOnStage(anchors[1]);

        for (int i = 0; i < pairCount; i++)
        {
            float zOffset = (i - middleIndex) * pairZGap;
            Vector3 offset = new Vector3(0f, 0f, zOffset);
            Vector3 logitPos = logitBasePosition + offset;
            Vector3 probPos = probBasePosition + offset;

            BuildBox(logitPos, boxSize, logitColor, $"Logit_{i}");
            BuildBox(probPos, boxSize, probColor, $"Prob_{i}");

            CreateLabel(logitValues[i], logitPos + valueLabelOffset, valueLabelFontSize, valueLabelColor);
            CreateLabel(probabilityValues[i], probPos + valueLabelOffset, valueLabelFontSize, valueLabelColor);

            if (probabilityWordLabels != null && i < probabilityWordLabels.Length && !string.IsNullOrEmpty(probabilityWordLabels[i]))
                CreateLabel(probabilityWordLabels[i], probPos + wordLabelOffset, valueLabelFontSize, valueLabelColor);

            Vector3 logitExit = logitPos + Vector3.right * (boxSize.x / 2f);
            Vector3 probEntry = probPos - Vector3.right * (boxSize.x / 2f);
            DrawLine(logitExit, probEntry, pairLineColor, pairLineWidth);

            if (i == middleIndex)
            {
                Vector3 middleEntry = logitPos - Vector3.right * (boxSize.x / 2f);
                DrawLine(sourcePoint, middleEntry, sourceLineColor, sourceLineWidth);
            }

            yield return new WaitForSeconds(pairRevealDelay);
        }
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

    private void BuildBox(
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
            return;

        Material mat = new Material(shader);
        mat.color = color;

        Renderer renderer = box.GetComponent<Renderer>();

        if (renderer != null)
            renderer.material = mat;
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
            material.SetFloat("_FaceDilate", textThickness);
    }
}
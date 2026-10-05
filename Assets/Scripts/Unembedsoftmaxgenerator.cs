using System.Collections;
using UnityEngine;
using TMPro;

[ExecuteAlways]
public class UnembedSoftmaxGenerator : MonoBehaviour, ISentenceAnimatable
{
    public Transform[] anchors;
    public IntegratedCameraController cameraController;
    public float stepDelay = 0.6f;

    [Header("Mirror this — pichli sheet (Norm Out) ka right edge, world position")]
    public Vector3 sourcePoint = new Vector3(470f, 8f, 0f);

    [Header("Unembedding Matrix block (grey)")]
    public Vector3 unembedPosition = new Vector3(500f, 8f, 0f);
    public Vector3 unembedSize = new Vector3(6f, 6f, 6f);
    public Color unembedColor = new Color(0.45f, 0.45f, 0.45f);
    public string unembedTitleLabel = "Unembedding Matrix";
    public string unembedSubtitleLabel = "960 x Vocab Size";
    public string unembedFormulaLabel = "[1x960] x [960xVocab] = [1xVocab]";
    public float unembedTitleFontSize = 8f;
    public float unembedSubtitleFontSize = 6f;
    public float unembedFormulaFontSize = 6f;
    public Color unembedLabelColor = Color.white;
    public Vector3 unembedTitleOffset = new Vector3(0f, 6f, 0f);
    public Vector3 unembedSubtitleOffset = new Vector3(0f, 4.5f, 0f);
    public Vector3 unembedFormulaOffset = new Vector3(0f, -5f, 0f);

    [Header("Softmax block (teal)")]
    public Vector3 softmaxPosition = new Vector3(1700f, 8f, 0f);
    public Vector3 softmaxSize = new Vector3(6f, 6f, 6f);
    public Color softmaxColor = new Color(0.549f, 0.851f, 0.8f);
    public string softmaxTitleLabel = "Softmax";
    public string softmaxTopLabel = "[1xVocab] -> Softmax -> [1xVocab]";
    public string softmaxBottomLabel = "top tokens by probability";
    public float softmaxTitleFontSize = 8f;
    public float softmaxTopFontSize = 6f;
    public float softmaxBottomFontSize = 5f;
    public Color softmaxLabelColor = Color.white;
    public Vector3 softmaxTitleOffset = new Vector3(0f, 4.5f, 0f);
    public Vector3 softmaxTopOffset = new Vector3(0f, 6.5f, 0f);
    public Vector3 softmaxBottomOffset = new Vector3(0f, -5f, 0f);

    [Header("Lines")]
    public Color lineColor = new Color(0.549f, 0.851f, 0.8f);
    public float lineWidth = 0.15f;
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

        BuildBox(
            unembedPosition,
            unembedSize,
            unembedColor,
            "Unembedding_Block"
        );

        CreateLabel(
            unembedTitleLabel,
            unembedPosition + unembedTitleOffset,
            unembedTitleFontSize,
            unembedLabelColor
        );

        CreateLabel(
            unembedSubtitleLabel,
            unembedPosition + unembedSubtitleOffset,
            unembedSubtitleFontSize,
            unembedLabelColor
        );

        CreateLabel(
            unembedFormulaLabel,
            unembedPosition + unembedFormulaOffset,
            unembedFormulaFontSize,
            unembedLabelColor
        );

        Vector3 unembedEntry =
            unembedPosition -
            Vector3.right * (unembedSize.x / 2f);

        DrawLine(sourcePoint, unembedEntry);

        BuildBox(
            softmaxPosition,
            softmaxSize,
            softmaxColor,
            "Softmax_Block"
        );

        CreateLabel(
            softmaxTitleLabel,
            softmaxPosition + softmaxTitleOffset,
            softmaxTitleFontSize,
            softmaxLabelColor
        );

        CreateLabel(
            softmaxTopLabel,
            softmaxPosition + softmaxTopOffset,
            softmaxTopFontSize,
            softmaxLabelColor
        );

        CreateLabel(
            softmaxBottomLabel,
            softmaxPosition + softmaxBottomOffset,
            softmaxBottomFontSize,
            softmaxLabelColor
        );

        Vector3 unembedExit =
            unembedPosition +
            Vector3.right * (unembedSize.x / 2f);

        Vector3 softmaxEntry =
            softmaxPosition -
            Vector3.right * (softmaxSize.x / 2f);

        DrawLine(unembedExit, softmaxEntry);
    }

    private IEnumerator AnimateBuild()
    {
        if (cameraController != null && anchors.Length > 0 && anchors[0] != null)
            cameraController.FocusOnStage(anchors[0]);

        BuildBox(unembedPosition, unembedSize, unembedColor, "Unembedding_Block");
        CreateLabel(unembedTitleLabel, unembedPosition + unembedTitleOffset, unembedTitleFontSize, unembedLabelColor);
        CreateLabel(unembedSubtitleLabel, unembedPosition + unembedSubtitleOffset, unembedSubtitleFontSize, unembedLabelColor);
        CreateLabel(unembedFormulaLabel, unembedPosition + unembedFormulaOffset, unembedFormulaFontSize, unembedLabelColor);

        Vector3 unembedEntry = unembedPosition - Vector3.right * (unembedSize.x / 2f);
        DrawLine(sourcePoint, unembedEntry);

        yield return new WaitForSeconds(stepDelay);

        if (cameraController != null && anchors.Length > 1 && anchors[1] != null)
            cameraController.FocusOnStage(anchors[1]);

        BuildBox(softmaxPosition, softmaxSize, softmaxColor, "Softmax_Block");
        CreateLabel(softmaxTitleLabel, softmaxPosition + softmaxTitleOffset, softmaxTitleFontSize, softmaxLabelColor);
        CreateLabel(softmaxTopLabel, softmaxPosition + softmaxTopOffset, softmaxTopFontSize, softmaxLabelColor);
        CreateLabel(softmaxBottomLabel, softmaxPosition + softmaxBottomOffset, softmaxBottomFontSize, softmaxLabelColor);

        Vector3 unembedExit = unembedPosition + Vector3.right * (unembedSize.x / 2f);
        Vector3 softmaxEntry = softmaxPosition - Vector3.right * (softmaxSize.x / 2f);
        DrawLine(unembedExit, softmaxEntry);
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

    private void DrawLine(Vector3 fromLocal, Vector3 toLocal)
    {
        EnsureLineMaterial();

        if (lineMaterial == null)
            return;

        GameObject lineObj =
            new GameObject("UnembedSoftmax_Line");

        lineObj.transform.SetParent(transform, false);

        LineRenderer lineRenderer =
            lineObj.AddComponent<LineRenderer>();

        lineRenderer.material = lineMaterial;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        lineRenderer.SetPosition(
            0,
            transform.TransformPoint(fromLocal)
        );

        lineRenderer.SetPosition(
            1,
            transform.TransformPoint(toLocal)
        );

        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
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
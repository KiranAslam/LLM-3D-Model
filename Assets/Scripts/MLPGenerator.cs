using System.Collections;
using UnityEngine;
using TMPro;

[ExecuteAlways]
public class MLPGenerator : MonoBehaviour, ISentenceAnimatable
{
    public Transform[] anchors;
    public IntegratedCameraController cameraController;
    public float stepDelay = 0.6f;
    public float neuronLayerDelay = 0.15f;

    [Header("Mirror these — AddNormGenerator ke Inspector values")]
    public Vector3 addNormPosition = new Vector3(300f, 8f, 0f);
    public Vector3 addNormSize = new Vector3(6f, 6f, 1f);

    [Header("MLP box (transparent, light teal)")]
    public Vector3 mlpPosition = new Vector3(340f, 8f, 0f);
    public Vector3 mlpSize = new Vector3(20f, 10f, 8f);
    public Color mlpColor = new Color(0.6f, 0.9f, 0.85f, 0.25f);
    public string mlpTitleLabel = "Feed-Forward (MLP)";
    public float mlpTitleFontSize = 10f;
    public Color mlpTitleColor = Color.white;
    public Vector3 mlpTitleOffset = new Vector3(0f, 7f, 0f);

    [Header("Feed-forward network — 3 input, 4-4 hidden, 3 output, fully connected")]
    public int inputNeurons = 3;
    public int hiddenLayer1Neurons = 4;
    public int hiddenLayer2Neurons = 4;
    public int outputNeurons = 3;
    public float neuronRadius = 0.4f;
    public float layerSpacingX = 4f;
    public float neuronSpacingY = 2f;
    public Color neuronColor = new Color(0.6f, 0.9f, 0.85f);
    public Color connectionColor = new Color(0.6f, 0.9f, 0.85f, 0.35f);
    public float connectionWidth = 0.03f;

    [Header("MLP Out sheet")]
    public Vector3 mlpOutPosition = new Vector3(390f, 8f, 0f);
    public Vector3 mlpOutSize = new Vector3(18f, 10f, 1f);
    public Color mlpOutColor = new Color(0.6f, 0.9f, 0.85f, 0.35f);
    public string mlpOutTitleLabel = "MLP Out";
    public string mlpOutDimensionLabel = "5x960";
    public float mlpOutTitleFontSize = 10f;
    public float mlpOutLabelFontSize = 10f;
    public Color mlpOutLabelColor = Color.white;
    public Vector3 mlpOutTitleOffset = new Vector3(0f, 7f, 0f);
    public Vector3 mlpOutDimensionOffset = new Vector3(0f, 5f, 0f);

    [Header("Connector lines (Add+Norm -> MLP -> MLP Out)")]
    public Color lineColor = new Color(0.549f, 0.851f, 0.8f);
    public float lineWidth = 0.2f;
    public Material lineMaterial;

    [Header("Text thickness")]
    public float textThickness = 0.4f;

    private void Start()
    {
        if (Application.isPlaying)
            Rebuild();
    }

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
            mlpPosition,
            mlpSize,
            mlpColor,
            "MLP_Box"
        );

        CreateLabel(
            mlpTitleLabel,
            mlpPosition + mlpTitleOffset,
            mlpTitleFontSize,
            mlpTitleColor
        );

        Vector3 addNormExit =
            addNormPosition +
            Vector3.right * (addNormSize.x / 2f);

        Vector3 mlpEntry =
            mlpPosition -
            Vector3.right * (mlpSize.x / 2f);

        DrawLine(
            new[] { addNormExit, mlpEntry },
            lineColor,
            lineWidth
        );

        BuildFeedForwardNetwork();

        Vector3 mlpExit =
            mlpPosition +
            Vector3.right * (mlpSize.x / 2f);

        Vector3 mlpOutEntry =
            mlpOutPosition -
            Vector3.right * (mlpOutSize.x / 2f);

        DrawLine(
            new[] { mlpExit, mlpOutEntry },
            lineColor,
            lineWidth
        );

        BuildBox(
            mlpOutPosition,
            mlpOutSize,
            mlpOutColor,
            "MLPOut_Sheet"
        );

        CreateLabel(
            mlpOutTitleLabel,
            mlpOutPosition + mlpOutTitleOffset,
            mlpOutTitleFontSize,
            mlpOutLabelColor
        );

        CreateLabel(
            mlpOutDimensionLabel,
            mlpOutPosition + mlpOutDimensionOffset,
            mlpOutLabelFontSize,
            mlpOutLabelColor
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

    private IEnumerator AnimateBuild()
    {
        if (cameraController != null && anchors.Length > 0 && anchors[0] != null)
            cameraController.FocusOnStage(anchors[0]);

        BuildBox(mlpPosition, mlpSize, mlpColor, "MLP_Box");
        CreateLabel(mlpTitleLabel, mlpPosition + mlpTitleOffset, mlpTitleFontSize, mlpTitleColor);

        Vector3 addNormExit = addNormPosition + Vector3.right * (addNormSize.x / 2f);
        Vector3 mlpEntry = mlpPosition - Vector3.right * (mlpSize.x / 2f);
        DrawLine(new[] { addNormExit, mlpEntry }, lineColor, lineWidth);

        yield return new WaitForSeconds(stepDelay);

        if (cameraController != null && anchors.Length > 1 && anchors[1] != null)
            cameraController.FocusOnStage(anchors[1]);

        yield return StartCoroutine(AnimateFeedForwardNetwork());

        yield return new WaitForSeconds(stepDelay);

        if (cameraController != null && anchors.Length > 2 && anchors[2] != null)
            cameraController.FocusOnStage(anchors[2]);

        Vector3 mlpExit = mlpPosition + Vector3.right * (mlpSize.x / 2f);
        Vector3 mlpOutEntry = mlpOutPosition - Vector3.right * (mlpOutSize.x / 2f);
        DrawLine(new[] { mlpExit, mlpOutEntry }, lineColor, lineWidth);

        BuildBox(mlpOutPosition, mlpOutSize, mlpOutColor, "MLPOut_Sheet");
        CreateLabel(mlpOutTitleLabel, mlpOutPosition + mlpOutTitleOffset, mlpOutTitleFontSize, mlpOutLabelColor);
        CreateLabel(mlpOutDimensionLabel, mlpOutPosition + mlpOutDimensionOffset, mlpOutLabelFontSize, mlpOutLabelColor);
    }

    private IEnumerator AnimateFeedForwardNetwork()
    {
        int[] counts = { inputNeurons, hiddenLayer1Neurons, hiddenLayer2Neurons, outputNeurons };
        Vector3[][] positions = new Vector3[counts.Length][];

        float totalWidth = layerSpacingX * (counts.Length - 1);
        float startX = mlpPosition.x - (totalWidth / 2f);

        for (int layer = 0; layer < counts.Length; layer++)
        {
            int count = counts[layer];
            float x = startX + layerSpacingX * layer;
            float totalHeight = neuronSpacingY * (count - 1);
            float topY = mlpPosition.y + totalHeight / 2f;
            positions[layer] = new Vector3[count];

            for (int n = 0; n < count; n++)
            {
                float y = topY - neuronSpacingY * n;
                Vector3 pos = new Vector3(x, y, mlpPosition.z);
                positions[layer][n] = pos;
                CreateNeuron(pos);
            }

            yield return new WaitForSeconds(neuronLayerDelay);
        }

        for (int layer = 0; layer < counts.Length - 1; layer++)
        {
            foreach (Vector3 from in positions[layer])
                foreach (Vector3 to in positions[layer + 1])
                    DrawLine(new[] { from, to }, connectionColor, connectionWidth);

            yield return new WaitForSeconds(neuronLayerDelay);
        }
    }

    private void BuildFeedForwardNetwork()
    {
        int[] counts =
        {
            inputNeurons,
            hiddenLayer1Neurons,
            hiddenLayer2Neurons,
            outputNeurons
        };

        Vector3[][] positions =
            new Vector3[counts.Length][];

        float totalWidth =
            layerSpacingX * (counts.Length - 1);

        float startX =
            mlpPosition.x - (totalWidth / 2f);

        for (int layer = 0; layer < counts.Length; layer++)
        {
            int count = counts[layer];

            float x =
                startX +
                layerSpacingX * layer;

            float totalHeight =
                neuronSpacingY * (count - 1);

            float topY =
                mlpPosition.y +
                totalHeight / 2f;

            positions[layer] =
                new Vector3[count];

            for (int n = 0; n < count; n++)
            {
                float y =
                    topY -
                    neuronSpacingY * n;

                Vector3 pos = new Vector3(
                    x,
                    y,
                    mlpPosition.z
                );

                positions[layer][n] = pos;

                CreateNeuron(pos);
            }
        }

        for (int layer = 0; layer < counts.Length - 1; layer++)
        {
            foreach (Vector3 from in positions[layer])
            {
                foreach (Vector3 to in positions[layer + 1])
                {
                    DrawLine(
                        new[] { from, to },
                        connectionColor,
                        connectionWidth
                    );
                }
            }
        }
    }

    private void CreateNeuron(Vector3 localPos)
    {
        GameObject neuron =
            GameObject.CreatePrimitive(
                PrimitiveType.Sphere
            );

        neuron.name = "Neuron";
        neuron.transform.SetParent(
            transform,
            false
        );

        neuron.transform.localPosition = localPos;
        neuron.transform.localScale =
            Vector3.one * (neuronRadius * 2f);

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
            return;

        Material mat = new Material(shader);
        mat.color = neuronColor;

        Renderer renderer =
            neuron.GetComponent<Renderer>();

        if (renderer != null)
            renderer.material = mat;
    }

    private void BuildBox(
        Vector3 position,
        Vector3 size,
        Color color,
        string objName
    )
    {
        GameObject box =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        box.name = objName;
        box.transform.SetParent(
            transform,
            false
        );

        box.transform.localPosition = position;
        box.transform.localScale = size;

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
            return;

        Material mat = new Material(shader);
        mat.color = color;

        Renderer renderer =
            box.GetComponent<Renderer>();

        if (renderer != null)
            renderer.material = mat;
    }

    private void DrawLine(
        Vector3[] localPoints,
        Color color,
        float width
    )
    {
        GameObject lineObj =
            new GameObject("MLP_Line");

        lineObj.transform.SetParent(
            transform,
            false
        );

        EnsureLineMaterial();

        LineRenderer lr =
            lineObj.AddComponent<LineRenderer>();

        lr.material = lineMaterial;
        lr.positionCount = localPoints.Length;
        lr.startWidth = width;
        lr.endWidth = width;

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
        lr.startColor = color;
        lr.endColor = color;
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
        GameObject labelObj =
            new GameObject($"Label_{text}");

        labelObj.transform.SetParent(
            transform,
            false
        );

        labelObj.transform.localPosition =
            localPos;

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
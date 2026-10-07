using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[ExecuteAlways]
public class FinalLayerNormGenerator : MonoBehaviour, ISentenceAnimatable
{
    public Transform[] anchors;
    public IntegratedCameraController cameraController;
    public float stepDelay = 0.6f;

    [Header("Mirror this — pichli (aakhri) layer ke MLP Out ka right edge, world position")]
    public Vector3 sourcePoint = new Vector3(1560f, 8f, 0f);

    [Header("Outer block (transparent, teal — MLP jaisi style)")]
    public Vector3 boxPosition = new Vector3(1610f, 8f, 0f);
    public Vector3 boxSize = new Vector3(50f, 16f, 10f);
    public Color boxColor = new Color(0.6f, 0.9f, 0.85f, 0.2f);
    public string boxTitleLabel = "Final Layer Normalization";
    public float boxTitleFontSize = 10f;
    public Color boxTitleColor = Color.white;
    public Vector3 boxTitleOffset = new Vector3(0f, 9f, 0f);

    [Header("Hexagon nodes — Mean / Var / Scale / Shift")]
    public float hexRadius = 1.6f;
    public float hexDepth = 0.6f;
    public Vector3 meanPosition = new Vector3(-14f, 4f, 0f);   // box ke relative offset
    public Vector3 varPosition = new Vector3(-14f, -4f, 0f);
    public Vector3 scalePosition = new Vector3(8f, 4f, 0f);
    public Vector3 shiftPosition = new Vector3(8f, -4f, 0f);
    public Color meanVarColor = new Color(0.549f, 0.851f, 0.8f);   // teal
    public Color scaleShiftColor = new Color(0.92f, 0.92f, 0.92f); // white/light gray
    public float hexLabelFontSize = 6f;
    public Color hexLabelColor = Color.white;
    public Vector3 hexLabelOffset = new Vector3(0f, 2.2f, 0f);

    [Header("Formula box — (x - mu) / sigma")]
    public Vector3 formulaPosition = new Vector3(-2f, 0f, 0f); // box ke relative offset
    public Vector3 formulaSize = new Vector3(4f, 3f, 3f);
    public Color formulaColor = new Color(0.12f, 0.12f, 0.12f); // black
    public string formulaLabel = "(x - mu) / sigma";
    public float formulaLabelFontSize = 5f;
    public Color formulaLabelColor = Color.white;

    [Header("Norm Out sheet")]
    public Vector3 normOutPosition = new Vector3(20f, 0f, 0f); // box ke relative offset
    public Vector3 normOutSize = new Vector3(14f, 10f, 1f);
    public Color normOutColor = new Color(0.549f, 0.851f, 0.8f, 0.35f);
    public string normOutTitleLabel = "Norm Out";
    public string normOutSubtitleLabel = "Select Last Token [1x960]";
    public float normOutTitleFontSize = 8f;
    public float normOutSubtitleFontSize = 6f;
    public Color normOutLabelColor = Color.white;
    public Vector3 normOutTitleOffset = new Vector3(0f, 6f, 0f);
    public Vector3 normOutSubtitleOffset = new Vector3(0f, -6f, 0f);

    [Header("Lines")]
    public Color lineColor = new Color(0.549f, 0.851f, 0.8f);
    public float lineWidth = 0.15f;
    public Material lineMaterial;

    [Header("Text thickness")]
    public float textThickness = 0.4f;

    private void OnEnable()
    {
        if (Application.isPlaying)
            BuildAllImmediate();
    }

    void Start()
    {
        if (Application.isPlaying)
            return;
        ClearPreviousModel();
        BuildAllImmediate();
    }

    public void Animate(string sentence)
    {
        ClearPreviousModel();
        if (Application.isPlaying)
            StartCoroutine(AnimateBuild());
        else
            BuildAllImmediate();
    }

    public void CompleteImmediately()
    {
        StopAllCoroutines();
        ClearPreviousModel();
        BuildAllImmediate();
    }

    private void BuildAllImmediate()
    {
        BuildBox(boxPosition, boxSize, boxColor, "FinalLayerNorm_Block");
        CreateLabel(boxTitleLabel, boxPosition + boxTitleOffset, boxTitleFontSize, boxTitleColor);

        Vector3 boxEntry = boxPosition - Vector3.right * (boxSize.x / 2f);
        DrawLine(sourcePoint, boxEntry);

        Vector3 meanWorld = boxPosition + meanPosition;
        Vector3 varWorld = boxPosition + varPosition;
        Vector3 formulaWorld = boxPosition + formulaPosition;
        Vector3 scaleWorld = boxPosition + scalePosition;
        Vector3 shiftWorld = boxPosition + shiftPosition;
        Vector3 normOutWorld = boxPosition + normOutPosition;

        CreateHexagonPrism("Mean_mu", meanWorld, hexRadius, hexDepth, meanVarColor);
        CreateLabel("Mean mu", meanWorld + hexLabelOffset, hexLabelFontSize, hexLabelColor);

        CreateHexagonPrism("Var_sigma2", varWorld, hexRadius, hexDepth, meanVarColor);
        CreateLabel("Var sigma^2", varWorld + hexLabelOffset, hexLabelFontSize, hexLabelColor);

        DrawLine(boxEntry, meanWorld);
        DrawLine(boxEntry, varWorld);

        BuildBox(formulaWorld, formulaSize, formulaColor, "Formula_Box");
        CreateLabel(formulaLabel, formulaWorld, formulaLabelFontSize, formulaLabelColor);

        DrawLine(meanWorld, formulaWorld);
        DrawLine(varWorld, formulaWorld);

        CreateHexagonPrism("Scale_gamma", scaleWorld, hexRadius, hexDepth, scaleShiftColor);
        CreateLabel("Scale gamma", scaleWorld + hexLabelOffset, hexLabelFontSize, hexLabelColor);

        CreateHexagonPrism("Shift_beta", shiftWorld, hexRadius, hexDepth, scaleShiftColor);
        CreateLabel("Shift beta", shiftWorld + hexLabelOffset, hexLabelFontSize, hexLabelColor);

        BuildBox(normOutWorld, normOutSize, normOutColor, "NormOut_Sheet");
        CreateLabel(normOutTitleLabel, normOutWorld + normOutTitleOffset, normOutTitleFontSize, normOutLabelColor);
        CreateLabel(normOutSubtitleLabel, normOutWorld + normOutSubtitleOffset, normOutSubtitleFontSize, normOutLabelColor);

        Vector3 normOutEntry = normOutWorld - Vector3.right * (normOutSize.x / 2f);
        DrawLine(formulaWorld, normOutEntry);
        DrawLine(scaleWorld, normOutEntry);
        DrawLine(shiftWorld, normOutEntry);
    }

    private IEnumerator AnimateBuild()
    {
        if (cameraController != null && anchors != null && anchors.Length > 0 && anchors[0] != null)
            cameraController.FocusOnStage(anchors[0]);

        BuildBox(boxPosition, boxSize, boxColor, "FinalLayerNorm_Block");
        CreateLabel(boxTitleLabel, boxPosition + boxTitleOffset, boxTitleFontSize, boxTitleColor);

        Vector3 boxEntry = boxPosition - Vector3.right * (boxSize.x / 2f);
        DrawLine(sourcePoint, boxEntry);

        Vector3 meanWorld = boxPosition + meanPosition;
        Vector3 varWorld = boxPosition + varPosition;
        Vector3 formulaWorld = boxPosition + formulaPosition;
        Vector3 scaleWorld = boxPosition + scalePosition;
        Vector3 shiftWorld = boxPosition + shiftPosition;
        Vector3 normOutWorld = boxPosition + normOutPosition;

        CreateHexagonPrism("Mean_mu", meanWorld, hexRadius, hexDepth, meanVarColor);
        CreateLabel("Mean mu", meanWorld + hexLabelOffset, hexLabelFontSize, hexLabelColor);

        CreateHexagonPrism("Var_sigma2", varWorld, hexRadius, hexDepth, meanVarColor);
        CreateLabel("Var sigma^2", varWorld + hexLabelOffset, hexLabelFontSize, hexLabelColor);

        DrawLine(boxEntry, meanWorld);
        DrawLine(boxEntry, varWorld);

        yield return new WaitForSeconds(stepDelay);

        if (cameraController != null && anchors != null && anchors.Length > 1 && anchors[1] != null)
            cameraController.FocusOnStage(anchors[1]);

        BuildBox(formulaWorld, formulaSize, formulaColor, "Formula_Box");
        CreateLabel(formulaLabel, formulaWorld, formulaLabelFontSize, formulaLabelColor);

        DrawLine(meanWorld, formulaWorld);
        DrawLine(varWorld, formulaWorld);

        CreateHexagonPrism("Scale_gamma", scaleWorld, hexRadius, hexDepth, scaleShiftColor);
        CreateLabel("Scale gamma", scaleWorld + hexLabelOffset, hexLabelFontSize, hexLabelColor);

        CreateHexagonPrism("Shift_beta", shiftWorld, hexRadius, hexDepth, scaleShiftColor);
        CreateLabel("Shift beta", shiftWorld + hexLabelOffset, hexLabelFontSize, hexLabelColor);

        yield return new WaitForSeconds(stepDelay);

        if (cameraController != null && anchors != null && anchors.Length > 2 && anchors[2] != null)
            cameraController.FocusOnStage(anchors[2]);

        BuildBox(normOutWorld, normOutSize, normOutColor, "NormOut_Sheet");
        CreateLabel(normOutTitleLabel, normOutWorld + normOutTitleOffset, normOutTitleFontSize, normOutLabelColor);
        CreateLabel(normOutSubtitleLabel, normOutWorld + normOutSubtitleOffset, normOutSubtitleFontSize, normOutLabelColor);

        Vector3 normOutEntry = normOutWorld - Vector3.right * (normOutSize.x / 2f);
        DrawLine(formulaWorld, normOutEntry);
        DrawLine(scaleWorld, normOutEntry);
        DrawLine(shiftWorld, normOutEntry);
    }

    void ClearPreviousModel()
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

    void BuildBox(Vector3 localPosition, Vector3 size, Color color, string objName)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = objName;
        box.transform.SetParent(transform, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = size;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        box.GetComponent<Renderer>().material = mat;
    }

    // Unity mein built-in hexagon primitive nahi hota, isliye ye ek chhota procedural
    // hexagonal prism mesh banata hai — flat 6-sided front/back face + 6 side quads
    GameObject CreateHexagonPrism(string name, Vector3 localPosition, float radius, float depth, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPosition;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();

        int sides = 6;
        Vector3[] front = new Vector3[sides];
        Vector3[] back = new Vector3[sides];

        for (int i = 0; i < sides; i++)
        {
            float angle = Mathf.Deg2Rad * (60f * i);
            float x = radius * Mathf.Cos(angle);
            float y = radius * Mathf.Sin(angle);
            front[i] = new Vector3(x, y, -depth / 2f);
            back[i] = new Vector3(x, y, depth / 2f);
        }

        Vector3[] vertices = new Vector3[2 + sides * 2];
        vertices[0] = new Vector3(0f, 0f, -depth / 2f);
        for (int i = 0; i < sides; i++) vertices[1 + i] = front[i];
        int backCenterIdx = 1 + sides;
        vertices[backCenterIdx] = new Vector3(0f, 0f, depth / 2f);
        for (int i = 0; i < sides; i++) vertices[2 + sides + i] = back[i];

        List<int> tris = new List<int>();

        for (int i = 0; i < sides; i++)
        {
            int a = 1 + i;
            int b = 1 + ((i + 1) % sides);
            tris.Add(0); tris.Add(b); tris.Add(a);
        }

        for (int i = 0; i < sides; i++)
        {
            int a = 2 + sides + i;
            int b = 2 + sides + ((i + 1) % sides);
            tris.Add(backCenterIdx); tris.Add(a); tris.Add(b);
        }

        for (int i = 0; i < sides; i++)
        {
            int f0 = 1 + i;
            int f1 = 1 + ((i + 1) % sides);
            int b0 = 2 + sides + i;
            int b1 = 2 + sides + ((i + 1) % sides);

            tris.Add(f0); tris.Add(f1); tris.Add(b1);
            tris.Add(f0); tris.Add(b1); tris.Add(b0);
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.mesh = mesh;

        Material mat = new Material(GetCompatibleShader());
        mat.color = color;
        mr.material = mat;

        return go;
    }

    Shader GetCompatibleShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null) return shader;

        shader = Shader.Find("Standard");
        if (shader != null) return shader;

        return Shader.Find("Sprites/Default");
    }

    void DrawLine(Vector3 fromLocal, Vector3 toLocal)
    {
        GameObject lineObj = new GameObject("FinalLayerNorm_Line");
        lineObj.transform.SetParent(transform);

        if (lineMaterial == null)
            lineMaterial = new Material(Shader.Find("Sprites/Default"));

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.positionCount = 2;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.SetPosition(0, transform.TransformPoint(fromLocal));
        lr.SetPosition(1, transform.TransformPoint(toLocal));
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.useWorldSpace = true;
    }

    void CreateLabel(string text, Vector3 localPos, float fontSize, Color color)
    {
        GameObject labelObj = new GameObject($"Label_{text}");
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition = localPos;

        TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
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

    void ApplyThickness(TextMeshPro tmp)
    {
        if (tmp.fontMaterial != null && tmp.fontMaterial.HasProperty("_FaceDilate"))
            tmp.fontMaterial.SetFloat("_FaceDilate", textThickness);
    }
}
using UnityEngine;
using TMPro;

[ExecuteAlways]
public class QKVGenerator : MonoBehaviour
{
    [System.Serializable]
    public class QKVSet
    {
        public string sectionLabel = "VALUES";
        public string formulaLabel = "V = XW_V";
        public string shapeLabel = "5x64";
        public Vector3 sourceBoxPosition = new Vector3(65f, 8f, 0f);
        public Color lineColor = new Color(0.55f, 0.35f, 0.85f);
        public Vector3 stackPosition = new Vector3(75f, 8f, 0f);
    }

    public QKVSet[] sets = new QKVSet[]
    {
        new QKVSet { sectionLabel = "VALUES", formulaLabel = "V = XW_V", shapeLabel = "5x64", lineColor = new Color(0.55f, 0.35f, 0.85f) },
        new QKVSet { sectionLabel = "KEYS", formulaLabel = "K = XW_K", shapeLabel = "5x64", lineColor = new Color(0.15f, 0.7f, 0.55f) },
        new QKVSet { sectionLabel = "QUERIES", formulaLabel = "Q = XW_Q", shapeLabel = "5x64", lineColor = new Color(0.75f, 0.3f, 0.3f) },
    };

    [Header("Stack shape")]
    public int slabCount = 8;
    public Vector3 slabSize = new Vector3(4f, 3f, 0.45f);
    public Vector3 slabStep = new Vector3(0.28f, -0.14f, 0f);

    [Header("Texture grid")]
    public int textureSize = 256;
    public int gridColumns = 16;
    public int gridRows = 20;
    public float grainStrength = 0.12f;
    public float gridLineDarken = 0.4f;
    public Color colorA = new Color(0.13f, 0.85f, 0.63f);
    public Color colorB = new Color(0.11f, 0.45f, 0.4f);
    public Color colorC = Color.white;

    [Header("Line settings")]
    public Material lineMaterial;
    public float lineWidth = 0.06f;

    [Header("Label")]
    public float labelFontSize = 6f;
    public Color labelColor = Color.white;
    public Vector3 labelOffset = new Vector3(0f, 2f, 0f);

    [Header("Text thickness")]
    public float textThickness = 0.15f;

    void Start()
    {
        ClearPreviousModel();
        foreach (var s in sets)
            BuildStack(s);
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

    void BuildStack(QKVSet s)
    {
        GameObject root = new GameObject($"Stack_{s.sectionLabel}");
        root.transform.SetParent(transform, false);

        for (int i = 0; i < slabCount; i++)
        {
            Vector3 pos = s.stackPosition + new Vector3(slabStep.x * i, slabStep.y * i, slabStep.z * i);

            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = $"{s.sectionLabel}_Slab_{i}";
            slab.transform.SetParent(root.transform, false);
            slab.transform.localPosition = pos;
            slab.transform.localScale = slabSize;

            Material mat = new Material(GetCompatibleShader());
            mat.mainTexture = GenerateNoiseTexture();
            slab.GetComponent<Renderer>().material = mat;
        }

        GameObject labelObj = new GameObject($"Label_{s.sectionLabel}");
        labelObj.transform.SetParent(root.transform, false);
        labelObj.transform.localPosition = s.stackPosition + labelOffset;

        TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = $"{s.sectionLabel}   {s.formulaLabel}   {s.shapeLabel}";
        tmp.fontSize = labelFontSize;
        tmp.color = labelColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Midline;
        ApplyThickness(tmp);

        Vector3 targetPoint = s.stackPosition - new Vector3(slabSize.x / 2f, 0f, 0f);
        DrawLine(s.sourceBoxPosition, targetPoint, s.lineColor);
    }

    Shader GetCompatibleShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null) return shader;

        shader = Shader.Find("Unlit/Texture");
        if (shader != null) return shader;

        shader = Shader.Find("Standard");
        if (shader != null) return shader;

        return Shader.Find("Sprites/Default");
    }

    Texture2D GenerateNoiseTexture()
    {
        Texture2D tex = new Texture2D(textureSize, textureSize);
        tex.filterMode = FilterMode.Point;

        int cellWidth = Mathf.Max(1, textureSize / gridColumns);
        int cellHeight = Mathf.Max(1, textureSize / gridRows);

        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                int cellX = x / cellWidth;
                int cellY = y / cellHeight;

                float cellNoise = Mathf.PerlinNoise(cellX * 0.35f, cellY * 0.35f);

                Color baseColor;
                if (cellNoise > 0.66f) baseColor = colorA;
                else if (cellNoise > 0.33f) baseColor = colorB;
                else baseColor = colorC;

                float grain = (Mathf.PerlinNoise(x * 0.6f, y * 0.6f) - 0.5f) * grainStrength;
                Color c = baseColor + new Color(grain, grain, grain, 0f);

                bool isGridLine = (x % cellWidth == 0) || (y % cellHeight == 0);
                if (isGridLine)
                    c = Color.Lerp(c, Color.black, gridLineDarken);

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return tex;
    }

    void DrawLine(Vector3 from, Vector3 to, Color color)
    {
        GameObject lineObj = new GameObject("QKV_Source_Line");
        lineObj.transform.SetParent(transform);

        if (lineMaterial == null)
            lineMaterial = new Material(Shader.Find("Sprites/Default"));

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.positionCount = 2;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.SetPosition(0, transform.TransformPoint(from));
        lr.SetPosition(1, transform.TransformPoint(to));
        lr.startColor = color;
        lr.endColor = color;
        lr.useWorldSpace = true;
    }

    void ApplyThickness(TextMeshPro tmp)
    {
        if (tmp.fontMaterial != null && tmp.fontMaterial.HasProperty("_FaceDilate"))
            tmp.fontMaterial.SetFloat("_FaceDilate", textThickness);
    }
}
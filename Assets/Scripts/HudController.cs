using UnityEngine;
using TMPro;

// Attach this to "Global_Static_Overlay" (or Dashboard_Canvas).
public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("Top bar")]
    [Tooltip("Drag Txt_HeaderRight here. In its text, replace the hardcoded '60' with the literal placeholder {FPS} - script will substitute the live number in each update.")]
    public TextMeshProUGUI headerRightText;

    [Tooltip("Drag Txt_HeaderLeft here. In its text, replace the hardcoded 'DASHBOARD' with the literal placeholder {MODE} - script will substitute the live value.")]
    public TextMeshProUGUI headerLeftText;

    private string headerRightTemplate; // raw text cached once, containing {FPS}
    private string headerLeftTemplate;  // raw text cached once, containing {MODE}
    private float fpsTimer;
    private int frameCount;
    private int currentFps = 60;

    void Awake()
    {
        Instance = this;

        if (headerRightText != null)
            headerRightTemplate = headerRightText.text; // cache BEFORE we ever overwrite it

        if (headerLeftText != null)
            headerLeftTemplate = headerLeftText.text; // cache BEFORE we ever overwrite it
    }

    void Start()
    {
        // Loading bar hata diya gaya hai, isliye yahan koi loading-transition call nahi hai.
        SetMode("DASHBOARD"); // taake {MODE} placeholder shuru se hi sahi text dikhaye
    }

    void Update()
    {
        // Real FPS - har 0.5 sec mein update, taake number bhaagta hua na lage
        frameCount++;
        fpsTimer += Time.unscaledDeltaTime;
        if (fpsTimer >= 0.5f)
        {
            currentFps = Mathf.RoundToInt(frameCount / fpsTimer);
            frameCount = 0;
            fpsTimer = 0f;

            if (headerRightText != null && !string.IsNullOrEmpty(headerRightTemplate))
                headerRightText.text = headerRightTemplate.Replace("{FPS}", currentFps.ToString());
        }
    }

    // ---- Public API: call from Explore Models button's OnClick, and from any Back button ----
    public void SetMode(string mode)
    {
        if (headerLeftText != null && !string.IsNullOrEmpty(headerLeftTemplate))
            headerLeftText.text = headerLeftTemplate.Replace("{MODE}", mode); // e.g. "DASHBOARD" or "MODEL VIEW"
    }
}
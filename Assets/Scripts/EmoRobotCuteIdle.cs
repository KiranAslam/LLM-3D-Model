using UnityEngine;

public class EmoRobotCuteIdle : MonoBehaviour
{
    [Header("1. Head Reference (Cube)")]
    public Transform robotHead; 

    [Header("2. Screen Tracking Limits")]
    public float lookTrackingSpeed = 6f; // Thoda fast aur responsive kiya h
    public float maxLookAngleX = 25f;    // Uper aur Niche dekhne ki max limit
    public float maxLookAngleY = 35f;    // Left aur Right dekhne ki max limit

    [Header("3. Eyes Material Matrix")]
    public int eyesMaterialIndex = 1; 
    
    private Renderer headRenderer;
    private Material eyesMaterial;
    private Color originalColor;
    
    [Header("4. Eyes Blinking Settings")]
    public float blinkInterval = 4f; 
    private float blinkTimer;

    [Header("5. Soft Body Breathing")]
    public float breatheAmount = 0.04f; 
    public float breatheSpeed = 1.5f;

    private Vector3 originalPos;
    private Quaternion originalHeadRot;

    void Start()
    {
        originalPos = transform.localPosition;

        // Auto-find Head (Cube)
        if (robotHead == null)
        {
            robotHead = transform.Find("Cube"); 
        }

        if (robotHead != null)
        {
            originalHeadRot = robotHead.localRotation;
            headRenderer = robotHead.GetComponent<Renderer>();
            
            if (headRenderer != null && headRenderer.materials.Length > eyesMaterialIndex)
            {
                eyesMaterial = headRenderer.materials[eyesMaterialIndex];
                if (eyesMaterial.HasProperty("_EmissionColor"))
                {
                    originalColor = eyesMaterial.GetColor("_EmissionColor");
                }
                if (originalColor == Color.black || !eyesMaterial.HasProperty("_EmissionColor"))
                {
                    originalColor = eyesMaterial.color;
                }
            }
        }
        
        blinkTimer = blinkInterval;
    }

    void Update()
    {
        // --- 1. Soft Breathing Effect ---
        float newY = originalPos.y + Mathf.Sin(Time.time * breatheSpeed) * breatheAmount;
        transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);

        // --- 2. Corrected Screen-Percentage Tracking ---
        HandleScreenPercentageTracking();

        // --- 3. Eyes Blinking Logic ---
        HandleBlinking();
    }

    void HandleScreenPercentageTracking()
    {
        if (robotHead == null) return;

        // Mouse position (0 se 1 percentage)
        float mouseXPercent = Input.mousePosition.x / Screen.width;
        float mouseYPercent = Input.mousePosition.y / Screen.height;

        // Center offsets (-0.5 se 0.5)
        float xOffset = mouseXPercent - 0.5f; 
        float yOffset = mouseYPercent - 0.5f; 

        // FIXED DIRECTIONS: Signs ko flip kar diya h taake inversion theek ho jaye
        float targetLookY = -xOffset * 2f * maxLookAngleY; // Left/Right direction match
        float targetLookX = yOffset * 2f * maxLookAngleX;  // Up/Down direction match

        // Constraints safety limits
        targetLookX = Mathf.Clamp(targetLookX, -maxLookAngleX, maxLookAngleX);
        targetLookY = Mathf.Clamp(targetLookY, -maxLookAngleY, maxLookAngleY);

        // Final local rotation structure
        Quaternion targetRotation = originalHeadRot * Quaternion.Euler(targetLookX, targetLookY, 0f);

        // Smooth movement apply karna
        robotHead.localRotation = Quaternion.Slerp(robotHead.localRotation, targetRotation, Time.deltaTime * lookTrackingSpeed);
    }

    void HandleBlinking()
    {
        if (eyesMaterial == null) return;
        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0)
        {
            StartCoroutine(BlinkRoutine());
            blinkTimer = Random.Range(blinkInterval - 1f, blinkInterval + 2f);
        }
    }

    System.Collections.IEnumerator BlinkRoutine()
    {
        SetEyesColor(Color.black);
        yield return new WaitForSeconds(0.15f);
        SetEyesColor(originalColor);
    }

    void SetEyesColor(Color col)
    {
        if (eyesMaterial.HasProperty("_EmissionColor"))
        {
            eyesMaterial.SetColor("_EmissionColor", col);
            DynamicGI.SetEmissive(headRenderer, col);
        }
        else
        {
            eyesMaterial.color = col;
        }
    }
}
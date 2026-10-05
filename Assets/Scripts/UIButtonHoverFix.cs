using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class UIButtonHoverFix : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    // Ek array ya list jo card ke andar ke saare text elements ko store karegi
    private TextMeshProUGUI[] allButtonTexts;
    
    // Har text ka original color format yaad rakhne ke liye list
    private List<Color> originalVertexColors = new List<Color>();

    [Header("🎨 Cyberpunk Dynamic Colors")]
    public Color hoverColor = new Color(0f, 1f, 0f);        // Neon Green
    public Color pressedColor = new Color(0f, 0.4f, 0f);     // Dark Green

    void Awake()
    {
        // Card ke andar majood SAARE TextMeshPro components ko dhundna
        allButtonTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        
        if (allButtonTexts != null)
        {
            // Har text ka jo original color h, usay list me save kar lena
            foreach (var txt in allButtonTexts)
            {
                originalVertexColors.Add(txt.color);
            }
        }
    }

    // 1. Hover -> Turn ALL texts Green
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (allButtonTexts == null) return;
        
        foreach (var txt in allButtonTexts)
        {
            txt.color = hoverColor;
        }
    }

    // 2. Exit -> Restore EVERY text to its original color
    public void OnPointerExit(PointerEventData eventData)
    {
        if (allButtonTexts == null) return;

        for (int i = 0; i < allButtonTexts.Length; i++)
        {
            if (allButtonTexts[i] != null)
            {
                allButtonTexts[i].color = originalVertexColors[i];
            }
        }
    }

    // 3. Click (Press) -> Turn ALL texts Dark Green
    public void OnPointerDown(PointerEventData eventData)
    {
        if (allButtonTexts == null) return;

        foreach (var txt in allButtonTexts)
        {
            txt.color = pressedColor;
        }
    }

    // 4. Release Click
    public void OnPointerUp(PointerEventData eventData)
    {
        if (allButtonTexts == null) return;

        bool isOver = eventData.pointerCurrentRaycast.gameObject == gameObject || 
                      (eventData.pointerCurrentRaycast.gameObject != null && 
                       eventData.pointerCurrentRaycast.gameObject.transform.IsChildOf(transform));

        for (int i = 0; i < allButtonTexts.Length; i++)
        {
            if (allButtonTexts[i] != null)
            {
                allButtonTexts[i].color = isOver ? hoverColor : originalVertexColors[i];
            }
        }
    }
}
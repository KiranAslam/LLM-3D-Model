using UnityEngine;
using System;

// Central place that decides which UI panel and the single LLM model are active.
public class SceneSwitcher : MonoBehaviour
{
    public static SceneSwitcher Instance { get; private set; }

    [Header("Panels")]
    public GameObject panelMainMenu;
    public GameObject panelLLM;

    [Header("LLM Model")]
    public GameObject llmModel;

    [Header("Robot (camera + model, jo bhi disable karna ho yahan daal do)")]
    [Tooltip("Robot_camera aur/ya Emo Robot - jo bhi GameObjects Main Menu ke ilawa kahin bhi nahi dikhne chahiye, sab yahan daal do")]
    public GameObject[] robotObjects;

    [Header("3D Camera (jo DNN/LLM model ko frame karta hai)")]
    public IntegratedCameraController modelCamera;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ShowMainMenu();
    }

    private void SetPanel(GameObject target)
    {
        if (panelMainMenu != null) panelMainMenu.SetActive(panelMainMenu == target);
        if (panelLLM != null) panelLLM.SetActive(panelLLM == target);
    }

    private void SetModels(bool llm)
    {
        if (llmModel == null) return;

        if (llm)
            DisableRuntimeGenerators();

        llmModel.SetActive(llm);
    }

    private void DisableRuntimeGenerators()
    {
        MonoBehaviour[] components = llmModel.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour component in components)
        {
            if (component == null) continue;

            Type componentType = component.GetType();
            if (componentType.Name == "FinalLayerNormGenerator")
                continue;

            if (componentType.Name.EndsWith("Generator", StringComparison.Ordinal))
                component.enabled = false;
        }
    }

    private void SetRobot(bool on)
    {
        if (robotObjects == null) return;
        foreach (var go in robotObjects)
            if (go != null) go.SetActive(on);
    }

    // ---------- Public API - buttons ke OnClick() mein wire karo ----------

    public void ShowMainMenu()
    {
        SetPanel(panelMainMenu);
        SetModels(false);
        SetRobot(true); // sirf yahan robot chalega
    }

    // Existing Explore button events can continue calling this entry point.
   

    public void ShowLLMModel()
    {
        SetPanel(panelLLM);
        SetModels(true);
        SetRobot(false);

        if (modelCamera != null && llmModel != null)
        {
            Transform origin = llmModel.transform.Find("NetworkOrigin");
            modelCamera.targetFocus = origin != null ? origin : llmModel.transform;
            modelCamera.ResetToDefaultView(xRot: 10f, yRot: 0f, distance: 40f, panOffset: new Vector3(0f, 8f, 0f));
        }
    }

}
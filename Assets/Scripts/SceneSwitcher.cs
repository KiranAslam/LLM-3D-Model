using UnityEngine;
using System;

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

    [Header("Explore Model camera view")]
    [Tooltip("Optional orbit/focus center. Leave empty to use the model's NetworkOrigin.")]
    public Transform exploreViewFocusOrigin;
    public float exploreViewXRotation = 99.4f;
    public float exploreViewYRotation = 0.9f;
    public float exploreViewDistance = 300f;
    [Tooltip("Positive value shifts the model left in the camera view without changing model positions.")]
    public float modelViewHorizontalPanOffset = 100f;
    [Tooltip("Positive value shifts the model lower in the camera view without changing model positions.")]
    public float modelViewVerticalPanOffset = 10f;

    private float lastAppliedHorizontalPanOffset;
    private float lastAppliedVerticalPanOffset;
    private float lastAppliedExploreViewDistance;
    private bool isExploreCameraView;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ShowMainMenu();
    }

    void Update()
    {
        if (!isExploreCameraView ||
            panelLLM == null ||
            !panelLLM.activeInHierarchy ||
            modelCamera == null ||
            (Mathf.Approximately(modelViewHorizontalPanOffset, lastAppliedHorizontalPanOffset) &&
             Mathf.Approximately(modelViewVerticalPanOffset, lastAppliedVerticalPanOffset) &&
             Mathf.Approximately(exploreViewDistance, lastAppliedExploreViewDistance)))
            return;

        modelCamera.SetPanOffset(new Vector3(
            modelViewHorizontalPanOffset,
            modelViewVerticalPanOffset,
            0f
        ));
        modelCamera.SetDistance(exploreViewDistance);
        lastAppliedHorizontalPanOffset = modelViewHorizontalPanOffset;
        lastAppliedVerticalPanOffset = modelViewVerticalPanOffset;
        lastAppliedExploreViewDistance = exploreViewDistance;
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

        isExploreCameraView = true;
        if (modelCamera != null)
            modelCamera.SetUserControls(
                allowRotation: false,
                allowZoom: false,
                allowHorizontalPan: true,
                allowVerticalPan: false
            );
        ApplyModelCameraView(
            exploreViewXRotation,
            exploreViewYRotation,
            exploreViewDistance,
            new Vector3(modelViewHorizontalPanOffset, modelViewVerticalPanOffset, 0f)
        );
    }

    public void ResetModelCameraView()
    {
        isExploreCameraView = false;
        if (modelCamera != null)
            modelCamera.SetUserControls(
                allowRotation: true,
                allowZoom: true,
                allowHorizontalPan: true,
                allowVerticalPan: true
            );
        ApplyModelCameraView(10f, 0f, 120f, new Vector3(100f, 0f, 0f));
    }

    private void ApplyModelCameraView(float xRotation, float yRotation, float distance, Vector3 panOffset)
    {
        if (modelCamera == null || llmModel == null)
        {
            Debug.LogError("Cannot reset the model view: assign both Model Camera and LLM Model in SceneSwitcher.", this);
            return;
        }

        Transform networkOrigin = llmModel.transform.Find("NetworkOrigin");
        Transform focusOrigin = exploreViewFocusOrigin != null
            ? exploreViewFocusOrigin
            : networkOrigin != null ? networkOrigin : llmModel.transform;
        modelCamera.SetFocusOrigin(focusOrigin);
        modelCamera.ResetToDefaultView(
            xRot: xRotation,
            yRot: yRotation,
            distance: distance,
            panOffset: panOffset
        );
        lastAppliedHorizontalPanOffset = modelViewHorizontalPanOffset;
        lastAppliedVerticalPanOffset = modelViewVerticalPanOffset;
        lastAppliedExploreViewDistance = exploreViewDistance;
    }

}
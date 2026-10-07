using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[System.Serializable]
public class PipelineStage
{
    public string stageName;
    public GameObject stageRoot;
    public Transform focusPoint;
    public Button stageButton;
}

public class PipelineSimulator : MonoBehaviour
{
    public static PipelineSimulator Instance { get; private set; }

    public PipelineStage[] stages;
    public Button nextButton;
    public Button resetViewButton;
    [Header("Action button selected colors")]
    public Color nextButtonSelectedColor = new Color32(32, 119, 101, 255);
    public Color resetViewButtonSelectedColor = new Color32(32, 119, 101, 255);
    public PredictedWordPanelController predictedWordPanel;
    public Color activeColor = new Color(0.114f, 0.62f, 0.459f);
    public Color normalColor = new Color(0.16f, 0.16f, 0.16f);
    public UnityEvent<Transform> onStageFocus;
    public UnityEvent onSequenceComplete;

    private int currentStageIndex = -1;
    private Color nextButtonNormalColor = Color.white;
    private Color resetViewButtonNormalColor = Color.white;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (predictedWordPanel == null)
            predictedWordPanel = FindObjectOfType<PredictedWordPanelController>(true);
        if (predictedWordPanel != null)
            predictedWordPanel.ClearAndHide();

        if (nextButton != null && nextButton.image != null)
            nextButtonNormalColor = nextButton.image.color;
        if (resetViewButton != null && resetViewButton.image != null)
            resetViewButtonNormalColor = resetViewButton.image.color;

        if (nextButton != null)
        {
            nextButton.interactable = !string.IsNullOrEmpty(InputSentencePanelController.SelectedSentence);
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        if (resetViewButton != null)
            resetViewButton.onClick.AddListener(OnResetViewButtonClicked);

        for (int i = 0; i < stages.Length; i++)
        {
            int index = i;
            if (stages[i].stageButton != null)
            {
                stages[i].stageButton.interactable = !string.IsNullOrEmpty(InputSentencePanelController.SelectedSentence);
                stages[i].stageButton.onClick.AddListener(() => JumpTo(index));
            }
        }
    }

    public void Play(string sentence)
    {
        if (string.IsNullOrEmpty(InputSentencePanelController.SelectedSentence))
            return;

        SetActiveStage(0);
    }

    public void ClearPredictionPanel()
    {
        if (predictedWordPanel != null)
            predictedWordPanel.ClearAndHide();
    }

    public void EnableStageButtons()
    {
        if (string.IsNullOrEmpty(InputSentencePanelController.SelectedSentence))
            return;

        foreach (PipelineStage stage in stages)
            if (stage.stageButton != null)
                stage.stageButton.interactable = true;

        if (nextButton != null)
            nextButton.interactable = true;
    }

    public void NextStage()
    {
        if (string.IsNullOrEmpty(InputSentencePanelController.SelectedSentence))
            return;

        if (currentStageIndex < stages.Length - 1)
            SetActiveStage(currentStageIndex + 1);
    }

    public void ResetModelView()
    {
        if (stages == null || stages.Length == 0)
        {
            Debug.LogError("Cannot reset the model view because no pipeline stages are configured.", this);
            return;
        }

        for (int i = 0; i < stages.Length; i++)
        {
            GameObject stageRoot = stages[i].stageRoot;
            if (stageRoot != null)
                stageRoot.SetActive(true);
        }

        foreach (PipelineStage stage in stages)
        {
            if (stage.stageRoot == null)
                continue;

            MonoBehaviour[] components = stage.stageRoot.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour component in components)
            {
                component.StopAllCoroutines();
                if (component is ISentenceAnimatable animatable)
                    animatable.CompleteImmediately();
            }
        }

        currentStageIndex = -1;
        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i].stageButton != null && stages[i].stageButton.image != null)
                stages[i].stageButton.image.color = normalColor;
        }

        if (predictedWordPanel != null)
            predictedWordPanel.ClearAndHide();

        if (SceneSwitcher.Instance == null)
        {
            Debug.LogError("Cannot reset the model view because SceneSwitcher is unavailable.", this);
            return;
        }

        SceneSwitcher.Instance.ResetModelCameraView();
    }

    private void OnNextButtonClicked()
    {
        NextStage();
        SetActionButtonSelection(nextButton, nextButtonSelectedColor, resetViewButton, resetViewButtonNormalColor);
    }

    private void OnResetViewButtonClicked()
    {
        ResetModelView();
        SetActionButtonSelection(resetViewButton, resetViewButtonSelectedColor, nextButton, nextButtonNormalColor);
    }

    private static void SetActionButtonSelection(
        Button selectedButton,
        Color selectedColor,
        Button otherButton,
        Color otherNormalColor)
    {
        if (selectedButton != null && selectedButton.image != null)
            selectedButton.image.color = selectedColor;
        if (otherButton != null && otherButton.image != null)
            otherButton.image.color = otherNormalColor;
    }

    public void JumpTo(int index)
    {
        if (string.IsNullOrEmpty(InputSentencePanelController.SelectedSentence))
            return;

        SetActiveStage(index);
    }

    void SetActiveStage(int index)
    {
        if (SceneSwitcher.Instance != null && SceneSwitcher.Instance.modelCamera != null)
        {
            SceneSwitcher.Instance.modelCamera.SetUserControls(
                allowRotation: true,
                allowZoom: true,
                allowHorizontalPan: true,
                allowVerticalPan: true
            );
        }

        if (currentStageIndex >= 0 && currentStageIndex < stages.Length && stages[currentStageIndex].stageRoot != null)
        {
            GameObject previousStageRoot = stages[currentStageIndex].stageRoot;
            MonoBehaviour[] previousComponents = previousStageRoot.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour mb in previousComponents)
            {
                mb.StopAllCoroutines();

                if (mb is ISentenceAnimatable previousAnimatable)
                    previousAnimatable.CompleteImmediately();
            }
        }

        currentStageIndex = index;
        if (predictedWordPanel != null)
        {
            if (index == stages.Length - 1)
                predictedWordPanel.ShowPredictions(InputSentencePanelController.SelectedSentence);
            else
                predictedWordPanel.ClearAndHide();
        }

        for (int i = 0; i < stages.Length; i++)
            if (stages[i].stageButton != null)
                stages[i].stageButton.image.color = (i == index) ? activeColor : normalColor;
        for (int i = 0; i < stages.Length; i++)
            if (stages[i].stageRoot != null)
                stages[i].stageRoot.SetActive(i <= index);
        if (stages[index].stageRoot != null)
        {
            MonoBehaviour[] stageComponents = stages[index].stageRoot.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour component in stageComponents)
                if (component is ISentenceAnimatable animatable)
                    animatable.Animate(InputSentencePanelController.SelectedSentence);
        }
        if (stages[index].focusPoint != null)
            onStageFocus.Invoke(stages[index].focusPoint);
        if (index == stages.Length - 1)
            onSequenceComplete.Invoke();
    }
}
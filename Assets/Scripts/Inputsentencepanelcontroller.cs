using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InputSentencePanelController : MonoBehaviour
{
    public Transform contentParent;
    public Button runButton;
    public Color normalColor = new Color(0.16f, 0.16f, 0.16f);
    public Color selectedColor = new Color(0.114f, 0.62f, 0.459f);
    public static string SelectedSentence { get; private set; }
    private Button[] existingButtons;
    private bool isStageVoicePlaying;
    private bool isStagePanelVoiceCompleted;
    private Color runButtonOriginalColor;

    void Start()
    {
        WireExistingButtons();
        if (runButton != null)
        {
            if (runButton.image != null)
                runButtonOriginalColor = runButton.image.color;

            runButton.interactable = false;
            runButton.onClick.AddListener(OnRunClicked);
            if (PipelineSimulator.Instance != null)
                PipelineSimulator.Instance.onSequenceComplete.AddListener(() =>
                {
                    if (runButton.interactable && runButton.image != null)
                        runButton.image.color = normalColor;
                });
        }

        if (AudioTutorialController.Instance != null)
        {
            isStagePanelVoiceCompleted = AudioTutorialController.Instance.IsStagePanelVoiceCompleted;
            isStageVoicePlaying = AudioTutorialController.Instance.IsStageVoicePendingOrPlaying;
            AudioTutorialController.Instance.StageVoicePlaybackChanged += OnStageVoicePlaybackChanged;
        }

        UpdateSentenceButtonsInteractable();
        UpdateRunButtonInteractable();
    }

    private void OnDestroy()
    {
        if (AudioTutorialController.Instance != null)
            AudioTutorialController.Instance.StageVoicePlaybackChanged -= OnStageVoicePlaybackChanged;
    }

    void WireExistingButtons()
    {
        existingButtons = contentParent.GetComponentsInChildren<Button>(true);
        foreach (Button btn in existingButtons)
        {
            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label == null)
                continue;
            string sentence = label.text.Trim();
            string capturedSentence = sentence;
            btn.onClick.AddListener(() => OnSentenceSelected(capturedSentence, btn));
        }
    }

    void OnSentenceSelected(string sentence, Button clickedButton)
    {
        SelectedSentence = sentence;
        if (PipelineSimulator.Instance != null)
            PipelineSimulator.Instance.ClearPredictionPanel();
        foreach (Button b in existingButtons)
            b.image.color = (b == clickedButton) ? selectedColor : normalColor;
        if (PipelineSimulator.Instance != null)
            PipelineSimulator.Instance.EnableStageButtons();
        UpdateRunButtonInteractable();
    }

    void OnRunClicked()
    {
        if (string.IsNullOrEmpty(SelectedSentence))
            return;
        runButton.image.color = selectedColor;
        PipelineSimulator.Instance.Play(SelectedSentence);
    }

    private void OnStageVoicePlaybackChanged(bool isPlaying)
    {
        isStageVoicePlaying = isPlaying;
        if (AudioTutorialController.Instance != null)
            isStagePanelVoiceCompleted = AudioTutorialController.Instance.IsStagePanelVoiceCompleted;
        UpdateSentenceButtonsInteractable();
        UpdateRunButtonInteractable();
    }

    private void UpdateSentenceButtonsInteractable()
    {
        if (existingButtons == null)
            return;

        foreach (Button button in existingButtons)
            if (button != null)
                button.interactable = isStagePanelVoiceCompleted;
    }

    private void UpdateRunButtonInteractable()
    {
        if (runButton == null)
            return;

        bool canRun = isStagePanelVoiceCompleted &&
                      !isStageVoicePlaying &&
                      !string.IsNullOrEmpty(SelectedSentence);
        runButton.interactable = canRun;

        if (runButton.image != null)
        {
            runButton.image.color = canRun
                ? runButtonOriginalColor
                : new Color(
                    runButtonOriginalColor.r * 0.5f,
                    runButtonOriginalColor.g * 0.5f,
                    runButtonOriginalColor.b * 0.5f,
                    runButtonOriginalColor.a
                );
        }
    }
}
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

    void Start()
    {
        WireExistingButtons();
        if (runButton != null)
        {
            runButton.interactable = false;
            runButton.onClick.AddListener(OnRunClicked);
            if (PipelineSimulator.Instance != null)
                PipelineSimulator.Instance.onSequenceComplete.AddListener(() => runButton.image.color = normalColor);
        }
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
        if (runButton != null)
            runButton.interactable = true;
    }

    void OnRunClicked()
    {
        if (string.IsNullOrEmpty(SelectedSentence))
            return;
        runButton.image.color = selectedColor;
        PipelineSimulator.Instance.Play(SelectedSentence);
    }
}
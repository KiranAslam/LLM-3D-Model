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
    public Color activeColor = new Color(0.114f, 0.62f, 0.459f);
    public Color normalColor = new Color(0.16f, 0.16f, 0.16f);
    public UnityEvent<Transform> onStageFocus;
    public UnityEvent onSequenceComplete;

    private int currentStageIndex = -1;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        for (int i = 0; i < stages.Length; i++)
        {
            int index = i;
            if (stages[i].stageButton != null)
                stages[i].stageButton.onClick.AddListener(() => JumpTo(index));
        }
    }

    public void Play(string sentence)
    {
        currentStageIndex = 0;
        SetActiveStage(0);
    }

    public void NextStage()
    {
        if (currentStageIndex < stages.Length - 1)
        {
            currentStageIndex++;
            SetActiveStage(currentStageIndex);
        }
    }

    public void JumpTo(int index)
    {
        SetActiveStage(index);
    }

    void SetActiveStage(int index)
    {
        if (currentStageIndex >= 0 && currentStageIndex < stages.Length && stages[currentStageIndex].stageRoot != null)
        {
            MonoBehaviour[] previousComponents = stages[currentStageIndex].stageRoot.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour mb in previousComponents)
                mb.StopAllCoroutines();

            ISentenceAnimatable previousAnimatable = stages[currentStageIndex].stageRoot.GetComponentInChildren<ISentenceAnimatable>(true);
            if (previousAnimatable != null)
                previousAnimatable.CompleteImmediately();
        }

        currentStageIndex = index;
        for (int i = 0; i < stages.Length; i++)
            if (stages[i].stageButton != null)
                stages[i].stageButton.image.color = (i == index) ? activeColor : normalColor;
        for (int i = 0; i < stages.Length; i++)
            if (stages[i].stageRoot != null)
                stages[i].stageRoot.SetActive(i <= index);
        if (stages[index].stageRoot != null)
        {
            ISentenceAnimatable animatable = stages[index].stageRoot.GetComponentInChildren<ISentenceAnimatable>(true);
            if (animatable != null)
                animatable.Animate(InputSentencePanelController.SelectedSentence);
        }
        if (stages[index].focusPoint != null)
            onStageFocus.Invoke(stages[index].focusPoint);
        if (index == stages.Length - 1)
            onSequenceComplete.Invoke();
    }
}
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class AudioTutorialController : MonoBehaviour
{
    public enum AttentionVoiceStep
    {
        ValueStack,
        KeyStack,
        QueryStack,
        Transpose,
        QKT,
        Softmax,
        AV,
        OAV,
        Concat,
        LayerOutput,
        AddNorm,
        PostAddNorm
    }

    [Header("Startup Audio")]
    [SerializeField] private AudioClip startupVoice;
    [SerializeField] private AudioClip exploreModelVoice;
    [SerializeField] private AudioClip inputSentenceVoice;
    [SerializeField] private AudioClip stagePanelVoice;
    [SerializeField] private AudioClip enableVoice;
    [SerializeField] private AudioClip resetViewVoice;
    [Header("Attention Layer Audio")]
    [SerializeField] private AudioClip valueStackVoice;
    [SerializeField] private AudioClip keyStackVoice;
    [SerializeField] private AudioClip queryStackVoice;
    [SerializeField] private AudioClip transposeVoice;
    [SerializeField] private AudioClip qktVoice;
    [SerializeField] private AudioClip softmaxVoice;
    [SerializeField] private AudioClip avVoice;
    [SerializeField] private AudioClip oavVoice;
    [SerializeField] private AudioClip concatVoice;
    [SerializeField] private AudioClip layerOutputVoice;
    [SerializeField] private AudioClip addNormVoice;
    [SerializeField] private AudioClip postAddNormVoice;
    [SerializeField, Tooltip("One voice per PipelineSimulator stage, in stage order.")]
    private AudioClip[] stageButtonVoices = new AudioClip[8];
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private Button voiceGuideButton;
    [SerializeField] private TMP_Text voiceGuideButtonLabel;

    [Header("Explore Model Panels")]
    [SerializeField] private GameObject inputSentencePanel;
    [SerializeField] private GameObject stagePanel;
    [SerializeField, Range(0f, 1f)] private float lockedPanelAlpha = 0.4f;

    public static AudioTutorialController Instance { get; private set; }
    public event Action<bool> StageVoicePlaybackChanged;

    private bool startupVoicePlayed;
    private bool resetViewVoicePlayed;
    private bool exploreVoiceSequenceCompleted;
    private bool stagePanelVoiceCompleted;
    private bool isStageVoicePlaying;
    private bool isVoiceGuidePaused;
    private bool isPipelinePlaybackPaused;
    private readonly Dictionary<GameObject, CanvasGroup> panelGroups = new Dictionary<GameObject, CanvasGroup>();
    private readonly Dictionary<GameObject, PanelState> originalPanelStates = new Dictionary<GameObject, PanelState>();
    private Coroutine exploreVoiceSequence;
    private Coroutine stageVoicePlayback;
    private Coroutine attentionVoicePlayback;
    private int pendingStageVoiceIndex = -1;
    private int currentStageVoiceIndex = -1;
    private AttentionVoiceStep? currentAttentionVoiceStep;
    public bool IsStageVoicePendingOrPlaying => isStageVoicePlaying || pendingStageVoiceIndex >= 0;
    public bool IsStagePanelVoiceCompleted => stagePanelVoiceCompleted;

    private struct PanelState
    {
        public float alpha;
        public bool interactable;
        public bool blocksRaycasts;
    }

    private void Awake()
    {
        Instance = this;

        if (voiceSource == null)
            voiceSource = GetComponent<AudioSource>();

        if (voiceSource == null)
            voiceSource = gameObject.AddComponent<AudioSource>();

        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
        CachePanel(inputSentencePanel);
        CachePanel(stagePanel);
        if (voiceGuideButtonLabel == null && voiceGuideButton != null)
            voiceGuideButtonLabel = voiceGuideButton.GetComponentInChildren<TMP_Text>();
        if (voiceGuideButton != null)
            voiceGuideButton.onClick.AddListener(HandleVoiceGuideButtonClicked);
        SetVoiceGuideButtonLabel("Play Voice Guide");
    }

    private void OnDestroy()
    {
        if (voiceGuideButton != null)
            voiceGuideButton.onClick.RemoveListener(HandleVoiceGuideButtonClicked);

        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        PlayStartupVoiceOnce();
    }

    public void PlayStartupVoiceOnce()
    {
        if (startupVoicePlayed || startupVoice == null)
            return;

        startupVoicePlayed = true;
        voiceSource.Stop();
        voiceSource.clip = startupVoice;
        voiceSource.Play();
    }

    public void PlayResetViewVoiceOnce()
    {
        if (resetViewVoicePlayed)
            return;

        if (resetViewVoice == null)
        {
            Debug.LogWarning("[AudioTutorialController] Assign the Reset View voice clip in the Inspector.", this);
            return;
        }

        resetViewVoicePlayed = true;
        currentStageVoiceIndex = -1;
        currentAttentionVoiceStep = null;
        PlayClip(resetViewVoice);
    }

    public void PlayAttentionVoice(AttentionVoiceStep step)
    {
        currentAttentionVoiceStep = step;
        if (isVoiceGuidePaused)
            return;

        if (!TryGetAttentionVoice(step, out AudioClip clip))
            return;

        StopStageVoicePlayback();
        voiceSource.clip = clip;
        voiceSource.Play();
        SetStageVoicePlaying(true);
        attentionVoicePlayback = StartCoroutine(WaitForAttentionVoiceToFinish());
    }

    private bool TryGetAttentionVoice(AttentionVoiceStep step, out AudioClip clip)
    {
        clip = step switch
        {
            AttentionVoiceStep.ValueStack => valueStackVoice,
            AttentionVoiceStep.KeyStack => keyStackVoice,
            AttentionVoiceStep.QueryStack => queryStackVoice,
            AttentionVoiceStep.Transpose => transposeVoice,
            AttentionVoiceStep.QKT => qktVoice,
            AttentionVoiceStep.Softmax => softmaxVoice,
            AttentionVoiceStep.AV => avVoice,
            AttentionVoiceStep.OAV => oavVoice,
            AttentionVoiceStep.Concat => concatVoice,
            AttentionVoiceStep.LayerOutput => layerOutputVoice,
            AttentionVoiceStep.AddNorm => addNormVoice,
            AttentionVoiceStep.PostAddNorm => postAddNormVoice,
            _ => null
        };

        if (clip != null)
            return true;

        Debug.LogWarning($"[AudioTutorialController] Assign the {step} voice clip in the Inspector.", this);
        return false;
    }

    private IEnumerator WaitForAttentionVoiceToFinish()
    {
        while (isPipelinePlaybackPaused || (voiceSource != null && voiceSource.isPlaying))
            yield return null;

        attentionVoicePlayback = null;
        SetStageVoicePlaying(false);
    }

    public void PlayExploreModelVoice()
    {
        if (exploreVoiceSequenceCompleted)
        {
            RestorePanel(inputSentencePanel);
            RestorePanel(stagePanel);
            return;
        }

        if (exploreVoiceSequence != null)
            return;

        LockPanel(inputSentencePanel);
        LockPanel(stagePanel);
        SetVoiceGuideButtonLabel("Skip Voice Guide");
        exploreVoiceSequence = StartCoroutine(PlayExploreVoiceSequence());
    }

    public void PlayStageButtonVoice(int stageIndex)
    {
        currentStageVoiceIndex = stageIndex;
        if (isVoiceGuidePaused)
            return;

        if (!TryGetStageButtonVoice(stageIndex, out AudioClip clip))
        {
            return;
        }

        if (exploreVoiceSequence != null)
        {
            pendingStageVoiceIndex = stageIndex;
            return;
        }

        PlayClip(clip);
    }

    private bool TryGetStageButtonVoice(int stageIndex, out AudioClip clip)
    {
        clip = null;
        if (stageButtonVoices == null || stageIndex < 0 || stageIndex >= stageButtonVoices.Length)
        {
            Debug.LogWarning($"[AudioTutorialController] No stage voice slot is configured for index {stageIndex}.", this);
            return false;
        }

        clip = stageButtonVoices[stageIndex];
        if (clip == null)
        {
            Debug.LogWarning($"[AudioTutorialController] Assign the voice clip for pipeline stage {stageIndex + 1}.", this);
            return false;
        }

        return true;
    }

    private void PlayClip(AudioClip clip)
    {
        StopStageVoicePlayback();
        currentAttentionVoiceStep = null;
        voiceSource.clip = clip;
        voiceSource.Play();
        SetStageVoicePlaying(true);
        stageVoicePlayback = StartCoroutine(WaitForStageVoiceToFinish());
    }

    private IEnumerator WaitForStageVoiceToFinish()
    {
        while (isPipelinePlaybackPaused || (voiceSource != null && voiceSource.isPlaying))
            yield return null;

        stageVoicePlayback = null;
        SetStageVoicePlaying(false);
    }

    public void PausePipelineVoice()
    {
        isPipelinePlaybackPaused = true;
        if (voiceSource != null && voiceSource.isPlaying)
            voiceSource.Pause();
        SetVoiceGuideButtonLabel("Resume");
    }

    public void ResumePipelineVoice()
    {
        isPipelinePlaybackPaused = false;
        if (voiceSource != null)
            voiceSource.UnPause();
        UpdateVoiceGuideButtonLabel();
    }

    private void SetStageVoicePlaying(bool isPlaying)
    {
        if (isStageVoicePlaying == isPlaying)
            return;

        isStageVoicePlaying = isPlaying;
        UpdateVoiceGuideButtonLabel();
        StageVoicePlaybackChanged?.Invoke(isPlaying);
    }

    public void HandleVoiceGuideButtonClicked()
    {
        if (isPipelinePlaybackPaused)
        {
            if (PipelineSimulator.Instance != null)
                PipelineSimulator.Instance.ResumePlaybackFromVoiceGuide();
            else
                Debug.LogError("[AudioTutorialController] Cannot resume playback because PipelineSimulator is unavailable.", this);
            return;
        }

        if (exploreVoiceSequence != null)
        {
            StopExploreModelVoiceSequence();
            return;
        }

        if (isStageVoicePlaying)
        {
            isVoiceGuidePaused = true;
            StopStageVoicePlayback();
            return;
        }

        if (currentAttentionVoiceStep.HasValue)
        {
            isVoiceGuidePaused = false;
            PlayAttentionVoice(currentAttentionVoiceStep.Value);
            return;
        }

        if (currentStageVoiceIndex >= 0)
        {
            isVoiceGuidePaused = false;
            PlayStageButtonVoice(currentStageVoiceIndex);
        }
    }

    public void StopCurrentStageVoice()
    {
        pendingStageVoiceIndex = -1;
        StopStageVoicePlayback();
        currentAttentionVoiceStep = null;
    }

    private void SetVoiceGuideButtonLabel(string label)
    {
        if (voiceGuideButtonLabel != null)
            voiceGuideButtonLabel.text = label;
    }

    private void UpdateVoiceGuideButtonLabel()
    {
        SetVoiceGuideButtonLabel(
            isPipelinePlaybackPaused
                ? "Resume"
                : isStageVoicePlaying || exploreVoiceSequence != null
                    ? "Skip Voice Guide"
                    : "Play Voice Guide"
        );
    }

    public void StopExploreModelVoiceSequence()
    {
        exploreVoiceSequenceCompleted = true;
        CompleteStagePanelVoice();
        pendingStageVoiceIndex = -1;
        currentStageVoiceIndex = -1;
        currentAttentionVoiceStep = null;

        if (exploreVoiceSequence != null)
        {
            StopCoroutine(exploreVoiceSequence);
            exploreVoiceSequence = null;
        }

        StopStageVoicePlayback();
        if (voiceSource != null)
            voiceSource.Stop();

        RestorePanel(inputSentencePanel);
        RestorePanel(stagePanel);
        SetVoiceGuideButtonLabel("Play Voice Guide");
    }

    private void StopStageVoicePlayback()
    {
        if (stageVoicePlayback != null)
        {
            StopCoroutine(stageVoicePlayback);
            stageVoicePlayback = null;
        }

        if (attentionVoicePlayback != null)
        {
            StopCoroutine(attentionVoicePlayback);
            attentionVoicePlayback = null;
        }

        if (voiceSource != null)
            voiceSource.Stop();
        SetStageVoicePlaying(false);
    }

    private IEnumerator PlayExploreVoiceSequence()
    {
        yield return PlayClipToCompletion(exploreModelVoice, "Explore Model");
        yield return new WaitForSecondsRealtime(1f);

        UnlockPanel(inputSentencePanel);
        yield return PlayClipToCompletion(inputSentenceVoice, "Input Sentence");
        yield return new WaitForSecondsRealtime(1f);

        UnlockPanel(stagePanel);
        yield return PlayClipToCompletion(
            stagePanelVoice,
            "Stages",
            blocksRunButton: true,
            keepPlaybackBlockedAfterCompletion: true
        );
        SetStageVoicePlaying(false);
        CompleteStagePanelVoice();
        yield return PlayClipToCompletion(enableVoice, "Enable");
        exploreVoiceSequence = null;
        exploreVoiceSequenceCompleted = true;

        int queuedStageVoice = pendingStageVoiceIndex;
        pendingStageVoiceIndex = -1;
        if (queuedStageVoice >= 0 && TryGetStageButtonVoice(queuedStageVoice, out AudioClip clip))
            PlayClip(clip);
    }

    private void CompleteStagePanelVoice()
    {
        if (stagePanelVoiceCompleted)
            return;

        stagePanelVoiceCompleted = true;
        StageVoicePlaybackChanged?.Invoke(isStageVoicePlaying);
    }

    private IEnumerator PlayClipToCompletion(
        AudioClip clip,
        string voiceName,
        bool blocksRunButton = false,
        bool keepPlaybackBlockedAfterCompletion = false)
    {
        if (clip == null)
        {
            Debug.LogWarning($"[AudioTutorialController] Assign the {voiceName} voice clip in the Inspector.", this);
            if (blocksRunButton)
                SetStageVoicePlaying(false);
            yield break;
        }

        while (isPipelinePlaybackPaused)
            yield return null;

        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.Play();
        if (blocksRunButton)
            SetStageVoicePlaying(true);

        while (isPipelinePlaybackPaused || (voiceSource != null && voiceSource.isPlaying))
            yield return null;

        if (blocksRunButton && !keepPlaybackBlockedAfterCompletion)
            SetStageVoicePlaying(false);
    }

    private void CachePanel(GameObject panel)
    {
        if (panel == null || panelGroups.ContainsKey(panel))
            return;

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group == null)
            group = panel.AddComponent<CanvasGroup>();

        panelGroups.Add(panel, group);
        originalPanelStates.Add(panel, new PanelState
        {
            alpha = group.alpha,
            interactable = group.interactable,
            blocksRaycasts = group.blocksRaycasts
        });
    }

    private void LockPanel(GameObject panel)
    {
        if (panel == null)
            return;

        CachePanel(panel);
        CanvasGroup group = panelGroups[panel];
        group.alpha = lockedPanelAlpha;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void UnlockPanel(GameObject panel)
    {
        RestorePanel(panel);
    }

    private void RestorePanel(GameObject panel)
    {
        if (panel == null || !panelGroups.ContainsKey(panel))
            return;

        CanvasGroup group = panelGroups[panel];
        PanelState original = originalPanelStates[panel];
        group.alpha = original.alpha;
        group.interactable = original.interactable;
        group.blocksRaycasts = original.blocksRaycasts;
    }
}
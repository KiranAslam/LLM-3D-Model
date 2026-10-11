using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttentionLayerAnimator : MonoBehaviour, ISentenceAnimatable, IStageAnimationStatus
{
    public Transform[] anchors;
    public GameObject[] revealObjects;
    public IntegratedCameraController cameraController;
    public float stepDelay = 0.6f;
    public float slabRevealDelay = 0.08f;

    public bool IsStageAnimationComplete { get; private set; } = true;

    public void Animate(string sentence)
    {
        StopAllCoroutines();
        IsStageAnimationComplete = false;
        foreach (GameObject go in revealObjects)
            if (go != null)
                go.SetActive(false);
        StartCoroutine(RevealSequence());
    }

    public void CompleteImmediately()
    {
        StopAllCoroutines();
        IsStageAnimationComplete = true;
        foreach (GameObject go in revealObjects)
        {
            if (go == null)
                continue;

            go.SetActive(true);
            CompleteRevealImmediately(go);
        }
    }

    private IEnumerator RevealSequence()
    {
        if (revealObjects == null || revealObjects.Length == 0)
        {
            IsStageAnimationComplete = true;
            yield break;
        }

        ActivateAndFocus(0);
        AudioTutorialController audioController = AudioTutorialController.Instance;
        while (audioController != null && audioController.IsStageVoicePendingOrPlaying)
            yield return null;

        int valueStackIndex = 0;
        bool addNormRevealed = false;
        for (int i = 1; i < revealObjects.Length; i++)
        {
            GameObject revealObject = revealObjects[i];
            if (revealObject == null)
                continue;

            ActivateAndFocus(i);
            if (TryGetVoiceStep(revealObject, ref valueStackIndex, out AudioTutorialController.AttentionVoiceStep voiceStep))
                audioController?.PlayAttentionVoice(voiceStep);

            yield return StartCoroutine(AnimateReveal(revealObject));
            while (audioController != null && audioController.IsStageVoicePendingOrPlaying)
                yield return null;

            if (revealObject.GetComponent<AddNormGenerator>() != null)
                addNormRevealed = true;
        }

        if (addNormRevealed && audioController != null)
        {
            if (SceneSwitcher.Instance != null)
                SceneSwitcher.Instance.TransitionToPostAttentionView();

            yield return new WaitForSeconds(1f);
            audioController.PlayAttentionVoice(AudioTutorialController.AttentionVoiceStep.PostAddNorm);
            while (audioController.IsStageVoicePendingOrPlaying)
                yield return null;
        }

        IsStageAnimationComplete = true;
    }

    private void ActivateAndFocus(int index)
    {
        if (index < revealObjects.Length && revealObjects[index] != null)
            revealObjects[index].SetActive(true);

        if (cameraController != null && anchors != null &&
            index < anchors.Length && anchors[index] != null)
            cameraController.FocusOnStage(anchors[index]);
    }

    private bool TryGetVoiceStep(
        GameObject revealObject,
        ref int valueStackIndex,
        out AudioTutorialController.AttentionVoiceStep voiceStep)
    {
        if (revealObject.GetComponent<ValueStackGenerator>() != null)
        {
            voiceStep = valueStackIndex switch
            {
                0 => AudioTutorialController.AttentionVoiceStep.ValueStack,
                1 => AudioTutorialController.AttentionVoiceStep.KeyStack,
                2 => AudioTutorialController.AttentionVoiceStep.QueryStack,
                _ => AudioTutorialController.AttentionVoiceStep.ValueStack
            };
            valueStackIndex++;
            return valueStackIndex <= 3;
        }

        if (revealObject.GetComponent<TransposeGenerator>() != null)
            voiceStep = AudioTutorialController.AttentionVoiceStep.Transpose;
        else if (revealObject.GetComponent<QKTGenerator>() != null)
            voiceStep = AudioTutorialController.AttentionVoiceStep.QKT;
        else if (revealObject.GetComponent<SoftmaxGenerator>() != null)
            voiceStep = AudioTutorialController.AttentionVoiceStep.Softmax;
        else if (revealObject.GetComponent<AVGenerator>() != null)
            voiceStep = AudioTutorialController.AttentionVoiceStep.AV;
        else if (revealObject.GetComponent<OAVGenerator>() != null)
            voiceStep = AudioTutorialController.AttentionVoiceStep.OAV;
        else if (revealObject.GetComponent<ConcatGenerator>() != null)
            voiceStep = AudioTutorialController.AttentionVoiceStep.Concat;
        else if (revealObject.GetComponent<LayerOutGenerator>() != null)
            voiceStep = AudioTutorialController.AttentionVoiceStep.LayerOutput;
        else if (revealObject.GetComponent<AddNormGenerator>() != null)
            voiceStep = AudioTutorialController.AttentionVoiceStep.AddNorm;
        else
        {
            voiceStep = default;
            return false;
        }

        return true;
    }

    private IEnumerator AnimateReveal(GameObject revealObject)
    {
        ValueStackGenerator valueStack = revealObject.GetComponent<ValueStackGenerator>();
        if (valueStack != null)
            yield return StartCoroutine(valueStack.AnimateReveal(slabRevealDelay));
        else if (revealObject.GetComponent<TransposeGenerator>() is TransposeGenerator transpose)
            yield return StartCoroutine(transpose.AnimateReveal(slabRevealDelay));
        else if (revealObject.GetComponent<QKTGenerator>() is QKTGenerator qkt)
            yield return StartCoroutine(qkt.AnimateReveal(slabRevealDelay));
        else if (revealObject.GetComponent<SoftmaxGenerator>() is SoftmaxGenerator softmax)
            yield return StartCoroutine(softmax.AnimateReveal(slabRevealDelay));
        else if (revealObject.GetComponent<AVGenerator>() is AVGenerator av)
            yield return StartCoroutine(av.AnimateReveal(slabRevealDelay));
        else if (revealObject.GetComponent<OAVGenerator>() is OAVGenerator oav)
            yield return StartCoroutine(oav.AnimateReveal(slabRevealDelay));
        else if (revealObject.GetComponent<ConcatGenerator>() is ConcatGenerator concat)
            yield return StartCoroutine(concat.AnimateReveal(slabRevealDelay));
        else if (revealObject.GetComponent<LayerOutGenerator>() is LayerOutGenerator layerOut)
            yield return StartCoroutine(layerOut.AnimateReveal(slabRevealDelay));
        else if (revealObject.GetComponent<AddNormGenerator>() is AddNormGenerator addNorm)
            yield return StartCoroutine(addNorm.AnimateReveal(slabRevealDelay));
    }

    private void CompleteRevealImmediately(GameObject revealObject)
    {
        revealObject.GetComponent<ValueStackGenerator>()?.CompleteImmediately();
        revealObject.GetComponent<TransposeGenerator>()?.CompleteImmediately();
        revealObject.GetComponent<QKTGenerator>()?.CompleteImmediately();
        revealObject.GetComponent<SoftmaxGenerator>()?.CompleteImmediately();
        revealObject.GetComponent<AVGenerator>()?.CompleteImmediately();
        revealObject.GetComponent<OAVGenerator>()?.CompleteImmediately();
        revealObject.GetComponent<ConcatGenerator>()?.CompleteImmediately();
        revealObject.GetComponent<LayerOutGenerator>()?.CompleteImmediately();
        revealObject.GetComponent<AddNormGenerator>()?.CompleteImmediately();
    }
}
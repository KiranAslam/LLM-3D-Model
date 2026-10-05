using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttentionLayerAnimator : MonoBehaviour, ISentenceAnimatable
{
    public Transform[] anchors;
    public GameObject[] revealObjects;
    public IntegratedCameraController cameraController;
    public float stepDelay = 0.6f;
    public float slabRevealDelay = 0.08f;

    public void Animate(string sentence)
    {
        StopAllCoroutines();
        foreach (GameObject go in revealObjects)
            if (go != null)
                go.SetActive(false);
        StartCoroutine(RevealSequence());
    }

    public void CompleteImmediately()
    {
        StopAllCoroutines();
        foreach (GameObject go in revealObjects)
        {
            if (go == null)
                continue;
            go.SetActive(true);
            ValueStackGenerator stackGen = go.GetComponent<ValueStackGenerator>();
            if (stackGen != null)
                stackGen.CompleteImmediately();
        }
    }

    private IEnumerator RevealSequence()
    {
        for (int i = 0; i < anchors.Length; i++)
        {
            if (cameraController != null && anchors[i] != null)
                cameraController.FocusOnStage(anchors[i]);

            if (i < revealObjects.Length && revealObjects[i] != null)
            {
                ValueStackGenerator stackGen = revealObjects[i].GetComponent<ValueStackGenerator>();
                revealObjects[i].SetActive(true);
                if (stackGen != null)
                    yield return StartCoroutine(stackGen.AnimateReveal(slabRevealDelay));
                else
                {
                    TransposeGenerator transposeGen = revealObjects[i].GetComponent<TransposeGenerator>();
                    if (transposeGen != null)
                        yield return StartCoroutine(transposeGen.AnimateReveal(slabRevealDelay));
                    else
                    {
                        QKTGenerator qktGen = revealObjects[i].GetComponent<QKTGenerator>();
                        if (qktGen != null)
                            yield return StartCoroutine(qktGen.AnimateReveal(slabRevealDelay));
                        else
                        {
                            SoftmaxGenerator softmaxGen = revealObjects[i].GetComponent<SoftmaxGenerator>();
                            if (softmaxGen != null)
                                yield return StartCoroutine(softmaxGen.AnimateReveal(slabRevealDelay));
                            else
                            {
                                AVGenerator avGen = revealObjects[i].GetComponent<AVGenerator>();
                                if (avGen != null)
                                    yield return StartCoroutine(avGen.AnimateReveal(slabRevealDelay));
                                else
                                {
                                    OAVGenerator oavGen = revealObjects[i].GetComponent<OAVGenerator>();
                                    if (oavGen != null)
                                        yield return StartCoroutine(oavGen.AnimateReveal(slabRevealDelay));
                                    else
                                    {
                                        ConcatGenerator concatGen = revealObjects[i].GetComponent<ConcatGenerator>();
                                        if (concatGen != null)
                                            yield return StartCoroutine(concatGen.AnimateReveal(slabRevealDelay));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            yield return new WaitForSeconds(stepDelay);
        }
    }
}
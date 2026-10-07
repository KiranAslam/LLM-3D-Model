using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PredictedWordPanelController : MonoBehaviour
{
    [System.Serializable]
    public class PredictionRow
    {
        public TextMeshProUGUI wordText;
        public TextMeshProUGUI probabilityText;
        public Slider probabilitySlider;
        public Image probabilityBar;
    }

    public GameObject panelRoot;
    public PredictionRow[] predictionRows;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        ClearAndHide();
    }

    public void ClearAndHide()
    {
        ClearRows();
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void ShowPredictions(string sentence)
    {
        ClearRows();

        DummyDataProvider provider = DummyDataProvider.Instance;
        DummyDataProvider.SentenceEntry entry = provider != null
            ? provider.GetEntry(sentence)
            : null;

        if (entry == null || entry.predictedWords == null || entry.predictedProbabilities == null)
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);
            return;
        }

        int count = Mathf.Min(
            predictionRows != null ? predictionRows.Length : 0,
            Mathf.Min(entry.predictedWords.Length, entry.predictedProbabilities.Length)
        );

        for (int i = 0; i < count; i++)
        {
            PredictionRow row = predictionRows[i];
            if (row == null)
                continue;

            float probability = Mathf.Clamp(entry.predictedProbabilities[i], 0f, 100f);
            if (row.wordText != null)
            {
                row.wordText.text = entry.predictedWords[i];
                row.wordText.gameObject.SetActive(true);
            }

            if (row.probabilityText != null)
            {
                row.probabilityText.text = $"{probability.ToString("0.####", CultureInfo.InvariantCulture)}%";
                row.probabilityText.gameObject.SetActive(true);
            }

            if (row.probabilitySlider != null)
            {
                row.probabilitySlider.minValue = 0f;
                row.probabilitySlider.maxValue = 1f;
                row.probabilitySlider.value = probability / 100f;
                row.probabilitySlider.gameObject.SetActive(true);
            }

            if (row.probabilityBar != null)
            {
                row.probabilityBar.fillAmount = probability / 100f;
                row.probabilityBar.gameObject.SetActive(true);
            }
        }

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    private void ClearRows()
    {
        if (predictionRows == null)
            return;

        foreach (PredictionRow row in predictionRows)
        {
            if (row == null)
                continue;

            if (row.wordText != null)
            {
                row.wordText.text = string.Empty;
                row.wordText.gameObject.SetActive(false);
            }

            if (row.probabilityText != null)
            {
                row.probabilityText.text = string.Empty;
                row.probabilityText.gameObject.SetActive(false);
            }

            if (row.probabilitySlider != null)
            {
                row.probabilitySlider.value = 0f;
                row.probabilitySlider.gameObject.SetActive(false);
            }

            if (row.probabilityBar != null)
            {
                row.probabilityBar.fillAmount = 0f;
                row.probabilityBar.gameObject.SetActive(false);
            }
        }
    }
}

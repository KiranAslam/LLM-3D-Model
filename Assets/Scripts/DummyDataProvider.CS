using UnityEngine;

// Ye script demo/dummy data ka central source hai — training complete hone tak
// har generator script isi se apni "realistic looking" random values leta hai,
// taake ek hi sentence dobara select karne par HAMESHA same values dikhein
// (token IDs, matrix numbers, attention values, waghera — sab consistent).
//
// Jab real ONNX model ready ho jaye, is script ki jagah ek InferenceBridge le
// lega jo yehi shape ka data real model se laayega — baaki generator scripts
// mein kuch badalne ki zaroorat nahi padegi.
public class DummyDataProvider : MonoBehaviour
{
    public static DummyDataProvider Instance { get; private set; }

    [System.Serializable]
    public class SentenceEntry
    {
        public string sentence;           // 5 words
        public string[] predictedWords;   // top-5, sabse zyada probability pehle
        public float[] predictedProbabilities; // isi order mein, percentages (jaise 33.1)
    }

    [Header("8 demo sentences — Input Sentences panel isi list se populate hoga")]
    public SentenceEntry[] sentences = new SentenceEntry[]
    {
        new SentenceEntry { sentence = "the cat sat on", predictedWords = new[] { "mat", "floor", "bird", "fox", "slept" }, predictedProbabilities = new[] { 99.993f, 0.0028f, 0.0008f, 0.0007f, 0.0005f } },
        new SentenceEntry { sentence = "a dog ran near", predictedWords = new[] { "tree", "rock", "ran", "walked", "stood" }, predictedProbabilities = new[] { 99.994f, 0.0032f, 0.0012f, 0.0005f, 0.0003f } },
        new SentenceEntry { sentence = "bird jumped under grass", predictedWords = new[] { "fox", "grass", "under", "slept", "mat" }, predictedProbabilities = new[] { 99.999f, 0.0004f, 0.0002f, 0.0002f, 0.0001f } },
        new SentenceEntry { sentence = "rabbit slept beside bed", predictedWords = new[] { "bear", "bed", "ran", "slept", "bird" }, predictedProbabilities = new[] { 99.998f, 0.0011f, 0.0003f, 0.0003f, 0.0002f } },
        new SentenceEntry { sentence = "wolf walked on floor", predictedWords = new[] { "mouse", "slept", "dog", "bed", "waited" }, predictedProbabilities = new[] { 99.999f, 0.0003f, 0.0002f, 0.0001f, 0.0001f } },
        new SentenceEntry { sentence = "mouse waited near rock", predictedWords = new[] { "cat", "walked", "fox", "played", "jumped" }, predictedProbabilities = new[] { 99.999f, 0.0002f, 0.0002f, 0.0001f, 0.0001f } },
        new SentenceEntry { sentence = "bear stood under log", predictedWords = new[] { "dog", "fox", "on", "bird", "tree" }, predictedProbabilities = new[] { 99.997f, 0.0007f, 0.0005f, 0.0004f, 0.0003f } },
        new SentenceEntry { sentence = "fox played beside box", predictedWords = new[] { "bird", "dog", "mat", "cat", "under" }, predictedProbabilities = new[] { 99.996f, 0.0008f, 0.0006f, 0.0005f, 0.0004f } },
    };

    void Awake()
    {
        Instance = this;
    }

    // string.GetHashCode() Unity/.NET mein run-to-run same guarantee nahi karta,
    // isliye apna simple stable hash function — same string se hamesha same number.
    public static int StableHash(string text)
    {
        unchecked
        {
            int hash = 17;
            foreach (char c in text)
                hash = hash * 31 + c;
            return hash;
        }
    }

    // stageOffset se har pipeline stage (Tokenization, Embedding, Attention L1, etc.)
    // ka apna alag-but-still-consistent noise milta hai, taake sab stages same na
    // lagein, phir bhi ek hi sentence dobara select karne par sab kuch same rahe.
    public static System.Random GetSeededRandom(string sentence, int stageOffset = 0)
    {
        int seed = StableHash(sentence) + stageOffset;
        return new System.Random(seed);
    }

    // 5 words ke liye "realistic looking" token IDs — GPT-style bade vocab range
    // (0-50000) se, har word ka apna consistent ID (khud us word ke hash se).
    public static int[] GenerateTokenIds(string sentence, int vocabRange = 50257)
    {
        string[] words = sentence.Split(' ');
        int[] ids = new int[words.Length];
        for (int i = 0; i < words.Length; i++)
            ids[i] = Mathf.Abs(StableHash(words[i].ToLower())) % vocabRange;
        return ids;
    }

    // Kisi bhi matrix-jaisi cheez (embedding cells, attention weights, logits) ke
    // liye ek 1D array of random-but-seeded floats — same rng se call karo to
    // consistent values milengi.
    public static float[] GenerateValues(System.Random rng, int count, float min, float max)
    {
        float[] values = new float[count];
        for (int i = 0; i < count; i++)
            values[i] = (float)(rng.NextDouble() * (max - min) + min);
        return values;
    }

    // Currently selected sentence ke liye poora predicted-word entry dhoondo.
    public SentenceEntry GetEntry(string sentence)
    {
        foreach (var entry in sentences)
            if (entry.sentence == sentence)
                return entry;
        return null;
    }
}
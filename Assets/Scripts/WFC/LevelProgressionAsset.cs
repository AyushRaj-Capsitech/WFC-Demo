// =====================================================
// LevelProgressionAsset.cs
//
// ScriptableObject that defines how difficulty changes
// across all 30 levels, split into 4 zones:
//
//   Zone 1 — Tutorial     (levels 1–5)
//   Zone 2 — Linear Ramp  (levels 6–20)
//   Zone 3 — Chaos        (levels 21–25)
//   Zone 4 — Brutal       (levels 26–30)
//
// HOW TO CREATE:
//   Right-click in Project window
//   → Create → Level Design → Level Progression Asset
//
// HOW TO USE:
//   Assign to LevelManager in the Inspector.
//   Call LevelManager.GetDifficultyForLevel(levelNumber)
//   to get a 0..1 difficulty value for any level.
// =====================================================

using UnityEngine;

[CreateAssetMenu(
    fileName = "LevelProgressionAsset",
    menuName  = "Level Design/Level Progression Asset"
)]
public class LevelProgressionAsset : ScriptableObject
{
    // =====================================================
    // ZONE BOUNDARIES
    // Changing these lets you shift where zones begin/end
    // =====================================================
    [Header("--- ZONE BOUNDARIES ---")]
    [Tooltip("Last level of the Tutorial zone (inclusive)")]
    [Range(1, 10)]
    public int tutorialEnd   = 5;

    [Tooltip("Last level of the Linear Ramp zone (inclusive)")]
    [Range(6, 25)]
    public int rampEnd       = 20;

    [Tooltip("Last level of the Chaos zone (inclusive)")]
    [Range(21, 28)]
    public int chaosEnd      = 25;

    // Zone 4 runs from chaosEnd+1 to totalLevels (30)

    [Tooltip("Total number of levels in the game")]
    [Range(10, 50)]
    public int totalLevels   = 30;

    // =====================================================
    // ZONE 1 — TUTORIAL (levels 1 → tutorialEnd)
    // Player learns mechanics. Difficulty is very low
    // and rises gently to tutorialMaxDifficulty by the
    // end of this zone.
    // =====================================================
    [Header("--- ZONE 1: TUTORIAL ---")]
    [Tooltip("Difficulty at level 1 (first level, very easy)")]
    [Range(0f, 0.2f)]
    public float tutorialStartDifficulty = 0f;

    [Tooltip("Difficulty ceiling at the end of the tutorial zone")]
    [Range(0f, 0.4f)]
    public float tutorialMaxDifficulty   = 0.15f;

    // =====================================================
    // ZONE 2 — LINEAR RAMP (levels tutorialEnd+1 → rampEnd)
    // Steady increase from easy to hard.
    // Use the AnimationCurve to shape how fast it climbs.
    // =====================================================
    [Header("--- ZONE 2: LINEAR RAMP ---")]
    [Tooltip("Difficulty at the start of the ramp (should match tutorial ceiling)")]
    [Range(0f, 0.5f)]
    public float rampStartDifficulty     = 0.15f;

    [Tooltip("Difficulty at the end of the ramp (just before chaos zone)")]
    [Range(0.4f, 1f)]
    public float rampEndDifficulty       = 0.75f;

    [Tooltip("Shape of the ramp.\n" +
             "X = 0..1 (progress through zone)\n" +
             "Y = 0..1 (output multiplier on the ramp range)\n\n" +
             "Linear = steady climb\n" +
             "Ease In = slow start, fast finish\n" +
             "Ease Out = fast start, plateau\n" +
             "S-Curve = gentle → steep → gentle")]
    public AnimationCurve rampCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // =====================================================
    // ZONE 3 — CHAOS (levels rampEnd+1 → chaosEnd)
    // Random difficulty oscillation around a base value.
    // Players cannot rely on steady progression here —
    // some levels feel easier, some much harder.
    // =====================================================
    [Header("--- ZONE 3: CHAOS ---")]
    [Tooltip("Base (average) difficulty in the chaos zone")]
    [Range(0.4f, 1f)]
    public float chaosBaseDifficulty     = 0.75f;

    [Tooltip("Max random swing above/below the base.\n" +
             "e.g. base=0.75, amplitude=0.25 → range 0.5..1.0")]
    [Range(0f, 0.5f)]
    public float chaosAmplitude          = 0.25f;

    [Tooltip("Seed for the chaos randomisation. Change this to get a " +
             "different pattern without touching other settings.")]
    public int chaosSeed                 = 42;

    [Tooltip("If true, chaos values are randomised freshly each play. " +
             "If false, same pattern every run (uses chaosSeed).")]
    public bool chaosRandomEachRun       = false;

    // =====================================================
    // ZONE 4 — BRUTAL (levels chaosEnd+1 → totalLevels)
    // Consistently very hard. Difficulty starts at
    // brutalFloor and climbs to 1.0 by the final level.
    // =====================================================
    [Header("--- ZONE 4: BRUTAL ---")]
    [Tooltip("Minimum difficulty floor at the start of the brutal zone")]
    [Range(0.6f, 1f)]
    public float brutalFloor             = 0.85f;

    [Tooltip("Shape of the brutal ramp toward 1.0.\n" +
             "A fast ease-in keeps most levels near the floor and " +
             "spikes only the final level.")]
    public AnimationCurve brutalCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    // =====================================================
    // PUBLIC API
    // Call this to get the 0..1 difficulty for any level.
    // =====================================================

    // Cached chaos values — generated once per run if chaosRandomEachRun=true
    private float[] _chaosCache;

    public float GetDifficultyForLevel(int levelNumber)
    {
        levelNumber = Mathf.Clamp(levelNumber, 1, totalLevels);

        // ---- Zone 1: Tutorial ----
        if (levelNumber <= tutorialEnd)
        {
            float t = tutorialEnd > 1
                ? (float)(levelNumber - 1) / (tutorialEnd - 1)
                : 0f;
            return Mathf.Lerp(tutorialStartDifficulty, tutorialMaxDifficulty, t);
        }

        // ---- Zone 2: Linear Ramp ----
        if (levelNumber <= rampEnd)
        {
            int   zoneLen = rampEnd - tutorialEnd;
            float t       = zoneLen > 0
                ? (float)(levelNumber - tutorialEnd - 1) / (zoneLen - 1)
                : 0f;

            float curveT = rampCurve.Evaluate(t);
            return Mathf.Lerp(rampStartDifficulty, rampEndDifficulty, curveT);
        }

        // ---- Zone 3: Chaos ----
        if (levelNumber <= chaosEnd)
        {
            BuildChaosCache();

            int localIndex = levelNumber - rampEnd - 1;
            int zoneLen    = chaosEnd - rampEnd;

            localIndex = Mathf.Clamp(localIndex, 0, zoneLen - 1);
            float noise = _chaosCache[localIndex]; // -1..+1

            float raw = chaosBaseDifficulty + noise * chaosAmplitude;
            return Mathf.Clamp01(raw);
        }

        // ---- Zone 4: Brutal ----
        {
            int   zoneLen = totalLevels - chaosEnd;
            float t       = zoneLen > 1
                ? (float)(levelNumber - chaosEnd - 1) / (zoneLen - 1)
                : 1f;

            float curveT = brutalCurve.Evaluate(t);
            return Mathf.Lerp(brutalFloor, 1f, curveT);
        }
    }

    // =====================================================
    // CHAOS CACHE BUILDER
    // Generates a set of noise values in -1..+1 range,
    // one per level in the chaos zone.
    // =====================================================
    private void BuildChaosCache()
    {
        int zoneLen = chaosEnd - rampEnd;
        if (_chaosCache != null && _chaosCache.Length == zoneLen && !chaosRandomEachRun)
            return;

        _chaosCache = new float[zoneLen];

        Random.State oldState = Random.state;

        if (!chaosRandomEachRun)
            Random.InitState(chaosSeed);

        // Generate values that swing up and down — not just random noise
        // Uses a pattern: alternating high/low with variation
        for (int i = 0; i < zoneLen; i++)
        {
            // Sinusoidal base so it feels like real oscillation,
            // plus a random offset for unpredictability
            float sineBase = Mathf.Sin(i * 1.9f + chaosSeed * 0.1f);
            float randOffset = Random.Range(-0.4f, 0.4f);
            _chaosCache[i] = Mathf.Clamp(sineBase * 0.6f + randOffset, -1f, 1f);
        }

        Random.state = oldState;
    }

    // =====================================================
    // EDITOR HELPER — preview all 30 levels in console
    // =====================================================
    [ContextMenu("Preview All Level Difficulties")]
    void PreviewAll()
    {
        _chaosCache = null; // force rebuild
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Level Difficulty Preview ===");
        for (int i = 1; i <= totalLevels; i++)
        {
            float d    = GetDifficultyForLevel(i);
            string bar = new string('|', Mathf.RoundToInt(d * 20));
            string zone = i <= tutorialEnd ? "Tutorial" :
                          i <= rampEnd     ? "Ramp    " :
                          i <= chaosEnd    ? "Chaos   " : "Brutal  ";
            sb.AppendLine($"Level {i,2} [{zone}]  {d:F2}  {bar}");
        }
        Debug.Log(sb.ToString());
    }
    [Header("--- LEVEL GENERATOR PARAMETERS ---")]
[Tooltip("Level length at minimum difficulty (easy levels)")]
[Range(3, 10)]
public int minLevelLength = 5;

[Tooltip("Level length at maximum difficulty (hard levels)")]
[Range(5, 20)]
public int maxLevelLength = 12;

[Tooltip("Max consecutive straights at minimum difficulty")]
[Range(2, 8)]
public int easyMaxStraights = 5;

[Tooltip("Max consecutive straights at maximum difficulty")]
[Range(1, 5)]
public int hardMaxStraights = 2;

[Tooltip("Max consecutive curves at minimum difficulty")]
[Range(1, 4)]
public int easyMaxCurves = 1;

[Tooltip("Max consecutive curves at maximum difficulty")]
[Range(2, 6)]
public int hardMaxCurves = 4;

[Tooltip("Difficulty influence at minimum difficulty")]
[Range(0f, 1f)]
public float easyDifficultyInfluence = 0.2f;

[Tooltip("Difficulty influence at maximum difficulty")]
[Range(0f, 1f)]
public float hardDifficultyInfluence = 0.95f;
}
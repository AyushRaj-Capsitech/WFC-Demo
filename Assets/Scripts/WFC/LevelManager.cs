// =====================================================
// LevelManager.cs
//
// Sits between LevelProgressionAsset and LevelGenerator.
// Tracks which level the player is on and feeds the
// correct difficulty value to LevelGenerator.
//
// SETUP:
//   1. Attach this to a GameObject in your scene.
//   2. Assign your LevelProgressionAsset in the Inspector.
//   3. Assign your LevelGenerator in the Inspector.
//   4. Call LevelManager.Instance.LoadLevel(n) to go to
//      any level, or NextLevel() to advance.
// =====================================================

using UnityEngine;

public class LevelManager : MonoBehaviour
{
    // =====================================================
    // SINGLETON
    // =====================================================
    public static LevelManager Instance { get; private set; }

    // =====================================================
    // INSPECTOR
    // =====================================================
    [Header("--- REFERENCES ---")]
    [Tooltip("The ScriptableObject asset that defines the 30-level curve")]
    public LevelProgressionAsset progressionAsset;

    [Tooltip("The LevelGenerator that will generate the level geometry")]
    public LevelGenerator levelGenerator;

    [Header("--- STATE ---")]
    [Tooltip("Starting level when the game boots")]
    [Range(1, 30)]
    public int startLevel = 1;

    [Tooltip("Read-only: shows current level number in play mode")]
    [SerializeField]
    private int currentLevel = 1;

    [Tooltip("Read-only: shows the computed difficulty for the current level")]
    [SerializeField, Range(0f, 1f)]
    private float currentDifficulty = 0f;

    // =====================================================
    // AWAKE / START
    // =====================================================
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (progressionAsset == null)
        {
            Debug.LogError("❌ LevelManager: No LevelProgressionAsset assigned!");
            return;
        }
        if (levelGenerator == null)
        {
            Debug.LogError("❌ LevelManager: No LevelGenerator assigned!");
            return;
        }

        LoadLevel(startLevel);
    }

    // =====================================================
    // PUBLIC API
    // =====================================================

    /// <summary>
    /// Load a specific level by number (1-indexed).
    /// Applies difficulty from the progression asset and
    /// triggers LevelGenerator to regenerate the level.
    /// </summary>
    public void LoadLevel(int levelNumber)
    {
        currentLevel = Mathf.Clamp(levelNumber, 1, progressionAsset.totalLevels);

        currentDifficulty = progressionAsset.GetDifficultyForLevel(currentLevel);

        ApplyDifficultyToGenerator(currentDifficulty);

        levelGenerator.GenerateLevel();

        Debug.Log($"📍 Level {currentLevel} | Zone: {GetZoneName(currentLevel)} " +
                  $"| Difficulty: {currentDifficulty:F2}");
    }

    /// <summary>
    /// Advance to the next level. Loops back to level 1 after the last.
    /// </summary>
    public void NextLevel()
    {
        int next = currentLevel >= progressionAsset.totalLevels ? 1 : currentLevel + 1;
        LoadLevel(next);
    }

    /// <summary>
    /// Reload the current level (e.g. after player death).
    /// </summary>
    public void ReloadCurrentLevel()
    {
        LoadLevel(currentLevel);
    }

    /// <summary>
    /// Returns the 0..1 difficulty value for the currently active level.
    /// </summary>
    public float GetCurrentDifficulty() => currentDifficulty;

    /// <summary>
    /// Returns the current level number.
    /// </summary>
    public int GetCurrentLevel() => currentLevel;

    // =====================================================
    // APPLY DIFFICULTY TO GENERATOR
    //
    // This is where the difficulty value drives actual
    // generator settings. Expand this method to control
    // speed, obstacle density, gap width, etc.
    // =====================================================
    void ApplyDifficultyToGenerator(float difficulty)
    {
        levelGenerator.levelLength = Mathf.RoundToInt(
    Mathf.Lerp(progressionAsset.minLevelLength, progressionAsset.maxLevelLength, difficulty));

levelGenerator.maxConsecutiveStraights = Mathf.RoundToInt(
    Mathf.Lerp(progressionAsset.easyMaxStraights, progressionAsset.hardMaxStraights, difficulty));

levelGenerator.maxConsecutiveCurves = Mathf.RoundToInt(
    Mathf.Lerp(progressionAsset.easyMaxCurves, progressionAsset.hardMaxCurves, difficulty));

levelGenerator.difficultyInfluence = Mathf.Lerp(
    progressionAsset.easyDifficultyInfluence, progressionAsset.hardDifficultyInfluence, difficulty);

levelGenerator.baseDifficulty = difficulty;
    }

    // =====================================================
    // HELPER
    // =====================================================
    string GetZoneName(int level)
    {
        if (level <= progressionAsset.tutorialEnd) return "Tutorial";
        if (level <= progressionAsset.rampEnd)     return "Ramp";
        if (level <= progressionAsset.chaosEnd)    return "Chaos";
        return "Brutal";
    }

    // =====================================================
    // EDITOR HELPER — test any level from Inspector
    // =====================================================
    [Header("--- EDITOR TEST ---")]
    [Tooltip("Set a level number and click 'Test Jump To Level' in context menu")]
    public int debugJumpToLevel = 1;

    [ContextMenu("Test Jump To Level")]
    void DebugJump()
    {
        LoadLevel(debugJumpToLevel);
    }
}

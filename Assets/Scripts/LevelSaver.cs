// LevelSaver.cs
// Saves the currently generated level as a prefab
// Attach this to the same GameObject as LevelGenerator

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelSaver : MonoBehaviour
{
    [Header("--- SAVE SETTINGS ---")]
    [Tooltip("Folder path inside Assets/ to save level prefabs")]
    public string saveFolderPath = "Assets/SavedLevels";

    [Tooltip("Base name for saved levels")]
    public string levelBaseName = "Level";

    [Header("--- AUTO NUMBERING ---")]
    [Tooltip("Current level number — auto increments after each save")]
    public int currentLevelNumber = 1;

    // =====================================================
    // SAVE CURRENT LEVEL
    // Call this after GenerateLevel() has run
    // =====================================================
    public void SaveCurrentLevel()
    {
#if UNITY_EDITOR
        // STEP 1 — Find all spawned platforms in scene
        // They are children of or named "Platform_" 
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        // Collect all platform objects
        System.Collections.Generic.List<GameObject> platforms 
            = new System.Collections.Generic.List<GameObject>();

        foreach (var obj in allObjects)
        {
            if (obj.name.StartsWith("Platform_"))
                platforms.Add(obj);
        }

        if (platforms.Count == 0)
        {
            Debug.LogError("❌ No platforms found in scene! " +
                           "Make sure GenerateLevel() has been called first.");
            return;
        }

        // STEP 2 — Create a parent GameObject to hold all platforms
        string levelName = levelBaseName + "_" + currentLevelNumber.ToString("D3");
        GameObject levelRoot = new GameObject(levelName);
        levelRoot.transform.position = Vector3.zero;
        levelRoot.transform.rotation = Quaternion.identity;

        // STEP 3 — Parent all platforms under levelRoot
        foreach (var platform in platforms)
        {
            platform.transform.SetParent(levelRoot.transform, true);
        }

        // STEP 4 — Make sure save folder exists
        if (!AssetDatabase.IsValidFolder(saveFolderPath))
        {
            // Create folder if it doesn't exist
            string parentFolder = System.IO.Path.GetDirectoryName(saveFolderPath);
            string newFolder    = System.IO.Path.GetFileName(saveFolderPath);
            AssetDatabase.CreateFolder(parentFolder, newFolder);
            Debug.Log("📁 Created folder: " + saveFolderPath);
        }

        // STEP 5 — Save as prefab
        string prefabPath = saveFolderPath + "/" + levelName + ".prefab";

        // Check if prefab already exists at this path
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            Debug.LogWarning("⚠️ Prefab already exists at: " + prefabPath 
                           + " — incrementing number and saving again");
            currentLevelNumber++;
            levelName  = levelBaseName + "_" + currentLevelNumber.ToString("D3");
            prefabPath = saveFolderPath + "/" + levelName + ".prefab";
        }

        bool success;
        PrefabUtility.SaveAsPrefabAssetAndConnect(
            levelRoot,
            prefabPath,
            InteractionMode.UserAction,
            out success
        );

        if (success)
        {
            Debug.Log("✅ Level saved as prefab: " + prefabPath);
            currentLevelNumber++; // Increment for next save
        }
        else
        {
            Debug.LogError("❌ Failed to save prefab at: " + prefabPath);
            // Unparent platforms so level still works
            foreach (var platform in platforms)
                platform.transform.SetParent(null, true);
            Destroy(levelRoot);
        }
#else
        Debug.LogWarning("⚠️ LevelSaver only works in Unity Editor.");
#endif
    }

    // =====================================================
    // SAVE AND GENERATE NEW
    // Saves current level then generates a fresh one
    // =====================================================
    public void SaveAndGenerateNew()
    {
        SaveCurrentLevel();

        LevelGenerator generator = GetComponent<LevelGenerator>();
        if (generator != null)
        {
            generator.GenerateLevel();
            Debug.Log("🔄 New level generated after save");
        }
        else
        {
            Debug.LogError("❌ LevelGenerator not found on this GameObject");
        }
    }
}
// LevelSaverEditor.cs
// Adds Save buttons to Inspector
// Must be inside an Editor/ folder

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelSaver))]
public class LevelSaverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default inspector fields
        DrawDefaultInspector();

        LevelSaver saver = (LevelSaver)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("--- ACTIONS ---", EditorStyles.boldLabel);

        // Save current level button
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("💾 Save Current Level As Prefab", GUILayout.Height(40)))
        {
            saver.SaveCurrentLevel();
        }

        EditorGUILayout.Space(5);

        // Save and generate new button
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("💾 Save + Generate New Level", GUILayout.Height(40)))
        {
            saver.SaveAndGenerateNew();
        }

        EditorGUILayout.Space(5);

        // Just generate new without saving
        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("🔄 Generate New Level (no save)", GUILayout.Height(40)))
        {
            LevelGenerator generator = saver.GetComponent<LevelGenerator>();
            if (generator != null)
                generator.GenerateLevel();
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif
// ```

// ---

// ## Setup Steps

// ### Step 1 — Add LevelSaver to scene
// 1. Click your `LevelGenerator` GameObject in Hierarchy
// 2. In Inspector → **Add Component** → search `LevelSaver`
// 3. Click it to add

// ### Step 2 — Set save folder
// In Inspector set:
// ```
// Save Folder Path  → Assets/SavedLevels
// Level Base Name   → Level
// Current Level Number → 1
// ```

// ### Step 3 — Create Editor folder
// 1. In Project window → `Assets/Scripts/WFC/`
// 2. Right-click → `Create → Folder` → name it `Editor`
// 3. Put `LevelSaverEditor.cs` inside this folder

// ---

// ## How To Use
// ```
// WORKFLOW:

// 1. Press Play in Unity
// 2. Level generates automatically
// 3. You like this level? 
//    → Click "💾 Save Current Level As Prefab"
//    → Saved to Assets/SavedLevels/Level_001.prefab

// 4. Want a different level?
//    → Click "🔄 Generate New Level"
//    → New level appears

// 5. Like this one too?
//    → Click "💾 Save Current Level As Prefab"  
//    → Saved as Level_002.prefab

// 6. Want to save AND immediately get new one?
//    → Click "💾 Save + Generate New Level"
//    → Saves current, generates next automatically
// ```

// ---

// ## What Gets Saved
// ```
// Assets/
// └── SavedLevels/
//     ├── Level_001.prefab  ← complete level with all platforms
//     ├── Level_002.prefab  ← another generated level
//     ├── Level_003.prefab
//     └── ...
// ```

// Each prefab contains:
// ```
// Level_001
// ├── Platform_Line
// ├── Platform_Line
// ├── Platform_Round_R01
// ├── Platform_V_Shape1
// └── ... (all platforms as children)
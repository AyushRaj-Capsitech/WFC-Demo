// LevelGenerator.cs
// Main WFC engine — attach this to an empty GameObject in scene

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Level Settings")]
    [Range(5, 10)]
    public int levelLength = 7;
    public Vector3 startPosition = Vector3.zero;

    [Header("Platform Settings")]
    public float platformSize = 5f;     // Match your model's actual size

    [Header("All Tiles")]
    public List<PlatformTile> allTiles; // Drag all ScriptableObjects here

    [Header("Variety Control")]
    public int maxConsecutiveTurns = 2;
    public int maxConsecutiveStraights = 3;

    // Runtime data
    private SlotData[] slots;
    private List<GameObject> spawnedObjects = new();

    // -------------------------------------------------------
    // Inner class to hold per-slot data
    // -------------------------------------------------------
    private class SlotData
    {
        public List<PlatformTile> possibleTiles;
        public PlatformTile collapsed;
        public Vector3 worldPosition;
        public FacingDirection approachFacing;
        public bool IsCollapsed => collapsed != null;
        public int Entropy => possibleTiles.Count;

        public SlotData(List<PlatformTile> tiles)
        {
            possibleTiles = new List<PlatformTile>(tiles);
        }
    }

    // -------------------------------------------------------
    void Start()
    {
        GenerateLevel();
    }

    // -------------------------------------------------------
    // Call this to regenerate (e.g. on new level load)
    // -------------------------------------------------------
    public void GenerateLevel()
    {
        // Clear previous level
        foreach (var obj in spawnedObjects)
            if (obj != null) Destroy(obj);
        spawnedObjects.Clear();

        // Try up to 20 times (WFC can fail, just retry)
        for (int attempt = 0; attempt < 20; attempt++)
        {
            if (TryGenerate())
            {
                Debug.Log($"Level generated in {attempt + 1} attempt(s)");
                return;
            }
        }

        Debug.LogError("WFC failed after 20 attempts! Check your tile sockets.");
    }

    // -------------------------------------------------------
    bool TryGenerate()
    {
        // STEP A: Initialize all slots
        slots = new SlotData[levelLength];
        for (int i = 0; i < levelLength; i++)
            slots[i] = new SlotData(allTiles);

        // STEP B: Force first slot
        // Entry must be South (player approaches from South)
        slots[0].possibleTiles = slots[0].possibleTiles
            .Where(t => t.entrySocket == SocketType.South).ToList();
        slots[0].worldPosition = startPosition;
        slots[0].approachFacing = FacingDirection.North;

        // STEP C: Force last slot
        // Exit must be North (leads to level end)
        slots[levelLength - 1].possibleTiles = slots[levelLength - 1].possibleTiles
            .Where(t => t.exitSocket == SocketType.North).ToList();

        int consecutiveTurns = 0;
        int consecutiveStraights = 0;

        // STEP D: Collapse each slot left to right
        for (int i = 0; i < levelLength; i++)
        {
            // Apply variety rules
            EnforceVariety(slots[i], consecutiveTurns, consecutiveStraights);

            // Dead end check
            if (slots[i].possibleTiles.Count == 0)
            {
                Debug.LogWarning($"Slot {i} has 0 options — retrying...");
                return false;
            }

            // Collapse: pick one tile via weighted random
            PlatformTile chosen = WeightedRandom(slots[i].possibleTiles);
            slots[i].collapsed = chosen;

            // Update consecutive counters
            bool isTurn = chosen.entrySocket != chosen.exitSocket;
            if (isTurn) { consecutiveTurns++;    consecutiveStraights = 0; }
            else        { consecutiveStraights++; consecutiveTurns = 0;    }

            // Propagate constraints to next slot
            if (i + 1 < levelLength)
            {
                if (!Propagate(slots[i], slots[i + 1]))
                {
                    Debug.LogWarning($"Propagation failed at slot {i} — retrying...");
                    return false;
                }
            }
        }

        // STEP E: Spawn all platforms
        SpawnAll();
        return true;
    }

    // -------------------------------------------------------
    void EnforceVariety(SlotData slot, int turns, int straights)
    {
        // Too many turns → force a straight
        if (turns >= maxConsecutiveTurns)
        {
            var onlyStraights = slot.possibleTiles
                .Where(t => t.entrySocket == t.exitSocket).ToList();
            if (onlyStraights.Count > 0)
                slot.possibleTiles = onlyStraights;
        }

        // Too many straights → force a turn
        if (straights >= maxConsecutiveStraights)
        {
            var onlyTurns = slot.possibleTiles
                .Where(t => t.entrySocket != t.exitSocket).ToList();
            if (onlyTurns.Count > 0)
                slot.possibleTiles = onlyTurns;
        }
    }

    // -------------------------------------------------------
    bool Propagate(SlotData current, SlotData next)
    {
        // Next slot entry must match current slot exit
        SocketType required = current.collapsed.exitSocket;

        next.possibleTiles = next.possibleTiles
            .Where(t => t.entrySocket == required).ToList();

        // Calculate next platform's world position
        next.approachFacing = current.collapsed.exitFacing;
        next.worldPosition  = NextPosition(
            current.worldPosition, 
            current.collapsed.exitFacing
        );

        return next.possibleTiles.Count > 0;
    }

    // -------------------------------------------------------
    Vector3 NextPosition(Vector3 from, FacingDirection facing)
    {
        return facing switch
        {
            FacingDirection.North => from + new Vector3(0, 0,  platformSize),
            FacingDirection.South => from + new Vector3(0, 0, -platformSize),
            FacingDirection.East  => from + new Vector3( platformSize, 0, 0),
            FacingDirection.West  => from + new Vector3(-platformSize, 0, 0),
            _ => from
        };
    }

    // -------------------------------------------------------
    void SpawnAll()
    {
        foreach (var slot in slots)
        {
            Quaternion rotation = Quaternion.Euler(0, slot.collapsed.yRotation, 0);
            GameObject go = Instantiate(
                slot.collapsed.prefab,
                slot.worldPosition,
                rotation
            );
            go.name = slot.collapsed.tileName;
            spawnedObjects.Add(go);
        }
    }

    // -------------------------------------------------------
    PlatformTile WeightedRandom(List<PlatformTile> tiles)
    {
        float total = tiles.Sum(t => t.weight);
        float roll  = Random.Range(0f, total);
        float cumulative = 0f;

        foreach (var tile in tiles)
        {
            cumulative += tile.weight;
            if (roll <= cumulative) return tile;
        }

        return tiles[^1];
    }

    // -------------------------------------------------------
    // Visualize path in Scene view (Editor only)
    // -------------------------------------------------------
    void OnDrawGizmos()
    {
        if (slots == null) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < slots.Length - 1; i++)
        {
            if (slots[i] == null || slots[i + 1] == null) continue;
            Gizmos.DrawLine(
                slots[i].worldPosition + Vector3.up,
                slots[i + 1].worldPosition + Vector3.up
            );
            Gizmos.DrawSphere(slots[i].worldPosition + Vector3.up, 0.3f);
        }
    }
}
// ```

// ---

// ## STEP 4 — Create ScriptableObject Tiles

// Go to `Assets/ScriptableObjects/Tiles/`

// Right-click → `Create → WFC → Platform Tile`

// Create **7 assets** with these exact values:
// ```
// Asset Name    | tileName  | Prefab  | Entry | Exit  | ExitFacing | yRot | Weight
// --------------|-----------|---------|-------|-------|------------|------|-------
// Tile_Line     | Line      | line    | South | North | North      | 0    | 2.0
// Tile_V01      | V_Shape1  | V_01    | South | North | North      | 0    | 1.5
// Tile_V02      | V_Shape2  | V_02    | South | North | North      | 0    | 1.5
// Tile_R01      | Round_Q1  | R01     | South | East  | East       | 0    | 1.0
// Tile_R02      | Round_Q2  | R02     | West  | South | South      | 90   | 1.0
// Tile_R03      | Round_Q3  | R03     | North | West  | West       | 180  | 1.0
// Tile_R04      | Round_Q4  | R04     | East  | North | North      | 270  | 1.0
// ```

// ---

// ## STEP 5 — Set Up The Scene

// 1. Create an **empty GameObject** → name it `LevelGenerator`
// 2. Drag `LevelGenerator.cs` script onto it
// 3. In the Inspector set:
//    - `levelLength` = 7
//    - `platformSize` = measure your `line` prefab's Z length (e.g. 5)
//    - `startPosition` = (0, 0, 0)
// 4. Drag all **7 ScriptableObject tiles** into the `allTiles` list

// ---

// ## STEP 6 — Measure Your platformSize

// This is **critical** — your platforms must snap together perfectly:

// 1. Click your `line` prefab in scene
// 2. In Inspector check **Transform Scale** and **Mesh bounds**
// 3. Or in Scene view use the ruler: the Z length = your `platformSize`
// ```
// Example: if line is 4 units long → set platformSize = 4
// ```

// ---

// ## STEP 7 — Press Play & Test

// Hit **Play** — you should see platforms spawn in a connected chain.

// **If platforms don't connect visually** (gap or overlap):
// > Adjust `platformSize` value until they snap perfectly

// **If you see "WFC failed" in Console:**
// > Check that your ScriptableObject socket values match the table above exactly

// ---

// ## Quick Debug Checklist
// ```
// ✅ 7 prefabs created in Prefabs/Platforms/
// ✅ 3 scripts created in Scripts/WFC/
// ✅ 7 ScriptableObjects created with correct socket values
// ✅ LevelGenerator GameObject in scene
// ✅ All 7 tiles dragged into allTiles list
// ✅ platformSize matches your actual model size
// ✅ Press Play → platforms appear connected
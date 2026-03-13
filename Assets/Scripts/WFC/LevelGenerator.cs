// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;

// public class LevelGenerator : MonoBehaviour
// {
//     [Header("--- LEVEL SETTINGS ---")]
//     [Range(5, 10)]
//     public int levelLength = 7;

//     [Header("--- ALL TILES ---")]
//     public List<PlatformTile> allTiles;

//     [Header("--- VARIETY ---")]
//     public int maxConsecutiveCurves = 2;
//     public int maxConsecutiveStraights = 3;

//     [Header("--- DEBUG ---")]
//     public bool showGizmos = true;

//     private SlotData[] slots;
//     private List<GameObject> spawnedObjects = new List<GameObject>();

//     private class SlotData
//     {
//         public List<PlatformTile> possibleTiles;
//         public PlatformTile collapsed;
//         public Vector3 spawnPosition;
//         public Quaternion spawnRotation;
//         public Vector3 exitWorldPosition;
//         public Quaternion exitWorldRotation;

//         public bool IsCollapsed => collapsed != null;
//         public int Entropy => possibleTiles.Count;

//         public SlotData(List<PlatformTile> tiles)
//         {
//             possibleTiles = new List<PlatformTile>(tiles);
//             spawnPosition = Vector3.zero;
//             spawnRotation = Quaternion.identity;
//             exitWorldRotation = Quaternion.identity;
//         }
//     }

//     void Start()
//     {
//         GenerateLevel();
//     }

//     public void GenerateLevel()
//     {
//         foreach (var obj in spawnedObjects)
//             if (obj != null) Destroy(obj);

//         spawnedObjects.Clear();

//         for (int attempt = 1; attempt <= 20; attempt++)
//         {
//             if (TryGenerate())
//             {
//                 Debug.Log("Level generated on attempt " + attempt);
//                 return;
//             }

//             Debug.LogWarning("Attempt " + attempt + " failed");
//         }

//         Debug.LogError("WFC Failed");
//     }

//     bool TryGenerate()
//     {
//         slots = new SlotData[levelLength];

//         for (int i = 0; i < levelLength; i++)
//             slots[i] = new SlotData(allTiles);

//         slots[0].spawnPosition = Vector3.zero;
//         slots[0].spawnRotation = Quaternion.identity;

//         int consecutiveCurves = 0;
//         int consecutiveStraights = 0;

//         for (int i = 0; i < levelLength; i++)
//         {
//             EnforceVariety(slots[i], consecutiveCurves, consecutiveStraights);

//             if (slots[i].possibleTiles.Count == 0)
//             {
//                 Debug.LogWarning("Slot " + i + " has 0 tiles");
//                 return false;
//             }

//             PlatformTile chosen = WeightedRandom(slots[i].possibleTiles);
//             if (chosen == null) return false;

//             slots[i].collapsed = chosen;

//             if (!CalculateExitPlane(slots[i]))
//                 return false;

//             bool isCurve = chosen.tileName.StartsWith("Round");

//             if (isCurve)
//             {
//                 consecutiveCurves++;
//                 consecutiveStraights = 0;
//             }
//             else
//             {
//                 consecutiveStraights++;
//                 consecutiveCurves = 0;
//             }

//             if (i + 1 < levelLength)
//                 AlignNextSlot(slots[i], slots[i + 1]);
//         }

//         SpawnAll();
//         return true;
//     }

//     bool CalculateExitPlane(SlotData slot)
//     {
//         GameObject prefab = slot.collapsed.prefab;

//         Transform entryPlane = FindChildByName(prefab, slot.collapsed.entryPlaneName);
//         Transform exitPlane = FindChildByName(prefab, slot.collapsed.exitPlaneName);

//         if (entryPlane == null)
//         {
//             Debug.LogError("Missing " + slot.collapsed.entryPlaneName + " on: " + prefab.name);
//             return false;
//         }

//         if (exitPlane == null)
//         {
//             Debug.LogError("Missing " + slot.collapsed.exitPlaneName + " on: " + prefab.name);
//             return false;
//         }

//         // The input slot.spawnRotation and slot.spawnPosition are the Target World Transform for the Entry Plane.
//         // We need to find the Prefab's World Transform that makes the Entry Plane match this target.
        
//         // 1. Calculate Prefab World Rotation
//         // prefabRotation * entryPlane.localRotation = targetEntryRotation
//         // prefabRotation = targetEntryRotation * Inverse(entryPlane.localRotation)
//         Quaternion prefabRotation = slot.spawnRotation * Quaternion.Inverse(entryPlane.localRotation);
        
//         // 2. Calculate Prefab World Position
//         // prefabPosition + prefabRotation * entryPlane.localPosition = targetEntryPosition
//         // prefabPosition = targetEntryPosition - (prefabRotation * entryPlane.localPosition)
//         Vector3 prefabPosition = slot.spawnPosition - (prefabRotation * entryPlane.localPosition);

//         // 3. Update slot with the actual spawn transform for the prefab
//         slot.spawnPosition = prefabPosition;
//         slot.spawnRotation = prefabRotation;

//         // 4. Calculate the World Transform of the Exit Plane for the NEXT tile
//         slot.exitWorldPosition = prefabPosition + (prefabRotation * exitPlane.localPosition);
//         slot.exitWorldRotation = prefabRotation * exitPlane.localRotation;

//         return true;
//     }

//     void AlignNextSlot(SlotData current, SlotData next)
//     {
//         // Align next slot's entry target to this slot's exit point
//         next.spawnPosition = current.exitWorldPosition;
//         next.spawnRotation = current.exitWorldRotation;
//     }

//     Transform FindChildByName(GameObject prefab, string childName)
//     {
//         Transform[] all = prefab.GetComponentsInChildren<Transform>(true);

//         foreach (var t in all)
//             if (t.name == childName)
//                 return t;

//         return null;
//     }

//     void EnforceVariety(SlotData slot, int curves, int straights)
//     {
//         if (curves >= maxConsecutiveCurves)
//         {
//             var s = slot.possibleTiles
//                 .Where(t => !t.tileName.StartsWith("Round")).ToList();

//             if (s.Count > 0)
//                 slot.possibleTiles = s;
//         }

//         if (straights >= maxConsecutiveStraights)
//         {
//             var c = slot.possibleTiles
//                 .Where(t => t.tileName.StartsWith("Round")).ToList();

//             if (c.Count > 0)
//                 slot.possibleTiles = c;
//         }
//     }

//     void SpawnAll()
//     {
//         foreach (var slot in slots)
//         {
//             GameObject go = Instantiate(
//                 slot.collapsed.prefab,
//                 slot.spawnPosition,
//                 slot.spawnRotation
//             );

//             go.name = "Platform_" + slot.collapsed.tileName;

//             spawnedObjects.Add(go);
//         }
//     }

//     PlatformTile WeightedRandom(List<PlatformTile> tiles)
//     {
//         float total = tiles.Sum(t => t.weight);

//         float roll = Random.Range(0f, total);

//         float cumulative = 0f;

//         foreach (var tile in tiles)
//         {
//             cumulative += tile.weight;

//             if (roll <= cumulative)
//                 return tile;
//         }

//         return tiles[tiles.Count - 1];
//     }


// new code collison detection 
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("--- LEVEL SETTINGS ---")]
    [Range(5, 10)]
    public int levelLength = 7;

    [Header("--- ALL TILES ---")]
    public List<PlatformTile> allTiles;

    [Header("--- VARIETY ---")]
    public int maxConsecutiveCurves = 2;
    public int maxConsecutiveStraights = 3;

    [Header("--- COLLISION DETECTION ---")]
    [Tooltip("Size of the overlap check box — match your platform size")]
    public Vector3 platformBoundsSize = new Vector3(0.2f, 0.1f, 0.2f);
    [Tooltip("Layer mask for platforms — assign a Platform layer")]
    public LayerMask platformLayer;
    [Tooltip("How far apart platforms must be to not count as overlapping")]
    public float minDistanceBetweenPlatforms = 0.1f;

    [Header("--- DEBUG ---")]
    public bool showGizmos = true;

    private SlotData[] slots;
    private List<GameObject> spawnedObjects = new List<GameObject>();

    // Stores world positions of all placed platforms this attempt
    // Used for fast distance-based overlap check
    private List<Vector3> placedPositions = new List<Vector3>();

    // =====================================================
    // SLOT DATA
    // =====================================================
    private class SlotData
    {
        public List<PlatformTile> possibleTiles;
        public PlatformTile collapsed;
        public Vector3 spawnPosition;
        public Quaternion spawnRotation;
        public Vector3 exitWorldPosition;
        public Quaternion exitWorldRotation;

        public bool IsCollapsed => collapsed != null;
        public int Entropy => possibleTiles.Count;

        public SlotData(List<PlatformTile> tiles)
        {
            possibleTiles = new List<PlatformTile>(tiles);
            spawnPosition = Vector3.zero;
            spawnRotation = Quaternion.identity;
            exitWorldRotation = Quaternion.identity;
        }
    }

    // =====================================================
    // START
    // =====================================================
    void Start()
    {
        GenerateLevel();
    }

    // =====================================================
    // GENERATE LEVEL
    // =====================================================
    public void GenerateLevel()
    {
        foreach (var obj in spawnedObjects)
            if (obj != null) Destroy(obj);
        spawnedObjects.Clear();

        for (int attempt = 1; attempt <= 20; attempt++)
        {
            if (TryGenerate())
            {
                Debug.Log("✅ Level generated on attempt " + attempt);
                return;
            }
            Debug.LogWarning("⚠️ Attempt " + attempt + " failed, retrying...");
        }

        Debug.LogError("❌ WFC Failed after 20 attempts!");
    }

    // =====================================================
    // TRY GENERATE
    // =====================================================
    bool TryGenerate()
    {
        slots = new SlotData[levelLength];
        for (int i = 0; i < levelLength; i++)
            slots[i] = new SlotData(allTiles);

        slots[0].spawnPosition = Vector3.zero;
        slots[0].spawnRotation = Quaternion.identity;

        // Clear placed positions for this attempt
        placedPositions.Clear();

        int consecutiveCurves = 0;
        int consecutiveStraights = 0;

        for (int i = 0; i < levelLength; i++)
        {
            EnforceVariety(slots[i], consecutiveCurves, consecutiveStraights);

            if (slots[i].possibleTiles.Count == 0)
            {
                Debug.LogWarning("Slot " + i + " has 0 tiles");
                return false;
            }

            // Try tiles in weighted random order
            // If one overlaps, try the next one
            List<PlatformTile> shuffledTiles = WeightedShuffle(slots[i].possibleTiles);
            bool placed = false;

            foreach (PlatformTile candidate in shuffledTiles)
            {
                // Temporarily assign this tile to calculate its position
                slots[i].collapsed = candidate;

                if (!CalculateExitPlane(slots[i]))
                    continue;

                // Check if this position overlaps with any previous platform
                if (IsOverlapping(slots[i].spawnPosition, i))
                {
                    Debug.LogWarning("⚠️ Overlap detected at slot " + i 
                                   + " with tile " + candidate.tileName 
                                   + " — trying next tile...");
                    // Reset slot spawn data for next candidate
                    slots[i].spawnPosition = i == 0 
                        ? Vector3.zero 
                        : slots[i - 1].exitWorldPosition;
                    slots[i].spawnRotation = i == 0 
                        ? Quaternion.identity 
                        : slots[i - 1].exitWorldRotation;
                    continue;
                }

                // No overlap — this tile is valid
                placed = true;
                break;
            }

            if (!placed)
            {
                Debug.LogWarning("❌ No valid tile found for slot " + i 
                               + " without overlap — retrying level...");
                return false;
            }

            // Record this platform's position
            placedPositions.Add(slots[i].spawnPosition);

            // Track variety
            bool isCurve = slots[i].collapsed.tileName.StartsWith("Round");
            if (isCurve) { consecutiveCurves++;    consecutiveStraights = 0; }
            else         { consecutiveStraights++;  consecutiveCurves    = 0; }

            // Propagate to next slot
            if (i + 1 < levelLength)
                AlignNextSlot(slots[i], slots[i + 1]);
        }

        SpawnAll();
        return true;
    }

    // =====================================================
    // IS OVERLAPPING
    // Checks if a new platform position is too close
    // to any already placed platform in this attempt
    // Skips checking against itself and its direct neighbor
    // =====================================================
    bool IsOverlapping(Vector3 newPosition, int currentSlotIndex)
    {
        for (int i = 0; i < placedPositions.Count; i++)
        {
            // Skip the immediately previous platform
            // (it is supposed to be close — it connects to us)
            if (i == currentSlotIndex - 1)
                continue;

            float distance = Vector3.Distance(newPosition, placedPositions[i]);

            if (distance < minDistanceBetweenPlatforms)
            {
                return true; // Overlapping!
            }
        }

        return false; // No overlap
    }

    // =====================================================
    // CALCULATE EXIT PLANE
    // =====================================================
    bool CalculateExitPlane(SlotData slot)
    {
        GameObject prefab = slot.collapsed.prefab;

        Transform entryPlane = FindChildByName(prefab, slot.collapsed.entryPlaneName);
        Transform exitPlane  = FindChildByName(prefab, slot.collapsed.exitPlaneName);

        if (entryPlane == null)
        {
            Debug.LogError("❌ Missing " + slot.collapsed.entryPlaneName 
                         + " on: " + prefab.name);
            return false;
        }
        if (exitPlane == null)
        {
            Debug.LogError("❌ Missing " + slot.collapsed.exitPlaneName 
                         + " on: " + prefab.name);
            return false;
        }

        // Calculate prefab world rotation
        Quaternion prefabRotation = slot.spawnRotation 
                                  * Quaternion.Inverse(entryPlane.localRotation);

        // Calculate prefab world position
        Vector3 prefabPosition = slot.spawnPosition 
                               - (prefabRotation * entryPlane.localPosition);

        // Update slot spawn transform
        slot.spawnPosition = prefabPosition;
        slot.spawnRotation = prefabRotation;

        // Calculate exit plane world transform
        slot.exitWorldPosition = prefabPosition 
                               + (prefabRotation * exitPlane.localPosition);
        slot.exitWorldRotation = prefabRotation * exitPlane.localRotation;

        return true;
    }

    // =====================================================
    // ALIGN NEXT SLOT
    // =====================================================
    void AlignNextSlot(SlotData current, SlotData next)
    {
        next.spawnPosition = current.exitWorldPosition;
        next.spawnRotation = current.exitWorldRotation;
    }

    // =====================================================
    // FIND CHILD BY NAME
    // =====================================================
    Transform FindChildByName(GameObject prefab, string childName)
    {
        Transform[] all = prefab.GetComponentsInChildren<Transform>(true);
        foreach (var t in all)
            if (t.name == childName) return t;
        return null;
    }

    // =====================================================
    // ENFORCE VARIETY
    // =====================================================
    void EnforceVariety(SlotData slot, int curves, int straights)
    {
        if (curves >= maxConsecutiveCurves)
        {
            var s = slot.possibleTiles
                .Where(t => !t.tileName.StartsWith("Round")).ToList();
            if (s.Count > 0) slot.possibleTiles = s;
        }

        if (straights >= maxConsecutiveStraights)
        {
            var c = slot.possibleTiles
                .Where(t => t.tileName.StartsWith("Round")).ToList();
            if (c.Count > 0) slot.possibleTiles = c;
        }
    }

    // =====================================================
    // SPAWN ALL
    // =====================================================
    void SpawnAll()
    {
        foreach (var slot in slots)
        {
            GameObject go = Instantiate(
                slot.collapsed.prefab,
                slot.spawnPosition,
                slot.spawnRotation
            );
            go.name = "Platform_" + slot.collapsed.tileName;
            spawnedObjects.Add(go);
        }
    }

    // =====================================================
    // WEIGHTED RANDOM — single pick
    // =====================================================
    PlatformTile WeightedRandom(List<PlatformTile> tiles)
    {
        float total      = tiles.Sum(t => t.weight);
        float roll       = Random.Range(0f, total);
        float cumulative = 0f;

        foreach (var tile in tiles)
        {
            cumulative += tile.weight;
            if (roll <= cumulative) return tile;
        }
        return tiles[tiles.Count - 1];
    }

    // =====================================================
    // WEIGHTED SHUFFLE
    // Returns all tiles sorted by weighted random order
    // So we can try each one until we find a non-overlapping one
    // =====================================================
    List<PlatformTile> WeightedShuffle(List<PlatformTile> tiles)
    {
        // Assign each tile a random score multiplied by its weight
        // Higher weight = more likely to appear earlier in the list
        return tiles
            .OrderByDescending(t => Random.value * t.weight)
            .ToList();
    }

    // =====================================================
    // GIZMOS
    // =====================================================
    void OnDrawGizmos()
    {
        if (!showGizmos || slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i]?.collapsed == null) continue;

            // Exit point — yellow sphere
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(
                slots[i].exitWorldPosition + Vector3.up * 0.02f, 
                0.03f
            );

            // Exit direction — green line
            Gizmos.color = Color.green;
            Vector3 exitFwd = slots[i].exitWorldRotation * Vector3.forward;
            Gizmos.DrawLine(
                slots[i].exitWorldPosition,
                slots[i].exitWorldPosition + exitFwd * 0.15f
            );

            // Platform center — show overlap check radius
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // orange transparent
            Gizmos.DrawWireSphere(
                slots[i].spawnPosition,
                minDistanceBetweenPlatforms * 0.5f
            );

            // Path line — cyan
            if (i < slots.Length - 1 && slots[i + 1]?.collapsed != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(
                    slots[i].exitWorldPosition + Vector3.up * 0.02f,
                    slots[i + 1].exitWorldPosition + Vector3.up * 0.02f
                );
            }
        }
    }
}
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

    [Header("--- DEBUG ---")]
    public bool showGizmos = true;

    private SlotData[] slots;
    private List<GameObject> spawnedObjects = new List<GameObject>();

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

    void Start()
    {
        GenerateLevel();
    }

    public void GenerateLevel()
    {
        foreach (var obj in spawnedObjects)
            if (obj != null) Destroy(obj);

        spawnedObjects.Clear();

        for (int attempt = 1; attempt <= 20; attempt++)
        {
            if (TryGenerate())
            {
                Debug.Log("Level generated on attempt " + attempt);
                return;
            }

            Debug.LogWarning("Attempt " + attempt + " failed");
        }

        Debug.LogError("WFC Failed");
    }

    bool TryGenerate()
    {
        slots = new SlotData[levelLength];

        for (int i = 0; i < levelLength; i++)
            slots[i] = new SlotData(allTiles);

        slots[0].spawnPosition = Vector3.zero;
        slots[0].spawnRotation = Quaternion.identity;

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

            PlatformTile chosen = WeightedRandom(slots[i].possibleTiles);
            if (chosen == null) return false;

            slots[i].collapsed = chosen;

            if (!CalculateExitPlane(slots[i]))
                return false;

            bool isCurve = chosen.tileName.StartsWith("Round");

            if (isCurve)
            {
                consecutiveCurves++;
                consecutiveStraights = 0;
            }
            else
            {
                consecutiveStraights++;
                consecutiveCurves = 0;
            }

            if (i + 1 < levelLength)
                AlignNextSlot(slots[i], slots[i + 1]);
        }

        SpawnAll();
        return true;
    }

    bool CalculateExitPlane(SlotData slot)
    {
        GameObject prefab = slot.collapsed.prefab;

        Transform entryPlane = FindChildByName(prefab, slot.collapsed.entryPlaneName);
        Transform exitPlane = FindChildByName(prefab, slot.collapsed.exitPlaneName);

        if (entryPlane == null)
        {
            Debug.LogError("Missing " + slot.collapsed.entryPlaneName + " on: " + prefab.name);
            return false;
        }

        if (exitPlane == null)
        {
            Debug.LogError("Missing " + slot.collapsed.exitPlaneName + " on: " + prefab.name);
            return false;
        }

        // The input slot.spawnRotation and slot.spawnPosition are the Target World Transform for the Entry Plane.
        // We need to find the Prefab's World Transform that makes the Entry Plane match this target.
        
        // 1. Calculate Prefab World Rotation
        // prefabRotation * entryPlane.localRotation = targetEntryRotation
        // prefabRotation = targetEntryRotation * Inverse(entryPlane.localRotation)
        Quaternion prefabRotation = slot.spawnRotation * Quaternion.Inverse(entryPlane.localRotation);
        
        // 2. Calculate Prefab World Position
        // prefabPosition + prefabRotation * entryPlane.localPosition = targetEntryPosition
        // prefabPosition = targetEntryPosition - (prefabRotation * entryPlane.localPosition)
        Vector3 prefabPosition = slot.spawnPosition - (prefabRotation * entryPlane.localPosition);

        // 3. Update slot with the actual spawn transform for the prefab
        slot.spawnPosition = prefabPosition;
        slot.spawnRotation = prefabRotation;

        // 4. Calculate the World Transform of the Exit Plane for the NEXT tile
        slot.exitWorldPosition = prefabPosition + (prefabRotation * exitPlane.localPosition);
        slot.exitWorldRotation = prefabRotation * exitPlane.localRotation;

        return true;
    }

    void AlignNextSlot(SlotData current, SlotData next)
    {
        // Align next slot's entry target to this slot's exit point
        next.spawnPosition = current.exitWorldPosition;
        next.spawnRotation = current.exitWorldRotation;
    }

    Transform FindChildByName(GameObject prefab, string childName)
    {
        Transform[] all = prefab.GetComponentsInChildren<Transform>(true);

        foreach (var t in all)
            if (t.name == childName)
                return t;

        return null;
    }

    void EnforceVariety(SlotData slot, int curves, int straights)
    {
        if (curves >= maxConsecutiveCurves)
        {
            var s = slot.possibleTiles
                .Where(t => !t.tileName.StartsWith("Round")).ToList();

            if (s.Count > 0)
                slot.possibleTiles = s;
        }

        if (straights >= maxConsecutiveStraights)
        {
            var c = slot.possibleTiles
                .Where(t => t.tileName.StartsWith("Round")).ToList();

            if (c.Count > 0)
                slot.possibleTiles = c;
        }
    }

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

    PlatformTile WeightedRandom(List<PlatformTile> tiles)
    {
        float total = tiles.Sum(t => t.weight);

        float roll = Random.Range(0f, total);

        float cumulative = 0f;

        foreach (var tile in tiles)
        {
            cumulative += tile.weight;

            if (roll <= cumulative)
                return tile;
        }

        return tiles[tiles.Count - 1];
    }
}
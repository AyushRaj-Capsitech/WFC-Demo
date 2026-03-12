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

        Quaternion entryWorldRot = slot.spawnRotation * entryPlane.localRotation;

        Vector3 entryForward = entryWorldRot * Vector3.forward;

        Vector3 entryWorldOffset = slot.spawnRotation * entryPlane.localPosition;

        Vector3 prefabCenter = slot.spawnPosition - entryWorldOffset;

        slot.spawnPosition = prefabCenter;

        Vector3 exitWorldOffset = slot.spawnRotation * exitPlane.localPosition;

        slot.exitWorldPosition = prefabCenter + exitWorldOffset;

        slot.exitWorldRotation = slot.spawnRotation * exitPlane.localRotation;

        return true;
    }

    void AlignNextSlot(SlotData current, SlotData next)
    {
        next.spawnPosition = current.exitWorldPosition;

        Vector3 exitForward = current.exitWorldRotation * Vector3.forward;
        Vector3 exitUp = current.exitWorldRotation * Vector3.up;

        Vector3 nextForward = exitForward;

        nextForward.y = 0f;

        if (nextForward.magnitude < 0.001f)
            nextForward = -exitForward;

        // next.spawnRotation = Quaternion.LookRotation(
        //     nextForward.normalized,
        //     Vector3.up
        // );

        // next.entryWorldRotation = Quaternion.LookRotation(
        //     nextForward.normalized,
        //     Vector3.up
        // );
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
// PlatformTile.cs - PLANE VERSION
// Uses Entry/Exit planes for perfect alignment

using UnityEngine;

[CreateAssetMenu(fileName = "New PlatformTile", menuName = "WFC/Platform Tile")]
public class PlatformTile : ScriptableObject
{
    [Header("--- IDENTITY ---")]
    public string tileName;
    public GameObject prefab;

    [Header("--- PLANE NAMES ---")]
    public string entryPlaneName = "EntryPlane";
    public string exitPlaneName  = "ExitPlane";

    [Header("--- FREQUENCY ---")]
    [Range(0.1f, 5f)]
    public float weight = 1f;
    [Range(0f, 1f)]
    public float difficulty = 0.5f;
}
// PlatformTile.cs
// Data container for each platform type
// This becomes a ScriptableObject you configure in Inspector

using UnityEngine;

[CreateAssetMenu(fileName = "New PlatformTile", 
                 menuName = "WFC/Platform Tile")]
public class PlatformTile : ScriptableObject
{
    [Header("Identity")]
    public string tileName;
    public GameObject prefab;           // Drag your prefab here

    [Header("Connection Sockets")]
    public SocketType entrySocket;      // Direction player arrives FROM
    public SocketType exitSocket;       // Direction player leaves TO

    [Header("Spawn Settings")]
    public FacingDirection exitFacing;  // Player facing after this tile
    public float yRotation = 0f;        // Rotation applied when spawning

    [Header("Balancing")]
    [Range(0.1f, 5f)]
    public float weight = 1f;           // Higher = appears more often
}
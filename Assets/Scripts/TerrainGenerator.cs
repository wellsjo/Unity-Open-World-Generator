using System.Collections.Generic;
using UnityEngine;

// Procedurally generate terrain based on various settings and the viewer's position. When the viewer moves,
// We calculate the changed terrain chunks in view / out of view, then update them if necessary.
public class TerrainGenerator : MonoBehaviour
{

    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public int colliderLODIndex;
    public LODInfo[] detailLevels;

    public MeshSettings meshSettings;
    public MapSettings mapSettings;
    public TextureData textureSettings;

    FixedHeightMap fixedHeightMap;

    public Transform viewer;
    public Material mapMaterial;

    Vector2 viewerPosition;
    Vector2 viewerPositionOld;

    float meshWorldSize;
    int chunksVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    void Start()
    {
        textureSettings.ApplyToMaterial(mapMaterial);
        textureSettings.UpdateMeshHeights(mapMaterial, mapSettings.minHeight, mapSettings.maxHeight);

        float maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        meshWorldSize = meshSettings.meshWorldSize;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);

        if (mapSettings.borderType == Map.BorderType.Fixed)
        {
            fixedHeightMap = new FixedHeightMap(
                mapSettings.fixedSize,
                meshSettings.numVertsPerLine,
                mapSettings.noiseSettings,
                mapSettings.heightCurve,
                mapSettings.heightMultiplier,
                mapSettings.useFalloff
            );
        }

        UpdateVisibleChunks();
    }

    // Chek if the player has moved, if so update the collision mesh
    // Check to see if the player has moved past the threshold; if so, update the visible chunks
    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if (viewerPosition != viewerPositionOld)
        {
            foreach (TerrainChunk chunk in visibleTerrainChunks)
            {
                chunk.UpdateCollisionMesh();
            }
        }

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        Debug.Log("UpdateVisibleChunks");
        Debug.Log(visibleTerrainChunks.Count);

        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
        for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
            visibleTerrainChunks[i].UpdateTerrainChunk();
        }

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                {
                    if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    }
                    else
                    {
                        //Debug.Log("EndlessTerrainChunk");
                        //Debug.Log(viewedChunkCoord.ToString());

                        TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);

                        terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
                        newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;

                        // For infinite we generate a height map for every new chunk
                        if (mapSettings.borderType == Map.BorderType.Infinite)
                        {
                            Debug.Log("Loading Infinite Terrain Chunk");
                            newChunk.LoadInfiniteTerrain(mapSettings);
                        }
                        else if (mapSettings.borderType == Map.BorderType.Fixed)
                        {
                            if (fixedHeightMap.ChunkCoordInRange(viewedChunkCoord))
                            {
                                //Debug.Log("Loading Fixed Chunk Coord");
                                //Debug.Log(viewedChunkCoord.ToString());
                                newChunk.LoadFromHeightMapValues(
                                    fixedHeightMap.GetHeightMapForChunkCoord(viewedChunkCoord)
                                );
                            }
                            else
                            {
                                //Debug.Log("Chunk Coord Not In Range");
                                //Debug.Log(viewedChunkCoord.ToString());
                            }

                        }
                    }
                }

            }
        }
    }

    void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
    {
        if (isVisible)
        {
            visibleTerrainChunks.Add(chunk);
        }
        else
        {
            visibleTerrainChunks.Remove(chunk);
        }
    }

}

[System.Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int lod;
    public float visibleDstThreshold;


    public float sqrVisibleDstThreshold
    {
        get
        {
            return visibleDstThreshold * visibleDstThreshold;
        }
    }
}
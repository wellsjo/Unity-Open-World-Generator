using System.Collections.Generic;
using UnityEngine;

// Procedurally generate terrain based on various settings and the viewer's position. When the viewer moves,
// We calculate the changed terrain chunks in view / out of view, then update them if necessary.
public class WorldBuilder : MonoBehaviour
{
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public int colliderLODIndex;
    public MapSettings mapSettings;
    public Transform viewer;
    public Material mapMaterial;

    Vector2 viewerPosition;
    Vector2 viewerPositionOld;

    float meshWorldSize;
    int chunksVisibleInViewDst;
    readonly Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new();
    readonly List<TerrainChunk> visibleTerrainChunks = new();
    HeightMapGenerator heightMapGenerator;

    void Start()
    {
        mapSettings.textureSettings.ApplyToMaterial(mapMaterial);
        mapSettings.textureSettings.UpdateMeshHeights(
            mapMaterial,
            mapSettings.MinHeight,
            mapSettings.MaxHeight
        );

        float maxViewDst = mapSettings.detailLevels[^1].visibleDstThreshold;

        // Calculate this once
        meshWorldSize = mapSettings.meshSettings.meshWorldSize;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);

        heightMapGenerator = new HeightMapGenerator(
            mapSettings.biomeSettings,
            mapSettings.meshSettings.numVertsPerLine,
            mapSettings.meshSettings.numVertsPerLine,
            mapSettings.seed
        );

        UpdateVisibleChunks(viewerPosition);
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
                chunk.UpdateCollisionMesh(viewerPosition);
            }
        }

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks(viewerPosition);
        }
    }

    void UpdateVisibleChunks(Vector2 viewerPosition)
    {

        HashSet<Vector2> alreadyUpdatedChunkCoords = new();
        for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            TerrainChunk chunk = visibleTerrainChunks[i];
            alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
            visibleTerrainChunks[i].UpdateTerrainChunk(viewerPosition);
        }

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (!ChunkCoordInRange(viewedChunkCoord))
                {
                    // TODO unload chunk
                    continue;
                }

                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                {
                    if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        TerrainChunk chunk = terrainChunkDictionary[viewedChunkCoord];
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk(viewerPosition);
                    }
                    else
                    {
                        GameObject terrainObject = new(string.Format("Terrain Chunk {0}", viewedChunkCoord.ToString()));
                        terrainObject.transform.parent = transform;
                        TerrainChunk newChunk = new(
                            viewedChunkCoord,
                            terrainObject,
                            mapSettings,
                            colliderLODIndex,
                            mapMaterial
                        );

                        terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
                        newChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;

                        Debug.Log("Loading Infinite Terrain Chunk");
                        newChunk.LoadHeightMapInThread(
                            heightMapGenerator,
                            mapSettings.meshSettings.numVertsPerLine,
                            viewedChunkCoord,
                            viewerPosition
                        );
                    }
                }

            }
        }
    }

    private bool ChunkCoordInRange(Vector2 chunkCoord)
    {
        if (mapSettings.borderType == Map.BorderType.Fixed)
        {
            Vector2 range = mapSettings.range;
            return (
                chunkCoord.x >= range.x
                && chunkCoord.x <= range.y
                && chunkCoord.y >= range.x
                && chunkCoord.y <= range.y
            );
        }

        return true;
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
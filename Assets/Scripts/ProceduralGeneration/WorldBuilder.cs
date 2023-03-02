using System.Collections.Generic;
using UnityEngine;

// Procedurally generate terrain based on various settings and the viewer's position. When the viewer moves,
// We calculate the changed terrain chunks in view / out of view, then update them if necessary.
public class WorldBuilder : MonoBehaviour
{
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public int colliderLODIndex;

    // public BiomeSettings biomeSettings;
    public MapSettings mapSettings;
    public TextureSettings textureSettings;

    public Transform viewer;
    public Material mapMaterial;

    Vector2 viewerPosition;
    Vector2 viewerPositionOld;

    float meshWorldSize;
    int chunksVisibleInViewDst;
    readonly Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new();
    readonly List<TerrainChunk> visibleTerrainChunks = new();
    Biome heightMapGenerator;

    void Start()
    {
        textureSettings.ApplyToMaterial(mapMaterial);
        textureSettings.UpdateMeshHeights(mapMaterial, mapSettings.MinHeight, mapSettings.MaxHeight);

        float maxViewDst = mapSettings.detailLevels[^1].visibleDstThreshold;

        // Calculate this once
        meshWorldSize = mapSettings.meshSettings.meshWorldSize;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);

        heightMapGenerator = new Biome(mapSettings.biome, mapSettings.seed);

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
        HashSet<Vector2> alreadyUpdatedChunkCoords = new();
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
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    }
                    else
                    {
                        GameObject terrainObject = new(string.Format("Terrain Chunk {0}", viewedChunkCoord.ToString()));
                        terrainObject.transform.parent = transform;
                        TerrainChunk newChunk = new(
                            viewedChunkCoord,
                            terrainObject,
                            mapSettings.meshSettings,
                            mapSettings.detailLevels,
                            colliderLODIndex,
                            viewer,
                            mapMaterial
                        );

                        terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
                        newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;

                        Debug.Log("Loading Infinite Terrain Chunk");
                        newChunk.LoadHeightMapInThread(heightMapGenerator, mapSettings.meshSettings.numVertsPerLine, viewedChunkCoord);
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

    public static void GeneratePreview(
        // TextureSettings textureData,
        MeshSettings meshSettings,
        MapSettings mapSettings,
        Material mapMaterial,
        Transform terrainChunkParent
    )
    {
        Biome heightMapGenerator = new(
            mapSettings.biome,
            mapSettings.seed
        );

        // Default to something reasonable for infinite view
        // TODO make this a map preview option
        Vector2 range = new(-3, 3);
        if (mapSettings.borderType == Map.BorderType.Fixed)
        {
            range = mapSettings.range;
        }

        for (int x = (int)range.x; x <= range.y; x++)
        {
            for (int y = (int)range.x; y <= range.y; y++)
            {
                // TODO move this to biome.Spawn(chunkCoord)
                Vector2 chunkCoord = new(x, y);
                string gameObjectName = string.Format("Preview Terrain Chunk {0}", chunkCoord.ToString());

                // Make a new terrain chunk under the Terrain Preview parent
                GameObject meshObject = new(gameObjectName);
                meshObject.transform.parent = terrainChunkParent;
                TerrainChunk newChunk = new(chunkCoord, meshObject, meshSettings, mapSettings.detailLevels, 0, null, mapMaterial);

                Vector2 sampleCenter = chunkCoord * meshSettings.meshWorldSize / meshSettings.meshScale;

                HeightMap heightMap = heightMapGenerator.BuildHeightMap(
                    meshSettings.numVertsPerLine,
                    meshSettings.numVertsPerLine,
                    sampleCenter
                );

                newChunk.LoadFromHeightMap(heightMap);
                newChunk.SetVisible(true);
            }
        }

    }

    // public override bool Equals(object obj)
    // {
    //     return obj is WorldBuilder builder &&
    //            base.Equals(obj) &&
    //            EqualityComparer<MapSettings>.Default.Equals(mapSettings, builder.mapSettings);
    // }
}
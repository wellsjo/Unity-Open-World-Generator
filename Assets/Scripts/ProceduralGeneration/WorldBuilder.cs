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
    Biome biome;

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

        biome = new Biome(mapSettings.biome, mapSettings.seed);

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
                            biome,
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

    public static void SpawnVegetation(HeightMap heightMap, float[,] treeNoise, Vector2 sampleCenter)
    {
        // float vegetationStartHeight = mapSettings.biome.vegetationSettings.startHeight;
        // float vegetationEndHeight = mapSettings.biome.vegetationSettings.endHeight;
        float vegetationStartHeight = 0;
        float vegetationEndHeight = 100;

        for (int i = 0; i < heightMap.width; i++)
        {
            for (int j = 0; j < heightMap.height; j++)
            {
                if (heightMap.values[i, j] > vegetationStartHeight && heightMap.values[i, j] < vegetationEndHeight)
                {
                    if (treeNoise[i, j] > 0.5f)
                    {
                        float x = i + sampleCenter.x - heightMap.width / 2f;
                        float y = heightMap.values[i, j];
                        float z = j + sampleCenter.y - heightMap.height / 2f;

                        Vector3 position = new(x, y, z);
                        GameObject tree = Instantiate(mapSettings.biome.vegetationSettings.treePrefab);
                        tree.transform.localScale = position;
                    }
                }
            }
        }
    }

    public static void GeneratePreview(
        MeshSettings meshSettings,
        MapSettings mapSettings,
        Material mapMaterial,
        Transform terrainChunkParent
    )
    {
        Biome biome = new(
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

                TerrainChunk newChunk = new(
                    chunkCoord,
                    meshObject,
                    mapSettings,
                    0,
                    mapMaterial
                );

                Vector2 sampleCenter = chunkCoord * meshSettings.meshWorldSize / meshSettings.meshScale;

                HeightMap heightMap = biome.BuildHeightMap(
                    meshSettings.numVertsPerLine,
                    meshSettings.numVertsPerLine,
                    sampleCenter
                );

                newChunk.LoadFromHeightMap(heightMap);
                newChunk.SetVisible(true);

                float[,] treeNoise = biome.noise.Generate(
                    meshSettings.numVertsPerLine,
                    meshSettings.numVertsPerLine,
                    sampleCenter,
                    mapSettings.biome.vegetationSettings.noiseSettings
                );

                SpawnVegetation(newChunk.heightMap, treeNoise, chunkCoord);
            }
        }

    }

}
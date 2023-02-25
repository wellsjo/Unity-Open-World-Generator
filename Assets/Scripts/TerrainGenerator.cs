using System.Collections.Generic;
using UnityEngine;

// Procedurally generate terrain based on various settings and the viewer's position. When the viewer moves,
// We calculate the changed terrain chunks in view / out of view, then update them if necessary.
public class TerrainGenerator : MonoBehaviour
{

    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public int colliderLODIndex;

    public MeshSettings meshSettings;
    public MapSettings mapSettings;
    public TextureData textureSettings;

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

        float maxViewDst = mapSettings.detailLevels[mapSettings.detailLevels.Length - 1].visibleDstThreshold;
        meshWorldSize = meshSettings.meshWorldSize;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);

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
                        GameObject meshObject = new GameObject(string.Format("Terrain Chunk {0}", viewedChunkCoord.ToString()));
                        TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, meshObject, meshSettings, mapSettings.detailLevels, colliderLODIndex, viewer, mapMaterial);

                        terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
                        newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;

                        Debug.Log("Loading Infinite Terrain Chunk");
                        newChunk.LoadHeightMapThreaded(mapSettings);

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

    public static void GeneratePreview(TextureData textureData, MeshSettings meshSettings, MapSettings mapSettings, Material mapMaterial, Transform parent)
    {
        // Clear out the old display
        foreach (Transform obj in parent)
        {
            GameObject.DestroyImmediate(obj.gameObject);
        }

        Debug.Log("Generate Preview");
        Vector2 range = mapSettings.range;
        for (int x = (int)range.x; x <= range.y; x++)
        {
            for (int y = (int)range.x; y <= range.y; y++)
            {
                Vector2 chunkCoord = new Vector2(x, y);

                // Remove any existing chunks
                string gameObjectName = string.Format("Preview Terrain Chunk {0}", chunkCoord.ToString());
                GameObject existingChunk = GameObject.Find(gameObjectName);
                if (existingChunk != null)
                {
                    DestroyImmediate(existingChunk);
                }

                // Make a new terrain chunk under the Terrain Preview parent
                GameObject meshObject = new GameObject(gameObjectName);
                meshObject.transform.parent = parent;
                TerrainChunk newChunk = new TerrainChunk(chunkCoord, meshObject, meshSettings, mapSettings.detailLevels, 0, null, mapMaterial);

                Vector2 sampleCenter = chunkCoord * meshSettings.meshWorldSize / meshSettings.meshScale;

                HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(
                    meshSettings.numVertsPerLine,
                    meshSettings.numVertsPerLine,
                    mapSettings.noiseSettings,
                    mapSettings.heightCurve,
                    mapSettings.heightMultiplier,
                    sampleCenter,
                    false
                );

                newChunk.LoadFromHeightMap(heightMap);
                newChunk.SetVisible(true);
            }
        }

    }

}


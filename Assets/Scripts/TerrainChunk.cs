using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk
{
    // Coordinate of the TerrainChunk where 0,0 is the center of the first tile, 1,1 is
    //one up and to the right, and so on.
    public Vector2 chunkCoord;
    protected readonly GameObject terrainMesh;
    private readonly MeshRenderer meshRenderer;
    protected readonly MeshFilter meshFilter;
    protected readonly MapSettings mapSettings;

    // A piece of terrain on a grid
    public TerrainChunk(
        Vector2 chunkCoord,
        GameObject terrainMesh,
        MapSettings mapSettings,
        Material material
    )
    {
        this.chunkCoord = chunkCoord;
        this.mapSettings = mapSettings;

        this.terrainMesh = terrainMesh;
        meshRenderer = terrainMesh.AddComponent<MeshRenderer>();
        meshFilter = terrainMesh.AddComponent<MeshFilter>();
        meshRenderer.material = material;

        // Get the game world coordinate of the chunk based on the chunk index,
        // then move the chunk there.
        Vector2 position = chunkCoord * mapSettings.meshSettings.meshWorldSize;
        terrainMesh.transform.position = new Vector3(position.x, 0, position.y);

        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        terrainMesh.SetActive(visible);
    }

    public void LoadFromHeightMap(HeightMap heightMap)
    {
        MeshData meshData = MeshGenerator.GetTerrainChunkMesh(
            heightMap.values,
            mapSettings.meshSettings,
            0
        );
        Mesh mesh = meshData.CreateMesh();
        meshFilter.sharedMesh = mesh;
    }
}

// Terrain mesh renderable in different quality settings, based on a height map
public class DynamicTerrainChunk : TerrainChunk
{
    private readonly Transform viewer;
    public event System.Action<DynamicTerrainChunk, bool> OnVisibilityChanged;
    readonly float maxViewDst;
    readonly Vector2 heightMapOffset;
    // Used to get the distance from nearest edge
    public Bounds bounds;
    const float colliderGenerationDistanceThreshold = 5;
    int previousLODIndex = -1;
    readonly MeshCollider meshCollider;
    readonly LODMesh[] lodMeshes;
    bool heightMapReceived;
    bool vegetationMapReceived;
    readonly int colliderLODIndex;
    bool hasSetCollider;
    private readonly HeightMapGenerator heightMapGenerator;
    private readonly VegetationGenerator vegetationGenerator;
    public HeightMap heightMap;
    private List<Vector3> vegetationMap;

    // A terrain chunk which updates its LOD based on the user's position.
    public DynamicTerrainChunk(
        Transform viewer,
        Vector2 coord,
        GameObject meshObject,
        MapSettings mapSettings,
        int colliderLODIndex,
        Material material,
        HeightMapGenerator heightMapGenerator,
        VegetationGenerator vegetationGenerator
    ) : base(coord, meshObject, mapSettings, material)
    {
        this.viewer = viewer;
        this.colliderLODIndex = colliderLODIndex;
        this.heightMapGenerator = heightMapGenerator;
        this.vegetationGenerator = vegetationGenerator;

        // position in 3d space
        Vector2 position = coord * mapSettings.meshSettings.meshWorldSize;
        bounds = new Bounds(position, Vector2.one * mapSettings.meshSettings.meshWorldSize);

        meshCollider = meshObject.AddComponent<MeshCollider>();

        lodMeshes = new LODMesh[mapSettings.detailLevels.Length];
        for (int i = 0; i < mapSettings.detailLevels.Length; i++)
        {
            lodMeshes[i] = new LODMesh(mapSettings.detailLevels[i].lod);
            lodMeshes[i].UpdateCallback += UpdateTerrainChunk;
            // TODO this seems like a bug, should be i < colliderLODIndex
            if (i == colliderLODIndex)
            {
                lodMeshes[i].UpdateCallback += UpdateCollisionMesh;
            }
        }

        maxViewDst = mapSettings.detailLevels[^1].visibleDstThreshold;
        heightMapOffset = chunkCoord * mapSettings.meshSettings.meshWorldSize / mapSettings.meshSettings.meshScale;
    }

    // TerrainChunk loads a new height map with only the number of vertices per mesh, then requests mesh data for it.
    // This is used for infinite terrain.
    public void Load()
    {
        // Center of the height map on the game world
        ThreadedDataRequester.RequestData(() =>
        {
            return heightMapGenerator.BuildHeightMap(heightMapOffset);
        }, OnHeightMapReceived);
    }

    public void LoadVegetationAsync()
    {
        Debug.Log("Loading Vegetation Async");
        // Center of the height map on the game world
        Vector2 heightMapOffSet = chunkCoord * mapSettings.meshSettings.meshWorldSize / mapSettings.meshSettings.meshScale;
        ThreadedDataRequester.RequestData(() =>
        {
            return vegetationGenerator.BuildVegetationMap(heightMapOffSet, heightMap.values);
        }, OnVegetationMapReceived);
    }

    void OnHeightMapReceived(object heightMapObj)
    {
        if (heightMapObj == null)
        {
            Debug.Log("Height Map Null");
            return;
        }

        HeightMap heightMap = (HeightMap)heightMapObj;
        this.heightMap = heightMap;
        heightMapReceived = true;

        LoadVegetationAsync();
    }

    void OnVegetationMapReceived(object vegetationMapObj)
    {
        if (vegetationMapObj == null)
        {
            Debug.Log("Vegetation Map Null");
            return;
        }

        List<Vector3> vegetationMap = (List<Vector3>)vegetationMapObj;
        Debug.LogFormat("Vegetation Map Received {0}", vegetationMap.Count);
        this.vegetationMap = vegetationMap;
        vegetationMapReceived = true;

        UpdateTerrainChunk();
    }

    void SpawnVegetation()
    {
        if (vegetationMap == null)
        {
            Debug.Log("Vegetation Map Null");
            return;
        }
        foreach (Vector3 vegetationValue in vegetationMap)
        {
            Debug.Log("Spawning Tree");
            UnityEngine.GameObject tree = UnityEngine.GameObject.Instantiate(mapSettings.biomeSettings.vegetationSettings.treePrefab);
            tree.transform.position = vegetationValue;
            tree.transform.parent = terrainMesh.transform;
        }
        vegetationMap = null;
    }

    // Called during the Update loop in WorldBuilder.
    // Show, update the level of detail, or hide the terrain chunk based on the viewer's position
    public void UpdateTerrainChunk()
    {
        if (!heightMapReceived || !vegetationMapReceived)
        {
            Debug.LogWarning("height map not received");
            return;
        }

        Vector2 viewerPosition = ViewerPosition();

        float viewerDstFromNearestEdge = Mathf.Sqrt(
            bounds.SqrDistance(viewerPosition)
        );

        // TODO replace this
        bool wasVisible = IsVisible();
        bool visible = viewerDstFromNearestEdge <= maxViewDst;

        if (visible)
        {
            SpawnVegetation();

            int lodIndex = 0;

            for (int i = 0; i < mapSettings.detailLevels.Length - 1; i++)
            {
                if (viewerDstFromNearestEdge > mapSettings.detailLevels[i].visibleDstThreshold)
                {
                    lodIndex = i + 1;
                }
                else
                {
                    break;
                }
            }

            if (lodIndex != previousLODIndex)
            {
                LODMesh lodMesh = lodMeshes[lodIndex];

                if (lodMesh.hasMesh)
                {
                    previousLODIndex = lodIndex;
                    base.meshFilter.mesh = lodMesh.mesh;
                }
                else if (!lodMesh.hasRequestedMesh)
                {
                    lodMesh.RequestMesh(this.heightMap, mapSettings.meshSettings);
                }
            }
        }

        if (wasVisible != visible)
        {
            SetVisible(visible);
            OnVisibilityChanged?.Invoke(this, visible);
        }
    }

    public void UpdateCollisionMesh()
    {
        if (hasSetCollider)
        {
            return;
        }
        Vector2 viewerPosition = ViewerPosition();
        float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

        if (sqrDstFromViewerToEdge < mapSettings.detailLevels[colliderLODIndex].sqrVisibleDstThreshold)
        {
            if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
            {
                lodMeshes[colliderLODIndex].RequestMesh(heightMap, mapSettings.meshSettings);
            }
        }

        if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
        {
            if (lodMeshes[colliderLODIndex].hasMesh)
            {
                meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                hasSetCollider = true;
            }
        }
    }

    public bool IsVisible()
    {
        return terrainMesh.activeSelf;
    }

    private Vector2 ViewerPosition()
    {
        return new Vector2(viewer.position.x, viewer.position.z);
    }

}

class LODMesh
{

    public Mesh mesh;
    public bool hasRequestedMesh;
    public bool hasMesh;
    readonly int lod;
    public event System.Action UpdateCallback;

    public LODMesh(int lod)
    {
        this.lod = lod;
    }

    void OnMeshDataReceived(object meshDataObject)
    {
        MeshData meshData = (MeshData)meshDataObject;
        mesh = meshData.CreateMesh();
        hasMesh = true;
        UpdateCallback();
    }

    public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
    {
        hasRequestedMesh = true;
        ThreadedDataRequester.RequestData(() =>
        {
            return MeshGenerator.GetTerrainChunkMesh(heightMap.values, meshSettings, lod);
        }, OnMeshDataReceived);
    }

}
using System;
using UnityEngine;

// Terrain mesh renderable in different quality settings, based on a height map
public class TerrainChunk
{
    const float colliderGenerationDistanceThreshold = 5;
    public event System.Action<TerrainChunk, bool> OnVisibilityChanged;
    public Vector2 coord;

    GameObject meshObject;
    Vector2 sampleCentre;
    public Bounds bounds;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;
    VegetationSettings vegetationSettings;

    LODInfo[] detailLevels;
    LODMesh[] lodMeshes;
    int colliderLODIndex;

    public HeightMap heightMap;
    bool heightMapReceived;
    int previousLODIndex = -1;
    bool hasSetCollider;
    float maxViewDst;
    readonly MeshSettings meshSettings;

    // A piece of terrain which also is aware of the user's position
    public TerrainChunk(
        Vector2 coord,
        GameObject meshObject,
        MapSettings mapSettings,
        int colliderLODIndex,
        Material material
    )
    {
        this.coord = coord;
        this.detailLevels = mapSettings.detailLevels;
        this.colliderLODIndex = colliderLODIndex;
        this.meshSettings = mapSettings.meshSettings;
        this.vegetationSettings = mapSettings.biome.vegetationSettings;

        sampleCentre = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
        Vector2 position = coord * meshSettings.meshWorldSize;
        bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);

        this.meshObject = meshObject;
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;

        meshObject.transform.position = new Vector3(position.x, 0, position.y);
        SetVisible(false);

        lodMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < detailLevels.Length; i++)
        {
            lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            lodMeshes[i].UpdateCallback += UpdateTerrainChunk;
            // TODO this seems like a bug, should be i < colliderLODIndex
            if (i == colliderLODIndex)
            {
                lodMeshes[i].UpdateCallback += UpdateCollisionMesh;
            }
        }

        maxViewDst = detailLevels[^1].visibleDstThreshold;
    }

    // TerrainChunk loads a new height map with only the number of vertices per mesh, then requests mesh data for it.
    // This is used for infinite terrain.
    public void LoadHeightMapInThread(
        Biome biome,
        int size,
        Vector2 chunkCoord,
        Vector2 viewerPosition
    )
    {
        Vector2 offset = chunkCoord * meshSettings.meshWorldSize / meshSettings.meshScale;
        ThreadedDataRequester.RequestData(() =>
        {
            return new HeightMapUpdateData(
                biome.BuildHeightMap(size, size, offset), viewerPosition
            );
        }, OnHeightMapReceived
        );
    }

    public void LoadFromHeightMap(HeightMap heightMap)
    {
        MeshData meshData = MeshGenerator.GetTerrainChunkMesh(heightMap.values, meshSettings, 0);
        Mesh mesh = meshData.CreateMesh();
        meshFilter.mesh = mesh;
    }


    void OnHeightMapReceived(object heightMapObject)
    {
        if (heightMapObject == null)
        {
            Debug.Log("Height Map Null");
            return;
        }

        Debug.Log("Height Map Received");
        HeightMapUpdateData updateData = (HeightMapUpdateData)heightMapObject;
        this.heightMap = updateData.heightMap;
        heightMapReceived = true;

        UpdateTerrainChunk(updateData.viewerPosition);
    }

    // Show, update the level of detail, or hide the terrain chunk based on the viewer's position
    public void UpdateTerrainChunk(Vector2 viewerPosition)
    {
        if (!heightMapReceived)
        {
            Debug.LogWarning("height map not received");
            return;
        }

        float viewerDstFromNearestEdge = Mathf.Sqrt(
            bounds.SqrDistance(viewerPosition)
        );

        // TODO replace this
        bool wasVisible = IsVisible();
        bool visible = viewerDstFromNearestEdge <= maxViewDst;

        if (visible)
        {
            int lodIndex = 0;

            for (int i = 0; i < detailLevels.Length - 1; i++)
            {
                if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold)
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
                    meshFilter.mesh = lodMesh.mesh;
                }
                else if (!lodMesh.hasRequestedMesh)
                {
                    lodMesh.RequestMesh(this.heightMap, meshSettings);
                }
            }
        }

        if (wasVisible != visible)
        {
            SetVisible(visible);
            OnVisibilityChanged?.Invoke(this, visible);
        }
    }

    public void UpdateCollisionMesh(Vector2 viewerPosition)
    {
        if (hasSetCollider)
        {
            return;
        }

        float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

        if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDstThreshold)
        {
            if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
            {
                lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);
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

    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);
    }

    public bool IsVisible()
    {
        return meshObject.activeSelf;
    }

}

class LODMesh
{

    public Mesh mesh;
    public bool hasRequestedMesh;
    public bool hasMesh;
    readonly int lod;
    public event System.Action<Vector2> UpdateCallback;

    public LODMesh(int lod)
    {
        this.lod = lod;
    }

    void OnMeshDataReceived(object meshDataObject)
    {
        MeshData meshData = (MeshData)meshDataObject;
        mesh = meshData.CreateMesh();
        hasMesh = true;
        UpdateCallback(meshData.ViewerPosition);
    }

    public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
    {
        hasRequestedMesh = true;
        MeshData meshData = MeshGenerator.GetTerrainChunkMesh(heightMap.values, meshSettings, lod);
        Debug.Log("RequestMeshData");
        ThreadedDataRequester.RequestData(() => meshData, OnMeshDataReceived);
    }

}
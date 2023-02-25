using UnityEngine;

// Terrain mesh renderable in different quality settings, based on a height map
public class TerrainChunk
{

    const float colliderGenerationDistanceThreshold = 5;
    public event System.Action<TerrainChunk, bool> onVisibilityChanged;
    public Vector2 coord;

    GameObject meshObject;
    Vector2 sampleCentre;
    Bounds bounds;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    LODInfo[] detailLevels;
    LODMesh[] lodMeshes;
    int colliderLODIndex;

    HeightMap heightMap;
    bool heightMapReceived;
    int previousLODIndex = -1;
    bool hasSetCollider;
    float maxViewDst;

    MeshSettings meshSettings;
    Transform viewer;

    // A piece of terrain which also is aware of the user's position
    public TerrainChunk(
        Vector2 coord,
        GameObject meshObject,
        MeshSettings meshSettings,
        LODInfo[] detailLevels,
        int colliderLODIndex,
        //Transform parent,
        Transform viewer,
        Material material
    )
    {
        this.coord = coord;
        this.detailLevels = detailLevels;
        this.colliderLODIndex = colliderLODIndex;
        this.meshSettings = meshSettings;
        this.viewer = viewer;

        sampleCentre = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
        Vector2 position = coord * meshSettings.meshWorldSize;
        bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);

        //meshObject = new GameObject(string.Format("Terrain Chunk {0}", coord.ToString()));
        this.meshObject = meshObject;
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;

        meshObject.transform.position = new Vector3(position.x, 0, position.y);
        //meshObject.transform.parent = parent;
        SetVisible(false);

        lodMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < detailLevels.Length; i++)
        {
            lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            lodMeshes[i].updateCallback += UpdateTerrainChunk;
            if (i == colliderLODIndex)
            {
                lodMeshes[i].updateCallback += UpdateCollisionMesh;
            }
        }

        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;

    }

    // TerrainChunk loads a new height map with only the number of vertices per mesh, then requests mesh data for it.
    // This is used for infinite terrain.
    public void LoadHeightMapThreaded(MapSettings heightMapSettings)
    {
        // TODO generate the height map in the threaded data requester
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(
            meshSettings.numVertsPerLine,
            meshSettings.numVertsPerLine,
            heightMapSettings.noiseSettings,
            heightMapSettings.heightCurve,
            heightMapSettings.heightMultiplier,
            sampleCentre,
            false
        );
        ThreadedDataRequester.RequestData(() => heightMap, OnHeightMapReceived);
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
        this.heightMap = (HeightMap)heightMapObject;
        heightMapReceived = true;

        UpdateTerrainChunk();
    }

    Vector2 viewerPosition
    {
        get
        {
            return new Vector2(viewer.position.x, viewer.position.z);
        }
    }

    // Show, update the level of detail, or hide the terrain chunk based on the viewer's position
    public void UpdateTerrainChunk()
    {
        if (!heightMapReceived)
        {
            Debug.LogWarning("height map not received");
            return;
        }

        float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
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
            onVisibilityChanged?.Invoke(this, visible);
        }
    }

    public void UpdateCollisionMesh()
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
    int lod;
    public event System.Action updateCallback;

    public LODMesh(int lod)
    {
        this.lod = lod;
    }

    void OnMeshDataReceived(object meshDataObject)
    {
        mesh = ((MeshData)meshDataObject).CreateMesh();
        hasMesh = true;

        updateCallback();
    }

    public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
    {
        hasRequestedMesh = true;
        MeshData meshData = MeshGenerator.GetTerrainChunkMesh(heightMap.values, meshSettings, lod);
        Debug.Log("RequestMeshData");
        ThreadedDataRequester.RequestData(() => meshData, OnMeshDataReceived);
    }

}
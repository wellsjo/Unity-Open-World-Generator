using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class TerrainChunk
{
    // Coordinate of the TerrainChunk where 0,0 is the center of the first tile, 1,1 is
    //one up and to the right, and so on.
    public Vector2 chunkCoord;
    protected readonly GameObject terrainMesh;
    private readonly MeshRenderer meshRenderer;
    protected MeshCollider meshCollider;
    public readonly MeshFilter meshFilter;
    protected readonly MapSettings mapSettings;
    protected readonly ObjectPlacer objectPlacer;

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
        Vector2 position = chunkCoord * mapSettings.meshSettings.MeshWorldSize;
        terrainMesh.transform.position = new Vector3(position.x, 0, position.y);

        this.objectPlacer = new ObjectPlacer(
            terrainMesh,
            mapSettings.terrainSettings.layerSettings,
            mapSettings.meshSettings.meshScale,
            mapSettings.terrainSettings.heightMultiplier,
            mapSettings.meshSettings.NumVertsPerLine,
            mapSettings.seed
        );

        SetLayer();
        ApplyWater();
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        terrainMesh.SetActive(visible);
    }

    private void SetLayer()
    {
        var layerName = "Default";
        int layerIndex = LayerMask.NameToLayer(layerName);

        if (layerIndex == -1)
        {
            Debug.LogError("Layer not found: " + layerName);
            return;
        }

        terrainMesh.layer = layerIndex;
    }

    public void AddMeshCollider()
    {
        meshCollider = terrainMesh.AddComponent<MeshCollider>();
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

    public void ApplyWater()
    {
        if (chunkCoord.x != 0 || chunkCoord.y != 0)
        {
            return;
        }
        if (mapSettings.terrainSettings.layerSettings.waterPlane == null)
        {
            return;
        }
        UnityEngine.GameObject waterPlane = UnityEngine.GameObject.Instantiate(mapSettings.terrainSettings.layerSettings.waterPlane);
        waterPlane.transform.parent = terrainMesh.transform;
        waterPlane.transform.localPosition = mapSettings.terrainSettings.layerSettings.WaterPlanePosition * mapSettings.terrainSettings.heightMultiplier * mapSettings.meshSettings.meshScale;
        waterPlane.transform.localScale = new Vector3(50, 1, 50);
    }

    public Vector2 WorldOffset
    {
        get
        {
            return chunkCoord * mapSettings.meshSettings.MeshWorldSize / mapSettings.meshSettings.meshScale;
        }
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
    readonly LODMesh[] lodMeshes;
    bool heightMapReceived;
    readonly int colliderLODIndex;
    bool hasSetCollider;
    private readonly HeightMapGenerator heightMapGenerator;
    public HeightMap heightMap;

    // A terrain chunk which updates its LOD based on the user's position.
    public DynamicTerrainChunk(
        Transform viewer,
        Vector2 coord,
        GameObject meshObject,
        MapSettings mapSettings,
        int colliderLODIndex,
        Material material,
        HeightMapGenerator heightMapGenerator
    ) : base(coord, meshObject, mapSettings, material)
    {
        this.viewer = viewer;
        this.colliderLODIndex = colliderLODIndex;
        this.heightMapGenerator = heightMapGenerator;

        // position in 3d space
        Vector2 position = coord * mapSettings.meshSettings.MeshWorldSize;
        bounds = new Bounds(position, Vector2.one * mapSettings.meshSettings.MeshWorldSize);

        AddMeshCollider();

        lodMeshes = new LODMesh[mapSettings.detailLevels.Length];
        for (int i = 0; i < mapSettings.detailLevels.Length; i++)
        {
            lodMeshes[i] = new LODMesh(mapSettings.detailLevels[i].lod);
            lodMeshes[i].UpdateCallback += UpdateTerrainChunk;
            // TODO this seems like a bug, should be i < colliderLODIndex
            // if (i == colliderLODIndex)
            // {
            lodMeshes[i].UpdateCallback += UpdateCollisionMesh;
            // }
        }

        maxViewDst = mapSettings.detailLevels[^1].visibleDstThreshold;
        heightMapOffset = chunkCoord * mapSettings.meshSettings.MeshWorldSize / mapSettings.meshSettings.meshScale;
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

        UpdateTerrainChunk();
    }

    // Called during the Update loop in WorldBuilder.
    // Show, update the level of detail, or hide the terrain chunk based on the viewer's position
    public void UpdateTerrainChunk()
    {
        if (!heightMapReceived)
        {
            Debug.LogWarning("height map not received");
            return;
        }

        Vector2 viewerPosition = ViewerPosition();

        float viewerDstFromNearestEdge = Mathf.Sqrt(
            bounds.SqrDistance(viewerPosition)
        );

        bool wasVisible = IsVisible();
        bool visible = viewerDstFromNearestEdge <= maxViewDst;

        if (visible)
        {
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
                    Debug.LogFormat("Check Object Data {0}", lodMesh.mesh.vertices.Length);
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

        if (sqrDstFromViewerToEdge < mapSettings.detailLevels[colliderLODIndex].SqrVisibleDstThreshold)
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
                this.meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                hasSetCollider = true;
            }
        }
    }

    public void UpdateObjects()
    {
        LODMesh lodMesh = lodMeshes[0];
        if (lodMesh.hasMesh)
        {
            // Debug.LogFormat("mesh {0}", lodMesh.mesh);
            // Debug.LogFormat("matrix {0}", terrainMesh.transform.localToWorldMatrix);
            objectPlacer.CheckAndLoadObjectData(lodMesh.mesh.vertices, terrainMesh.transform.localToWorldMatrix);
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
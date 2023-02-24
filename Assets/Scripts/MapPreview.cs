using UnityEngine;

public class MapPreview : MonoBehaviour
{
    public Map.DrawMode drawMode;

    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int editorPreviewLOD;
    public bool autoUpdate;

    public Renderer textureRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public MapSettings mapSettings;

    public MeshSettings meshSettings;
    public TextureData textureData;

    public Material terrainMaterial;

    public void DrawMapInEditor()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, mapSettings.minHeight, mapSettings.maxHeight);

        if (drawMode == Map.DrawMode.NoiseMap)
        {
            int heightMapSize;
            if (mapSettings.borderType == Map.BorderType.Infinite)
            {
                heightMapSize = meshSettings.numVertsPerLine;
            }
            else
            {
                heightMapSize = meshSettings.numVertsPerLine * mapSettings.fixedSize;
            }
            HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(heightMapSize, heightMapSize, mapSettings.noiseSettings, mapSettings.heightCurve, mapSettings.heightMultiplier, Vector2.zero, mapSettings.useFalloff);
            Texture2D texture = TextureGenerator.TextureFromHeightMap(heightMap);
            DrawTexture(texture);
        }
        else if (drawMode == Map.DrawMode.Preview)
        {
            MeshGenerator.GeneratePreview(meshSettings, mapSettings);
        }
        else if (drawMode == Map.DrawMode.TerrainGenerator)
        {
            HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, mapSettings.noiseSettings, mapSettings.heightCurve, mapSettings.heightMultiplier, Vector2.zero, mapSettings.useFalloff);
            MeshData meshData = MeshGenerator.GenerateTerrainChunkMesh(heightMap.values, meshSettings, editorPreviewLOD);
            DrawMesh(meshData);
        }

    }

    public void DrawTexture(Texture2D texture)
    {
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;

        textureRender.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    public void DrawMesh(MeshData meshData)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();

        textureRender.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }

    //public void DrawSimpleMesh(SimpleMeshData meshData, Texture2D texture)
    public void DrawSimpleMesh(SimpleMeshData meshData)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        textureRender.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
        //meshRenderer.sharedMaterial.mainTexture = texture;
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    void OnValidate()
    {

        if (meshSettings != null)
        {
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (mapSettings != null)
        {
            mapSettings.OnValuesUpdated -= OnValuesUpdated;
            mapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }

}
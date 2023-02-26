using UnityEngine;

public class MapPreview : MonoBehaviour
{
    public Map.DrawMode drawMode;

    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int editorPreviewLOD;
    public bool autoUpdate;

    // GameObject wrapper around a bunch of TerrainChunks used to preview map
    public GameObject previewTerrain;
    public Renderer previewTexture;
    public MeshFilter previewMeshFilter;
    public MeshRenderer previewMeshRenderer;

    public MapSettings mapSettings;
    public MeshSettings meshSettings;
    public TextureData textureData;

    public Material terrainMaterial;


    // Update the preview, or prepare the terrain mesh for procedural generation
    public void DrawMapInEditor()
    {
        this.Reset();

        if (drawMode == Map.DrawMode.NoiseMap)
        {
            previewTexture.gameObject.SetActive(true);

            int heightMapSize;
            if (mapSettings.borderType == Map.BorderType.Infinite)
            {
                heightMapSize = meshSettings.numVertsPerLine;
            }
            else
            {
                heightMapSize = meshSettings.numVertsPerLine * mapSettings.fixedSize;
            }
            HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(
                heightMapSize,
                heightMapSize,
                mapSettings.noiseSettings,
                mapSettings.heightCurve,
                mapSettings.heightMultiplier,
                Vector2.zero,
                mapSettings.seed);
            Texture2D texture = TextureGenerator.TextureFromHeightMap(heightMap);
            DrawTexture(texture);
        }
        else if (drawMode == Map.DrawMode.Terrain)
        {
            textureData.ApplyToMaterial(terrainMaterial);
            textureData.UpdateMeshHeights(terrainMaterial, mapSettings.minHeight, mapSettings.maxHeight);
            previewTerrain.SetActive(true);
            TerrainGenerator.GeneratePreview(textureData, meshSettings, mapSettings, terrainMaterial, previewTerrain.transform);
        }
        else if (drawMode == Map.DrawMode.Play)
        {
            textureData.ApplyToMaterial(terrainMaterial);
            textureData.UpdateMeshHeights(terrainMaterial, mapSettings.minHeight, mapSettings.maxHeight);
            HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(
                meshSettings.numVertsPerLine,
                meshSettings.numVertsPerLine,
                mapSettings.noiseSettings,
                mapSettings.heightCurve,
                mapSettings.heightMultiplier,
                Vector2.zero,
                mapSettings.seed);
            MeshData meshData = MeshGenerator.GetTerrainChunkMesh(heightMap.values, meshSettings, editorPreviewLOD);
            DrawMesh(meshData);
        }

    }

    // Reset all the preview objects, clear out memory
    private void Reset()
    {
        previewTerrain.SetActive(false);
        previewTexture.gameObject.SetActive(false);
        previewMeshFilter.gameObject.SetActive(false);
        while (previewTerrain.transform.childCount > 0)
        {
            DestroyImmediate(previewTerrain.transform.GetChild(0).gameObject);
        }
    }

    public void DrawTexture(Texture2D texture)
    {
        previewTexture.sharedMaterial.mainTexture = texture;
        previewTexture.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;

        previewTexture.gameObject.SetActive(true);
        previewMeshFilter.gameObject.SetActive(false);
    }

    public void DrawMesh(MeshData meshData)
    {
        previewMeshFilter.sharedMesh = meshData.CreateMesh();

        previewTexture.gameObject.SetActive(false);
        previewMeshFilter.gameObject.SetActive(true);
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
            //meshSettings.OnValuesUpdated -= OnValuesUpdated;
            //meshSettings.OnValuesUpdated += OnValuesUpdated;
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
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
    // public TextureSettings textureData;

    public Material terrainMaterial;


    // Update the preview, or prepare the terrain mesh for procedural generation
    public void DrawMapInEditor()
    {
        this.Reset();
        HeightMapGenerator heightMapGenerator = new HeightMapGenerator(mapSettings.noiseSettings, mapSettings.heightCurve, mapSettings.heightMultiplier, mapSettings.seed);

        if (drawMode == Map.DrawMode.NoiseMap)
        {
            previewTexture.gameObject.SetActive(true);

            int heightMapSize;
            if (mapSettings.borderType == Map.BorderType.Infinite)
            {
                heightMapSize = mapSettings.meshSettings.numVertsPerLine;
            }
            else
            {
                heightMapSize = mapSettings.meshSettings.numVertsPerLine * mapSettings.fixedSize;
            }
            Debug.LogFormat("Height {0}", heightMapSize);

            HeightMap heightMap = heightMapGenerator.BuildHeightMap(
                heightMapSize,
                heightMapSize,
                //mapSettings.noiseSettings,
                //mapSettings.heightCurve,
                //mapSettings.heightMultiplier,
                Vector2.zero
            //mapSettings.seed
            );
            Texture2D texture = TextureGenerator.TextureFromHeightMap(heightMap);
            DrawTexture(texture);
        }
        else if (drawMode == Map.DrawMode.Terrain)
        {
            mapSettings.textureSettings.ApplyToMaterial(terrainMaterial);
            mapSettings.textureSettings.UpdateMeshHeights(terrainMaterial, mapSettings.minHeight, mapSettings.maxHeight);
            previewTerrain.SetActive(true);
            TerrainGenerator.GeneratePreview(mapSettings.textureSettings, mapSettings.meshSettings, mapSettings, terrainMaterial, previewTerrain.transform);
        }
        else if (drawMode == Map.DrawMode.Play)
        {
            mapSettings.textureSettings.ApplyToMaterial(terrainMaterial);
            mapSettings.textureSettings.UpdateMeshHeights(terrainMaterial, mapSettings.minHeight, mapSettings.maxHeight);
            HeightMap heightMap = heightMapGenerator.BuildHeightMap(
                mapSettings.meshSettings.numVertsPerLine,
                mapSettings.meshSettings.numVertsPerLine,
                //mapSettings.noiseSettings,
                //mapSettings.heightCurve,
                //mapSettings.heightMultiplier,
                Vector2.zero
            //mapSettings.seed
            );
            MeshData meshData = MeshGenerator.GetTerrainChunkMesh(heightMap.values, mapSettings.meshSettings, editorPreviewLOD);
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
    }

    public void DrawMesh(MeshData meshData)
    {
        previewMeshFilter.sharedMesh = meshData.CreateMesh();
        previewMeshFilter.gameObject.SetActive(true);
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    // void OnTextureValuesUpdated()
    // {
    //     mapSettings.textureSettings.ApplyToMaterial(terrainMaterial);
    // }

    void OnValidate()
    {

        if (mapSettings != null)
        {
            mapSettings.OnValuesUpdated -= OnValuesUpdated;
            mapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        // if (textureData != null)
        // {
        //     textureData.OnValuesUpdated -= OnTextureValuesUpdated;
        //     textureData.OnValuesUpdated += OnTextureValuesUpdated;
        // }
    }

}
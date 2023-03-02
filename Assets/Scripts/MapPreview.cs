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
    public Material terrainMaterial;


    // Update the preview, or prepare the terrain mesh for procedural generation
    public void DrawMapInEditor()
    {
        this.Reset();
        Biome heightMapGenerator = new(
            mapSettings.biome,
            mapSettings.seed
        );

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

            HeightMap heightMap = heightMapGenerator.BuildHeightMap(
                heightMapSize,
                heightMapSize,
                Vector2.zero
            );
            Texture2D texture = TextureGenerator.TextureFromHeightMap(heightMap);
            DrawTexture(texture);
        }
        else if (drawMode == Map.DrawMode.Terrain)
        {
            mapSettings.textureSettings.ApplyToMaterial(terrainMaterial);
            mapSettings.textureSettings.UpdateMeshHeights(terrainMaterial, mapSettings.MinHeight, mapSettings.MaxHeight);
            previewTerrain.SetActive(true);
            WorldBuilder.GeneratePreview(
                // mapSettings.textureSettings,
                mapSettings.meshSettings,
                mapSettings,
                terrainMaterial,
                previewTerrain.transform
            );
        }
        else if (drawMode == Map.DrawMode.Play)
        {
            mapSettings.textureSettings.ApplyToMaterial(terrainMaterial);
            mapSettings.textureSettings.UpdateMeshHeights(terrainMaterial, mapSettings.MinHeight, mapSettings.MaxHeight);
            HeightMap heightMap = heightMapGenerator.BuildHeightMap(
                mapSettings.meshSettings.numVertsPerLine,
                mapSettings.meshSettings.numVertsPerLine,
                Vector2.zero
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

    void OnValidate()
    {

        if (mapSettings != null)
        {
            mapSettings.OnValuesUpdated -= OnValuesUpdated;
            mapSettings.OnValuesUpdated += OnValuesUpdated;
            if (mapSettings.biome != null)
            {
                Debug.Log("BiomeSettings OnValuesUpdated");
                mapSettings.biome.OnValuesUpdated -= OnValuesUpdated;
                mapSettings.biome.OnValuesUpdated += OnValuesUpdated;
            }
        }
    }

}
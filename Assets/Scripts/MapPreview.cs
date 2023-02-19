using UnityEngine;

public class MapPreview : MonoBehaviour
{

    public enum DrawMode { NoiseMap, Terrain };
    public DrawMode drawMode;
    public enum MapSize { Fixed, Infinite }
    public MapSize mapSize;

    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int editorPreviewLOD;
    public bool autoUpdate;

    public Renderer textureRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public InfiniteMapSettings infiniteMapSettings;
    public FixedMapSettings fixedMapSettings;


    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;

    public Material terrainMaterial;

    public void DrawMapInEditor()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        if (mapSize == MapSize.Fixed)
        {
            if (drawMode == DrawMode.NoiseMap)
            {
                // TODO fix bug where width/height are limited to mesh width
                HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(fixedMapSettings.width, fixedMapSettings.height, heightMapSettings, Vector2.zero, fixedMapSettings.useFalloff);
                Texture2D texture = TextureGenerator.TextureFromHeightMap(heightMap);
                DrawTexture(texture);
            }
            else if (drawMode == DrawMode.Terrain)
            {
                // TODO draw each possible terrain chunk based on the map size
                //int mapSize = (int)Mathf.Ceil(meshSettings.meshWorldSize);
                //HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(mapSize, mapSize, heightMapSettings, Vector2.zero);
                //MeshData meshData = MeshGenerator.GenerateHeightMapMesh(heightMap);
                //DrawMesh(meshData);
            }
        }
        else if (mapSize == MapSize.Infinite)
        {
            if (drawMode == DrawMode.NoiseMap)
            {
                HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero, infiniteMapSettings.useFalloffPerChunk);
                Texture2D texture = TextureGenerator.TextureFromHeightMap(heightMap);
                DrawTexture(texture);
            }
            else if (drawMode == DrawMode.Terrain)
            {
                HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero, infiniteMapSettings.useFalloffPerChunk);
                MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLOD);
                DrawMesh(meshData);
            }
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
        if (heightMapSettings != null)
        {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
        if (infiniteMapSettings != null)
        {
            infiniteMapSettings.OnValuesUpdated -= OnValuesUpdated;
            infiniteMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (fixedMapSettings != null)
        {
            fixedMapSettings.OnValuesUpdated -= OnValuesUpdated;
            fixedMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
    }

}
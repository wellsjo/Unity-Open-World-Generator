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

        if (mapSettings.borderType == Map.BorderType.Fixed)
        {
            int heightMapSize = meshSettings.numVertsPerLine * mapSettings.fixedSize;

            if (drawMode == Map.DrawMode.NoiseMap)
            {
                HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(heightMapSize, heightMapSize, mapSettings.noiseSettings, mapSettings.heightCurve, mapSettings.heightMultiplier, Vector2.zero, mapSettings.useFalloff);
                Texture2D texture = TextureGenerator.TextureFromHeightMap(heightMap);
                DrawTexture(texture);
            }
            else if (drawMode == Map.DrawMode.Preview)
            {
                // TODO check if it is 250 or less
                // TODO remove this, it's not good because it requires using the max mesh size
                HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(heightMapSize, heightMapSize, mapSettings.noiseSettings, mapSettings.heightCurve, mapSettings.heightMultiplier, Vector2.zero, mapSettings.useFalloff);
                SimpleMeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap.values);
                DrawSimpleMesh(meshData);
            }
            else if (drawMode == Map.DrawMode.TerrainGenerator)
            {
                Debug.Log("verts per line");
                Debug.Log(meshSettings.numVertsPerLine);
                //HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(heightMapSize, heightMapSize, mapSettings.noiseSettings, mapSettings.heightCurve, mapSettings.heightMultiplier, Vector2.zero, mapSettings.useFalloff);
                //MeshGenerator.GenerateTerrainChunkMesh();

                // Maybe show noise map here?
            }
        }
        else if (mapSettings.borderType == Map.BorderType.Infinite)
        {
            if (drawMode == Map.DrawMode.NoiseMap)
            {
                HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, mapSettings.noiseSettings, mapSettings.heightCurve, mapSettings.heightMultiplier, Vector2.zero, mapSettings.useFalloff);
                Texture2D texture = TextureGenerator.TextureFromHeightMap(heightMap);
                DrawTexture(texture);
            }
            else if (drawMode == Map.DrawMode.Preview)
            {
                // Render 9 chunks instead of 1
                /*                HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine * 9, meshSettings.numVertsPerLine * 9, mapSettings.noiseSettings, mapSettings.heightCurve, mapSettings.heightMultiplier, Vector2.zero, mapSettings.useFalloff);
                                for (int x = -1; x <= 1; x++)
                                {
                                    for (int y = -1; y <= 1; y++)
                                    {
                                        int indexX = (x + 1) * meshSettings.numVertsPerLine;
                                        int indexY = (y + 1) * meshSettings.numVertsPerLine;
                                        float[,] heightMapValues = new float[meshSettings.numVertsPerLine, meshSettings.numVertsPerLine];
                                        for (int i = 0; i < meshSettings.numVertsPerLine; i++)
                                        {
                                            for (int j = 0; j < meshSettings.numVertsPerLine; j++)
                                            {
                                                heightMapValues[i, j] = heightMap.values[indexX + i, indexY + j];
                                            }
                                        }

                                        MeshData meshData = MeshGenerator.GenerateTerrainChunkMesh(heightMapValues, meshSettings, editorPreviewLOD);
                                        DrawMesh(meshData);
                                    }
                                }*/
            }
            else if (drawMode == Map.DrawMode.TerrainGenerator)
            {
                HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, mapSettings.noiseSettings, mapSettings.heightCurve, mapSettings.heightMultiplier, Vector2.zero, mapSettings.useFalloff);
                MeshData meshData = MeshGenerator.GenerateTerrainChunkMesh(heightMap.values, meshSettings, editorPreviewLOD);
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
            //meshSettings.OnValuesUpdated -= OnValuesUpdated;
            //meshSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (mapSettings != null)
        {
            //mapSettings.OnValuesUpdated -= OnValuesUpdated;
            //mapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }

}
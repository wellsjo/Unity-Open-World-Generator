using System.Collections.Generic;
using UnityEngine;

public class MapPreview : MonoBehaviour
{
    public Map.DrawMode drawMode;

    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int editorPreviewLOD;
    public bool autoUpdate;
    public bool spawnVegetation;

    // GameObject wrapper around a bunch of TerrainChunks used to preview map
    public GameObject previewTerrain;
    public Renderer previewTexture;
    public MapSettings mapSettings;
    public Material terrainMaterial;


    // Update the preview, or prepare the terrain mesh for procedural generation
    public void DrawMapInEditor()
    {
        this.Reset();

        Random.InitState(mapSettings.seed);

        if (drawMode == Map.DrawMode.NoiseMap)
        {
            previewTexture.gameObject.SetActive(true);

            int heightMapSize;
            if (mapSettings.borderType == Map.BorderType.Infinite)
            {
                heightMapSize = mapSettings.meshSettings.NumVertsPerLine;
            }
            else
            {
                heightMapSize = mapSettings.meshSettings.NumVertsPerLine * mapSettings.fixedSize;
            }

            HeightMapGenerator heightMapGenerator = new(
                mapSettings.biomeSettings.terrainSettings,
                heightMapSize,
                heightMapSize,
                mapSettings.seed
            );

            HeightMap heightMap = heightMapGenerator.BuildHeightMap(
                Vector2.zero
            );
            Texture2D texture = TextureGenerator.TextureFromHeightMap(heightMap);
            DrawTexture(texture);
        }
        else if (drawMode == Map.DrawMode.Terrain)
        {
            mapSettings.biomeSettings.textureSettings.ApplyToMaterial(terrainMaterial);
            mapSettings.biomeSettings.textureSettings.UpdateMeshHeights(terrainMaterial, mapSettings.MinHeight, mapSettings.MaxHeight);
            previewTerrain.SetActive(true);

            GeneratePreview(
                spawnVegetation,
                mapSettings,
                terrainMaterial,
                previewTerrain.transform
            );
        }

    }
    public static void GeneratePreview(
        bool spawnVegetation,
        MapSettings mapSettings,
        Material mapMaterial,
        Transform terrainChunkParent
    )
    {
        HeightMapGenerator terrainChunkHeightMapGenerator = new(
            mapSettings.biomeSettings.terrainSettings,
            mapSettings.meshSettings.NumVertsPerLine,
            mapSettings.meshSettings.NumVertsPerLine,
            mapSettings.seed
        );

        // Default to something reasonable for infinite view
        // TODO make this a map preview option
        Vector2 range = new(0, 0);
        if (mapSettings.borderType == Map.BorderType.Fixed)
        {
            range = mapSettings.Range;
        }

        for (int x = (int)range.x; x <= range.y; x++)
        {
            for (int y = (int)range.x; y <= range.y; y++)
            {
                // TODO move this to biome.Spawn(chunkCoord)
                Vector2 chunkCoord = new(x, y);
                string gameObjectName = string.Format("Terrain Chunk {0}", chunkCoord.ToString());

                // Make a new terrain chunk under the Terrain Preview parent
                GameObject terrainChunkObject = new(gameObjectName);
                terrainChunkObject.transform.parent = terrainChunkParent;

                TerrainChunk newChunk = new(
                    chunkCoord,
                    terrainChunkObject,
                    mapSettings,
                    mapMaterial
                );

                Vector2 sampleCenter = chunkCoord * mapSettings.meshSettings.MeshWorldSize / mapSettings.meshSettings.meshScale;
                HeightMap heightMap = terrainChunkHeightMapGenerator.BuildHeightMap(sampleCenter);

                newChunk.LoadFromHeightMap(heightMap);
                newChunk.SetVisible(true);

                if (!spawnVegetation)
                {
                    continue;
                }

                List<ObjectPlacement> vegetationMap = VegetationGenerator.BuildVegetationMap(
                    mapSettings.biomeSettings.textureSettings.layers[1].layerObjectSettings,
                    mapSettings.meshSettings.NumVertsPerLine,
                    newChunk.meshFilter.sharedMesh.vertices
                );

                newChunk.LoadVegetationFromMap(vegetationMap);
            }
        }

    }

    // Reset all the preview objects, clear out memory
    private void Reset()
    {
        previewTerrain.SetActive(false);
        previewTexture.gameObject.SetActive(false);
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
            if (mapSettings.biomeSettings != null)
            {
                mapSettings.biomeSettings.OnValuesUpdated -= OnValuesUpdated;
                mapSettings.biomeSettings.OnValuesUpdated += OnValuesUpdated;
            }
        }
    }

}
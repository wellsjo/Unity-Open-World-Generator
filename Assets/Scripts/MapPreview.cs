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
            mapSettings.textureSettings.ApplyToMaterial(terrainMaterial);
            mapSettings.textureSettings.UpdateMeshHeights(terrainMaterial, mapSettings.MinHeight, mapSettings.MaxHeight);
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
            mapSettings.meshSettings.numVertsPerLine,
            mapSettings.meshSettings.numVertsPerLine,
            mapSettings.seed
        );

        // Default to something reasonable for infinite view
        // TODO make this a map preview option
        Vector2 range = new(0, 0);
        if (mapSettings.borderType == Map.BorderType.Fixed)
        {
            range = mapSettings.range;
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

                Vector2 sampleCenter = chunkCoord * mapSettings.meshSettings.meshWorldSize / mapSettings.meshSettings.meshScale;
                HeightMap heightMap = terrainChunkHeightMapGenerator.BuildHeightMap(sampleCenter);

                newChunk.LoadFromHeightMap(heightMap);
                newChunk.SetVisible(true);

                if (!spawnVegetation)
                {
                    continue;
                }

                // List<Vector3> treePositions = new();
                VegetationGenerator vegetationGenerator = new(
                    mapSettings.biomeSettings.vegetationSettings,
                    mapSettings.meshSettings.numVertsPerLine,
                    mapSettings.meshSettings.numVertsPerLine,
                    mapSettings.seed
                );

                List<Vector3> vegetationMap = vegetationGenerator.BuildVegetationMap(sampleCenter, mapSettings.meshSettings.numVertsPerLine, newChunk.meshFilter.sharedMesh.vertices);
                newChunk.LoadVegetationFromMap(vegetationMap);

                // foreach (Vector3 vegetationValue in vegetationMap)
                // {
                //     Debug.Log("Spawning Tree");
                //     UnityEngine.GameObject tree = UnityEngine.GameObject.Instantiate(
                //         mapSettings.biomeSettings.vegetationSettings.treePrefab
                //     );
                //     tree.transform.position = vegetationValue;
                //     tree.transform.parent = terrainChunkObject.transform;
                // }

                // for (int i = 0; i < mesh.vertices.Length; i++)
                // {
                //     Vector3 worldPosVertex = mesh.vertices[i];
                //     Vector3 worldPos = newChunk.gameObject.transform.TransformPoint(worldPosVertex);
                //     if (Random.Range(0, 10) == 1)
                //     {
                //         GameObject tree = Instantiate(mapSettings.biomeSettings.vegetationSettings.treePrefab, worldPos, Quaternion.identity);
                //         tree.transform.parent = terrainChunkObject.transform;
                //         tree.transform.position = worldPos;
                //     }
                // }
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
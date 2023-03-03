using System.CodeDom.Compiler;
using UnityEngine;

public class Biome
{
    readonly Noise noise;
    readonly BiomeSettings settings;
    public Biome(BiomeSettings settings, int seed)
    {

        this.noise = new Noise(seed);
        this.settings = settings;
    }
    public HeightMap BuildHeightMap(
        int width,
        int height,
        Vector2 sampleCenter
    )
    {
        float[,] values = noise.Generate(
            width,
            height,
            sampleCenter,
            settings.terrainSettings.noiseSettings
        );
        AnimationCurve heightCurve_threadsafe = new(settings.terrainSettings.heightCurve.keys);

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j]) * settings.terrainSettings.heightMultiplier;

                if (values[i, j] > maxValue)
                {
                    maxValue = values[i, j];
                }
                if (values[i, j] < minValue)
                {
                    minValue = values[i, j];
                }
            }
        }

        return new HeightMap(values, minValue, maxValue);
    }

    // public void SpawnVegetation(
    // HeightMap heightMap,
    // Vector2 sampleCenter,
    // Transform parent
    // )
    // {
    //     float[,] noiseMap = noise.Generate(
    //         heightMap.width,
    //         heightMap.height,
    //         sampleCenter,
    //         settings.vegetationSettings.noiseSettings
    //     );

    //     float vegetationStartHeight = settings.vegetationSettings.startHeight;
    //     float vegetationEndHeight = settings.vegetationSettings.endHeight;

    //     for (int i = 0; i < heightMap.width; i++)
    //     {
    //         for (int j = 0; j < heightMap.height; j++)
    //         {
    //             if (heightMap[i, j] > vegetationStartHeight && heightMap[i, j] < vegetationEndHeight)
    //             {
    //                 if (heightMap.values[i, j] > settings.endDistance)
    //                 {
    //                     float x = i + sampleCenter.x - heightMap.width / 2f;
    //                     float y = heightMap.values[i, j];
    //                     float z = j + sampleCenter.y - heightMap.height / 2f;

    //                     Vector3 position = new(x, y, z);

    //                     GameObject vegetation = Instantiate(settings.vegetationSettings.vegetationPrefab, position, Quaternion.identity, parent);
    //                     vegetation.transform.localScale = Vector3.one * noiseMap[i, j];
    //                 }
    //             }
    //         }
    //     }
    // }
}

// Useful for updating mesh in thread
public struct HeightMapUpdateData
{
    public HeightMap heightMap;
    public Vector2 viewerPosition;
    public HeightMapUpdateData(HeightMap heightMap, Vector2 viewerPosition)
    {
        this.heightMap = heightMap;
        this.viewerPosition = viewerPosition;
    }
}

public struct HeightMap
{
    public readonly float[,] values;
    public readonly float minValue;
    public readonly float maxValue;
    public readonly int width;
    public readonly int height;

    public HeightMap(float[,] values, float minValue, float maxValue)
    {
        this.values = values;
        this.minValue = minValue;
        this.maxValue = maxValue;
        this.width = values.GetLength(0);
        this.height = values.GetLength(1);
    }
}
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.CodeDom.Compiler;
using UnityEngine;

// Used by WorldBuilder to generate height maps for terrain chunks.
public class HeightMapGenerator : NoiseGenerator
{
    public TerrainSettings settings;
    readonly int width;
    readonly int height;
    public HeightMapGenerator(
        TerrainSettings settings,
        int width,
        int height,
        int seed
    ) : base(seed)
    {
        this.settings = settings;
        this.width = width;
        this.height = height;
    }
    public HeightMap BuildTerrainHeightMap(
        Vector2 sampleCenter
    )
    {
        float[,] values = this.Generate(
            this.width,
            this.height,
            sampleCenter,
            settings.noiseSettings
        );

        AnimationCurve heightCurve_threadsafe = new(settings.heightCurve.keys);

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j]) * settings.heightMultiplier;

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
}

// Useful for updating mesh in thread
// public struct HeightMapUpdateData
// {
//     public HeightMap heightMap;
//     public Vector2 viewerPosition;
//     public HeightMapUpdateData(HeightMap heightMap, Vector2 viewerPosition)
//     {
//         this.heightMap = heightMap;
//         this.viewerPosition = viewerPosition;
//     }
// }

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
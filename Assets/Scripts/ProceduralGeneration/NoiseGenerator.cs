using System;
using Palmmedia.ReportGenerator.Core;
using UnityEngine;

public class NoiseGenerator
{
    private readonly int seed;
    public NoiseGenerator(int seed)
    {
        this.seed = seed;
    }

    public float[,] Generate(
        int width,
        int height,
        Vector2 offset,
        NoiseSettings noiseSettings
    )
    {
        float[,] noiseMap = new float[width, height];

        System.Random rng = new(seed);

        Vector2[] octaveOffsets = new Vector2[noiseSettings.octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        for (int i = 0; i < noiseSettings.octaves; i++)
        {
            // this range -100000,100000 gives best random numbers from testing
            // adding and subtracting the center makes the tiles not repeat
            float offsetX = rng.Next(-100000, 100000) + noiseSettings.startingOffset.x + offset.x;
            float offsetY = rng.Next(-100000, 100000) - noiseSettings.startingOffset.y - offset.y;

            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= noiseSettings.persistance;
        }

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {

                amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < noiseSettings.octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / noiseSettings.scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / noiseSettings.scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= noiseSettings.persistance;
                    frequency *= noiseSettings.lacunarity;
                }

                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }
                if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight;

                // TODO maybe only need this for terrain
                float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f);
                noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
            }
        }

        return noiseMap;
    }

}

[System.Serializable]
public class NoiseSettings
{
    public Vector2 startingOffset;
    public float persistance;
    public int octaves;
    public float scale;
    public float lacunarity;
}
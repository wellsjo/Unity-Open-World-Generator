using Palmmedia.ReportGenerator.Core;
using UnityEngine;

public class NoiseGenerator
{
    NoiseSettings noiseSettings;
    private int seed;
    public NoiseGenerator(NoiseSettings noiseSettings, int seed)
    {
        this.noiseSettings = noiseSettings;
        this.seed = seed;
    }

    public float[,] BuildNoiseMap(
        int width,
        int height,
        Vector2 offset
    )
    {
        float[,] noiseMap = new float[width, height];

        System.Random rng = new(seed);
        // TODO remove after determinig if different
        //Debug.LogFormat("RNG {0}", rng);

        Vector2[] octaveOffsets = new Vector2[noiseSettings.octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

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
                frequency = 1;
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
    //public NoiseLayer[] noiseLayers;
    public Vector2 startingOffset;
    public float persistance;
    //public float amplitude;
    public int octaves;
    public float scale;
    public float lacunarity;
}

/// <summary>
/// Noise layer.
/// </summary>
[System.Serializable]
public class NoiseLayer
{
    // Multiplier for noise.
    [SerializeField]
    private float noisePower = 1;
    // Noise offset.
    //[SerializeField]
    //private Vector2 noiseOffset;
    // Noise scale.
    [SerializeField]
    private float noiseScale = 1;
    /// <summary>
    /// Evalate value for X and Y coords.
    /// </summary>
    /// <returns>Returns value from noise.</returns>
    /// <param name="x">The x coordinate (0.0-1.0).</param>
    /// <param name="y">The y coordinate (0.0-1.0).</param>
    public float Evalate(float x, float y, Vector2 offset)
    {
        // Adding elevation from perlin noise.
        float noiseXCoord = offset.x + x * noiseScale;
        float noiseYCoord = y * noiseScale - offset.y;
        return (Mathf.PerlinNoise(noiseXCoord, noiseYCoord) - 0.5f) * noisePower;
    }
}
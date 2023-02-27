using UnityEngine;

public class HeightMapGenerator
{
    NoiseGenerator noiseGenerator;
    AnimationCurve heightCurve;
    int heightMultiplier;
    public HeightMapGenerator(NoiseSettings noiseSettings, AnimationCurve heightCurve, int heightMultiplier, int seed)
    {

        this.noiseGenerator = new NoiseGenerator(noiseSettings, seed);
        this.heightCurve = heightCurve;
        this.heightMultiplier = heightMultiplier;
    }
    public HeightMap BuildHeightMap(
        int width,
        int height,
        Vector2 sampleCenter
    )
    {
        float[,] values = noiseGenerator.BuildNoiseMap(width, height, sampleCenter);
        AnimationCurve heightCurve_threadsafe = new(heightCurve.keys);

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j]) * heightMultiplier;

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
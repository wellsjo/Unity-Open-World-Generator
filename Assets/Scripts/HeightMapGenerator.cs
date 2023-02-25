using UnityEngine;

public static class HeightMapGenerator
{
    // TODO move this away from this class, and remove the if statement below
    static float[,] falloffMap;

    public static HeightMap GenerateHeightMap(int width, int height, NoiseSettings noiseSettings, AnimationCurve heightCurve, float heightMultiplier, Vector2 sampleCentre, bool useFalloff)
    {
        //Debug.LogFormat("GenerateHeightMap {0}", sampleCentre);
        float[,] values = Noise.GenerateNoiseMap(width, height, noiseSettings, sampleCentre);

        AnimationCurve heightCurve_threadsafe = new(heightCurve.keys);

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float falloffAmount = useFalloff ? falloffMap[i, j] : 0;
                values[i, j] *= heightCurve_threadsafe.Evaluate(values[i, j] - (falloffAmount)) * heightMultiplier;

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
using System.Collections.Generic;
using UnityEngine;

public class VegetationGenerator : NoiseGenerator
{
    private readonly VegetationSettings vegetationSettings;
    private readonly int width;
    private readonly int height;
    public VegetationGenerator(
        VegetationSettings vegetationSettings,
        int width,
        int height,
        int seed
    ) : base(seed)
    {
        this.vegetationSettings = vegetationSettings;
        this.width = width;
        this.height = height;
    }

    public List<Vector3> BuildVegetationMap(Vector2 sampleCenter, float[,] heightMapValues)
    {
        float[,] values = this.Generate(
            this.width,
            this.height,
            sampleCenter,
            vegetationSettings.noiseSettings
        );

        List<Vector3> returnValues = new();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float height = heightMapValues[x, y];
                // Debug.LogFormat("Processing {0}, {1}", values[x, y], height);
                if (values[x, y] > vegetationSettings.noiseThreshold
                    && height > vegetationSettings.startHeight
                    && height < vegetationSettings.endHeight)
                {
                    returnValues.Add(new Vector3(x, heightMapValues[x, y], y));
                }
            }
        }

        return returnValues;
    }
}
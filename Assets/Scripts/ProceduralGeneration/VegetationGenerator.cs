using System.Collections.Generic;
using UnityEngine;

public static class VegetationGenerator
{
    public static List<ObjectPlacement> BuildVegetationMap(
        Layer[] layers,
        int numVertsPerLine,
        Vector3[] vertices,
        int seed
    )
    {
        // TODO fix object placement + LOD
        int levelOfDetail = 0;
        int skipIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        System.Random rng = new(seed);

        List<ObjectPlacement> returnValues = new();

        int vertexIndex = 0;

        for (int y = 0; y < numVertsPerLine; y++)
        {
            for (int x = 0; x < numVertsPerLine; x++)
            {
                bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);

                if (isOutOfMeshVertex)
                {
                    continue;
                }
                else if (isSkippedVertex)
                {
                    continue;
                }
                for (int i = 0; i < layers.Length; i++)
                {
                    LayerObjectSettings[] settings = layers[i].layerObjectSettings;
                    if (settings.Length == 0)
                    {
                        continue;
                    }

                    float[] weights = new float[settings.Length];
                    for (int j = 0; j < settings.Length; j++)
                    {
                        weights[j] = settings[j].density;
                    }

                    returnValues.Add(
                        new ObjectPlacement(
                            vertices[vertexIndex],
                            GetRandomWeightedIndex(weights, rng)
                        )
                    );
                }

                vertexIndex++;
            }
        }

        return returnValues;
    }

    public static int GetRandomWeightedIndex(float[] weights, System.Random rng)
    {
        if (weights == null || weights.Length == 0) return -1;

        float w;
        float total = 0f;
        int i;
        for (i = 0; i < weights.Length; i++)
        {
            w = weights[i];
            if (w >= 0f && !float.IsNaN(w)) total += weights[i];
        }

        // Get number between 0 and 1
        float r = (float)rng.Next(0, 100) / 100;
        float s = 0f;

        for (i = 0; i < weights.Length; i++)
        {
            w = weights[i];
            if (float.IsNaN(w) || w <= 0f) continue;

            s += w / total;
            if (s >= r) return i;
        }

        return -1;
    }
}

// Algorithm which takes a list of LayerObjectSettings, and returns a prefab based on the density of each layer.
public struct ObjectPlacement
{
    public Vector3 position;
    // public int layerIndex;
    public int prefabIndex;

    public ObjectPlacement(Vector3 position, int prefabIndex)
    {
        this.position = position;
        // this.layerIndex = layerIndex;
        this.prefabIndex = prefabIndex;
    }
}
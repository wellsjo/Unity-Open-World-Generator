using UnityEngine;

public static class HeightMapGenerator
{
    // TODO move this away from this class, and remove the if statement below
    static float[,] falloffMap;

    public static HeightMap GenerateHeightMap(int width, int height, NoiseSettings noiseSettings, AnimationCurve heightCurve, float heightMultiplier, Vector2 sampleCentre, bool useFalloff)
    {
        Debug.LogFormat("GenerateHeightMap {0}", sampleCentre);
        float[,] values = Noise.GenerateNoiseMap(width, height, noiseSettings, sampleCentre);

        AnimationCurve heightCurve_threadsafe = new AnimationCurve(heightCurve.keys);

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

// Never got this working, but keep around just in case
public class FixedHeightMap
{
    // Large, fixed height map to take subsections of on request
    HeightMap heightMap;

    // Ex: map of size 3 -> 3x3 -> chunk range -1, 1
    Vector2 chunkRange;
    readonly int numVertsPerChunk;

    public FixedHeightMap(int size, int numVertsPerChunk, NoiseSettings noiseSettings, AnimationCurve heightCurve, float heightMultiplier, bool useFalloff)
    {
        this.numVertsPerChunk = numVertsPerChunk;

        int heightMapSize = size * numVertsPerChunk;
        this.heightMap = HeightMapGenerator.GenerateHeightMap(heightMapSize, heightMapSize, noiseSettings, heightCurve, heightMultiplier, Vector2.zero, useFalloff);

        Debug.LogFormat("Creating FixedHeightMap Size {1} {0}x{0}", heightMapSize, size);

        int r = (int)Mathf.Floor((float)size / 2);
        this.chunkRange = new Vector2(-r, r);
    }

    public HeightMap GetHeightMapForChunkCoord(Vector2 chunkCoord)
    {
        float[,] values = new float[numVertsPerChunk, numVertsPerChunk];

        // for 3x3
        // -1, -1 --> 0, 0
        // 0, 0 --> numVertsPerChunk, numVertsPerChunk
        // we assume heightMap is a square and chunkRange applies to both x and y, which is why we use chunkRange.x
        int startX = ((int)chunkCoord.x + (int)chunkRange.y) * numVertsPerChunk;
        int startY = ((int)chunkCoord.y + (int)chunkRange.y) * numVertsPerChunk;
        int endX = startX + numVertsPerChunk;
        int endY = startY + numVertsPerChunk;
        int x = 0;
        int y = 0;
        int xIndex = 0;
        int yIndex = 0;
        //float minValue = float.MaxValue;
        //float maxValue = float.MinValue;

        Debug.LogFormat("Viewed Chunk Coord {2} Chunk Range {3} Start {0}, {1}", startX, startY, chunkCoord, chunkRange);

        for (y = endY - 1; y >= startY; y--)
        {
            xIndex = 0;
            for (x = endX - 1; x >= startX; x--)
            {
                float val = heightMap.values[y, x];
                values[xIndex, yIndex] = val;

                xIndex++;
            }
            yIndex++;
        }

        Debug.LogFormat("End coords {0}, {1} - {2}, {3}", xIndex, yIndex, x, y);

        return new HeightMap(values, heightMap.minValue, heightMap.maxValue);
    }

    public bool ChunkCoordInRange(Vector2 chunkCoord)
    {
        return (
            chunkCoord.x >= this.chunkRange.x
            && chunkCoord.x <= this.chunkRange.y
            && chunkCoord.y >= this.chunkRange.x
            && chunkCoord.y <= this.chunkRange.y
        );
    }
}
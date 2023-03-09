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

    public List<Vector3> BuildVegetationMap(Vector2 sampleCenter, int numVertsPerLine, Vector3[] vertices)
    {
        float[,] values = this.Generate(
            vertices.Length,
            vertices.Length,
            sampleCenter,
            vegetationSettings.noiseSettings
        );
        Debug.LogFormat("BuildVegetationMap {0} {1} {2}", vertices.Length, numVertsPerLine, numVertsPerLine * numVertsPerLine);

        int levelOfDetail = 0;
        int skipIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        List<Vector3> returnValues = new();
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

                if (Random.Range(0, 10) > 7)
                {
                    returnValues.Add(vertices[vertexIndex]);
                }

                vertexIndex++;
            }
        }

        Debug.LogFormat("Vegetation Vertices {0} {1}", vertexIndex, returnValues.Count);

        return returnValues;

        // for (int i = 0; i < vertices.Length; i++)
        // {
        //     Vector3 vertex = vertices[vertexIndex];

        // Vector3 worldPos = newChunk.gameObject.transform.TransformPoint(vertex);
        // GameObject tree = Instantiate(mapSettings.biomeSettings.vegetationSettings.treePrefab, worldPos, Quaternion.identity);
        // Vector3 worldPosVertex = terrainMesh.vertices[i];
        // tree.transform.parent = terrainChunkObject.transform;
        // tree.transform.position = worldPos;
    }
}
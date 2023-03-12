using System.Collections.Generic;
using UnityEngine;

// Calculates where objects should go, safe to use in threads.
public static class ObjectMapper
{
    // Take in a list of layers, determine where to place objects based on the layer's settings, return a list of ObjectPlacements.
    public static List<ObjectPlacement> BuildObjectMap(
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

                    Debug.LogFormat("vertices son {0} {1}", vertices.Length, vertexIndex);
                    returnValues.Add(
                        new ObjectPlacement(
                            vertices[vertexIndex],
                            RandomWeightedIndex.Get(weights, rng)
                        )
                    );
                }

                vertexIndex++;
            }
        }

        return returnValues;
    }
}

// Keeps track of the state of object request and spawn cycle.
public class ObjectPlacer
{
    private readonly GameObject terrainMesh;
    private readonly TextureSettings textureSettings;
    private readonly float heightMultiplier;
    private readonly Vector2 heightMapOffSet;
    private readonly int numVertsPerLine;

    private List<ObjectPlacement> objectPlacements;
    private bool objectsRequested = false;
    private readonly int seed;
    private bool done = false;
    public ObjectPlacer(
        GameObject terrainMesh,
        Vector2 heightMapOffSet,
        TextureSettings textureSettings,
        float heightMultiplier,
        int numVertsPerLine,
        int seed
    )
    {
        this.terrainMesh = terrainMesh;
        this.heightMapOffSet = heightMapOffSet;
        this.textureSettings = textureSettings;
        this.heightMultiplier = heightMultiplier;
        this.numVertsPerLine = numVertsPerLine;
        this.seed = seed;
    }

    public void CheckAndLoadObjectData(Vector3[] vertices)
    {
        if (done || objectsRequested)
        {
            return;
        }
        if (!objectsRequested)
        {
            LoadAsync(vertices);
            objectsRequested = true;
            return;
        }
        if (objectPlacements == null)
        {
            return;
        }
        PlaceObjects(objectPlacements);
        objectPlacements = null;
    }

    public void LoadAsync(Vector3[] vertices)
    {
        Debug.Log("Loading Vegetation Async");
        // Center of the height map on the game world
        // Vector3[] vertices = lodMeshes[0].mesh.vertices;
        ThreadedDataRequester.RequestData(() =>
        {
            return ObjectMapper.BuildObjectMap(
                textureSettings.layers,
                numVertsPerLine,
                vertices,
                seed
            );
        }, OnLoad);
    }

    private void OnLoad(object objectPlacementsList)
    {
        if (objectPlacementsList == null)
        {
            Debug.Log("Object Placement Map Null");
            return;
        }

        Debug.Log("Onload");

        List<ObjectPlacement> objectPlacements = (List<ObjectPlacement>)objectPlacementsList;
        Debug.LogFormat("Vegetation Map Received {0}", objectPlacements.Count);
        this.objectPlacements = objectPlacements;
    }

    public void PlaceObjects(List<ObjectPlacement> objectPlacements)
    {
        foreach (ObjectPlacement objectPlacement in objectPlacements)
        {
            if (objectPlacement.prefabIndex == -1)
            {
                continue;
            }

            Vector3 worldPos = terrainMesh.transform.TransformPoint(objectPlacement.position);

            for (int i = 0; i < textureSettings.layers.Length - 1; i++)
            {
                Layer layer = textureSettings.layers[i];
                Layer nextLayer = textureSettings.layers[i + 1];

                if (worldPos.y > layer.startHeight * heightMultiplier
                    && worldPos.y < nextLayer.startHeight * heightMultiplier)
                {
                    PlaceObject(objectPlacement, layer);
                }
            }

            Layer lastLayer = textureSettings.layers[^1];
            if (worldPos.y > lastLayer.startHeight * heightMultiplier)
            {
                PlaceObject(objectPlacement, lastLayer);
            }

        }
    }
    private void PlaceObject(
        ObjectPlacement obj,
        Layer layer
    )
    {
        if (obj.prefabIndex > layer.layerObjectSettings.Length - 1)
        {
            return;
        }
        if (obj.prefabIndex == -1)
        {
            return;
        }
        if (UnityEngine.Random.Range(0f, 1f) < (1 - layer.ObjectFrequency))
        {
            return;
        }

        UnityEngine.GameObject tree = UnityEngine.GameObject.Instantiate(layer.layerObjectSettings[obj.prefabIndex].prefab);
        tree.transform.parent = terrainMesh.transform;
        tree.transform.localPosition = obj.position;
    }
}

// Data to pass back to the main thread to spawn the objects.
public struct ObjectPlacement
{
    public Vector3 position;
    public int prefabIndex;

    public ObjectPlacement(Vector3 position, int prefabIndex)
    {
        this.position = position;
        this.prefabIndex = prefabIndex;
    }
}
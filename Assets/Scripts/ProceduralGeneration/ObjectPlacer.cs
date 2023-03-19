using System.Collections.Generic;
using UnityEngine;

// Calculates where objects should go, safe to use in threads.
public static class ObjectMapper
{
    // Take in a list of layers, determine where to place objects based on the layer's settings, return a list of ObjectPlacements.
}

// Keeps track of the state of object request and spawn cycle.
public class ObjectPlacer
{
    private readonly GameObject terrainMesh;
    private readonly LayerSettings layerSettings;
    private readonly float meshScale;
    private readonly float heightMultiplier;
    private readonly int numVertsPerLine;

    private List<ObjectPlacement> objectPlacements;
    private bool objectsRequested = false;
    private readonly int seed;
    private bool done = false;

    public ObjectPlacer(
        GameObject terrainMesh,
        LayerSettings layerSettings,
        float meshScale,
        float heightMultiplier,
        int numVertsPerLine,
        int seed
    )
    {
        this.terrainMesh = terrainMesh;
        this.layerSettings = layerSettings;
        this.meshScale = meshScale;
        this.heightMultiplier = heightMultiplier;
        this.numVertsPerLine = numVertsPerLine;
        this.seed = seed;
    }

    public void CheckAndLoadObjectData(Vector3[] vertices)
    {
        if (done)
        {
            return;
        }
        if (!objectsRequested)
        {
            Debug.Log("loading async");
            objectsRequested = true;
            LoadAsync(vertices);
            return;
        }
        PlaceObjects(objectPlacements);
        objectPlacements = null;
        done = true;
    }

    public void LoadAsync(Vector3[] vertices)
    {
        ThreadedDataRequester.RequestData(() =>
        {
            return BuildObjectMap(vertices);
        }, OnLoad);
    }

    // Needs to be thread safe
    public List<ObjectPlacement> BuildObjectMap(Vector3[] vertices)
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
                Debug.Log("Adding Object Placement 1");

                var vertex = vertices[vertexIndex];
                var layer = GetLayerForVertex(vertex);
                Debug.LogFormat("layer {0}", layer);

                ObjectSettings[] settings = layer.layerObjectSettings;
                if (settings.Length == 0)
                {
                    Debug.Log("No Settings");
                    continue;
                }

                if (UnityEngine.Random.Range(0f, 1f) < (1 - layer.ObjectFrequency))
                {
                    Debug.Log("Continuing Ransom");
                    continue;
                }

                float[] weights = new float[settings.Length];
                for (int j = 0; j < settings.Length; j++)
                {
                    weights[j] = settings[j].density;
                }

                var objectIndex = RandomWeightedIndex.Get(weights, rng);
                Debug.Log("Adding Object Placement 2");
                returnValues.Add(
                    new ObjectPlacement(vertex, objectIndex)
                );

                vertexIndex++;
            }
        }

        return returnValues;
    }

    private Layer GetLayerForVertex(Vector3 vertex)
    {
        for (int i = 0; i < layerSettings.layers.Length - 1; i++)
        {
            Layer layer = layerSettings.layers[i];
            Layer nextLayer = layerSettings.layers[i + 1];

            if (vertex.y > layer.startHeight * heightMultiplier * meshScale
                && vertex.y < nextLayer.startHeight * heightMultiplier * meshScale)
            {
                Debug.LogFormat("Found Layer {0}", i);
                return layer;
            }
        }
        Debug.LogFormat("Found Last Layer");
        return layerSettings.layers[^1];
    }

    private void OnLoad(object objectPlacementsList)
    {
        if (objectPlacementsList == null)
        {
            Debug.Log("Object Placement Map Null");
            return;
        }

        List<ObjectPlacement> objectPlacements = (List<ObjectPlacement>)objectPlacementsList;
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
            Debug.Log("Placeing Object in PlaceObjects");
            PlaceObject(objectPlacement, GetLayerForVertex(objectPlacement.position));
        }
    }

    // Take the object placement, plcae it on the terrain mesh, give it a random location.
    private void PlaceObject(
        ObjectPlacement obj,
        Layer layer
    )
    {
        var origin = Random.Range(0, 360);
        var position = obj.position;

        var randomRotation = Quaternion.Euler(0, origin, 0);
        UnityEngine.GameObject gameObject = UnityEngine.GameObject.Instantiate(
            layer.layerObjectSettings[obj.prefabIndex].prefab
        );

        gameObject.transform.parent = terrainMesh.transform;
        gameObject.transform.localPosition = position;
        gameObject.transform.rotation = randomRotation;
    }

    private Vector3 GetPositionOnTerrain(Vector3 position)
    {
        int terrainLayerMask = 1 << LayerMask.NameToLayer(terrainMesh.transform.name); // Make sure your terrain has the layer "Terrain" assigned

        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, Mathf.Infinity, terrainLayerMask))
        {
            return hit.point;
        }

        return position; // If there's no hit, return the original position
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
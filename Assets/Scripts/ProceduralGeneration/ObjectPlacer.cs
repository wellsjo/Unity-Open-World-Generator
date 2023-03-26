using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

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

    public void CheckAndLoadObjectData(Vector3[] vertices, Matrix4x4 localToWorldMatrix)
    {
        if (done)
        {
            return;
        }
        if (!objectsRequested)
        {
            objectsRequested = true;
            LoadAsync(vertices, localToWorldMatrix);
            return;
        }
        if (objectPlacements != null)
        {
            PlaceObjects(objectPlacements);
            objectPlacements = null;
            done = true;
        }
    }

    public void LoadAsync(Vector3[] vertices, Matrix4x4 localToWorldMatrix)
    {
        ThreadedDataRequester.RequestData(() =>
        {
            return BuildObjectMap(vertices, localToWorldMatrix);
        }, OnLoad);
    }

    // Needs to be thread safe
    public List<ObjectPlacement> BuildObjectMap(Vector3[] vertices, Matrix4x4 localToWorldMatrix)
    {
        // TODO fix object placement + LOD
        int skipIncrement = 1;
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

                var vertex = vertices[vertexIndex];
                var layerIndex = GetLayerIndex(vertex, localToWorldMatrix);
                var layer = layerSettings.layers[layerIndex];

                vertexIndex++;

                ObjectSetting[] settings = layer.objectSettings.Settings;
                if (settings.Length == 0)
                {
                    continue;
                }

                // Replicate UnityEngine.Random.Range
                var min = 0f;
                var max = 1f;
                if ((float)(min + rng.NextDouble() * (max - min)) < (1 - layer.objectSettings.Frequency))
                {
                    continue;
                }

                float[] weights = new float[settings.Length];
                for (int j = 0; j < settings.Length; j++)
                {
                    weights[j] = settings[j].density;
                }

                var objectIndex = RandomWeightedIndex.Get(weights, rng);
                returnValues.Add(new ObjectPlacement(vertex, layerIndex, objectIndex));
            }
        }

        return returnValues;
    }

    // Determine which layer the vertex is in and return the layer index.
    private int GetLayerIndex(Vector3 localPosition, Matrix4x4 localToWorldMatrix)
    {
        var worldPos = localToWorldMatrix.MultiplyPoint3x4(localPosition);

        for (int i = 0; i < layerSettings.layers.Length - 1; i++)
        {
            Layer layer = layerSettings.layers[i];
            Layer nextLayer = layerSettings.layers[i + 1];

            if (InBounds(worldPos.y, layer.startHeight, nextLayer.startHeight))
            {
                return i;
            }
        }

        return layerSettings.layers.Length - 1;
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
            PlaceObject(objectPlacement);
        }
    }

    // Take the object placement, plcae it on the terrain mesh, give it a random location.
    private void PlaceObject(ObjectPlacement obj)
    {
        var positionXY = new Vector3(obj.position.x, meshScale * heightMultiplier, obj.position.z);
        var position = GetPositionOnTerrain(positionXY);
        if (position == Vector3.one)
        {
            return;
        }

        var layer = layerSettings.layers[obj.layerIndex];
        var objectSettings = layer.objectSettings.Settings[obj.prefabIndex];
        UnityEngine.GameObject gameObject = UnityEngine.GameObject.Instantiate(objectSettings.prefab);

        gameObject.transform.parent = terrainMesh.transform;
        gameObject.transform.localPosition = position;

        var origin = UnityEngine.Random.Range(0, 360);
        var randomRotation = Quaternion.Euler(0, origin, 0);
        gameObject.transform.rotation = randomRotation;

        var layerName = "Environment";
        int layerIndex = LayerMask.NameToLayer(layerName);

        if (layerIndex == -1)
        {
            Debug.LogError("Layer not found: " + layerName);
            return;
        }

        gameObject.layer = layerIndex;

        if (!objectSettings.hasChildren)
        {
            return;
        }

        Layer nextLayer = null;
        if (obj.layerIndex != layerSettings.layers.Length - 1)
        {
            nextLayer = layerSettings.layers[obj.layerIndex + 1];
        }

        // Fix the child positions
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            GameObject child = gameObject.transform.GetChild(i).gameObject;

            // If the child is within the bounds of the current layer, try to place it on the terrain. otherwise destroy it.
            if (nextLayer != null && !InBounds(child.transform.position.y, layer.startHeight, nextLayer.startHeight))
            {
                DestroyObject(child);
                continue;
            }

            var newChildPos = GetPositionOnTerrain(child.transform.position);
            if (newChildPos == Vector3.one)
            {
                DestroyObject(child);
                continue;
            }

            child.transform.position = newChildPos;
        }
    }

    private void DestroyObject(GameObject gameObject)
    {
        if (Application.isPlaying)
        {
            UnityEngine.GameObject.Destroy(gameObject);
        }
        else
        {
            UnityEngine.GameObject.DestroyImmediate(gameObject);
        }
    }

    // Returns true if position is withint the bounds of the terrain mesh and layer.
    private bool InBounds(float height, float layerStartHeight, float layerEndHeight)
    {
        return height > layerStartHeight * heightMultiplier * meshScale
            && height < layerEndHeight * heightMultiplier * meshScale;
    }

    // Return the position of the object on the terrain mesh by raycasting down.
    // Vector3.one is returned if no hit is found.
    private Vector3 GetPositionOnTerrain(Vector3 position)
    {
        int terrainLayerMask = 1 << LayerMask.NameToLayer("Default");
        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, Mathf.Infinity, terrainLayerMask))
        {
            return hit.point;
        }

        return Vector3.one;
    }
}

// Data to pass back to the main thread to spawn the objects.
public struct ObjectPlacement
{
    public Vector3 position;
    public int layerIndex;
    public int prefabIndex;

    public ObjectPlacement(Vector3 position, int layerIndex, int prefabIndex)
    {
        this.position = position;
        this.layerIndex = layerIndex;
        this.prefabIndex = prefabIndex;
    }
}

[System.Serializable()]
public struct ObjectSettings
{
    // How often to spawn objects overall
    [Range(0, 1)]
    public float Frequency;
    // Individual settings for each object
    public ObjectSetting[] Settings;
}

[System.Serializable()]
public struct ObjectSetting
{
    // Prefab to spawn
    public GameObject prefab;
    [Range(0, 1)]
    // How often to spawn this object
    public float density;
    // Tells the object spawner to cull children outside bounds
    public bool hasChildren;
}
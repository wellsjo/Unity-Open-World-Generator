using UnityEngine;

[CreateAssetMenu()]
public class FixedMapSettings : UpdatableData
{
    public int width;
    public int height;
    public bool useFalloff;
    public HeightMapSettings heightMapSettings;

    protected override void OnValidate()
    {
        // TODO do we need to validate noise settings?
        Debug.Log("FixedMapSettings OnValidate called");
        base.OnValidate();
    }
}

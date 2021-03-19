using UnityEngine;


public class FourdRecFrame : ScriptableObject
{
    [HideInInspector]
    public int verticesCount;
    [HideInInspector]
    public TextureFormat textureFormat;
    [HideInInspector]
    public byte[] textureData;
    [HideInInspector]
    public Vector3[] positionDataArray;
    [HideInInspector]
    public Vector2[] uvDataArray;
}

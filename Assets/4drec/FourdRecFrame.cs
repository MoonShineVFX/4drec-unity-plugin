using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(FourdRecFrame))]
class FourdRecFrameGUI : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FourdRecFrame frame = (FourdRecFrame)target;
        
        GUILayout.Label(frame.GetSizeInfo());
    }

}
#endif

public class FourdRecFrame : ScriptableObject
{
    [HideInInspector]
    public int verticesCount;
    [HideInInspector]
    public int textureSize;
    [HideInInspector]
    public TextureFormat textureFormat;
    [HideInInspector]
    public byte[] textureData;
    [HideInInspector]
    public Vector3[] positionDataArray;
    [HideInInspector]
    public Vector2[] uvDataArray;

    public string GetSizeInfo()
    {
        return $"Vertex: {verticesCount}\nTex: {textureSize}";
    }
}


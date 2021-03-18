using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AssetImporters;

[ScriptedImporter(1, "4du")]
public class FourdRecFrameImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        // ctx.AddObjectToAsset("main obj", cube);
        // ctx.SetMainObject(cube);

        Debug.Log("hi, im here");
        Debug.Log(ctx.assetPath);
    }
}

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
    public byte[] positionData;
    [HideInInspector]
    public byte[] uvData;
    [HideInInspector]
    public byte[] textureData;
    [HideInInspector]
    public Vector3[] positionDataArray;
    [HideInInspector]
    public Vector2[] uvDataArray;

    public string GetSizeInfo()
    {
        int size = (positionData.Length + uvData.Length + textureData.Length) / 1024;
        return $"Vertex: {verticesCount}\nTex: {textureSize}\nSize: {size}kb";
    }
}


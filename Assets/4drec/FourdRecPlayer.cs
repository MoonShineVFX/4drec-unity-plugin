using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;


[CustomEditor(typeof(FourdRecPlayer))]
public class FourdRecBuildMeshButton : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FourdRecPlayer player = (FourdRecPlayer)target;
        
        if (GUILayout.Button("Build Mesh"))
        {
            var profiler = new FourdProfiler();
            player.buildMesh();
            profiler.Stop();
        }

        GUI.enabled = player.hasBuild;
        if (GUILayout.Button("Clean Mesh"))
        {
            player.cleanMesh();
        }
    }

}


public class FourdRecPlayer : MonoBehaviour
{
    public FourdRecData fourdRecData;
    [HideInInspector]
    public Boolean hasBuild = false;
    
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;

    void Start()
    {
        buildMesh();
    }

    public void buildMesh()
    {
        // Check fourdRecData
        if (fourdRecData == null)
        {
            Debug.LogError("fourdRecData is invalid.");
        }

        // Get data
        byte[] verticesBuffer = lz4.Decompress(fourdRecData.CompressedVertices);
        Vector3[] vertices = new Vector3[fourdRecData.vertexCount];
        FourdUtility.ConvertFromBytes(verticesBuffer, vertices);
        
        byte[] uvsBuffer = lz4.Decompress(fourdRecData.CompressedUvs);
        Vector2[] uvs = new Vector2[fourdRecData.vertexCount];
        FourdUtility.ConvertFromBytes(uvsBuffer, uvs);
        
        // Get texture
        Texture2D texture = new Texture2D(fourdRecData.textureSize, fourdRecData.textureSize, fourdRecData.textureFormat, false);
        texture.LoadRawTextureData(fourdRecData.CompressedTexture);
        texture.Apply();

        // Build mesh
        if (!hasBuild)
        {
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = new Material(Shader.Find("Unlit/Texture"));
            _meshFilter = gameObject.AddComponent<MeshFilter>();
            hasBuild = true;
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = Enumerable.Range(0, fourdRecData.vertexCount).ToArray();
        mesh.uv = uvs;
        _meshFilter.mesh = mesh;
        _meshRenderer.sharedMaterial.mainTexture = texture;
    }

    public void cleanMesh()
    {
        DestroyImmediate(_meshFilter);
        DestroyImmediate(_meshRenderer);
        hasBuild = false;
    }
}

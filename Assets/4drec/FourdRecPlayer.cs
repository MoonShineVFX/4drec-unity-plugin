using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;


#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(FourdRecPlayer))]
class FourdRecBuildMeshButton : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FourdRecPlayer player = (FourdRecPlayer)target;

        if (GUILayout.Button("Build Mesh"))
        {
            var profiler = new FourdProfiler();
            player.BuildMesh();
            profiler.Stop();
        }

        GUI.enabled = player.hasBuild;
        if (GUILayout.Button("Clean Mesh"))
        {
            player.CleanMesh();
        }
    }

}
#endif


public class FourdRecPlayer : MonoBehaviour
{
    
    public AssetBundle assetBundle = null;
    [HideInInspector]
    public Boolean hasBuild = false;

    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;

    void Start()
    {
        CleanMesh();
        BuildMesh();
    }

    public void BuildMesh()
    {
        // Init
        Mesh mesh;
        if (!hasBuild)
        {
            assetBundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/bundles/fourdFramesnlz");
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = new Material(Shader.Find("Unlit/Texture"));
            _meshFilter = gameObject.AddComponent<MeshFilter>();
            mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            _meshFilter.mesh = mesh;
            hasBuild = true;
        }
        
        // Check fourdRecData
        if (assetBundle == null)
        {
            Debug.LogError("fourdRecFolder is invalid.");
            return;
        }
        
        // Load fourdRecFrame
        FourdRecFrame frame = assetBundle.LoadAsset<FourdRecFrame>("Assets/Resources/testFramesNlz/001703.asset");

        // Get texture
        Texture2D texture = new Texture2D(frame.textureSize, frame.textureSize, frame.textureFormat, false);
        texture.LoadRawTextureData(frame.textureData);
        texture.Apply();

        // Apply data
        Debug.Log(_meshFilter.sharedMesh);
        mesh = _meshFilter.sharedMesh;
        mesh.Clear();
        mesh.vertices = frame.positionDataArray;
        mesh.triangles = Enumerable.Range(0, frame.verticesCount).ToArray();
        mesh.uv = frame.uvDataArray;
        
        _meshRenderer.sharedMaterial.mainTexture = texture;
    }

    public void CleanMesh()
    {
        if (!hasBuild) return;
        var meshRenderer = gameObject.GetComponent<MeshRenderer>();
        var meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshRenderer != null) DestroyImmediate(meshRenderer);
        if (meshFilter != null) DestroyImmediate(meshFilter);
        AssetBundle.UnloadAllAssetBundles(true);

        hasBuild = false;
    }
}

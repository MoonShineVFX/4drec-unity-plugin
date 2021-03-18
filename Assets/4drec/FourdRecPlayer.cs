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

        // GUI.enabled = true;
        // player.fourdRecFolder = (UnityEngine.Object)EditorGUILayout.ObjectField(
        //     "Select Folder", 
        //     player.fourdRecFolder, 
        //     typeof(UnityEngine.Object), 
        //     false);
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
        // TextAsset fourdRecFrame = assetBundle.LoadAsset<TextAsset>("Assets/Resources/test/001703.bytes");
        FourdRecFrame frame = assetBundle.LoadAsset<FourdRecFrame>("Assets/Resources/testFramesNlz/001703.asset");
        // byte[] rawData = fourdRecFrame.bytes;
        // int vertexCount = BitConverter.ToInt32(rawData, 0);
        // int textureSize = BitConverter.ToInt32(rawData, 4);
        // int positionDataSize = BitConverter.ToInt32(rawData, 8);
        // int uvDataSize = BitConverter.ToInt32(rawData, 12);
        // int textureDataSize = BitConverter.ToInt32(rawData, 16);
        //
        // GCHandle handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
        // IntPtr rawPtr = handle.AddrOfPinnedObject();

        // Get data
        // IntPtr rawVerticesPtr = IntPtr.Add(rawPtr, 20);
        // byte[] verticesBuffer = lz4.Decompress(frame.positionData);
        // Vector3[] vertices = new Vector3[frame.verticesCount];
        // FourdUtility.ConvertFromBytes(verticesBuffer, vertices);
        
        // IntPtr rawUvPtr = IntPtr.Add(rawPtr, 20 + positionDataSize);
        // byte[] uvsBuffer = lz4.Decompress(frame.uvData);
        // Vector2[] uvs = new Vector2[frame.verticesCount];
        // FourdUtility.ConvertFromBytes(uvsBuffer, uvs);
        
        // Get texture
        Texture2D texture = new Texture2D(frame.textureSize, frame.textureSize, TextureFormat.DXT1, false);
        // IntPtr rawTexPtr = IntPtr.Add(rawPtr, 20 + positionDataSize + uvDataSize);
        texture.LoadRawTextureData(frame.textureData);
        texture.Apply();
        
        // handle.Free();
        
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

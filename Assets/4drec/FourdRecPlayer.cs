using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;


public class FourdRecPlayer : MonoBehaviour
{
#if UNITY_EDITOR
    [CustomEditor(typeof(FourdRecPlayer))]
    class FourdRecBuildPlayerGUI : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            FourdRecPlayer player = (FourdRecPlayer)target;
            
            // Select Loader
            FourdRecLoader nextLoader = (FourdRecLoader)EditorGUILayout.ObjectField(
                "4DREC Loader", player.loader, typeof(FourdRecLoader), false
                );
            if (player.loader != nextLoader) player.ConnectLoader(nextLoader);
            
            if (player.hasLoader)
            {
                EditorGUI.BeginChangeCheck();
                FourdRecLoader loader = player.loader;
                int frame = EditorGUILayout.IntSlider(
                    player.currentFrame, loader.startFrame, loader.endFrame
                    );
            
                if (EditorGUI.EndChangeCheck() && frame != player.currentFrame)
                {
                    player.SetFrame(frame);
                }
                
                GUILayout.Label("[Shot Detail]\n" + player.loader.GetInfo());
            }
            
            // if (GUILayout.Button("Build Mesh"))
            // {
            //     player.BuildMesh();
            // }
            //
            // GUI.enabled = player.hasBuild;
            // if (GUILayout.Button("Clean Mesh"))
            // {
            //     player.Clean();
            // }
        }

    }
#endif
    
    [HideInInspector]
    public int currentFrame;
    [HideInInspector]
    public bool hasLoader = false;
    
    [SerializeField, HideInInspector]
    private FourdRecLoader loader;
    private AssetBundle _assetBundle;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;

    void Start()
    {
        if (!hasLoader) return;
        Clean();
        Initialize();
        UpdateMesh();
    }

    public void ConnectLoader(FourdRecLoader nextLoader)
    {
        loader = nextLoader;
        Clean();
        if (nextLoader == null)
        {
            hasLoader = false;
            return;
        }
        Initialize();
        if (currentFrame < loader.startFrame) currentFrame = loader.startFrame;
        if (currentFrame > loader.endFrame) currentFrame = loader.endFrame;
        hasLoader = true;
        UpdateMesh();
    }

    public void SetFrame(int nextFrame)
    {
        if (nextFrame == currentFrame) return;
        currentFrame = nextFrame;
        UpdateMesh();
    }

    public void UpdateMesh()
    {
        // Load fourdRecFrame
        FourdRecFrame frame = _assetBundle.LoadAsset<FourdRecFrame>($"{currentFrame:D6}.asset");

        // Get texture
        Texture2D texture = new Texture2D(
            loader.textureSize, loader.textureSize,
            frame.textureFormat, false
            );
        texture.LoadRawTextureData(frame.textureData);
        texture.Apply();

        // Apply data
        Mesh mesh = _meshFilter.sharedMesh;
        mesh.Clear();
        mesh.vertices = frame.positionDataArray;
        mesh.triangles = Enumerable.Range(0, frame.verticesCount).ToArray();
        mesh.uv = frame.uvDataArray;
        
        _meshRenderer.sharedMaterial.mainTexture = texture;
    }

    public void Initialize()
    {
        // Asset Bundle
        var assetBundles = AssetBundle.GetAllLoadedAssetBundles();
        bool isFound = false;
        foreach (var assetBundle in assetBundles)
        {
            if (assetBundle.name == loader.shotName)
            {
                isFound = true;
                _assetBundle = assetBundle;
                break;
            }
        }
        if (!isFound) _assetBundle = AssetBundle.LoadFromFile($"{FourdRecUtility.AbPath}/{loader.shotName}");
        
        // Components
        _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        _meshRenderer.material = new Material(Shader.Find("Unlit/Texture"));
        _meshFilter = gameObject.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        _meshFilter.mesh = mesh;
    }

    public void Clean()
    {
        var meshRenderer = gameObject.GetComponent<MeshRenderer>();
        var meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshRenderer != null) DestroyImmediate(meshRenderer);
        if (meshFilter != null) DestroyImmediate(meshFilter);
        if (_assetBundle != null) _assetBundle.Unload(true);
        _assetBundle = null;
        _meshRenderer = null;
        _meshFilter = null;
    }
}

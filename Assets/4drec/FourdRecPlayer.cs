using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace _4drec
{
    public class FourdRecPlayer : MonoBehaviour
    {
#if UNITY_EDITOR
        [CustomEditor(typeof(FourdRecPlayer))]
        private class FourdRecPlayerGUI : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                FourdRecPlayer player = (FourdRecPlayer)target;
            
                // Select Loader
                FourdRecLoader nextLoader = (FourdRecLoader)EditorGUILayout.ObjectField(
                    "4DREC Loader", player.loader, typeof(FourdRecLoader), false
                );
                if (player.loader != nextLoader)
                {
                    player.ConnectLoader(nextLoader);
                    EditorUtility.SetDirty(player);
                }
            
                if (player.hasLoader)
                {
                    // CustomMaterial
                    Material nextMaterial = (Material)EditorGUILayout.ObjectField(
                        "Custom Material", player.customMaterial, typeof(Material), false
                    );
            
                    if (EditorGUI.EndChangeCheck() && nextMaterial != player.customMaterial)
                    {
                        player.ConnectMaterial(nextMaterial);
                        EditorUtility.SetDirty(player);
                    }
                    
                    // Current Frame
                    EditorGUI.BeginChangeCheck();
                    FourdRecLoader loader = player.loader;
                    int frame = EditorGUILayout.IntSlider(
                        player.currentFrame, loader.startFrame, loader.endFrame
                    );
            
                    if (EditorGUI.EndChangeCheck() && frame != player.currentFrame)
                    {
                        player.SetFrame(frame);
                        EditorUtility.SetDirty(player);
                    }
                
                    GUILayout.Label("[Shot Detail]\n" + player.loader.GetInfo());
                }
            }

        }
#endif

        public bool autoPlay = true;
        public bool loop = true;

        [HideInInspector] public int currentFrame;
        [HideInInspector] public bool hasLoader = false;
        [HideInInspector] public FourdRecLoader loader;
        [HideInInspector] public Material customMaterial = null;
    
        [SerializeField, HideInInspector] private float frameDuration;
        [SerializeField, HideInInspector] private MeshRenderer meshRenderer;
        [SerializeField, HideInInspector] private MeshFilter meshFilter;

        private AssetBundle _assetBundle;
        private bool _isAssignAssetBundle = false;
        private bool _isPlaying = false;
        private float _deltaTime = 0f;
        private FourdRecFrame _lastFrame;
        private bool _hasLastFrame = true;
        private Material _defaultMaterial = null;
    
        void Start()
        {
            if (!hasLoader) return;
            Clean();
            Initialize();
            UpdateMesh();
            if (autoPlay) Play();
        }

        public void Play()
        {
            _isPlaying = true;
        }

        public void Stop()
        {
            _isPlaying = false;
            _deltaTime = 0f;
        }

        private void Update()
        {
            if (_isPlaying) _deltaTime += Time.deltaTime;
            if (_deltaTime > frameDuration)
            {
                _deltaTime -= frameDuration;
                PlayNextFrame();
            }
        }

        public void ConnectMaterial(Material material)
        {
            if (customMaterial != null)
            {
                customMaterial.mainTexture = null;
            }
            customMaterial = material;
            Initialize();
            UpdateMesh();
        }

        public void ConnectLoader(FourdRecLoader nextLoader)
        {
            _isAssignAssetBundle = false;
            loader = nextLoader;
            Clean();
            if (nextLoader == null)
            {
                hasLoader = false;
                if (_isPlaying) _isPlaying = false;
                return;
            }
            Initialize();
            if (currentFrame < loader.startFrame) currentFrame = loader.startFrame;
            if (currentFrame > loader.endFrame) currentFrame = loader.endFrame;
            hasLoader = true;
            frameDuration = 1f / loader.fps;
            UpdateMesh();
        }

        private void SetFrame(int nextFrame)
        {
#if UNITY_EDITOR
            if (!_isAssignAssetBundle) AssignAssetBundle();
#endif
            if (nextFrame == currentFrame) return;
            currentFrame = nextFrame;
            UpdateMesh();
        }

        private void PlayNextFrame()
        {
            int nextFrame = currentFrame + 1;
            if (nextFrame > loader.endFrame)
            {
                if (!loop)
                {
                    Stop();
                    _assetBundle.Unload(true);
                    return;
                }
                nextFrame = loader.startFrame;
            }
            SetFrame(nextFrame);
        }

        private void UpdateMesh()
        {
            // Load fourdRecFrame
            FourdRecFrame frame = _assetBundle.LoadAsset<FourdRecFrame>($"{currentFrame:D6}.asset");

            // Get texture
            Texture2D texture = (Texture2D)meshRenderer.sharedMaterial.mainTexture;
            texture.LoadRawTextureData(frame.textureData);
            texture.Apply();

            // Apply data
            Mesh mesh = meshFilter.sharedMesh;
            mesh.Clear();
            mesh.vertices = frame.positionArray;
            mesh.triangles = Enumerable.Range(0, frame.verticesCount).ToArray();
            mesh.uv = frame.uvArray;
        
            meshRenderer.sharedMaterial.mainTexture = texture;

            if (_hasLastFrame)
            {
                Resources.UnloadAsset(_lastFrame);
                // Destroy(_lastFrame);         
            }
            _hasLastFrame = true;
            _lastFrame = frame;
        }

        private void Initialize()
        {
            if (!_isAssignAssetBundle) AssignAssetBundle();

            // Mesh Renderer
            bool emptyMeshRenderer = meshRenderer == null;
            var meshRenderComponent = gameObject.GetComponent<MeshRenderer>();
            bool emptyMeshRenderComponent = meshRenderComponent == null;
            if (emptyMeshRenderer || emptyMeshRenderComponent)
            {
                if (emptyMeshRenderComponent)
                {
                    meshRenderer = gameObject.AddComponent<MeshRenderer>();
                }
                else
                {
                    meshRenderer = meshRenderComponent;
                }
            }
            // Material assignment
            if (_defaultMaterial == null) _defaultMaterial = new Material(Shader.Find("Unlit/Texture"));
            meshRenderer.sharedMaterial = customMaterial == null ? _defaultMaterial : customMaterial;
            Texture2D texture = new Texture2D(
                loader.textureSize, loader.textureSize,
                loader.textureFormat, false
            );
            meshRenderer.sharedMaterial.mainTexture = texture;
        
            // Mesh Filter
            bool emptyMeshFilter = meshFilter == null;
            var meshFilterComponent = gameObject.GetComponent<MeshFilter>();
            bool emptyMeshFilterComponent = meshFilterComponent == null;
            if (emptyMeshFilter || emptyMeshFilterComponent)
            {
                if (emptyMeshFilterComponent)
                {
                    meshFilter = gameObject.AddComponent<MeshFilter>();
                    Mesh mesh = new Mesh {indexFormat = IndexFormat.UInt32};
                    meshFilter.mesh = mesh;
                }
                else
                {
                    meshFilter = meshFilterComponent;
                }
            }
        }

        private void AssignAssetBundle()
        {
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
            if (!isFound) _assetBundle = AssetBundle.LoadFromFile(
                $"{Application.streamingAssetsPath}/{FourdRecUtility.AssetBundleNameTag}/{loader.shotName}");
            _isAssignAssetBundle = true;
        }

        private void Clean(bool destroyComponent = true)
        {
            if (_assetBundle != null) _assetBundle.Unload(true);
            _assetBundle = null;
            if (destroyComponent)
            {
                if (meshFilter != null)
                {
                    DestroyImmediate(meshFilter);
                }
                if (meshRenderer != null)
                {
                    DestroyImmediate(meshRenderer);
                }
            }
        }

        private void OnDestroy()
        {
            Clean(false);
        }
    }
}

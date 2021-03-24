using UnityEditor;
using UnityEngine;

namespace _4drec
{
    public class FourdRecLoader : ScriptableObject
    {
#if UNITY_EDITOR
        [CustomEditor(typeof(FourdRecLoader))]
        private class FourdRecLoaderGUI : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                FourdRecLoader loader = (FourdRecLoader)target;
                GUILayout.Label(loader.GetInfo());
            }

        }
#endif

        [HideInInspector]
        public string shotName;
        [HideInInspector]
        public int fps = 30;
        [HideInInspector]
        public int startFrame;
        [HideInInspector]
        public int endFrame;
        [HideInInspector]
        public int textureSize;
        [HideInInspector]
        public TextureFormat textureFormat;

        public string GetInfo()
        {
            return $"Name:   {shotName}\n" +
                   $"FPS:   {fps}\n" +
                   $"Frame Range:   {startFrame} - {endFrame}\n" +
                   $"Texture Size:   {textureSize}x{textureSize}\n" +
                   $"Texture Format:   {textureFormat}";
        }
    }
}

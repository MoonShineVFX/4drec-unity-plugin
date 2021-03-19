using Unity.Collections;
using UnityEngine;
using UnityEditor;


public class FourdRecLoader : ScriptableObject
{
#if UNITY_EDITOR
    [CustomEditor(typeof(FourdRecLoader))]
    class FourdRecBuildPlayerGUI : Editor
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

    public string GetInfo()
    {
        return $"Name:   {shotName}\n" +
               $"FPS:   {fps}\n" +
               $"Frame Range:   {startFrame} - {endFrame}\n" +
               $"Texture Size:   {textureSize}x{textureSize}";
    }
}

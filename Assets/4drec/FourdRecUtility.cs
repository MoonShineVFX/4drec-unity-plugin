using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public static class FourdRecUtility
{
    public const string TempPath = "Assets/Resources/TempFourdRec";
    public const string AbPath = "Assets/StreamingAssets/FourdRec";
    public const string LoaderAbPath = "Assets/StreamingAssets/FourdRec/loader";
    public const string LoaderAssetPath = "Assets/4drecLoader";
    
    public static byte[] ConvertToBytes<T>(T source, int size)
    {
        GCHandle handle = GCHandle.Alloc(source, GCHandleType.Pinned);
        IntPtr ptr = handle.AddrOfPinnedObject();
        byte[] buffer = new byte[size];
        Marshal.Copy(ptr, buffer, 0, size);
        handle.Free();
        return buffer;
    }

    public static void ConvertFromBytes<T>(byte[] source, T dest)
    {
        GCHandle handle = GCHandle.Alloc(dest, GCHandleType.Pinned);
        IntPtr ptr = handle.AddrOfPinnedObject();
        Marshal.Copy(source, 0, ptr, source.Length);
        handle.Free();
    }
    
    public class Profiler
    {
        private System.Diagnostics.Stopwatch watch;

        public Profiler()
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
        }

        public void Restart()
        {
            watch.Restart();
        }

        public void Stop(string prefix = "profiler")
        {
            watch.Stop();
            Debug.Log($"> {prefix}: {watch.Elapsed.TotalMilliseconds} ms");
        }
    
        public void Mark(string prefix)
        {
            Stop(prefix);
            Restart();
        }
    }
    
    public static Texture2D Resize(ref Texture2D texture, int width, int height)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height);
        RenderTexture.active = renderTexture;
        Graphics.Blit(texture, renderTexture);
        
        Texture2D scaledTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        scaledTexture.filterMode = FilterMode.Bilinear;
        scaledTexture.wrapMode = TextureWrapMode.Clamp;
        scaledTexture.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
        scaledTexture.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);

        return scaledTexture;
    }
}

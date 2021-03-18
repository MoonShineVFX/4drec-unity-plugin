using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

public static class FourdUtility
{
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
}

public class FourdProfiler
{
    private System.Diagnostics.Stopwatch watch;

    public FourdProfiler()
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

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;


#if UNITY_EDITOR
public class FourdRecImporter : MonoBehaviour
{
    private const int TextureSize = 1024;

    [MenuItem("4DREC/Import 4DF file")]
    static void Import4dfFile()
    {
        ImportFolder();
    }
    
    [MenuItem("4DREC/Build Bundles")]
    static void BuildAssetBundles()
    {
        string assetBundleDirectory = "Assets/StreamingAssets/bundles";
        if(!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }
        BuildPipeline.BuildAssetBundles(assetBundleDirectory, 
            BuildAssetBundleOptions.ChunkBasedCompression, 
            BuildTarget.StandaloneWindows);
    }

    static void ImportFolder()
    {
        string path = EditorUtility.OpenFolderPanel("Import 4DREC file", "", "");
        string[] files = Directory.GetFiles(path);

        if (files.Length == 0) return;

        Directory.CreateDirectory($"{Application.dataPath}/Resources/testFramesNlz");
        
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            EditorUtility.DisplayProgressBar(
                "Import Files", 
                file,
                (float)i / files.Length
                );
            Convert4DF(file);
        }
        EditorUtility.ClearProgressBar();
        
        AssetDatabase.Refresh();
    }

    static void Convert4DF(string path)
    {
        var profiler = new FourdProfiler();
        // Get raw buffer
        byte[] fileBuffer = File.ReadAllBytes(path);

        Int32 geoCompressedSize = BitConverter.ToInt32(fileBuffer, 64);
        Int32 textureCompressedSize = BitConverter.ToInt32(fileBuffer, 68);

        byte[] compressedGeoBuffer = new byte[geoCompressedSize];
        Array.Copy(fileBuffer, 1024, compressedGeoBuffer, 0, geoCompressedSize);

        byte[] compressedTextureBuffer = new byte[textureCompressedSize];
        Array.Copy(fileBuffer, 1024 + geoCompressedSize, compressedTextureBuffer, 0, textureCompressedSize);
    
        byte[] geoBuffer = lz4.Decompress(compressedGeoBuffer);
        
        profiler.Mark("Raw buffer");
        // Geo
        int verticesArrayCount = geoBuffer.Length / 5 / 4;
        byte[] positionBuffer = new byte[verticesArrayCount * 4 * 3];
        byte[] uvBuffer = new byte[verticesArrayCount * 4 * 2];
        for (int i = 0; i < verticesArrayCount; i++)
        {
            Array.Copy(geoBuffer, i * 4 * 5, positionBuffer, i * 4 * 3, 12);
            Array.Copy(geoBuffer, i * 4 * 5 + 12, uvBuffer, i * 4 * 2, 8);
        }
        
        profiler.Mark("Geo");
        // UV modify
        Vector2[] uvArray = new Vector2[verticesArrayCount];
        GCHandle uvHandle = GCHandle.Alloc(uvArray, GCHandleType.Pinned);
        IntPtr uvPointer = uvHandle.AddrOfPinnedObject();
        Marshal.Copy(uvBuffer, 0, uvPointer, uvBuffer.Length);
        uvHandle.Free();

        for (int i = 0; i < verticesArrayCount; i++)
        {
            uvArray[i].y = 1 - uvArray[i].y;
        }
        
        profiler.Mark("UV");
        // Texture
        Texture2D sourceTexture = new Texture2D(2, 2);
        sourceTexture.LoadImage(compressedTextureBuffer);
        TextureScale.Bilinear(sourceTexture, TextureSize, TextureSize);
        sourceTexture.Compress(false);
        
        profiler.Mark("Texture");
        // Write
        // string assetName = $"{Application.dataPath}/Resources/test/{Path.GetFileNameWithoutExtension(path)}.bytes";
        // BinaryWriter writer = new BinaryWriter(File.Open(assetName, FileMode.Create));
        FourdRecFrame frame = ScriptableObject.CreateInstance<FourdRecFrame>();
        // byte[] positionData = lz4.Compress(positionBuffer);
        // byte[] uvData = lz4.Compress(FourdUtility.ConvertToBytes(uvArray, uvBuffer.Length));
        byte[] textureData = sourceTexture.GetRawTextureData().ToArray();

        // writer.Write(verticesArrayCount);
        // writer.Write(TextureSize);
        // writer.Write(positionData.Length);
        // writer.Write(uvData.Length);
        // writer.Write(textureData.Length);
        // writer.Write(positionData);
        // writer.Write(uvData);
        // writer.Write(textureData);
        // writer.Close();
        frame.verticesCount = verticesArrayCount;
        frame.textureSize = TextureSize;
        // frame.positionData = positionData;
        // frame.uvData = uvData;
        frame.textureData = textureData;
        frame.uvDataArray = uvArray;
        frame.positionDataArray = new Vector3[verticesArrayCount];
        FourdUtility.ConvertFromBytes(positionBuffer, frame.positionDataArray);

        AssetDatabase.CreateAsset(frame, $"Assets/Resources/testFramesNlz/{Path.GetFileNameWithoutExtension(path)}.asset");
        AssetDatabase.SaveAssets();
        // EditorUtility.FocusProjectWindow();
        // Selection.activeObject = frame;

        profiler.Mark("Write data");
        // Return
        DestroyImmediate(sourceTexture);
    }
}
#endif

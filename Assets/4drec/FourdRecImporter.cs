using System;
using System.IO;
using System.Linq;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
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
        
        string savePath = $"{Application.dataPath}/Resources/fourd";
        if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
        
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            EditorUtility.DisplayProgressBar(
                "Import Files", 
                file,
                (float)i / files.Length
                );
            Convert4DF(file, savePath);
        }
        EditorUtility.ClearProgressBar();
        
        AssetDatabase.Refresh();
    }

    static void Convert4DF(string importPath, string savePath)
    {
        // Get raw buffer
        byte[] fileBuffer = File.ReadAllBytes(importPath);

        Int32 geoCompressedSize = BitConverter.ToInt32(fileBuffer, 64);
        Int32 textureCompressedSize = BitConverter.ToInt32(fileBuffer, 68);

        byte[] compressedGeoBuffer = new byte[geoCompressedSize];
        Array.Copy(fileBuffer, 1024, compressedGeoBuffer, 0, geoCompressedSize);

        byte[] compressedTextureBuffer = new byte[textureCompressedSize];
        Array.Copy(fileBuffer, 1024 + geoCompressedSize, compressedTextureBuffer, 0, textureCompressedSize);
        
        byte[] geoBuffer = lz4.Decompress(compressedGeoBuffer);
        
        // Geo
        int verticesArrayCount = geoBuffer.Length / 5 / 4;
        byte[] positionBuffer = new byte[verticesArrayCount * 4 * 3];
        byte[] uvBuffer = new byte[verticesArrayCount * 4 * 2];
        for (int i = 0; i < verticesArrayCount; i++)
        {
            Array.Copy(geoBuffer, i * 4 * 5, positionBuffer, i * 4 * 3, 12);
            Array.Copy(geoBuffer, i * 4 * 5 + 12, uvBuffer, i * 4 * 2, 8);
        }

        // UV modify
        Vector2[] uvArray = new Vector2[verticesArrayCount];
        FourdUtility.ConvertFromBytes(uvBuffer, uvArray);

        for (int i = 0; i < verticesArrayCount; i++)
        {
            uvArray[i].y = 1 - uvArray[i].y;
        }
        
        // Texture
        Texture2D sourceTexture = new Texture2D(2, 2);
        sourceTexture.LoadImage(compressedTextureBuffer);
        TextureScale.Bilinear(sourceTexture, TextureSize, TextureSize);
        sourceTexture.Compress(false);
        
        // Write
        FourdRecFrame frame = ScriptableObject.CreateInstance<FourdRecFrame>();
        byte[] textureData = sourceTexture.GetRawTextureData().ToArray();

        frame.verticesCount = verticesArrayCount;
        frame.textureSize = TextureSize;
        frame.textureFormat = sourceTexture.format;
        frame.textureData = textureData;
        frame.uvDataArray = uvArray;
        frame.positionDataArray = new Vector3[verticesArrayCount];
        FourdUtility.ConvertFromBytes(positionBuffer, frame.positionDataArray);

        AssetDatabase.CreateAsset(frame, $"Assets/Resources/fourd/{Path.GetFileNameWithoutExtension(importPath)}.asset");
        AssetDatabase.SaveAssets();

        // Return
        DestroyImmediate(sourceTexture);
    }
}
#endif

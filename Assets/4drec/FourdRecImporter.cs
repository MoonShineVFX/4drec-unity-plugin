using System;
using System.IO;
using System.Linq;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
public class FourdRecImporter : MonoBehaviour
{
    private const int TextureSize = 1024;
    private const string SavePath = "Assets/Resources/fourd";
    private const string AbPath = "Assets/StreamingAssets/fourd";

    [MenuItem("4DREC/Import 4DF file")]
    static void Import4dfFile()
    {
        ImportFolder();
    }
    
    static void ImportFolder()
    {
        string path = EditorUtility.OpenFolderPanel("Import 4DREC file", "", "");
        string shotName = Path.GetFileName(path);
        string[] files = Directory.GetFiles(path);

        if (files.Length == 0) return;
        
        Directory.CreateDirectory(SavePath);
        string[] assetPaths = new string[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            EditorUtility.DisplayProgressBar(
                "Import Files", 
                file,
                (float)i / files.Length
                );
            assetPaths[i] = Convert4DF(file);
        }
        EditorUtility.ClearProgressBar();
        
        AssetDatabase.Refresh();
        
        BuildAssetBundle(assetPaths, shotName);

        AssetDatabase.DeleteAsset(SavePath);
        AssetDatabase.Refresh();
    }

    static string Convert4DF(string importPath)
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

        string assetPath = $"{SavePath}/{Path.GetFileNameWithoutExtension(importPath)}.asset";
        AssetDatabase.CreateAsset(frame, assetPath);
        AssetDatabase.SaveAssets();

        // Return
        DestroyImmediate(sourceTexture);
        return assetPath;
    }

    static void BuildAssetBundle(string[] assetPaths, string shotName)
    {
        AssetBundleBuild bundleBuild = new AssetBundleBuild();
        bundleBuild.assetBundleName = shotName;

        string[] addressableNames = new string[assetPaths.Length];
        for (int i = 0; i < assetPaths.Length; i++)
        {
            string assetPath = assetPaths[i];
            string fileName = Path.GetFileName(assetPath);
            assetPaths[i] = assetPath;
            addressableNames[i] = fileName;
        }

        bundleBuild.addressableNames = addressableNames;
        bundleBuild.assetNames = assetPaths;

        Directory.CreateDirectory(AbPath);

        BuildPipeline.BuildAssetBundles(
            AbPath,
            new []{bundleBuild},
            BuildAssetBundleOptions.ChunkBasedCompression, 
            BuildTarget.StandaloneWindows
            );
    }
}
#endif

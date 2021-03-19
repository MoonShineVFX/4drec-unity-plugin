using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;


#if UNITY_EDITOR
public class FourdRecImporter : MonoBehaviour
{
    [MenuItem("4DREC/Import 4DF file")]
    static void Import4dfFile()
    {
        ImportFolder();
    }
    
    static void ImportFolder()
    {
        // Open folder
        string path = EditorUtility.OpenFolderPanel("Import 4DREC file", "", "");
        string shotName = Path.GetFileName(path);
        string[] files = Directory.GetFiles(path);

        if (files.Length == 0) return;

        int textureSize = 1024;
        
        // Create directories
        Directory.CreateDirectory(FourdRecUtility.TempPath);
        Directory.CreateDirectory(FourdRecUtility.AbPath);
        Directory.CreateDirectory(FourdRecUtility.LoaderAbPath);
        Directory.CreateDirectory(FourdRecUtility.LoaderAssetPath);
        
        // Create assets
        string[] assetPaths = new string[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            EditorUtility.DisplayProgressBar(
                "Import Files", 
                file,
                (float)i / files.Length
                );
            assetPaths[i] = Convert4DF(file, textureSize);
            // TODO Batch Ijob multi-threaded optimized
            // Memory condition watch with buffer
            // Playback system
            // Editor restart will lose 4drec loader
            // Texture channel swap
        }
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        
        // Turn asset to bundle
        BuildAssetBundle(assetPaths, shotName);

        // Create loader
        int startFrame = Int32.Parse(Path.GetFileNameWithoutExtension(assetPaths[0]));
        int endFrame = Int32.Parse(Path.GetFileNameWithoutExtension(assetPaths[assetPaths.Length - 1]));
        FourdRecLoader loader = ScriptableObject.CreateInstance<FourdRecLoader>();
        loader.shotName = shotName;
        loader.startFrame = startFrame;
        loader.endFrame = endFrame;
        loader.textureSize = textureSize;
        string loaderPath = $"{FourdRecUtility.LoaderAbPath}/{shotName}.asset";
        AssetDatabase.CreateAsset(loader, loaderPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.CopyAsset(loaderPath, $"{FourdRecUtility.LoaderAssetPath}/{shotName}.asset");
        
        AssetDatabase.DeleteAsset(FourdRecUtility.TempPath);
        AssetDatabase.Refresh();
    }

    static string Convert4DF(string importPath, int textureSize)
    {
        // Get raw buffer
        byte[] fileBuffer = File.ReadAllBytes(importPath);

        Int32 geoCompressedSize = BitConverter.ToInt32(fileBuffer, 64);
        Int32 textureCompressedSize = BitConverter.ToInt32(fileBuffer, 68);

        byte[] compressedGeoBuffer = new byte[geoCompressedSize];
        Array.Copy(
            fileBuffer, 1024, 
            compressedGeoBuffer, 0, 
            geoCompressedSize
            );

        byte[] compressedTextureBuffer = new byte[textureCompressedSize];
        Array.Copy(
            fileBuffer, 1024 + geoCompressedSize,
            compressedTextureBuffer,0, 
            textureCompressedSize
            );
        
        byte[] geoBuffer = lz4.Decompress(compressedGeoBuffer);
        
        // Geo
        int verticesArrayCount = geoBuffer.Length / 5 / 4;
        byte[] positionBuffer = new byte[verticesArrayCount * 4 * 3];
        byte[] uvBuffer = new byte[verticesArrayCount * 4 * 2];
        for (int i = 0; i < verticesArrayCount; i++)
        {
            Array.Copy(
                geoBuffer, i * 4 * 5, 
                positionBuffer, i * 4 * 3, 
                12
                );
            Array.Copy(
                geoBuffer, i * 4 * 5 + 12, 
                uvBuffer, i * 4 * 2, 
                8
                );
        }

        // UV modify
        Vector2[] uvArray = new Vector2[verticesArrayCount];
        FourdRecUtility.ConvertFromBytes(uvBuffer, uvArray);

        for (int i = 0; i < verticesArrayCount; i++)
        {
            uvArray[i].y = 1 - uvArray[i].y;
        }
        
        // Texture
        Texture2D sourceTexture = new Texture2D(2, 2);
        sourceTexture.LoadImage(compressedTextureBuffer);
        if (textureSize != sourceTexture.width)
        {
            TextureScale.Bilinear(sourceTexture, textureSize, textureSize);
        }
        sourceTexture.Compress(false);
        
        // Write
        FourdRecFrame frame = ScriptableObject.CreateInstance<FourdRecFrame>();
        byte[] textureData = sourceTexture.GetRawTextureData().ToArray();

        frame.verticesCount = verticesArrayCount;
        frame.textureFormat = sourceTexture.format;
        frame.textureData = textureData;
        frame.uvDataArray = uvArray;
        frame.positionDataArray = new Vector3[verticesArrayCount];
        FourdRecUtility.ConvertFromBytes(positionBuffer, frame.positionDataArray);

        string assetPath = $"{FourdRecUtility.TempPath}/{Path.GetFileNameWithoutExtension(importPath)}.asset";
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

        BuildPipeline.BuildAssetBundles(
            FourdRecUtility.AbPath,
            new []{bundleBuild},
            BuildAssetBundleOptions.ChunkBasedCompression, 
            BuildTarget.StandaloneWindows
            );
    }
}
#endif

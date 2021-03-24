using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEditor;
using TurboJpegWrapper;


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
        
        // job test
        JobHandle[] jobs = new JobHandle[files.Length];
        NativeArray<byte>[] importPathList = new NativeArray<byte>[files.Length]; 
        NativeArray<int>[] nVerticesCount = new NativeArray<int>[files.Length];
        NativeArray<Vector3>[] nPositionArray = new NativeArray<Vector3>[files.Length];
        NativeArray<Vector2>[] nUvArray = new NativeArray<Vector2>[files.Length];
        NativeArray<Color32>[] textureDataList = new NativeArray<Color32>[files.Length]; 
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            byte[] fileBytes = Encoding.UTF8.GetBytes(file);
            importPathList[i] = new NativeArray<byte>(fileBytes, Allocator.Persistent);
            nVerticesCount[i] = new NativeArray<int>(1, Allocator.Persistent);
            nPositionArray[i] = new NativeArray<Vector3>(200000, Allocator.Persistent);
            nUvArray[i] = new NativeArray<Vector2>(200000, Allocator.Persistent);
            textureDataList[i] = new NativeArray<Color32>(4096 * 4096, Allocator.Persistent);
            var job = new FourdConverter()
            {
                ImportPathBytes = importPathList[i],
                nPositionArray = nPositionArray[i],
                nVerticesCount = nVerticesCount[i],
                nUvArray = nUvArray[i],
                TextureData = textureDataList[i],
                TextureSize = 4096
            };
            jobs[i] = job.Schedule();
        }
        
        string[] assetPaths = new string[files.Length];
        for (int i = 0; i < jobs.Length; i++)
        {
            EditorUtility.DisplayProgressBar(
                "Import Files", 
                i.ToString(),
                (float)i / files.Length
            );
            
            // wait job
            var job = jobs[i];
            job.Complete();
            
            // texture manage
            Texture2D sourceTexture = new Texture2D(4096, 4096);
            sourceTexture.SetPixels32(textureDataList[i].ToArray());
            sourceTexture.Apply();
            
            var scaledTexture = FourdRecUtility.Resize(ref sourceTexture, 1024, 1024);
            scaledTexture.Compress(true);
            //EditorUtility.CompressTexture(scaledTexture, TextureFormat.DXT1, TextureCompressionQuality.Best);
            
            // asset apply
            FourdRecFrame frame = ScriptableObject.CreateInstance<FourdRecFrame>();

            frame.verticesCount = nVerticesCount[i][0];
            frame.textureFormat = TextureFormat.DXT1;
            frame.textureData = scaledTexture.GetRawTextureData();
            frame.uvDataArray = new NativeSlice<Vector2>(nUvArray[i], 0, frame.verticesCount).ToArray();
            frame.positionDataArray = new NativeSlice<Vector3>(nPositionArray[i], 0, frame.verticesCount).ToArray();
            
            string assetPath = $"{FourdRecUtility.TempPath}/{Path.GetFileNameWithoutExtension(Encoding.UTF8.GetString(importPathList[i].ToArray()))}.asset";
            AssetDatabase.CreateAsset(frame, assetPath);
            
            assetPaths[i] = assetPath;
            
            DestroyImmediate(sourceTexture);
            DestroyImmediate(scaledTexture);
            importPathList[i].Dispose();
            nPositionArray[i].Dispose();
            nVerticesCount[i].Dispose();
            nUvArray[i].Dispose();
            textureDataList[i].Dispose();
            Debug.Log($"job {i} completed");
        }
        AssetDatabase.SaveAssets();
        EditorUtility.ClearProgressBar();
        
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

        // // Create assets
        // string[] assetPaths = new string[files.Length];
        // for (int i = 0; i < files.Length; i++)
        // {
        //     string file = files[i];
        //     EditorUtility.DisplayProgressBar(
        //         "Import Files", 
        //         file,
        //         (float)i / files.Length
        //         );
        //     assetPaths[i] = Convert4DF(file, textureSize);
        // }
        // EditorUtility.ClearProgressBar();
        // AssetDatabase.Refresh();
        //
        // // Turn asset to bundle
        // BuildAssetBundle(assetPaths, shotName);
        //
        // // Create loader
        // int startFrame = Int32.Parse(Path.GetFileNameWithoutExtension(assetPaths[0]));
        // int endFrame = Int32.Parse(Path.GetFileNameWithoutExtension(assetPaths[assetPaths.Length - 1]));
        // FourdRecLoader loader = ScriptableObject.CreateInstance<FourdRecLoader>();
        // loader.shotName = shotName;
        // loader.startFrame = startFrame;
        // loader.endFrame = endFrame;
        // loader.textureSize = textureSize;
        // string loaderPath = $"{FourdRecUtility.LoaderAbPath}/{shotName}.asset";
        // AssetDatabase.CreateAsset(loader, loaderPath);
        // AssetDatabase.SaveAssets();
        // AssetDatabase.CopyAsset(loaderPath, $"{FourdRecUtility.LoaderAssetPath}/{shotName}.asset");
        //
        // AssetDatabase.DeleteAsset(FourdRecUtility.TempPath);
        // AssetDatabase.Refresh();
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
        // - swap channel
        byte[] textureRaw = sourceTexture.GetRawTextureData();
        int textureRawSize = sourceTexture.width * sourceTexture.height;
        for (int i = 0; i < textureRawSize; i++)
        {
            byte tempByte = textureRaw[i * 4 + 2];
            textureRaw[i * 4 + 2] = textureRaw[i * 4];
            textureRaw[i * 4] = tempByte;
        }
        sourceTexture.LoadRawTextureData(textureRaw);
        // - resize
        if (textureSize != sourceTexture.width)
        {
            // TextureScale.Bilinear(sourceTexture, textureSize, textureSize);
        }
        // - compress
        sourceTexture.Compress(false);
        
        // Write
        FourdRecFrame frame = ScriptableObject.CreateInstance<FourdRecFrame>();
        byte[] textureData = sourceTexture.GetRawTextureData();

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
    
    public struct FourdConverter : IJob
    {
        public NativeArray<byte> ImportPathBytes;
        public NativeArray<int> nVerticesCount;
        public NativeArray<Vector3> nPositionArray;
        public NativeArray<Vector2> nUvArray;
        public NativeArray<Color32> TextureData;
        public int TextureSize;
        public void Execute()
        {
            string importPath = Encoding.UTF8.GetString(ImportPathBytes.ToArray());

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
            TJDecompressor decompressor = new TJDecompressor();
            DecompressedImage image = decompressor.Decompress(
                compressedTextureBuffer, TJPixelFormat.BGR, TJFlags.BottomUp);
            var imageData = image.Data;
            Color32[] texturePixels = new Color32[image.Width * image.Height];
            GCHandle handle = GCHandle.Alloc(texturePixels, GCHandleType.Pinned);
            IntPtr pointer = handle.AddrOfPinnedObject();
            for (int i = 0; i < image.Width * image.Height; i++)
            {
                Marshal.Copy(imageData, i * 3, pointer, 3);
                pointer += 4;
                texturePixels[i].a = 255;
            }

            // if (image.Width > TextureSize)
            // {
            //     Debug.Log("RESIZE");
            //     texturePixels = TextureScale.Bilinear(
            //         texturePixels, image.Width, image.Height, TextureSize, TextureSize);
            // }
            handle.Free();
            decompressor.Dispose();
            TextureData.CopyFrom(texturePixels);

            // geo
            nVerticesCount[0] = verticesArrayCount;
            var slice = new NativeSlice<Vector2>(nUvArray, 0, uvArray.Length);
            slice.CopyFrom(uvArray);
            var positionDataArray = new Vector3[verticesArrayCount];
            FourdRecUtility.ConvertFromBytes(positionBuffer, positionDataArray);
            var poSlice = new NativeSlice<Vector3>(nPositionArray, 0, positionDataArray.Length);
            poSlice.CopyFrom(positionDataArray);
        }
    }
}
#endif

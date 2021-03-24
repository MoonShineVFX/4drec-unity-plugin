using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using TurboJpegWrapper;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace _4drec
{
    public class FourdRecImporter : MonoBehaviour
    {
        [MenuItem("4DREC/Import 4DF file")]
        private static void Import()
        {
            // Open folder
            string importFolderPath = EditorUtility.OpenFolderPanel("Import 4DREC file", "", "");
            string shotName = Path.GetFileName(importFolderPath);
            string[] importFilePaths = Directory.GetFiles(importFolderPath);

            if (importFilePaths.Length == 0) return;

            // Create directories
            Directory.CreateDirectory(FourdRecUtility.TempPath);
            Directory.CreateDirectory(FourdRecUtility.AssetBundlePath);
            Directory.CreateDirectory(FourdRecUtility.LoaderAbPath);
            Directory.CreateDirectory(FourdRecUtility.LoaderAssetPath);
        
            // Job define
            const int textureOriginalWidth = 4096;
            const int textureResizeWidth = 1024;
            const int maxMeshVerticesCount = 300000;
            const int poolSize = 100;
            const int importPathMaxLength = 1024;
            
            int importFilePathIndex = 0;
            int completedJobsCount = 0;
            string[] generatedAssetPaths = new string[importFilePaths.Length];
            FourdRecDecompressJobHandle[] threadedJobs = new FourdRecDecompressJobHandle[poolSize];
            
            // Shared memory
            NativeArray<byte>[] importPathBytesCluster = new NativeArray<byte>[poolSize];
            NativeArray<int>[] importPathBytesCountCluster = new NativeArray<int>[poolSize];
            NativeArray<int>[] verticesCount = new NativeArray<int>[poolSize];
            NativeArray<Vector3>[] positionArrayCluster = new NativeArray<Vector3>[poolSize];
            NativeArray<Vector2>[] uvArrayCluster = new NativeArray<Vector2>[poolSize];
            NativeArray<Color32>[] textureColorArrayCluster = new NativeArray<Color32>[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                importPathBytesCluster[i] = new NativeArray<byte>(importPathMaxLength, Allocator.Persistent);
                verticesCount[i] = new NativeArray<int>(1, Allocator.Persistent);
                positionArrayCluster[i] = new NativeArray<Vector3>(maxMeshVerticesCount, Allocator.Persistent);
                uvArrayCluster[i] = new NativeArray<Vector2>(maxMeshVerticesCount, Allocator.Persistent);
                textureColorArrayCluster[i] = 
                    new NativeArray<Color32>(textureOriginalWidth * textureOriginalWidth, Allocator.Persistent);
                importPathBytesCountCluster[i] = new NativeArray<int>(1, Allocator.Persistent);
            }
            
            // Resize gpu prepare
            Texture2D originalTexture = new Texture2D(textureOriginalWidth, textureOriginalWidth);
            RenderTexture renderTexture = RenderTexture.GetTemporary(textureResizeWidth, textureResizeWidth);
            RenderTexture.active = renderTexture;
            
            // Execute
            while (completedJobsCount != importFilePaths.Length)
            {
                // int remainImportFilePathsCount = importFilePaths.Length - completedJobsCount;
                // int realPoolSize = remainImportFilePathsCount < poolSize ? remainImportFilePathsCount : poolSize;
                //
                // // Create jobs
                // if (importFilePathIndex == 0)
                // {
                //     for (int i = 0; i < realPoolSize; i++)
                //     {
                //         string file = importFilePaths[importFilePathIndex];
                //         importFilePathIndex++;
                //         byte[] fileBytes = Encoding.UTF8.GetBytes(file);
                //         var slice = new NativeSlice<byte>(importPathBytesCluster[i], 0, fileBytes.Length);
                //         slice.CopyFrom(fileBytes);
                //         importPathBytesCountCluster[i][0] = fileBytes.Length;
                //         var job = new FourdRecDecompressJob()
                //         {
                //             ImportPathBytes = importPathBytesCluster[i],
                //             PositionArray = positionArrayCluster[i],
                //             VerticesCount = verticesCount[i],
                //             UvArray = uvArrayCluster[i],
                //             TextureColorArray = textureColorArrayCluster[i],
                //             ImportPathBytesCount = importPathBytesCountCluster[i]
                //         };
                //         threadedJobs[i] = job.Schedule();
                //     }
                // }

                // Complete jobs callback
                for (int i = 0; i < threadedJobs.Length; i++)
                {
                    EditorUtility.DisplayProgressBar(
                        "Import Files", 
                        $"Decompressing 4DF frame - {completedJobsCount + 1}",
                        (float)(completedJobsCount + 1) / importFilePaths.Length
                    );
            
                    // Wait job and create asset
                    FourdRecDecompressJobHandle jobHandle = threadedJobs[i];
                    if (jobHandle.ImportFilePath != null)
                    {
                        jobHandle.Handle.Complete();
                        completedJobsCount++;

                        // texture resize
                        originalTexture.SetPixels32(textureColorArrayCluster[i].ToArray());
                        originalTexture.Apply();
                        Graphics.Blit(originalTexture, renderTexture);
                        Texture2D scaledTexture = new Texture2D(textureResizeWidth, textureResizeWidth, TextureFormat.RGB24, false)
                        {
                            filterMode = FilterMode.Bilinear,
                            wrapMode = TextureWrapMode.Clamp
                        };
                        scaledTexture.ReadPixels(new Rect(0, 0, textureResizeWidth, textureResizeWidth), 0, 0);
                        scaledTexture.Apply();
                        scaledTexture.Compress(true);
            
                        // asset apply
                        FourdRecFrame frame = ScriptableObject.CreateInstance<FourdRecFrame>();
                        frame.verticesCount = verticesCount[i][0];
                        frame.textureData = scaledTexture.GetRawTextureData();
                        frame.uvArray = new NativeSlice<Vector2>(uvArrayCluster[i], 0, frame.verticesCount).ToArray();
                        frame.positionArray = new NativeSlice<Vector3>(positionArrayCluster[i], 0, frame.verticesCount).ToArray();
                    
                        string importPath = jobHandle.ImportFilePath;
                        string assetPath = $"{FourdRecUtility.TempPath}/{Path.GetFileNameWithoutExtension(importPath)}.asset";
                        AssetDatabase.CreateAsset(frame, assetPath);
                        generatedAssetPaths[completedJobsCount - 1] = assetPath;
                
                        DestroyImmediate(scaledTexture);
                    }

                    // Create job if not completed
                    if (completedJobsCount == importFilePaths.Length) break;
                    if (importFilePathIndex == importFilePaths.Length) continue;
                    string file = importFilePaths[importFilePathIndex];
                    importFilePathIndex++;
                    byte[] fileBytes = Encoding.UTF8.GetBytes(file);
                    var slice = new NativeSlice<byte>(importPathBytesCluster[i], 0, fileBytes.Length);
                    slice.CopyFrom(fileBytes);
                    importPathBytesCountCluster[i][0] = fileBytes.Length;
                    var nextJob = new FourdRecDecompressJob()
                    {
                        ImportPathBytes = importPathBytesCluster[i],
                        PositionArray = positionArrayCluster[i],
                        VerticesCount = verticesCount[i],
                        UvArray = uvArrayCluster[i],
                        TextureColorArray = textureColorArrayCluster[i],
                        ImportPathBytesCount = importPathBytesCountCluster[i]
                    };
                    threadedJobs[i] = new FourdRecDecompressJobHandle()
                    {
                        Handle = nextJob.Schedule(),
                        ImportFilePath = file
                    };
                }
            }

            // Clean and save
            for (int i = 0; i < poolSize; i++)
            {
                importPathBytesCluster[i].Dispose();
                importPathBytesCountCluster[i].Dispose();
                positionArrayCluster[i].Dispose();
                verticesCount[i].Dispose();
                uvArrayCluster[i].Dispose();
                textureColorArrayCluster[i].Dispose();
            }
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(renderTexture);
            DestroyImmediate(originalTexture);
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
        
            // Turn asset to bundle
            BuildAssetBundle(generatedAssetPaths, shotName);
        
            // Create loader
            int startFrame = int.Parse(Path.GetFileNameWithoutExtension(generatedAssetPaths[0]));
            int endFrame = int.Parse(Path.GetFileNameWithoutExtension(generatedAssetPaths[generatedAssetPaths.Length - 1]));
            FourdRecLoader loader = ScriptableObject.CreateInstance<FourdRecLoader>();
            loader.shotName = shotName;
            loader.startFrame = startFrame;
            loader.endFrame = endFrame;
            loader.textureSize = textureResizeWidth;
            loader.textureFormat = TextureFormat.DXT1;
            string loaderPath = $"{FourdRecUtility.LoaderAbPath}/{shotName}.asset";
            AssetDatabase.CreateAsset(loader, loaderPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.CopyAsset(loaderPath, $"{FourdRecUtility.LoaderAssetPath}/{shotName}.asset");
            AssetDatabase.DeleteAsset(FourdRecUtility.TempPath);
            AssetDatabase.Refresh();
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
                FourdRecUtility.AssetBundlePath,
                new []{bundleBuild},
                BuildAssetBundleOptions.ChunkBasedCompression, 
                BuildTarget.StandaloneWindows);
        }

        private struct FourdRecDecompressJob : IJob
        {
            public NativeArray<byte> ImportPathBytes;
            public NativeArray<int> ImportPathBytesCount;
            public NativeArray<int> VerticesCount;
            public NativeArray<Vector3> PositionArray;
            public NativeArray<Vector2> UvArray;
            public NativeArray<Color32> TextureColorArray;
            
            public void Execute()
            {
                NativeSlice<byte> importPathBytesSlice = 
                    new NativeSlice<byte>(ImportPathBytes, 0, ImportPathBytesCount[0]);
                string importPath = Encoding.UTF8.GetString(importPathBytesSlice.ToArray());

                // Get raw buffer
                const int geoStartCursor = 64;
                const int textureStartCursor = 68;
                const int headerLength = 1024;
                byte[] fileBuffer = File.ReadAllBytes(importPath);

                Int32 geoCompressedSize = BitConverter.ToInt32(fileBuffer, geoStartCursor);
                Int32 textureCompressedSize = BitConverter.ToInt32(fileBuffer, textureStartCursor);

                byte[] compressedGeoBuffer = new byte[geoCompressedSize];
                Array.Copy(
                    fileBuffer, headerLength, 
                    compressedGeoBuffer, 0, 
                    geoCompressedSize
                );

                byte[] compressedTextureBuffer = new byte[textureCompressedSize];
                Array.Copy(
                    fileBuffer, headerLength + geoCompressedSize,
                    compressedTextureBuffer,0, 
                    textureCompressedSize
                );

                // Geo
                byte[] geoBuffer = lz4.Decompress(compressedGeoBuffer);
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
                DecompressedImage rawImage = decompressor.Decompress(
                    compressedTextureBuffer, TJPixelFormat.BGR, TJFlags.BottomUp);
                byte[] rawImageData = rawImage.Data;
                Color32[] textureColorArray = new Color32[rawImage.Width * rawImage.Height];
                GCHandle handle = GCHandle.Alloc(textureColorArray, GCHandleType.Pinned);
                IntPtr pointer = handle.AddrOfPinnedObject();
                for (int i = 0; i < rawImage.Width * rawImage.Height; i++)
                {
                    Marshal.Copy(rawImageData, i * 3, pointer, 3);
                    pointer += 4;
                    textureColorArray[i].a = 255;
                }
                handle.Free();
                decompressor.Dispose();
                TextureColorArray.CopyFrom(textureColorArray);

                // Geo
                VerticesCount[0] = verticesArrayCount;
                NativeSlice<Vector2> uvArraySlice = new NativeSlice<Vector2>(UvArray, 0, uvArray.Length);
                uvArraySlice.CopyFrom(uvArray);
                Vector3[] positionArray = new Vector3[verticesArrayCount];
                FourdRecUtility.ConvertFromBytes(positionBuffer, positionArray);
                NativeSlice<Vector3> positionArraySlice = 
                    new NativeSlice<Vector3>(PositionArray, 0, positionArray.Length);
                positionArraySlice.CopyFrom(positionArray);
            }
        }

        private struct FourdRecDecompressJobHandle
        {
            public JobHandle Handle;
            public string ImportFilePath;
        }
    }
}
#endif

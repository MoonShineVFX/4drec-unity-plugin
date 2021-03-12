using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;


[ScriptedImporter(1, "4df")]
public class FourdRecImporter : ScriptedImporter
{
    public int textureResize = 1024;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        // Create asset
        Debug.LogFormat("Create asset: {0}", ctx.assetPath);
        var profiler = new FourdProfiler();
        
        FourdRecData data = ConvertFourdRecFrameToFourdRData(ctx.assetPath);
        
        profiler.Stop("Total");

        // Apply
        EditorUtility.SetDirty(this);
        ctx.AddObjectToAsset("4drec_frame", data);
        ctx.SetMainObject(data);
    }
    
    FourdRecData ConvertFourdRecFrameToFourdRData(string fourdRecFramePath)
    {
        var profiler = new FourdProfiler();
        // Get raw buffer
        byte[] fileBuffer = File.ReadAllBytes(fourdRecFramePath);

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
        TextureScale.Bilinear(sourceTexture, textureResize, textureResize);
        sourceTexture.Compress(false);
        
        profiler.Mark("Texture");
        // Apply to FourdRecData
        FourdRecData data = ScriptableObject.CreateInstance<FourdRecData>();
        data.vertexCount = verticesArrayCount;
        data.textureSize = textureResize;
        data.textureFormat = sourceTexture.format;
        data.CompressedVertices = lz4.Compress(positionBuffer);
        data.CompressedUvs = lz4.Compress(FourdUtility.ConvertToBytes(uvArray, uvBuffer.Length));
        data.CompressedTexture = sourceTexture.GetRawTextureData().ToArray();

        data.Uvs = $"{data.CompressedUvs.Length / 1024}k";
        data.Vertices = $"{data.CompressedVertices.Length / 1024}k";
        data.Texture = $"{data.CompressedTexture.Length / 1024}k";

        profiler.Mark("Create data");
        // Return
        DestroyImmediate(sourceTexture);
        return data;
    }

}

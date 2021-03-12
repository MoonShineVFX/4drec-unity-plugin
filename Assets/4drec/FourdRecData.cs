using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FourdRecData : ScriptableObject
{
    public int vertexCount;
    public int textureSize;
    public TextureFormat textureFormat;

    [Header("File Size")]
    public string Vertices;
    public string Uvs;
    public string Texture;

    [HideInInspector]
    public byte[] CompressedVertices;
    [HideInInspector]
    public byte[] CompressedUvs;
    [HideInInspector]
    public byte[] CompressedTexture;
}

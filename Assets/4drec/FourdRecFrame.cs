using UnityEngine;

namespace _4drec
{
    public class FourdRecFrame : ScriptableObject
    {
        [HideInInspector]
        public int verticesCount;
        [HideInInspector]
        public byte[] textureData;
        [HideInInspector]
        public Vector3[] positionArray;
        [HideInInspector]
        public Vector2[] uvArray;
    }
}

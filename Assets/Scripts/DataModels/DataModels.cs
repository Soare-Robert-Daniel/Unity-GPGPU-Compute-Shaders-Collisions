using System;
using UnityEngine;

namespace DataModels
{
    public enum ModelType
    {
        Sphere = 1,
        Triangle
    }
    [Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct SphereModel
    {
        public float radius;
        public Vector3 center;
    }
    
    [Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct TriangleModel
    {
        public Vector3 center;
        public float verticesNum;
        public float indicesNum;
        public Vector3[] vertices;
        public int[] indices;
    }
    
    [Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct AABBModel
    {
        public Vector3 max;
        public Vector3 min;
    }
    
}
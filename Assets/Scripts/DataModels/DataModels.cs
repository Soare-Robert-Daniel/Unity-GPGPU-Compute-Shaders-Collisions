using System;
using UnityEngine;

namespace DataModels
{
    public enum ModelType
    {
        Sphere = 1,
        Triangle = 2
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
        public int verticesNum;
        public int indicesNum;
        public Vector3[] vertices;
        public int[] indices;
    }

    [Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct TriangleModelSimple
    {
        public Vector3 center;

        public int index;
        public int verticesNum;
        public int indicesNum;
        public int verticesOffset;
        public int indicesOffset;
    }

    [Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct AABBModel
    {
        public Vector3 max;
        public Vector3 min;
    }

    [Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct PhysicsData
    {
        public Vector3 force;
    }
}
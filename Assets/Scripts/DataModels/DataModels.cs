using System;
using Unity.Collections;
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
        public int verticesNum;
        public int indicesNum;
        public Vector3[] vertices;
        public int[] indices;
    }

    public struct NativeTriangleModel
    {
        public Vector3 center;
        public int verticesNum;
        public int indicesNum;
        public NativeArray<Vector3> vertices;
        public NativeArray<int> indices;

        public NativeTriangleModel(TriangleModel t)
        {
            center = t.center;
            verticesNum = t.verticesNum;
            indicesNum = t.indicesNum;
            vertices = new NativeArray<Vector3>(verticesNum, Allocator.TempJob);
            indices = new NativeArray<int>(indicesNum, Allocator.TempJob);

            for (int i = 0; i < verticesNum; i++)
            {
                vertices[i] = t.vertices[i];
            }
            
            for (int i = 0; i < indicesNum; i++)
            {
                indices[i] = t.indices[i];
            }
        }
    }
    
    [Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct AABBModel
    {
        public Vector3 max;
        public Vector3 min;
    }
    
}
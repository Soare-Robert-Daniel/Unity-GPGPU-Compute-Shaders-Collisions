using System;
using UnityEngine;

namespace DataModels
{
    public enum ModelType
    {
        Sphere = 1,
        Cube,
        Cylinder
    }
    [Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct SphereModel
    {
        public float radius;
        public Vector3 center;
    }
}
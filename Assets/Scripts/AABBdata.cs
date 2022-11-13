
    using System;
    using UnityEngine;

    [Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct AABBdata
    {
        public Vector3 max;
        public Vector3 min;
        public Vector3 localMax;
        public Vector3 localMin;
    }

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DataModels
{
    [Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct ObjectData
    {
        public float mass;
        public Vector3 velocity;
        public Vector3 rotationVelocity;
    }
    
    [Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct CollisionData
    {
        public Vector3[] normals;
        public float[] depths;
        public Vector3[] points;

        public CollisionData(int size)
        {
            normals = new Vector3[size];
            depths = new float[size];
            points = new Vector3[size];
        }

        public override string ToString()
        {
            return string.Join("\n", normals.Zip(depths, (normal, depth) => normal + " with " + depth));
        }
    }
    public static class ModelsCollision
    {
        public static CollisionData GetSphereToSphereCollision(SphereModel a, SphereModel b)
        {
            var data = new CollisionData(1);
            var distance = Vector3.Distance(a.center, b.center);

            if (distance < a.radius + b.radius)
            {
                data.normals[0] = (a.center - b.center).normalized;
                data.depths[0] = Mathf.Abs(a.radius + b.radius - distance);
                data.points[0] = a.center - data.normals[0] * data.depths[0];
            }

            return data;
        }

        private static Vector2 GetIntervalForAxisOnTriangle(Vector3[] vertices, Vector3 axis)
        {
            // Proiecteaza toate punctele pe axa si ia doar extremitatile
            // Compararea se face prin lungimea proiectiei fiecarui vector/varf pe axa
            var result = Vector2.zero;

            result.x = Vector3.Dot(axis, vertices[0]);
            result.y = result.x;
            for (var i = 1; i < vertices.Length; ++i)
            {
                var value = Vector3.Dot(axis, vertices[i]);
                result.x = Mathf.Min(result.x, value);
                result.y = Mathf.Max(result.y, value);
            }

            //Debug.Log(result);
            //Debug.Log($"Vert {string.Join("-", vertices)} with {axis}");
            return result;
        }

        private static bool AreTriangleTriangleOverlappingOnAxis(Vector3[] triVert1, Vector3[] triVert2, Vector3 axis)
        {
            if (axis.sqrMagnitude < 0.0001f)
            {
                // Debug.Log("Invalid axis");
                return false;
            }

            // Calculeaza intervalul pe proiectii pentru fiecare triunghi la o axa data
            var interval1 = GetIntervalForAxisOnTriangle(triVert1, axis);
            var interval2 = GetIntervalForAxisOnTriangle(triVert2, axis);

            // Verifica daca se intersecteaza
            return interval1.y >= interval2.x && interval1.x <= interval2.y;
        }

        private static Vector3 TryFindValidAxis(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var ab = a - b;
            var cd = c - d;

            var axis = Vector3.Cross(ab, cd).normalized;

            if (axis.sqrMagnitude >= 0.0001f)
            {
                return axis;
            }

            var support = Vector3.Cross(ab, c - a);
            axis = Vector3.Cross(ab, support).normalized;

            if (axis.sqrMagnitude >= 0.0001f)
            {
                return axis;
            }

            return Vector3.zero;
        }

        private static bool TriangleTriangleIntersection(Vector3[] triVert1, Vector3[] triVert2)
        {
            var result = false;

            // Triangle 1 Normal
            {
                var AB = triVert1[1] - triVert1[0];
                var AC = triVert1[2] - triVert1[0];
                var N = Vector3.Cross(AB, AC).normalized;

                // Compare to triangle Normal
                result = AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, N);
            }

            // Debug.Log($"Normal T1 {result}");

            // Triangle 2 Normal
            {
                var AB = triVert2[1] - triVert2[0];
                var AC = triVert2[2] - triVert2[0];
                var N = Vector3.Cross(AB, AC).normalized;

                // Compare to Normal
                result = result && AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, N);
            }

            // Debug.Log($"Normal T1 {result}");

            // Edge cross products 3x3
            {
                // T1
                var a1 = triVert1[0];
                var b1 = triVert1[1];
                var c1 = triVert1[2];

                // T2
                var a2 = triVert2[0];
                var b2 = triVert2[1];
                var c2 = triVert2[2];

                // (a1 - b1) x (...)
                {
                    result = result &&
                             AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(a1, b1, a2, b2));
                    result = result &&
                             AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(a1, b1, b2, c2));
                    result = result &&
                             AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(a1, b1, c2, a1));
                }

                // (b1 - c1) x (...)
                {
                    result = result &&
                             AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(b1, c1, a2, b2));
                    result = result &&
                             AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(b1, c1, b2, c2));
                    result = result &&
                             AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(b1, c1, c2, a1));
                }

                // (c1 - a1) x (...)
                {
                    result = result &&
                             AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(c1, a1, a2, b2));
                    result = result &&
                             AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(c1, a1, b2, c2));
                    result = result &&
                             AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(c1, a1, c2, a1));
                }
            }

            return result;
        }


        public static CollisionData HasCollisionTriangleToTriangleOnTriangles(TriangleModel a, TriangleModel b)
        {
            var collisionOnTriangles = new List<int>();

            for (var i = 0; i < a.indicesNum - 1; i += 3)
            {
                var tri1 = new[]
                    {a.vertices[a.indices[i]], a.vertices[a.indices[i + 1]], a.vertices[a.indices[i + 2]]};
                for (var j = 0; j < b.indicesNum - 1; j += 3)
                {
                    var tri2 = new[]
                        {b.vertices[b.indices[j]], b.vertices[b.indices[j + 1]], b.vertices[b.indices[j + 2]]};

                    if (TriangleTriangleIntersection(tri1, tri2))
                    {
                        collisionOnTriangles.Add(i / 3);
                        // Debug.Log($"Tri 1: {string.Join("-", tri1)} | Tri 2: {string.Join("-", tri2)}");
                        break;
                    }
                }
            }

            var data = new CollisionData(collisionOnTriangles.Count);
            
            var index = 0;
            foreach (var triangle in collisionOnTriangles)
            {
                var tri = new[]
                    {a.vertices[a.indices[triangle]], a.vertices[a.indices[triangle + 1]], a.vertices[a.indices[triangle + 2]]};
                var AB = tri[1] - tri[0];
                var AC = tri[2] - tri[0];
                var N = Vector3.Cross(AB, AC).normalized;
                data.normals[index] = N;
                data.depths[index] = 0.1f;
                data.points[index] = (tri[0] + tri[1] + tri[2]) / 3;
                index += 1;
            }

            return data;
        }

        private static Vector3 GetClosest(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            var d1 = p - a;
            var d2 = p - b;
            var d3 = p - c;

            var dist1 = d1.sqrMagnitude;
            var dist2 = d2.sqrMagnitude;
            var dist3 = d3.sqrMagnitude;

            var min = Mathf.Min(dist1, dist2);
            min = Mathf.Min(min, dist3);

            if (Math.Abs(min - dist1) < 0.00001f)
            {
                return d1;
            }

            if (Math.Abs(min - dist2) < 0.00001f)
            {
                return d2;
            }

            return d3;
        }

        private static bool IsPointInTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
        {
            var _a = a - p;
            var _b = b - p;
            var _c = c - p;

            var u = Vector3.Cross(_b, _c);
            var v = Vector3.Cross(_c, _a);
            var w = Vector3.Cross(_a, _b);

            if (Vector3.Dot(u, v) < 0f)
            {
                return false;
            }

            if (Vector3.Dot(u, w) < 0f)
            {
                return false;
            }

            return true;
        }

        private static Vector3 ClosestPointToLine(Vector3 a, Vector3 b, Vector3 p)
        {
            var t = Vector3.Dot(p - a, b - a) / Vector3.Dot(b - a, b - a);

            t = Mathf.Clamp01(t);

            return a + t * (b - a);
        }

        private static Vector3 TrianglePointIntersection(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
        {
            // Create a plane
            var planeNormal = Vector3.Cross(b - a, c - a).normalized;
            var planeDistance = Vector3.Dot(planeNormal, a);

            // Create proiectia punctului pe plan printr-un vector de translatie pe directia normalei planului
            var proj = p - (Vector3.Dot(p, planeNormal) - planeDistance) * planeNormal;

            if (IsPointInTriangle(a, b, c, proj))
            {
                return proj;
            }

            var pAB = ClosestPointToLine(b, a, p);
            var pBC = ClosestPointToLine(c, b, p);
            var pCA = ClosestPointToLine(a, c, p);

            return GetClosest(p, pAB, pBC, pCA);
        }

        public static CollisionData HasCollisionTriangleToSphere(TriangleModel a, SphereModel b)
        {
            var collisionOnTriangles = new List<int>();
            var collisionDepth = new List<float>();

            for (var i = 0; i < a.indicesNum - 1; i += 3)
            {
                var closesPoint = TrianglePointIntersection(a.vertices[a.indices[i]], a.vertices[a.indices[i + 1]],
                    a.vertices[a.indices[i + 2]], b.center);
                if ((closesPoint - b.center).sqrMagnitude <= b.radius * b.radius)
                {
                    collisionOnTriangles.Add(i / 3);
                    collisionDepth.Add(b.radius - (closesPoint - b.center).magnitude);
                }
            }

            var data = new CollisionData(collisionOnTriangles.Count);
            
            var index = 0;
            foreach (var triangle in collisionOnTriangles)
            {
                var tri = new[]
                    {a.vertices[a.indices[triangle]], a.vertices[a.indices[triangle + 1]], a.vertices[a.indices[triangle + 2]]};
                var AB = tri[1] - tri[0];
                var AC = tri[2] - tri[0];
                var N = Vector3.Cross(AB, AC).normalized;
                data.normals[index] = N;
                data.depths[index] = collisionDepth[index];
                data.points[index] = (tri[0] + tri[1] + tri[2]) / 3;
                index += 1;
            }

            return data;
        }

        public static bool IsPointInAABB(Vector3 point, AABBModel cage)
        {
            return cage.min.x <= point.x && point.x <= cage.max.x &&
                   cage.min.y <= point.y && point.y <= cage.max.y &&
                   cage.min.z <= point.z && point.z <= cage.max.z;
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace DataModels
{
    public static class ModelsCollision
    {
        public static bool HasCollisionSphereToSphere(SphereModel a, SphereModel b)
        {
            return Vector3.Distance(a.center, b.center) < (a.radius + b.radius);
        }

        public static Vector3 NormalCollisionSphereToSphere(SphereModel a, SphereModel b)
        {
            var collNormal = (a.center - b.center).normalized;
            // var angleDeg = Vector3.Angle(moveDirection, collNormal);

            return collNormal; // Mathf.Abs(angleDeg) >= 90 ? moveDirection : (collNormal).normalized;
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
                    result = result && AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(a1, b1, a2, b2));
                    result = result && AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(a1, b1, b2, c2));
                    result = result && AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(a1, b1, c2, a1));
                }

                // (b1 - c1) x (...)
                {
                    result = result && AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(b1, c1, a2, b2));
                    result = result && AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(b1, c1, b2, c2));
                    result = result && AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(b1, c1, c2, a1));
                }

                // (c1 - a1) x (...)
                {
                    result = result && AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(c1, a1, a2, b2));
                    result = result && AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(c1, a1, b2, c2));
                    result = result && AreTriangleTriangleOverlappingOnAxis(triVert1, triVert2, TryFindValidAxis(c1, a1, c2, a1));
                }
            }
            
            return result;
        }

        
        public static List<int> HasCollisionTriangleToTriangleOnTriangles(TriangleModel a, TriangleModel b)
        {
            var collisionOnTriangles = new List<int>();
            
            for (var i = 0; i < a.indicesNum-1; i += 3)
            {
                var tri1 = new []
                    {a.vertices[a.indices[i]], a.vertices[a.indices[i + 1]], a.vertices[a.indices[i + 2]]};
                for (var j = 0; j < b.indicesNum-1; j+=3)
                {
                    var tri2 = new []
                        {b.vertices[b.indices[j]], b.vertices[b.indices[j + 1]], b.vertices[b.indices[j + 2]]};

                    if (TriangleTriangleIntersection(tri1, tri2))
                    {
                        collisionOnTriangles.Add( i / 3);
                        Debug.Log($"Tri 1: {string.Join("-", tri1)} | Tri 2: {string.Join("-", tri2)}");
                        break;
                    }
                }           
                
            }

            return collisionOnTriangles;
        }
    }
}
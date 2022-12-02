using UnityEngine;

namespace DataModels
{
    public static class ModelsCollision
    {
        public static bool HasCollisionSphereToSphere(SphereModel a, SphereModel b)
        {
            return Vector3.Distance(a.center, b.center) < (a.radius + b.radius);
        }

        public static Vector3 ResolveCollisionSphereToSphere(SphereModel a, SphereModel b, Vector3 moveDirection)
        {
            var collNormal = (a.center - b.center).normalized;
            var angleDeg = Vector3.Angle(moveDirection, collNormal);

            return collNormal; // Mathf.Abs(angleDeg) >= 90 ? moveDirection : (collNormal).normalized;
        }
    }
}
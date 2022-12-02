using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DataModels;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class CPUCollision : MonoBehaviour
{
   public UIHandler uiHandler;
   public bool canRun;
   public float gravityForce;
   public Vector3 gravityDirection;
   
   [SerializeField] private GameObject[] cubes;
   [SerializeField] private AABBdata[] cubesAABB;
   [SerializeField] private bool[] cubesCanMove;

   [SerializeField] private ModelType[] modelTypes;
   
   [SerializeField] private SphereModel[] sphereModels;
   [SerializeField] private GameObject[] spheres;
   
   [SerializeField] private Vector3[] movementDirection;

   private Vector3 _gravity;
   
   private Stopwatch _stopwatch;

   private void Start()
   {
      _gravity = gravityForce * gravityDirection;
      canRun = false;
      _stopwatch = new Stopwatch();
      gravityDirection = gravityDirection.normalized;
      UpdateObjects();
   }

   private void Update()
   {
      if (!canRun)
      {
         return;
      }
      
      UpdateAABB();
      UpdateSphereModels();
      ResetMovement();

      var displacementByGravity = gravityDirection * (gravityForce * Time.deltaTime);
      
      _stopwatch.Reset();
      _stopwatch.Start();

      for (var i = 0; i < modelTypes.Length; i++)
      {
         if (sphereModels[i].center.y - sphereModels[i].radius <= 0)
         {
            continue;
         }
         movementDirection[i] = gravityDirection;
         
         for (var j = 0; j < modelTypes.Length; j++)
         {
            if (i == j)
            {
               continue;
            }
            Debug.Log($"Check {i} and {j}");
            if (modelTypes[i] == ModelType.Sphere && modelTypes[j] == ModelType.Sphere)
            {
               if (ModelsCollision.HasCollisionSphereToSphere(sphereModels[i], sphereModels[j]))
               {
                  Debug.Log($"{i} collide with {j}");
                  // movementDirection[i] = Vector3.zero;
                  movementDirection[i] += 
                      ModelsCollision.ResolveCollisionSphereToSphere(sphereModels[i], sphereModels[j], gravityDirection);
                  Debug.Log(ModelsCollision.ResolveCollisionSphereToSphere(sphereModels[i], sphereModels[j], gravityDirection));
                  movementDirection[i].Normalize();
               }
            }
         }
         
         movementDirection[i] *= Time.deltaTime;
      }
      
      _stopwatch.Stop();
      
      uiHandler.UpdateCPUTime(_stopwatch.Elapsed);
  
      for (var i = 0; i < cubes.Length; i++)
      {
         if (cubesCanMove[i])
         {
            cubes[i].transform.Translate(displacementByGravity);
         }
      }
      
      for (var i = 0; i < spheres.Length; i++)
      {
         spheres[i].transform.Translate(movementDirection[i]);
      }
   }

   public void UpdateObjects()
   {
      cubes = GameObject.FindGameObjectsWithTag("Cube");
      
      cubesAABB = cubes
         .Select(c => c.GetComponent<MeshFilter>().mesh)
         .Select(m => new AABBdata {localMax = m.bounds.max, localMin = m.bounds.min})
         .ToArray();

      cubesCanMove = new bool[cubesAABB.Length];
      
      spheres = GameObject.FindGameObjectsWithTag("Sphere");
      
      sphereModels = new SphereModel[spheres.Length];
      modelTypes = new ModelType[cubes.Length + spheres.Length];
      movementDirection = new Vector3[cubes.Length + spheres.Length];

      for (var i = 0; i < spheres.Length; i++)
      {
         modelTypes[i] = ModelType.Sphere;
         sphereModels[i] = new SphereModel
         {
            center = spheres[i].transform.position,
            radius = spheres[i].transform.lossyScale.x / 2.0f
         };
      }
      
      ResetMovement();
   }

   private void ResetMovement()
   {
      for (var i = 0; i < movementDirection.Length; i++)
      {
         movementDirection[i] = Vector3.zero;
      }
   }

   private void UpdateSphereModels()
   {
      for (var i = 0; i < spheres.Length; i++)
      {
         sphereModels[i].center = spheres[i].transform.position;
      }
   }

   private void UpdateAABB()
   {
      for (var i = 0; i < cubes.Length; i++)
      {
         cubesAABB[i].max = cubes[i].transform.InverseTransformPoint(cubesAABB[i].localMax);
         cubesAABB[i].min = cubes[i].transform.InverseTransformPoint(cubesAABB[i].localMin);
      }
   }

   private bool CanMove(int id, Vector3 displacement)
   {
      var box = new AABBdata { min = cubesAABB[id].min + displacement, max = cubesAABB[id].max + displacement };
      for (var i = 0; i < cubes.Length; i++)
      {
         if (i != id && Intersect(box, cubesAABB[i]))
         {
            return false;
         }
      }
      return true;
   }

   private static bool Intersect(AABBdata a, AABBdata b)
   {
      return a.min.x <= b.max.x &&
             a.max.x >= b.min.x &&
            
             a.min.y <= b.max.y &&
             a.max.y >= b.min.y &&
            
             a.min.z <= b.max.z &&
             a.max.z >= b.min.z;
   }
}

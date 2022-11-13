using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

   private Stopwatch _stopwatch;

   private void Start()
   {
      canRun = false;
      _stopwatch = new Stopwatch();
      UpdateObjects();
   }

   private void Update()
   {
      if (!canRun)
      {
         return;
      }
      
      UpdateAABB();

      var displacementByGravity = gravityDirection * (gravityForce * Time.deltaTime);
      
      _stopwatch.Reset();
      _stopwatch.Start();
      for (var i = 0; i < cubes.Length; i++)
      {
         cubesCanMove[i] = cubes[i].transform.position.y >= 0 && CanMove(i, displacementByGravity);
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
   }

   public void UpdateObjects()
   {
      cubes = GameObject.FindGameObjectsWithTag("Cube");
      
      cubesAABB = cubes
         .Select(c => c.GetComponent<MeshFilter>().mesh)
         .Select(m => new AABBdata {localMax = m.bounds.max, localMin = m.bounds.min})
         .ToArray();

      cubesCanMove = new bool[cubesAABB.Length];
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

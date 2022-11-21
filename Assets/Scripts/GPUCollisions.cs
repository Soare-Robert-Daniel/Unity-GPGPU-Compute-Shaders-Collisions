 using System;
 using System.Collections;
using System.Collections.Generic;
 using System.Diagnostics;
 using System.Linq;
 using UnityEngine;
 using Debug = UnityEngine.Debug;

 public class GPUCollisions : MonoBehaviour
{
    public UIHandler uiHandler;
    public bool canRun;
    public float gravityForce;
    public Vector3 gravityDirection;
   
    [SerializeField] private GameObject[] cubes;
    private AABBdata[] cubesAABB;
    [SerializeField] private int[] cubesCanMove;

    private Stopwatch _stopwatch;

    [SerializeField] private ComputeShader collisionShader;
    private ComputeBuffer boxes;
    private ComputeBuffer canMove;

    private const int AABB_DATA_STRIDE = sizeof(float) * (3 * 4);

    private int kernelId;
    [SerializeField] private int dispatchSize;
    [SerializeField] private uint threadGroupSize;
    

    private void Start()
       {
          canRun = false;
          _stopwatch = new Stopwatch();
          // UpdateObjects();
       }
    
       private void Update()
       {
          if (!canRun)
          {
             return;
          }
          
          UpdateAABB();
    
          var displacementByGravity = gravityDirection * (gravityForce * Time.deltaTime);
          var displacement = new float[3];
          displacement[0] = displacementByGravity.x;
          displacement[1] = displacementByGravity.y;
          displacement[2] = displacementByGravity.z;
          
          _stopwatch.Reset();
          _stopwatch.Start();
          
          boxes.SetData(cubesAABB);
          // canMove.SetData(cubesCanMove);
          collisionShader.SetInt("num_boxes", cubesAABB.Length);
          collisionShader.SetInt("num_per_group", (int)threadGroupSize);
          collisionShader.SetFloats("displacement", displacement);
          
          collisionShader.Dispatch(kernelId, dispatchSize, 1, 1);
          _stopwatch.Stop();
          
          canMove.GetData(cubesCanMove);
          
          uiHandler.UpdateGPUTime(_stopwatch.Elapsed);
      
          for (var i = 0; i < cubes.Length; i++)
          {
             if (cubesCanMove[i] != 0)
             {
                cubes[i].transform.Translate(displacementByGravity);
             }
          }
          
          // for (var i = 0; i < cubesCanMove.Length; i++)
          // {
          //    cubesCanMove[i] = 0;
          // }
       }
    
       public void UpdateObjects()
       {
          Debug.Log("Update Object");
          cubes = GameObject.FindGameObjectsWithTag("Cube");
          
          cubesAABB = cubes
             .Select(c => c.GetComponent<MeshFilter>().mesh)
             .Select(m => new AABBdata {localMax = m.bounds.max, localMin = m.bounds.min})
             .ToArray();
    
          cubesCanMove = new int[cubesAABB.Length];
          for (var i = 0; i < cubesCanMove.Length; i++)
          {
             cubesCanMove[i] = 0;
          }
          
          boxes = new ComputeBuffer(
             cubesAABB.Length,
             AABB_DATA_STRIDE,
             ComputeBufferType.Structured,
             ComputeBufferMode.Immutable
          );

          canMove = new ComputeBuffer(
             cubesAABB.Length,
             sizeof(int),
             ComputeBufferType.Structured,
             ComputeBufferMode.Dynamic
          );

          kernelId = collisionShader.FindKernel("main");
          
          collisionShader.GetKernelThreadGroupSizes(kernelId, out threadGroupSize, out _, out _);
          dispatchSize = Mathf.CeilToInt((float)cubesAABB.Length / threadGroupSize);
          
          collisionShader.SetBuffer(kernelId,"boxes", boxes);
          collisionShader.SetBuffer(kernelId, "can_move_boxes", canMove);
          
          collisionShader.SetInt("num_boxes", cubesAABB.Length);
          collisionShader.SetInt("num_per_group", (int)threadGroupSize);
       }
       

   

       private void UpdateAABB()
       {
          for (var i = 0; i < cubes.Length; i++)
          {
             cubesAABB[i].max = cubes[i].transform.InverseTransformPoint(cubesAABB[i].localMax);
             cubesAABB[i].min = cubes[i].transform.InverseTransformPoint(cubesAABB[i].localMin);
          }
       }

       private void OnDisable()
       {
          boxes.Dispose();
          canMove.Dispose();
       }
}

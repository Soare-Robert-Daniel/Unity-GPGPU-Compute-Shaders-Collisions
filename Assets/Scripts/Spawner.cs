using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private CPUCollision cpuCollision;
    [SerializeField] private GPUCollisions gpuCollisions;

    [Header("Spawn Matrix")]
    public int rows;
    public int columns;
    public int height;
    public Vector3 offsetRow;
    public Vector3 offsetColumn;
    public Vector3 offsetHeight;
    
    [Header("Cubes")]
    public int cubesNumber;
    [SerializeField] private GameObject cubeTemplate;

    private void Start()
    {
        var k = 0;

        var posOrigin = transform.position;
        
        for (var h = 0; h < height; ++h)
        {
            for (var c = 0; c < columns; ++c)
            {
                for (var r = 0; r < rows; ++r)
                {
                    if (k >= cubesNumber) continue;
                    
                    k++;

                    Instantiate(
                        cubeTemplate, 
                        posOrigin + r * offsetRow + c * offsetColumn + h * offsetHeight, 
                        Quaternion.identity
                    );
                }
            }
        }

        if (cpuCollision.isActiveAndEnabled)
        {
            cpuCollision.UpdateObjects();
            cpuCollision.canRun = true;
        }

        if (gpuCollisions.isActiveAndEnabled)
        {
            gpuCollisions.UpdateObjects();
            gpuCollisions.canRun = true;
        }
    }
}

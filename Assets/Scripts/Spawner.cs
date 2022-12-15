using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

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
    public bool random;
    public float randomRange;
    
    [Header("Cubes")]
    public int cubesNumber;
    [SerializeField] private GameObject cubeTemplate;
    
    [Header("Spheres")]
    public int spheresNumber;
    [SerializeField] private GameObject sphereTemplate;
    
    [Header("Cylinders")]
    public int cylindersNumber;
    [SerializeField] private GameObject cylinderTemplate;

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
                    var randomXZ = Vector3.zero;
                    if (random)
                    {
                        randomXZ.x += Random.Range(0f, randomRange);
                        randomXZ.z += Random.Range(0f, randomRange);
                    }
                    
                    if (k < cubesNumber)
                    {
                        k++;

                        Instantiate(
                            cubeTemplate, 
                            posOrigin + r * offsetRow + c * offsetColumn + h * offsetHeight + randomXZ, 
                            new Quaternion(randomXZ.x, randomXZ.y, 0, 1)
                        );
                    } 
                    else if (k < cubesNumber + spheresNumber)
                    {
                        k++;

                        Instantiate(
                            sphereTemplate, 
                            posOrigin + r * offsetRow + c * offsetColumn + h * offsetHeight + randomXZ, 
                            Quaternion.identity
                        );
                    }
                    else if (k < cubesNumber + spheresNumber + cylindersNumber)
                    {
                        k++;

                        Instantiate(
                            cylinderTemplate, 
                            posOrigin + r * offsetRow + c * offsetColumn + h * offsetHeight + randomXZ, 
                            new Quaternion(randomXZ.x, randomXZ.y, 0, 1)
                        );
                    }
                }
            }
        }

        if (cpuCollision.isActiveAndEnabled)
        {
            cpuCollision.InitObjects();
            cpuCollision.canRun = true;
        }

        if (gpuCollisions.isActiveAndEnabled)
        {
            gpuCollisions.UpdateObjects();
            gpuCollisions.canRun = true;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using SysRandom = System.Random;

public class Spawner : MonoBehaviour
{
    [SerializeField] private CPUCollision cpuCollision;
    [SerializeField] private GPUCollisionsV2 gpuCollisions;

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

        var positions = new List<Vector3>();

        for (var h = 0; h < height; ++h)
        {
            for (var c = 0; c < columns; ++c)
            {
                for (var r = 0; r < rows; ++r)
                {
                    positions.Add(posOrigin + r * offsetRow + c * offsetColumn + h * offsetHeight );
                }
            }
        }

        Shuffle(positions);

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
                       

                        Instantiate(
                            cubeTemplate, 
                            positions[k], 
                            new Quaternion(randomXZ.x, randomXZ.y, 0, 1)
                        );
                        
                        k++;
                    } 
                    else if (k < cubesNumber + spheresNumber)
                    {
                        

                        Instantiate(
                            sphereTemplate, 
                            positions[k], 
                            Quaternion.identity
                        );
                        
                        k++;
                    }
                    else if (k < cubesNumber + spheresNumber + cylindersNumber)
                    {
                        

                        Instantiate(
                            cylinderTemplate, 
                            positions[k], 
                            new Quaternion(randomXZ.x, randomXZ.y, 0, 1)
                        );
                        
                        k++;
                    }
                }
            }
        }

        if (cpuCollision.isActiveAndEnabled)
        {
            cpuCollision.InitObjects();
            // cpuCollision.canRun = true;
        }

        if (gpuCollisions.isActiveAndEnabled)
        {
            gpuCollisions.InitObjects();
            // gpuCollisions.canRun = true;
        }
    }

    public static void Shuffle<T>(IList<T> list)  
    {  
        var rng = new SysRandom();
        int n = list.Count;  
        while (n > 1) {  
            n--;  
            int k = rng.Next(n + 1);  
            (list[k], list[n]) = (list[n], list[k]);
        }  
    }
}

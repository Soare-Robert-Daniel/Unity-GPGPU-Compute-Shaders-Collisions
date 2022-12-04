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
    [SerializeField] private GameObject[] spheresGameObjects;

    [SerializeField] private TriangleModel[] triangleModels;
    [SerializeField] private GameObject[] trianglesGameObjects;
    [SerializeField] private Mesh[] triangleMeshes;

    [SerializeField] private Vector3[] movementDirection;

    private Vector3 _gravity;

    private Stopwatch _stopwatch;

    private void Start()
    {
        _gravity = gravityForce * gravityDirection;
        canRun = false;
        _stopwatch = new Stopwatch();
        gravityDirection = gravityDirection.normalized;
        InitObjects();
    }

    private void Update()
    {
        if (!canRun)
        {
            return;
        }

        UpdateAABB();
        UpdateSphereModels();
        UpdateTriangleModels();
        ResetMovement();

        var displacementByGravity = gravityDirection * (gravityForce * Time.deltaTime);

        _stopwatch.Reset();
        _stopwatch.Start();

        for (var i = 0; i < modelTypes.Length; i++)
        {
            if (i < sphereModels.Length)
            {
                if (sphereModels[i].center.y - sphereModels[i].radius <= 0)
                {
                    continue;
                }
            } else if (modelTypes[i] == ModelType.Triangle)
            {
                if (trianglesGameObjects[i + spheresGameObjects.Length].transform.position.y - 0.5f <= 0)
                {
                    continue;
                }
            }

            movementDirection[i] = gravityDirection;

            for (var j = 0; j < modelTypes.Length; j++)
            {
                if (i == j)
                {
                    continue;
                }

                // Debug.Log($"Check {i} and {j}");
                if (modelTypes[i] == ModelType.Sphere && modelTypes[j] == ModelType.Sphere)
                {
                    if (ModelsCollision.HasCollisionSphereToSphere(sphereModels[i], sphereModels[j]))
                    {
                        Debug.Log($"{i} collide with {j}");
                        // movementDirection[i] = Vector3.zero;
                        movementDirection[i] +=
                            ModelsCollision.NormalCollisionSphereToSphere(sphereModels[i], sphereModels[j]);
                        // Debug.Log(ModelsCollision.NormalCollisionSphereToSphere(sphereModels[i], sphereModels[j], gravityDirection));
                        movementDirection[i].Normalize();
                    }
                }
                else if (modelTypes[i] == ModelType.Triangle && modelTypes[j] == ModelType.Triangle)
                {
                    var collision =
                        ModelsCollision.HasCollisionTriangleToTriangleOnTriangles(
                            triangleModels[i - sphereModels.Length], triangleModels[j - sphereModels.Length]);
                    if (collision.Count > 0)
                    {
                        movementDirection[i] = Vector3.zero;
                        Debug.Log(string.Join(" - ", collision));
                        
                        var vertices = triangleModels[i - sphereModels.Length].vertices;
                        var indices = triangleModels[i - sphereModels.Length].indices;
                        foreach (var triangle in collision)
                        {
                            var tri = new []
                                {vertices[indices[triangle]], vertices[indices[triangle + 1]],vertices[indices[triangle + 2]]};
                            var AB = tri[1] - tri[0];
                            var AC = tri[2] - tri[0];
                            var N = Vector3.Cross(AB, AC).normalized;
                            movementDirection[i] += N;
                            movementDirection[i].Normalize();
                        }
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

        for (var i = 0; i < spheresGameObjects.Length; i++)
        {
            spheresGameObjects[i].transform.Translate(movementDirection[i]);
        }
        
        for (var i = 0; i < trianglesGameObjects.Length; i++)
        {
            trianglesGameObjects[i].transform.Translate(movementDirection[i + spheresGameObjects.Length ]);
        }
    }

    public void InitObjects()
    {
        cubes = GameObject.FindGameObjectsWithTag("Cube");

        cubesAABB = cubes
            .Select(c => c.GetComponent<MeshFilter>().mesh)
            .Select(m => new AABBdata {localMax = m.bounds.max, localMin = m.bounds.min})
            .ToArray();

        cubesCanMove = new bool[cubesAABB.Length];

        spheresGameObjects = GameObject.FindGameObjectsWithTag("Sphere");
        trianglesGameObjects = GameObject.FindGameObjectsWithTag("Triangles");

        modelTypes = new ModelType[spheresGameObjects.Length + trianglesGameObjects.Length];

        sphereModels = new SphereModel[spheresGameObjects.Length];
        triangleModels = new TriangleModel[trianglesGameObjects.Length];
        triangleMeshes = new Mesh[trianglesGameObjects.Length];

        movementDirection = new Vector3[spheresGameObjects.Length + trianglesGameObjects.Length];

        for (var i = 0; i < spheresGameObjects.Length; i++)
        {
            modelTypes[i] = ModelType.Sphere;
            sphereModels[i] = new SphereModel
            {
                center = spheresGameObjects[i].transform.position,
                radius = spheresGameObjects[i].transform.lossyScale.x / 2.0f
            };
        }

        for (var i = 0; i < trianglesGameObjects.Length; i++)
        {
            modelTypes[i + sphereModels.Length] = ModelType.Triangle;
            triangleMeshes[i] = trianglesGameObjects[i].GetComponent<MeshFilter>().mesh;
            triangleModels[i] = new TriangleModel
            {
                verticesNum = triangleMeshes[i].vertexCount,
                indicesNum = triangleMeshes[i].triangles.Length,
                vertices = triangleMeshes[i].vertices.Select(v => trianglesGameObjects[i].transform.TransformPoint(v))
                    .ToArray(),
                indices = triangleMeshes[i].triangles
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
        for (var i = 0; i < spheresGameObjects.Length; i++)
        {
            sphereModels[i].center = spheresGameObjects[i].transform.position;
        }
    }

    private void UpdateTriangleModels()
    {
        for (var i = 0; i < trianglesGameObjects.Length; i++)
        {
            for (var j = 0; j < triangleModels[i].verticesNum; j++)
            {
                triangleModels[i].vertices[j] =
                    trianglesGameObjects[i].transform.TransformPoint(triangleMeshes[i].vertices[j]);
            }
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
        var box = new AABBdata {min = cubesAABB[id].min + displacement, max = cubesAABB[id].max + displacement};
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DataModels;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

public class CPUCollision : MonoBehaviour
{
    public UIHandler uiHandler;
    public bool canRun;
    public bool addGravity;
    public float gravityAcceleration;
    public Vector3 gravityDirection;
    
    [SerializeField] private ModelType[] modelTypes;

    [SerializeField] private SphereModel[] sphereModels;
    [SerializeField] private GameObject[] spheresGameObjects;

    [SerializeField] private TriangleModel[] triangleModels;
    [SerializeField] private GameObject[] trianglesGameObjects;
    [SerializeField] private Mesh[] triangleMeshes;

    [SerializeField] private ObjectData[] objectsInfo;
    [SerializeField] private Vector3[] movementForces;

    private Vector3 _gravity;

    private Stopwatch _stopwatch;

    private void Start()
    {
        _gravity = gravityAcceleration * gravityDirection;
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
        
        UpdateSphereModels();
        UpdateTriangleModels();
        // ResetMovement();

        var displacementByGravity = gravityDirection * (gravityAcceleration * Time.deltaTime);

        _stopwatch.Reset();
        _stopwatch.Start();

        for (var i = 0; i < modelTypes.Length; i++)
        {
            if (modelTypes[i] == ModelType.Sphere)
            {
                if (sphereModels[i].center.y - sphereModels[i].radius <= 0)
                {
                    objectsInfo[i].velocity = Vector3.zero;
                    continue;
                }
            }
            else if (modelTypes[i] == ModelType.Triangle)
            {
                if (trianglesGameObjects[tLocation(i)].transform.position.y - 0.5f <= 0)
                {
                    objectsInfo[i].velocity = Vector3.zero;
                    continue;
                }
            }
            
            var forces = Vector3.zero;
            var rotation = Vector3.zero;
            
            if ( addGravity )
            {
                forces += _gravity * objectsInfo[i].mass;
                if (i > sphereModels.Length)
                {
                    var x = Vector3.Cross(-trianglesGameObjects[tLocation(i)].transform.up, gravityDirection);
                    float theta = Mathf.Asin(x.magnitude);
                    rotation += theta * x ;
                }
            }

            var collCount = 0;

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
                        // movementDirection[i] +=
                        //     ModelsCollision.NormalCollisionSphereToSphere(sphereModels[i], sphereModels[j]);
                        // // Debug.Log(ModelsCollision.NormalCollisionSphereToSphere(sphereModels[i], sphereModels[j], gravityDirection));
                        // movementDirection[i].Normalize();

                        var collisions = ModelsCollision.GetSphereToSphereCollision(sphereModels[i], sphereModels[j]);
                        
                        for (var c = 0; c < collisions.normals.Length; c++)
                        {
                            // forces = (forces + forces.magnitude * collisions.depths[c] * collisions.normals[c]  );
                            forces += collisions.normals[c] * (collisions.depths[c] + forces.magnitude);
                            collCount += 1;
                        }
                        
                    }
                }
                else if (modelTypes[i] == ModelType.Triangle && modelTypes[j] == ModelType.Triangle)
                {
                    var collisions =
                        ModelsCollision.HasCollisionTriangleToTriangleOnTriangles(
                            triangleModels[tLocation(i)], triangleModels[tLocation(j)]);
                    for (var c = 0; c < collisions.normals.Length; c++)
                    {
                        // forces = (forces + forces.magnitude * collisions.depths[c] * collisions.normals[c]  );
                        var tmp = forces;
                        forces += collisions.normals[c] * (collisions.depths[c] + forces.magnitude);

                        var x = Vector3.Cross(collisions.normals[c], forces.normalized);
                        float theta = Mathf.Asin(x.magnitude);
                        rotation += theta * x ;
                        
                        collCount += 1;
                    }
                    // if (collision.no.Count > 0)
                    // {
                    //     // Debug.Log(string.Join(" - ", collision));
                    //
                    //     var vertices = triangleModels[tLocation(i)].vertices;
                    //     var indices = triangleModels[tLocation(i)].indices;
                    //     foreach (var triangle in collision)
                    //     {
                    //         var tri = new[]
                    //         {
                    //             vertices[indices[triangle]], vertices[indices[triangle + 1]],
                    //             vertices[indices[triangle + 2]]
                    //         };
                    //         var AB = tri[1] - tri[0];
                    //         var AC = tri[2] - tri[0];
                    //         var N = Vector3.Cross(AB, AC).normalized;
                    //         movementForces[i] += N;
                    //         movementForces[i].Normalize();
                    //     }
                    // }
                }
                else if (modelTypes[i] == ModelType.Triangle && modelTypes[j] == ModelType.Sphere)
                {
                    var collision =
                        ModelsCollision.HasCollisionTriangleToSphere(
                            triangleModels[tLocation(i)], sphereModels[j]);
                    if (collision.Count > 0)
                    {
                        // Debug.Log(string.Join(" |-| ", collision));

                        var vertices = triangleModels[tLocation(i)].vertices;
                        var indices = triangleModels[tLocation(i)].indices;
                        foreach (var triangle in collision)
                        {
                            var tri = new[]
                            {
                                vertices[indices[triangle]], vertices[indices[triangle + 1]],
                                vertices[indices[triangle + 2]]
                            };
                            var AB = tri[1] - tri[0];
                            var AC = tri[2] - tri[0];
                            var N = Vector3.Cross(AB, AC).normalized;
                            movementForces[i] += N;
                            movementForces[i].Normalize();
                        }
                    }
                } else if (modelTypes[i] == ModelType.Sphere && modelTypes[j] == ModelType.Triangle)
                {
                    var collision =
                        ModelsCollision.HasCollisionTriangleToSphere(
                            triangleModels[tLocation(j)], sphereModels[i]);
                    if (collision.Count > 0)
                    {
                        // Debug.Log(string.Join(" |-| ", collision));
                        // Debug.Log($"{i} with {j}");

                        var vertices = triangleModels[tLocation(j)].vertices;
                        var indices = triangleModels[tLocation(j)].indices;
                        foreach (var triangle in collision)
                        {
                            var tri = new[]
                            {
                                vertices[indices[triangle]], vertices[indices[triangle + 1]],
                                vertices[indices[triangle + 2]]
                            };
                            var AB = tri[1] - tri[0];
                            var AC = tri[2] - tri[0];
                            var N = Vector3.Cross(AB, AC).normalized;
                            movementForces[i] -= N;
                            movementForces[i].Normalize();
                        }
                    }
                }
            }

            objectsInfo[i].velocity *= 0.6f;
            objectsInfo[i].rotationVelocity *= 0.1f;
            
            objectsInfo[i].velocity += forces * Time.deltaTime;
            objectsInfo[i].rotationVelocity += rotation * Time.deltaTime;
            // if (collCount > 0)
            // {
            //     objectsInfo[i].velocity *= collCount;
            // }
            
            
        }

        _stopwatch.Stop();

        uiHandler.UpdateCPUTime(_stopwatch.Elapsed);
        
        for (var i = 0; i < spheresGameObjects.Length; i++)
        {
            spheresGameObjects[i].transform.Translate(objectsInfo[i].velocity);
        }

        for (var i = 0; i < trianglesGameObjects.Length; i++)
        {
            trianglesGameObjects[i].transform.Translate(objectsInfo[tLocation(i)].velocity);
            trianglesGameObjects[i].transform.Rotate(objectsInfo[tLocation(i)].rotationVelocity.normalized, Mathf.Rad2Deg * objectsInfo[tLocation(i)].rotationVelocity.magnitude );
        }
    }

    private int tLocation(int i)
    {
        return i - sphereModels.Length;
    }

    public void InitObjects()
    {
        spheresGameObjects = GameObject.FindGameObjectsWithTag("Sphere");
        trianglesGameObjects = GameObject.FindGameObjectsWithTag("Triangles");

        modelTypes = new ModelType[spheresGameObjects.Length + trianglesGameObjects.Length];

        sphereModels = new SphereModel[spheresGameObjects.Length];
        triangleModels = new TriangleModel[trianglesGameObjects.Length];
        triangleMeshes = new Mesh[trianglesGameObjects.Length];

        movementForces = new Vector3[spheresGameObjects.Length + trianglesGameObjects.Length];
        objectsInfo = new ObjectData[spheresGameObjects.Length + trianglesGameObjects.Length];
        
        for (var i = 0; i < objectsInfo.Length; i++)
        {
            objectsInfo[i].mass = 1.0f;
            objectsInfo[i].velocity = Vector3.zero;
        }

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

        ResetForces();
    }
    
    private void ResetForces()
    {
        for (var i = 0; i < movementForces.Length; i++)
        {
            movementForces[i] = Vector3.zero;
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
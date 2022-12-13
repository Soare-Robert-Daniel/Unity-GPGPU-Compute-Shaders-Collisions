using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DataModels;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

public class CPUCollision : MonoBehaviour
{
    public UIHandler uiHandler;
    public bool canRun;
    public bool addGravity;
    public float gravityAcceleration;
    public Vector3 gravityDirection;

    [SerializeField] private GameObject cage;
    private TriangleModel cageModel;
    private AABBModel cageAABB;
    
    [SerializeField] private ModelType[] modelTypes;

    [SerializeField] private SphereModel[] sphereModels;
    [SerializeField] private GameObject[] spheresGameObjects;

    [SerializeField] private TriangleModel[] triangleModels;
    [SerializeField] private GameObject[] trianglesGameObjects;
    [SerializeField] private Mesh[] triangleMeshes;

    [SerializeField] private ObjectData[] objectsInfo;
    [SerializeField] private Vector3[] movementForces;

    private Vector3 Gravity => gravityDirection * gravityAcceleration;

    private Stopwatch _stopwatch;

    private void Start()
    {
        // Gravity = gravityAcceleration * gravityDirection;
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
            var forces = Vector3.zero;
            var rotation = Vector3.zero;
            var collCount = 0;
            
            
            if ( addGravity )
            {
                forces += Gravity * objectsInfo[i].mass;
                if (i > sphereModels.Length)
                {
                    var x = Vector3.Cross(-trianglesGameObjects[tLocation(i)].transform.up, gravityDirection);
                    float theta = Mathf.Asin(x.magnitude);
                    rotation += theta * x ;
                }
            }

            

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
                }
                else if (modelTypes[i] == ModelType.Triangle && modelTypes[j] == ModelType.Sphere)
                {
                    var collisions =
                        ModelsCollision.HasCollisionTriangleToSphere(
                            triangleModels[tLocation(i)], sphereModels[j]);
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
                } else if (modelTypes[i] == ModelType.Sphere && modelTypes[j] == ModelType.Triangle)
                {
                    var collisions =
                        ModelsCollision.HasCollisionTriangleToSphere(
                            triangleModels[tLocation(j)], sphereModels[i]);

                    
                    for (var c = 0; c < collisions.normals.Length; c++)
                    {
                        // forces = (forces + forces.magnitude * collisions.depths[c] * collisions.normals[c]  );
                        var tmp = forces;
                        forces += collisions.normals[c] * (collisions.depths[c]);// + forces.magnitude);

                        var x = Vector3.Cross(collisions.normals[c], forces.normalized);
                        float theta = Mathf.Asin(x.magnitude);
                        rotation += theta * x ;
                        
                        collCount += 1;
                    }
                }
            }
            
            

            objectsInfo[i].velocity *= 0.6f;
            objectsInfo[i].rotationVelocity *= 0.8f;
            
            objectsInfo[i].velocity += forces * Time.deltaTime;
            objectsInfo[i].rotationVelocity += rotation * Time.deltaTime;
            
            if (modelTypes[i] == ModelType.Sphere)
            {
                var isOutside = new []
                {
                    sphereModels[i].center + Vector3.up * sphereModels[i].radius,
                    sphereModels[i].center - Vector3.up * sphereModels[i].radius,
                    sphereModels[i].center + Vector3.right * sphereModels[i].radius,
                    sphereModels[i].center - Vector3.right * sphereModels[i].radius,
                    sphereModels[i].center + Vector3.forward * sphereModels[i].radius,
                    sphereModels[i].center - Vector3.forward * sphereModels[i].radius,
                }
                    .Any(p => !ModelsCollision.IsPointInAABB(p, cageAABB));
                if (isOutside)
                {
                    objectsInfo[i].velocity *= -5f;
                    objectsInfo[i].rotationVelocity = Vector3.zero;
                }
            }
            else if (modelTypes[i] == ModelType.Triangle)
            {
                // if (trianglesGameObjects[tLocation(i)].transform.position.y - 0.5f <= 0)
                // {
                //     objectsInfo[i].velocity = Vector3.zero;
                //     objectsInfo[i].rotationVelocity = Vector3.zero;
                //     continue;
                // }

                var isOutside = triangleModels[tLocation(i)].vertices
                    .Any(p => !ModelsCollision.IsPointInAABB(p, cageAABB));
                if (isOutside)
                {
                    objectsInfo[i].velocity *= -5f;
                    objectsInfo[i].rotationVelocity = Vector3.zero;
                }
            }
            
            objectsInfo[i].velocity = Mathf.Clamp(objectsInfo[i].velocity.magnitude, 0f, 1.7f) * objectsInfo[i].velocity.normalized;
            objectsInfo[i].rotationVelocity = Mathf.Clamp(objectsInfo[i].rotationVelocity.magnitude, 0f, 5f) * objectsInfo[i].rotationVelocity.normalized;
            
            
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
            trianglesGameObjects[i].transform.Translate(objectsInfo[i + sphereModels.Length].velocity);
            trianglesGameObjects[i].transform.Rotate(objectsInfo[i + sphereModels.Length].rotationVelocity.normalized, Mathf.Rad2Deg * objectsInfo[i + sphereModels.Length].rotationVelocity.magnitude );
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

        {
            var cageMesh = cage.GetComponent<MeshFilter>().mesh;
            cageModel.vertices = cageMesh.vertices.Select(v => cage.transform.TransformPoint(v))
                .ToArray();
            cageModel.indices = cageMesh.triangles;
            cageModel.verticesNum = cageModel.vertices.Length;
            cageModel.indicesNum = cageMesh.triangles.Length;

            cageAABB.min = cage.transform.TransformPoint(cageMesh.bounds.min);
            cageAABB.max = cage.transform.TransformPoint(cageMesh.bounds.max);
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
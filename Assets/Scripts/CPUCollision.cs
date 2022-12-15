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
using Random = UnityEngine.Random;

public class CPUCollision : MonoBehaviour
{
    public UIHandler uiHandler;
    public bool canRun;
    public bool addGravity;
    public float gravityAcceleration;
    public Vector3 gravityDirection;
    public bool addRandomInitVelocity;

    [SerializeField] private GameObject cage;
    private TriangleModel cageModel;
    private AABBModel cageAABB;
    
    private ModelType[] modelTypes;

    private SphereModel[] sphereModels;
    private GameObject[] spheresGameObjects;

    private TriangleModel[] triangleModels;
    private GameObject[] trianglesGameObjects;
    private Mesh[] triangleMeshes;

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
            
            if ( addGravity )
            {
                forces += Gravity * objectsInfo[i].mass;
            }
            
            for (var j = 0; j < modelTypes.Length; j++)
            {
                if (i == j)
                {
                    continue;
                }
                
                #region Sphere - Sphere
                if (modelTypes[i] == ModelType.Sphere && modelTypes[j] == ModelType.Sphere)
                {
                    var collisions = ModelsCollision.GetSphereToSphereCollision(sphereModels[i], sphereModels[j]);
                    
                    for (var c = 0; c < collisions.normals.Length; c++)
                    {
                        forces += collisions.normals[c] * (collisions.depths[c] + objectsInfo[i].velocity.magnitude);
                    }
                }
                #endregion
                
                #region Triangle - Triangle
                else if (modelTypes[i] == ModelType.Triangle && modelTypes[j] == ModelType.Triangle)
                {
                    var collisions =
                        ModelsCollision.HasCollisionTriangleToTriangleOnTriangles(
                            triangleModels[TLocation(i)], triangleModels[TLocation(j)]);
                    for (var c = 0; c < collisions.normals.Length; c++)
                    {
                        // forces = (forces + forces.magnitude * collisions.depths[c] * collisions.normals[c]  );
                        var tmp = forces;
                        forces += collisions.normals[c] * (collisions.depths[c] + forces.magnitude);
                    }
                }
                #endregion
                
                #region Triangle - Sphere
                else if (modelTypes[i] == ModelType.Triangle && modelTypes[j] == ModelType.Sphere)
                {
                    var collisions =
                        ModelsCollision.HasCollisionTriangleToSphere(
                            triangleModels[TLocation(i)], sphereModels[j]);
                    for (var c = 0; c < collisions.normals.Length; c++)
                    {
                        // forces = (forces + forces.magnitude * collisions.depths[c] * collisions.normals[c]  );
                        var tmp = forces;
                        forces += collisions.normals[c] * (collisions.depths[c] + forces.magnitude);
                    }
                } 
                #endregion

                #region Sphere - Triangle
                else if (modelTypes[i] == ModelType.Sphere && modelTypes[j] == ModelType.Triangle)
                {
                    var collisions =
                        ModelsCollision.HasCollisionTriangleToSphere(
                            triangleModels[TLocation(j)], sphereModels[i]);
                    
                    for (var c = 0; c < collisions.normals.Length; c++)
                    {
                        // forces = (forces + forces.magnitude * collisions.depths[c] * collisions.normals[c]  );
                        var tmp = forces;
                        forces += collisions.normals[c] * (collisions.depths[c]);// + forces.magnitude);
                    }
                }
                #endregion
            }
            
            objectsInfo[i].velocity = objectsInfo[i].velocity * 0.99f +  forces * Time.deltaTime;

            var isOutside = false;

            if (modelTypes[i] == ModelType.Sphere)
            {
                isOutside = new []
                {
                    sphereModels[i].center + Vector3.up * sphereModels[i].radius,
                    sphereModels[i].center - Vector3.up * sphereModels[i].radius,
                    sphereModels[i].center + Vector3.right * sphereModels[i].radius,
                    sphereModels[i].center - Vector3.right * sphereModels[i].radius,
                    sphereModels[i].center + Vector3.forward * sphereModels[i].radius,
                    sphereModels[i].center - Vector3.forward * sphereModels[i].radius,
                }
                    .Any(p => !ModelsCollision.IsPointInAABB(p, cageAABB));
                
            }
            else if (modelTypes[i] == ModelType.Triangle)
            {
                isOutside = triangleModels[TLocation(i)].vertices
                    .Any(p => !ModelsCollision.IsPointInAABB(p, cageAABB));
            }
            
            if (isOutside)
            {
                objectsInfo[i].velocity *= -5f;
            }
            
            objectsInfo[i].velocity = Mathf.Clamp(objectsInfo[i].velocity.magnitude, 0f, 0.5f) * objectsInfo[i].velocity.normalized;
        }

        _stopwatch.Stop();

        uiHandler.UpdateCPUTime(_stopwatch.Elapsed);
        
        // UPDATE UNITY OBJECTS
        for (var i = 0; i < spheresGameObjects.Length; i++)
        {
            spheresGameObjects[i].transform.Translate(objectsInfo[i].velocity);
        }

        for (var i = 0; i < trianglesGameObjects.Length; i++)
        {
            trianglesGameObjects[i].transform.LookAt(trianglesGameObjects[i].transform.position + objectsInfo[i + sphereModels.Length].velocity);
            trianglesGameObjects[i].transform.position = (trianglesGameObjects[i].transform.position + objectsInfo[i + sphereModels.Length].velocity);
        }
    }

    private int TLocation(int i)
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

            if (addRandomInitVelocity)
            {
                objectsInfo[i].velocity = new Vector3(
                        Random.Range(0.5f, 2f),
                        Random.Range(0f, 2f),
                        Random.Range(1f, 2f)
                    );
            }
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
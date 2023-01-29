using System.Diagnostics;
using System.Linq;
using DataModels;
using UnityEngine;

public class CPUCollision : MonoBehaviour
{
    public UIHandler uiHandler;
    public bool canRun;
    public bool addGravity;
    public float gravityAcceleration;
    public Vector3 gravityDirection;
    public bool addRandomInitVelocity;

    [SerializeField] private GameObject cage;

    [SerializeField] TriangleModel[] triangleModels;

    [SerializeField] private ObjectData[] objectsInfo;
    [SerializeField] private Vector3[] movementForces;

    private Stopwatch _stopwatch;
    private AABBModel cageAABB;
    private TriangleModel cageModel;

    private ModelType[] modelTypes;

    private SphereModel[] sphereModels;
    private GameObject[] spheresGameObjects;
    private Mesh[] triangleMeshes;
    private GameObject[] trianglesGameObjects;

    private Vector3 Gravity => gravityDirection * gravityAcceleration;

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
        
        _stopwatch.Reset();
        _stopwatch.Start();

        #region Spatial Hashing

        var spatialHash = new SpatialHash(2);

        for (int i = 0; i < objectsInfo.Length; i++)
        {
            spatialHash.Insert(objectsInfo[i].position, i);
        }

        #endregion

        for (var i = 0; i < modelTypes.Length; i++)
        {
            var forces = Vector3.zero;
            
            if ( addGravity )
            {
                forces += Gravity * objectsInfo[i].mass;
            }

            var nearModels = spatialHash.Query(objectsInfo[i].position);
            
            foreach (int j in nearModels)
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
            var pos = spheresGameObjects[i].transform.position;
            sphereModels[i].center = pos;
            objectsInfo[i].position = pos;
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
            objectsInfo[i + sphereModels.Length].position = trianglesGameObjects[i].transform.position;
        }
    }
}
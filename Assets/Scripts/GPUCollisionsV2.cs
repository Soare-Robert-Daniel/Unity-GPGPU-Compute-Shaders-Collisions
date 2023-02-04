using System.Diagnostics;
using System.Linq;
using DataModels;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class GPUCollisionsV2 : MonoBehaviour
{
    public UIHandler uiHandler;
    public bool canRun;
    public bool addGravity;
    public float gravityAcceleration;
    public Vector3 gravityDirection;
    public bool addRandomInitVelocity;

    [SerializeField] private GameObject cage;
    [SerializeField] private Vector3[] triangleVertices;
    [SerializeField] private int[] triangleIndices;

    [SerializeField] private ObjectData[] objectsInfo;

    [SerializeField] private PhysicsData[] physicsForces;

    private Stopwatch _stopwatch;
    private AABBModel cageAABB;
    private TriangleModel cageModel;

    private ModelType[] modelTypes;
    private Vector3[] movementForces;

    private SphereModel[] sphereModels;
    private GameObject[] spheresGameObjects;
    private Mesh[] triangleMeshes;

    private TriangleModel[] triangleModels;
    private TriangleModelSimple[] triangleModelsSimple;

    private GameObject[] trianglesGameObjects;

    private Vector3 Gravity => addGravity ? gravityDirection * gravityAcceleration : Vector3.zero;

    private void Start()
    {
        // Gravity = gravityAcceleration * gravityDirection;
        canRun = false;
        _stopwatch = new Stopwatch();
        gravityDirection = gravityDirection.normalized;
        // InitObjects();
        // CreateBuffers();
    }

    private void Update()
    {
        if (!canRun)
        {
            return;
        }

        UpdateSphereModels();
        UpdateTriangleVerticesArray();
        // UpdateTriangleModels();
        // ResetMovement();

        _stopwatch.Reset();
        _stopwatch.Start();

        SetDataToBuffer();
        collisionShader.Dispatch(kernelId, dispatchSize, 1, 1);
    }

    private void LateUpdate()
    {
        if (!canRun)
        {
            return;
        }

        RetrieveDataFromBuffer();

        Debug.Log(string.Join("|", physicsForces.Select(x => $"{x.force}")));

        for (var i = 0; i < modelTypes.Length; i++)
        {
            physicsForces[i].force += Gravity;
            var forceDirection = physicsForces[i].force.normalized;
            var forceMagnitude = physicsForces[i].force.magnitude;
            var force = forceDirection * Mathf.Clamp(forceMagnitude, 0f, 1f);

            objectsInfo[i].velocity = objectsInfo[i].velocity * 0.99f + force * Time.deltaTime;

            var isOutside = false;

            if (modelTypes[i] == ModelType.Sphere)
            {
                isOutside = new[]
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

        uiHandler.UpdateGPUTime(_stopwatch.Elapsed);

        // UPDATE UNITY OBJECTS
        for (var i = 0; i < spheresGameObjects.Length; i++)
        {
            spheresGameObjects[i].transform.Translate(objectsInfo[i].velocity);
        }

        for (var i = 0; i < trianglesGameObjects.Length; i++)
        {
            trianglesGameObjects[i].transform
                .LookAt(trianglesGameObjects[i].transform.position + objectsInfo[i + sphereModels.Length].velocity);
            trianglesGameObjects[i].transform.position =
                (trianglesGameObjects[i].transform.position + objectsInfo[i + sphereModels.Length].velocity);
        }
    }

    private void OnDisable()
    {
        ReleaseBuffers();
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
        triangleModelsSimple = new TriangleModelSimple[trianglesGameObjects.Length];

        var totalVertices = 0;
        var totalIndices = 0;

        movementForces = new Vector3[spheresGameObjects.Length + trianglesGameObjects.Length];
        objectsInfo = new ObjectData[spheresGameObjects.Length + trianglesGameObjects.Length];
        physicsForces = new PhysicsData[spheresGameObjects.Length + trianglesGameObjects.Length];

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

            physicsForces[i].force = Vector3.zero;
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

            triangleModelsSimple[i] = new TriangleModelSimple
            {
                index = i,
                verticesNum = triangleMeshes[i].vertexCount,
                indicesNum = triangleMeshes[i].triangles.Length,
            };

            totalVertices += triangleModels[i].verticesNum;
            totalIndices += triangleModels[i].indicesNum;
        }

        triangleVertices = new Vector3[totalVertices];
        triangleIndices = new int[totalIndices];

        // Add triangle vertices in triangleVertices array and save the offset in triangleModelsSimple
        var verticesOffset = 0;
        var indicesOffset = 0;
        for (var i = 0; i < triangleModels.Length; i++)
        {
            triangleModels[i].vertices.CopyTo(triangleVertices, verticesOffset);
            triangleModels[i].indices.CopyTo(triangleIndices, indicesOffset);

            triangleModelsSimple[i].verticesOffset = verticesOffset;
            triangleModelsSimple[i].indicesOffset = indicesOffset;

            verticesOffset += triangleModels[i].verticesNum;
            indicesOffset += triangleModels[i].indicesNum;
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

        UpdateTriangleVerticesArray();
        ResetForces();
        CreateBuffers();
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

    private void UpdateTriangleVerticesArray()
    {
        var verticesOffset = 0;

        // Update triangle vertices location based on object transform
        for (var i = 0; i < triangleModelsSimple.Length; i++)
        {
            var triangleModel = triangleModelsSimple[i];
            var triangleGameObject = trianglesGameObjects[i];
            var triangleMesh = triangleMeshes[i];

            for (var j = 0; j < triangleModel.verticesNum; j++)
            {
                triangleVertices[verticesOffset + j] =
                    triangleGameObject.transform.TransformPoint(triangleMesh.vertices[j]);
            }

            verticesOffset += triangleModel.verticesNum;
        }

    }

    #region GPU

    [SerializeField] private ComputeShader collisionShader;
    private ComputeBuffer objectsBuffer;
    private ComputeBuffer trianglesBuffer;
    private ComputeBuffer spheresBuffer;
    private ComputeBuffer objectsTypeBuffer;

    private ComputeBuffer triangleVerticesBuffer;
    private ComputeBuffer triangleIndicesBuffer;

    private ComputeBuffer physicsForcesBuffer;

    private const int OBJECT_DATA_STRIDE = sizeof(float) * (3 + 3 + 1);
    private const int SPHERE_MODEL_STRIDE = sizeof(float) * (3 + 1);
    private const int OBJECT_TYPE_STRIDE = sizeof(int);

    private const int TRIANGLE_MODEL_SIMPLE_STRIDE = sizeof(float) * (3) + sizeof(int) * 5;
    private const int PHYSICS_FORCES_STRIDE = sizeof(float) * 3;

    private int kernelId;
    [SerializeField] private int dispatchSize;
    [SerializeField] private uint threadGroupSize;


    private void CreateBuffers()
    {
        objectsBuffer = new ComputeBuffer(objectsInfo.Length,
            OBJECT_DATA_STRIDE,
            ComputeBufferType.Structured,
            ComputeBufferMode.Immutable);
        spheresBuffer = new ComputeBuffer(sphereModels.Length,
            SPHERE_MODEL_STRIDE,
            ComputeBufferType.Structured,
            ComputeBufferMode.Immutable);
        trianglesBuffer = new ComputeBuffer(triangleModels.Length,
            TRIANGLE_MODEL_SIMPLE_STRIDE,
            ComputeBufferType.Structured,
            ComputeBufferMode.Immutable);

        objectsTypeBuffer = new ComputeBuffer(modelTypes.Length,
            OBJECT_TYPE_STRIDE,
            ComputeBufferType.Structured,
            ComputeBufferMode.Immutable);

        triangleVerticesBuffer = new ComputeBuffer(triangleVertices.Length,
            sizeof(float) * 3,
            ComputeBufferType.Structured,
            ComputeBufferMode.Immutable);

        triangleIndicesBuffer = new ComputeBuffer(triangleIndices.Length,
            sizeof(int),
            ComputeBufferType.Structured,
            ComputeBufferMode.Immutable);

        physicsForcesBuffer = new ComputeBuffer(objectsInfo.Length,
            PHYSICS_FORCES_STRIDE,
            ComputeBufferType.Structured,
            ComputeBufferMode.Immutable);

        kernelId = collisionShader.FindKernel("main");

        collisionShader.GetKernelThreadGroupSizes(kernelId, out threadGroupSize, out _, out _);
        dispatchSize = Mathf.CeilToInt((float)objectsInfo.Length / threadGroupSize);

        collisionShader.SetBuffer(kernelId, "objects", objectsBuffer);
        collisionShader.SetBuffer(kernelId, "objects_type", objectsTypeBuffer);
        collisionShader.SetBuffer(kernelId, "spheres", spheresBuffer);
        collisionShader.SetBuffer(kernelId, "triangles", trianglesBuffer);
        collisionShader.SetBuffer(kernelId, "triangle_vertices", triangleVerticesBuffer);
        collisionShader.SetBuffer(kernelId, "triangle_indices", triangleIndicesBuffer);
        collisionShader.SetBuffer(kernelId, "physics", physicsForcesBuffer);

        collisionShader.SetInt("num_objects", objectsInfo.Length);
        collisionShader.SetInt("spheres_num", sphereModels.Length);
        collisionShader.SetInt("triangles_num", triangleModels.Length);

    }

    private void SetDataToBuffer()
    {
        objectsBuffer.SetData(objectsInfo);
        spheresBuffer.SetData(sphereModels);
        trianglesBuffer.SetData(triangleModelsSimple);
        objectsTypeBuffer.SetData(modelTypes);
        triangleIndicesBuffer.SetData(triangleIndices);
        triangleVerticesBuffer.SetData(triangleVertices);
        physicsForcesBuffer.SetData(physicsForces);

        collisionShader.SetInt("num_objects", objectsInfo.Length);
        collisionShader.SetInt("spheres_num", sphereModels.Length);
        collisionShader.SetInt("triangles_num", triangleModels.Length);
    }

    private void RetrieveDataFromBuffer()
    {
        physicsForcesBuffer.GetData(physicsForces);
    }

    private void ReleaseBuffers()
    {
        objectsBuffer.Dispose();
        spheresBuffer.Dispose();
        trianglesBuffer.Dispose();
        objectsTypeBuffer.Dispose();
        triangleVerticesBuffer.Dispose();
        triangleIndicesBuffer.Dispose();
        physicsForcesBuffer.Dispose();
    }

    #endregion

}
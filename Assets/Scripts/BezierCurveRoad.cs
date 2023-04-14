using System;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using UniRx;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class BezierCurveRoad : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)]
    struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector4 tangent;
        public Vector2 texCoord0;
    }

    private const int MAX_SEGMENTS = 500;
    private const int uniformResolution = 100;

    [RangeReactiveProperty(0, 1)]
    public FloatReactiveProperty t = new(0);
    public BoolReactiveProperty Looping = new(false);
    [SerializeField, RangeReactiveProperty(10, MAX_SEGMENTS)]
    private IntReactiveProperty pointCount = new(50);
    [SerializeField] private BoolReactiveProperty useUniformSpacing = new(false);
    [SerializeField] private GameObject car;
    [SerializeField] private SO_RoadSliceData roadSlice;
    [SerializeField] private ComputeShader triangulatorShader;

    public CubicBezierSpline BezierSpline = new();

    public Color knotColor = Color.red;
    public Color controlColor = Color.blue;
    public Color controlLineColor = Color.green;
    public Color curveColor = Color.white;

    private ComputeBuffer roadBuffer;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer vertexBuffer;
    private ComputeBuffer triangleBuffer;

    private readonly float4x4[] locations = new float4x4[MAX_SEGMENTS + 1];
    private readonly Vertex[] vertexArray = new Vertex[MAX_SEGMENTS * 16 * 3];
    private readonly uint[] triangleArray = new uint[MAX_SEGMENTS * 16 * 3];

    private Mesh mesh;
    private MeshFilter meshFilter;

    private IDisposable transformSub;
    private CompositeDisposable objSub = new();

    private void OnDisable()
    {
        transformSub?.Dispose();
        objSub.Dispose();

        roadBuffer?.Release();
        positionBuffer?.Release();
        vertexBuffer?.Release();
        triangleBuffer?.Release();
    }

    private void OnDestroy()
    {
        meshFilter.mesh = null;
    }

    private void OnEnable()
    {
        objSub = new();

        roadBuffer = new(8, sizeof(float) * 3);
        positionBuffer = new(MAX_SEGMENTS + 1, sizeof(float) * 16);
        vertexBuffer = new(MAX_SEGMENTS * 16 * 3, sizeof(float) * 12);
        triangleBuffer = new(MAX_SEGMENTS * 16 * 3, sizeof(int));

        meshFilter = GetComponent<MeshFilter>();

        mesh = new Mesh {
            name = "Generated Road Mesh"
        };

        // OnValidate SUCKS >:(
        Looping.Subscribe(_ => OnValueChanged()).AddTo(objSub);
        useUniformSpacing.Subscribe(_ => OnValueChanged()).AddTo(objSub);
        pointCount.Subscribe(_ => OnValueChanged()).AddTo(objSub);
        t.Subscribe(_ => OnValueChanged()).AddTo(objSub);

        BezierSpline.uniformResolution = uniformResolution;
        OnValueChanged();
    }

    private void LateUpdate()
    {
        BezierSpline.Position = transform.position;
        UpdateCar();
    }

    private void OnValueChanged()
    {
        BezierSpline.IsClosed = Looping.Value;
        UpdateMesh();
    }

    public void AddPoint()
    {
        BezierSpline.AddNode();
        UpdateMesh();
    }

    public void RemovePoint()
    {
        BezierSpline.RemoveNode();
        UpdateMesh();
    }

    public void UpdateCar()
    {
        if (car == null) return;

        Vector3 currentPoint = BezierSpline.EvaluateUniform(t.Value);
        Vector3 currentForward = (BezierSpline.EvaluateUniform(t.Value < 1 ? t.Value + 0.0001f : 0.0001f) - currentPoint).normalized;
        Vector3 currentRight = Vector3.Cross(Vector3.up, currentForward).normalized;
        Vector3 currentUp = Vector3.Cross(currentForward, currentRight).normalized;

        car.transform.SetPositionAndRotation(currentPoint, Quaternion.LookRotation(currentForward, currentUp));
    }

    public void UpdateMesh()
    {
        if (!isActiveAndEnabled) return;

        if (roadSlice == null)
        {
            Debug.LogWarning("Assign a Road Slice!");
            return;
        }

        if (triangulatorShader == null)
        {
            Debug.LogWarning("Assign a Triangulator!");
            return;
        }

        BezierSpline.UpdateLUT();

        Vector3 prevFwd = Vector3.zero;

        for (int i = 0; i < pointCount.Value + (Looping.Value ? 0 : 1); i++)
        {
            float posSample = i / (float)pointCount.Value;
            float fwdSample = posSample < 1 ? posSample + 0.0001f : 0.0001f;

            Vector3 pos = useUniformSpacing.Value ? BezierSpline.EvaluateUniform(posSample) : BezierSpline.Evaluate(posSample);
            Vector3 fwd = i < pointCount.Value ? ((useUniformSpacing.Value ? BezierSpline.EvaluateUniform(fwdSample) : BezierSpline.Evaluate(fwdSample)) - pos).normalized : prevFwd;
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 up = Vector3.Cross(fwd, right).normalized;

            locations[i] = transform.worldToLocalMatrix * Matrix4x4.TRS(pos, Quaternion.LookRotation(fwd, up), Vector3.one);

            prevFwd = fwd;
        }

        if (Looping.Value)
            locations[pointCount.Value] = locations[0];

        roadBuffer.SetData(roadSlice.points);
        positionBuffer.SetData(locations);

        var kernel = triangulatorShader.FindKernel("CSMain");
        triangulatorShader.SetBuffer(kernel, "roadData", roadBuffer);
        triangulatorShader.SetBuffer(kernel, "locationMatrices", positionBuffer);
        triangulatorShader.SetBuffer(kernel, "vertices", vertexBuffer);
        triangulatorShader.SetBuffer(kernel, "triangles", triangleBuffer);
        triangulatorShader.SetInt("numSegments", pointCount.Value);

        triangulatorShader.Dispatch(kernel, Mathf.CeilToInt(pointCount.Value / 16f), 1, 1);

        vertexBuffer.GetData(vertexArray);
        triangleBuffer.GetData(triangleArray);

        // Causes road mesh to update late, optimal for performance though.
        /*
        AsyncGPUReadback.Request(vertexBuffer, request =>
        {
            if (!request.hasError)
                request.GetData<Vertex>().CopyTo(vertexArray);
        });

        AsyncGPUReadback.Request(triangleBuffer, request =>
        {
            if (!request.hasError)
                request.GetData<uint>().CopyTo(triangleArray);
        });*/

        GenerateMesh();
    }

    private void GenerateMesh()
    {
        int vertexAttributeCount = 4;
        int vertexCount = pointCount.Value * 16 * 3;
        int triangleIndexCount = pointCount.Value * 16 * 3;

        Vertex[] verts = new Vertex[pointCount.Value * 16 * 3];
        uint[] triangles = new uint[pointCount.Value * 16 * 3];

        Array.Copy(triangleArray, triangles, triangles.Length);
        Array.Copy(vertexArray, verts, verts.Length);

        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(
            vertexAttributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory
        );

        vertexAttributes[0] = new VertexAttributeDescriptor(dimension: 3);
        vertexAttributes[1] = new VertexAttributeDescriptor(
            VertexAttribute.Normal, dimension: 3
        );
        vertexAttributes[2] = new VertexAttributeDescriptor(
            VertexAttribute.Tangent, dimension: 4
        );
        vertexAttributes[3] = new VertexAttributeDescriptor(
            VertexAttribute.TexCoord0, dimension: 2
        );

        meshData.SetVertexBufferParams(vertexCount, vertexAttributes);
        vertexAttributes.Dispose();

        NativeArray<Vertex> vertices = meshData.GetVertexData<Vertex>();
        vertices.CopyFrom(verts);

        meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt32);

        NativeArray<uint> triangleIndices = meshData.GetIndexData<uint>();
        triangleIndices.CopyFrom(triangles);

        meshData.subMeshCount = 1;

        meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount) {
            vertexCount = vertexCount
        });

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        mesh.RecalculateBounds();
        meshFilter.mesh = mesh;
    }
}
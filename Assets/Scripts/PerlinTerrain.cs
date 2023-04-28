using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Jobs;

public class PerlinTerrain : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)]
    struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector4 tangent;
        public Vector2 texCoord0;
    }

    NativeArray<Vertex> VertexResult;

    struct ParallelVertexJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> ObjParams;

        public NativeArray<Vertex> result;

        public void Execute(int i)
        {
            int vertexStride = (int)(Mathf.Pow(2, ObjParams[0]) + 1);
            float y = i / vertexStride;
            float x = i % vertexStride;

            y = Remap(y, 0, vertexStride, 0, 1);
            x = Remap(x, 0, vertexStride, 0, 1);

            Vector3 pos = new Vector3(x - 0.5f, Mathf.PerlinNoise(x * 10, y * 10) * ObjParams[1], y - 0.5f);
            Vector3 normal = NormalFromHeight(new Vector2(x * 10, y * 10), ObjParams[1]);

            result[i] = new Vertex()
            {
                position = pos,
                normal = normal,
                tangent = new Vector4(1, 0, 0, 1),
                texCoord0 = new Vector2(x, y)
            };
        }
    }

    NativeArray<uint> IndexResult;

    private static Vector3 NormalFromHeight(Vector2 id, float amplitude)
    {
        float L = Mathf.PerlinNoise(id.x - 0.0001f, id.y) * amplitude;
        float R = Mathf.PerlinNoise(id.x + 0.0001f, id.y) * amplitude;
        float T = Mathf.PerlinNoise(id.x, id.y + 0.0001f) * amplitude;
        float B = Mathf.PerlinNoise(id.x, id.y - 0.0001f) * amplitude;

        return -new Vector3(2 * (R - L), -4, 2 * (B - T)).normalized;
    }

    struct ParallelIndexJob : IJobParallelForBatch
    {
        [ReadOnly]
        public NativeArray<float> ObjParams;

        public NativeArray<uint> result;

        public void Execute(int startIndex, int count)
        {
            // count is always 6 for processing quads
            int i = startIndex / count;

            int quadStride = (int)Mathf.Pow(2, ObjParams[0]);
            int y = i / quadStride;

            int vertTopLeft = i + y;
            int vertBottomLeft = i + quadStride + 1 + y;
            int vertTopRight = vertTopLeft + 1;
            int vertBottomRight = vertBottomLeft + 1;

            result[startIndex ] = (uint)vertTopLeft;
            result[startIndex + 1] = (uint)vertBottomLeft;
            result[startIndex + 2] = (uint)vertBottomRight;

            result[startIndex + 3] = (uint)vertTopLeft;
            result[startIndex + 4] = (uint)vertBottomRight;
            result[startIndex + 5] = (uint)vertTopRight;
        }
    }

    [SerializeField, Range(0, 10)]
    private int SubdivisionCount = 0;

    [SerializeField, Range(0.1f, 100f)]
    private float Amplitude = 1;

    [SerializeField]
    private MeshFilter meshFilter;


    private void OnValidate()
    {
        GenerateMesh();
    }

    private static float Remap(float value, float low1, float high1, float low2, float high2)
        => low2 + (value - low1) * (high2 - low2) / (high1 - low1);

    [ContextMenu("Generate Mesh")]
    private void GenerateMesh()
    {
        meshFilter.sharedMesh = new Mesh()
        {
            name = "Generated Terrain Mesh"
        };

        int vertexAttributeCount = 4;

        float vc = Mathf.Pow(2, SubdivisionCount) + 1f;
        int vertexCount = (int)(vc * vc);

        float tc = Mathf.Pow(4, SubdivisionCount);
        int triangleIndexCount = (int)(6 * tc);

        NativeArray<float> variables = new NativeArray<float>(2, Allocator.TempJob);
        variables[0] = SubdivisionCount;
        variables[1] = Amplitude;
        VertexResult = new NativeArray<Vertex>(vertexCount, Allocator.TempJob);
        IndexResult = new NativeArray<uint>(triangleIndexCount, Allocator.TempJob);

        ParallelVertexJob vertJobData = new ParallelVertexJob();
        vertJobData.ObjParams = variables;
        vertJobData.result = VertexResult;

        ParallelIndexJob indexJobData = new ParallelIndexJob();
        indexJobData.ObjParams = variables;
        indexJobData.result = IndexResult;

        JobHandle vertHandle = vertJobData.Schedule(vertexCount, 16);
        JobHandle indexHandle = indexJobData.ScheduleBatch(triangleIndexCount, 6);

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

        vertHandle.Complete();

        NativeArray<Vertex> vertices = meshData.GetVertexData<Vertex>();
        vertices.CopyFrom(VertexResult);

        VertexResult.Dispose();

        meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt32);

        indexHandle.Complete();

        NativeArray<uint> triangleIndices = meshData.GetIndexData<uint>();
        triangleIndices.CopyFrom(IndexResult);

        IndexResult.Dispose();

        meshData.subMeshCount = 1;

        meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount)
        {
            vertexCount = vertexCount
        });

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, meshFilter.sharedMesh);

        meshFilter.sharedMesh.RecalculateBounds();
        meshFilter.sharedMesh.RecalculateNormals();
        meshFilter.sharedMesh.RecalculateTangents();

        variables.Dispose();
    }
}

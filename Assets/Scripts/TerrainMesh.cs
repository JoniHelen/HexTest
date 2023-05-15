using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class TerrainMesh : MonoBehaviour
{
    [SerializeField] private Texture2D heightTexture;
    [SerializeField] private ComputeShader terrainTriangulator;
    [SerializeField] [Range(0, 100)] private float heightMultiplier;

    [StructLayout(LayoutKind.Sequential)]
    struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector4 tangent;
        public Vector2 texCoord0;
    }

    private ComputeBuffer vertexBuffer;
    private ComputeBuffer triangleBuffer;

    private Vertex[] vertexArray;
    private uint[] triangleArray;

    private Mesh mesh;
    private MeshFilter meshFilter;

    private void OnValidate()
    {
        vertexArray = new Vertex[heightTexture.width * heightTexture.height];
        triangleArray = new uint[(heightTexture.width - 1) * (heightTexture.height - 1) * 2 * 3];

        vertexBuffer?.Release();
        triangleBuffer?.Release();

        vertexBuffer = new(heightTexture.width * heightTexture.height, sizeof(float) * 12);
        triangleBuffer = new((heightTexture.width - 1) * (heightTexture.height - 1) * 2 * 3, sizeof(int));

        GenerateMesh();
    }

    private void OnEnable()
    {
        vertexBuffer?.Release();
        triangleBuffer?.Release();

        vertexArray = new Vertex[heightTexture.width * heightTexture.height];
        triangleArray = new uint[(heightTexture.width - 1) * (heightTexture.height - 1) * 2 * 3];

        vertexBuffer = new(heightTexture.width * heightTexture.height, sizeof(float) * 12);
        triangleBuffer = new((heightTexture.width - 1) * (heightTexture.height - 1) * 2 * 3, sizeof(int));

        meshFilter = GetComponent<MeshFilter>();

        mesh = new Mesh
        {
            name = "Generated Terrain Mesh"
        };

        meshFilter.mesh = mesh;

        GenerateMesh();
    }

    private void OnDestroy()
    {
        meshFilter.mesh = null;
        vertexBuffer?.Release();
        triangleBuffer?.Release();
    }

    private void OnDisable()
    {
        vertexBuffer?.Release();
        triangleBuffer?.Release();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void GenerateMesh()
    {
        if (!isActiveAndEnabled) return;

        if (terrainTriangulator == null)
        {
            Debug.LogWarning("Assign a Triangulator!");
            return;
        }

        var kernel = terrainTriangulator.FindKernel("CSMain");
        terrainTriangulator.SetBuffer(kernel, "vertices", vertexBuffer);
        terrainTriangulator.SetBuffer(kernel, "triangles", triangleBuffer);
        terrainTriangulator.SetTexture(kernel, "heightMap", heightTexture);

        terrainTriangulator.SetFloat("heightAmplitude", heightMultiplier);

        terrainTriangulator.SetInt("maxWidth", heightTexture.width - 1);
        terrainTriangulator.SetInt("maxHeight", heightTexture.height - 1);

        terrainTriangulator.Dispatch(kernel, Mathf.CeilToInt((heightTexture.width - 1) / 8f), Mathf.CeilToInt((heightTexture.height - 1) / 8f), 1);
        
        vertexBuffer.GetData(vertexArray);
        triangleBuffer.GetData(triangleArray);

        int vertexAttributeCount = 4;
        int vertexCount = vertexArray.Length;
        int triangleIndexCount = triangleArray.Length;

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
        vertices.CopyFrom(vertexArray);

        meshData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt32);

        NativeArray<uint> triangleIndices = meshData.GetIndexData<uint>();
        triangleIndices.CopyFrom(triangleArray);

        meshData.subMeshCount = 1;

        meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount)
        {
            vertexCount = vertexCount
        });

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
        //mesh.RecalculateNormals();
        //mesh.RecalculateTangents();
        mesh.RecalculateBounds();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LoadTree : MonoBehaviour
{

    public MeshFilter meshFilter;
    public ComputeShader tree;

    private Mesh mesh;
    private GraphicsBuffer vertexBuffer;
    private GraphicsBuffer indexBuffer;

    private ComputeBuffer metadataBuffer;
    private ComputeBuffer vertexCount;
    private ComputeBuffer triangleCount;

    void Awake()
    {
        AllocateMesh();
        CreateMesh();
    }

    void OnDestroy() 
    {
        ReleaseBuffers();
    }

    private void CreateMesh() 
    {
        tree.SetInt("TreePolygon", Constants.TREE_POLY);
        tree.SetInt("VertexCount", Constants.VERTEX_COUNT);
        tree.SetInt("MeshVertexCount", Constants.MESH_VERTEX_COUNT);
        tree.SetInt("MeshTriangleCount", Constants.MESH_TRIANGLE_COUNT);

        tree.SetFloat("BranchLength", Constants.BRANCH_LENGTH);
        tree.SetFloat("BranchRadius", Constants.BRANCH_RADIUS);

        tree.SetBuffer(0, "VertexBuffer", vertexBuffer);
        tree.SetBuffer(0, "IndexBuffer", indexBuffer);
        tree.SetBuffer(0, "VertexCounter", vertexCount);
        tree.SetBuffer(0, "TriangleCounter", triangleCount);
        tree.Dispatch(0, 1, 1, 1);

        Debug.Log("Created Tree.");

        tree.SetBuffer(1, "VertexBuffer", vertexBuffer);
        tree.SetBuffer(1, "IndexBuffer", indexBuffer);
        tree.SetBuffer(1, "VertexCounter", vertexCount);
        tree.SetBuffer(1, "TriangleCounter", triangleCount);
        tree.Dispatch(1, 1, 1, 1);

        Debug.Log("Completed Cleanup.");
    }

    private void AllocateMesh() 
    {
        metadataBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        vertexCount = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Counter);
        triangleCount = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Counter);

        mesh = new Mesh();
        // large bounding box [update dimensions]
        Vector3 dimensions = new Vector3(100.0f, 100.0f, 100.0f);
        mesh.bounds = new Bounds(dimensions / 2, dimensions);
        mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.SetVertexBufferParams(Constants.MESH_VERTEX_COUNT, new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),  new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3));
        mesh.SetIndexBufferParams(Constants.MESH_VERTEX_COUNT, IndexFormat.UInt32);
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, Constants.MESH_VERTEX_COUNT), MeshUpdateFlags.DontRecalculateBounds);
        
        vertexBuffer = mesh.GetVertexBuffer(0);
        indexBuffer = mesh.GetIndexBuffer();
        meshFilter.sharedMesh = mesh;
        // meshFilter.mesh.RecalculateNormals();
    }

    private int ReadVertexCount() {
        int[] count = { 0 };
        ComputeBuffer.CopyCount(vertexCount, metadataBuffer, 0);
        metadataBuffer.GetData(count);
        return count[0];
    }

    private int ReadTriangleCount() {
        int[] count = { 0 };
        ComputeBuffer.CopyCount(triangleCount, metadataBuffer, 0);
        metadataBuffer.GetData(count);
        return count[0];
    }

    private void ReleaseBuffers()
    {
        // release buffers
        vertexBuffer.Release();
        indexBuffer.Release();
        metadataBuffer.Release();
       
        // release counters
        vertexCount.Release();
        triangleCount.Release();
        
        // destroy mesh
        meshFilter.sharedMesh = null;
        Object.Destroy(mesh);
    }
}

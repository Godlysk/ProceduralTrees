using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LoadTree : MonoBehaviour
{

    public MeshFilter treeFilter;
    public MeshFilter leafFilter;
    public ComputeShader tree;

    private Mesh treeMesh;
    private Mesh leafMesh;
    private GraphicsBuffer vertexBuffer;
    private GraphicsBuffer indexBuffer;
    private GraphicsBuffer pointBuffer;
    private GraphicsBuffer ptIdxBuffer;

    private ComputeBuffer metadataBuffer;
    private ComputeBuffer vertexCount;
    private ComputeBuffer triangleCount;

    void Awake()
    {
        AllocateMesh();
        AllocatePoints();
        CreateMesh();
    }

    void OnDestroy() 
    {
        ReleaseBuffers();
    }

    private void CreateMesh() 
    {   
        vertexCount.SetCounterValue(0);
        triangleCount.SetCounterValue(0);

        tree.SetInt("TreePolygon", Constants.TREE_POLY);
        tree.SetInt("MeshVertexCount", Constants.MESH_VERTEX_COUNT);
        tree.SetInt("MeshTriangleCount", Constants.MESH_TRIANGLE_COUNT);
        tree.SetInt("LeafCount", Constants.ATTRACTOR_COUNT);

        tree.SetFloat("BranchLength", Constants.BRANCH_LENGTH);
        tree.SetFloat("BranchRadius", Constants.BRANCH_RADIUS);

        tree.SetFloat("TreeCenterX", Constants.TREE_CENTER[0]);
        tree.SetFloat("TreeCenterY", Constants.TREE_CENTER[1]);
        tree.SetFloat("TreeCenterZ", Constants.TREE_CENTER[2]);
        tree.SetFloat("TreeDimensionsX", Constants.TREE_DIMENSIONS[0]);
        tree.SetFloat("TreeDimensionsY", Constants.TREE_DIMENSIONS[1]);
        tree.SetFloat("TreeDimensionsZ", Constants.TREE_DIMENSIONS[2]);

        tree.SetFloat("AttractionRadius", Constants.ATTRACTION_RADIUS);
        tree.SetFloat("KillRadius", Constants.KILL_RADIUS);
        
        tree.SetBuffer(0, "PointBuffer", pointBuffer);
        tree.SetBuffer(0, "PointIndexBuffer", ptIdxBuffer);
        tree.Dispatch(0, 1, 1, 1);

        Debug.Log("Generated Leaves");

        tree.SetBuffer(1, "PointBuffer", pointBuffer);
        tree.SetBuffer(1, "VertexBuffer", vertexBuffer);
        tree.SetBuffer(1, "IndexBuffer", indexBuffer);
        tree.SetBuffer(1, "VertexCounter", vertexCount);
        tree.SetBuffer(1, "TriangleCounter", triangleCount);
        tree.Dispatch(1, 1, 1, 1);

        Debug.Log("Created Tree.");

        tree.SetBuffer(2, "VertexBuffer", vertexBuffer);
        tree.SetBuffer(2, "IndexBuffer", indexBuffer);
        tree.SetBuffer(2, "VertexCounter", vertexCount);
        tree.SetBuffer(2, "TriangleCounter", triangleCount);
        tree.Dispatch(2, 1, 1, 1);

        Debug.Log("Completed Cleanup.");
    }

    private void AllocatePoints() 
    {
        leafMesh = new Mesh();
        Vector3 dimensions = new Vector3(100.0f, 100.0f, 100.0f);
        leafMesh.bounds = new Bounds(dimensions / 2, dimensions);
        leafMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        leafMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        leafMesh.SetVertexBufferParams(Constants.ATTRACTOR_COUNT, new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3));
        leafMesh.SetIndexBufferParams(Constants.ATTRACTOR_COUNT, IndexFormat.UInt32);
        leafMesh.SetSubMesh(0, new SubMeshDescriptor(0, Constants.ATTRACTOR_COUNT, MeshTopology.Points), MeshUpdateFlags.DontRecalculateBounds);
        
        pointBuffer = leafMesh.GetVertexBuffer(0);
        ptIdxBuffer = leafMesh.GetIndexBuffer();
        leafFilter.sharedMesh = leafMesh;
    }

    private void AllocateMesh() 
    {
        metadataBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        vertexCount = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Counter);
        triangleCount = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Counter);

        treeMesh = new Mesh();
        // large bounding box [update dimensions]
        Vector3 dimensions = new Vector3(100.0f, 100.0f, 100.0f);
        treeMesh.bounds = new Bounds(dimensions / 2, dimensions);
        treeMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        treeMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        treeMesh.SetVertexBufferParams(Constants.MESH_VERTEX_COUNT, new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),  new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3));
        treeMesh.SetIndexBufferParams(Constants.MESH_TRIANGLE_COUNT, IndexFormat.UInt32);
        treeMesh.SetSubMesh(0, new SubMeshDescriptor(0, Constants.MESH_VERTEX_COUNT), MeshUpdateFlags.DontRecalculateBounds);
        
        vertexBuffer = treeMesh.GetVertexBuffer(0);
        indexBuffer = treeMesh.GetIndexBuffer();
        treeFilter.sharedMesh = treeMesh;
        // treeFilter.mesh.RecalculateNormals();
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
        pointBuffer.Release();
        ptIdxBuffer.Release();
       
        // release counters
        vertexCount.Release();
        triangleCount.Release();
        
        // destroy meshes
        treeFilter.sharedMesh = null;
        leafFilter.sharedMesh = null;
        Object.Destroy(treeMesh);
        Object.Destroy(leafMesh);
    }
}

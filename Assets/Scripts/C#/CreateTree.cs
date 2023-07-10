using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CreateTree : MonoBehaviour
{
    public ComputeShader algorithm;

    private Mesh treeMesh;
    private Mesh leafMesh;
    public MeshFilter treeFilter;
    public MeshFilter leafFilter;

    private GraphicsBuffer vertexBuffer;
    private GraphicsBuffer indexBuffer;
    private GraphicsBuffer attractorBuffer;
    private GraphicsBuffer attractorIndexBuffer;
    private ComputeBuffer growDirectionBuffer;
    private ComputeBuffer closestVertexBuffer;

    private ComputeBuffer metadataBuffer;
    private ComputeBuffer vertexCounter;

    void Awake()
    {
        AllocateMeshes();
        RunAlgorithm();
    }

    void RunAlgorithm() 
    {
        vertexCounter.SetCounterValue(2);

        algorithm.SetFloat("TreeCenterX", Constants.TREE_CENTER[0]);
        algorithm.SetFloat("TreeCenterY", Constants.TREE_CENTER[1]);
        algorithm.SetFloat("TreeCenterZ", Constants.TREE_CENTER[2]);
        algorithm.SetFloat("TreeDimensionsX", Constants.TREE_DIMENSIONS[0]);
        algorithm.SetFloat("TreeDimensionsY", Constants.TREE_DIMENSIONS[1]);
        algorithm.SetFloat("TreeDimensionsZ", Constants.TREE_DIMENSIONS[2]);

        algorithm.SetFloat("AttractionRadius", Constants.ATTRACTION_RADIUS);
        algorithm.SetFloat("KillRadius", Constants.KILL_RADIUS);
        algorithm.SetFloat("BranchLength", Constants.BRANCH_LENGTH);
        algorithm.SetInt("VertexCount", Constants.VERTEX_COUNT);

        algorithm.SetBuffer(0, "AttractorBuffer", attractorBuffer);
        algorithm.SetBuffer(0, "AttractorIndexBuffer", attractorIndexBuffer);
        algorithm.SetBuffer(0, "ClosestVertexBuffer", closestVertexBuffer);
        algorithm.SetBuffer(0, "VertexBuffer", vertexBuffer);
        algorithm.SetBuffer(0, "GrowDirectionBuffer", growDirectionBuffer);
        algorithm.SetBuffer(0, "IndexBuffer", indexBuffer);
        algorithm.Dispatch(0, Constants.VERTEX_COUNT / Constants.THREAD_COUNT, 1, 1);

        algorithm.SetBuffer(1, "AttractorBuffer", attractorBuffer);
        algorithm.SetBuffer(1, "AttractorIndexBuffer", attractorIndexBuffer);
        algorithm.SetBuffer(1, "ClosestVertexBuffer", closestVertexBuffer);
        algorithm.SetBuffer(1, "VertexBuffer", vertexBuffer);
        algorithm.SetBuffer(1, "GrowDirectionBuffer", growDirectionBuffer);
        algorithm.SetBuffer(1, "IndexBuffer", indexBuffer);
        algorithm.Dispatch(1, Constants.ATTRACTOR_COUNT / Constants.THREAD_COUNT, 1, 1);

        for (int i=0; i<Constants.ITERATIONS; i++) 
        {
            algorithm.SetBuffer(2, "AttractorBuffer", attractorBuffer);
            algorithm.SetBuffer(2, "AttractorIndexBuffer", attractorIndexBuffer);
            algorithm.SetBuffer(2, "ClosestVertexBuffer", closestVertexBuffer);
            algorithm.SetBuffer(2, "VertexBuffer", vertexBuffer);
            algorithm.SetBuffer(2, "GrowDirectionBuffer", growDirectionBuffer);
            algorithm.SetBuffer(2, "IndexBuffer", indexBuffer);
            algorithm.Dispatch(2, Constants.ATTRACTOR_COUNT / Constants.THREAD_COUNT, 1, 1);

            algorithm.SetBuffer(3, "AttractorBuffer", attractorBuffer);
            algorithm.SetBuffer(3, "AttractorIndexBuffer", attractorIndexBuffer);
            algorithm.SetBuffer(3, "ClosestVertexBuffer", closestVertexBuffer);
            algorithm.SetBuffer(3, "VertexBuffer", vertexBuffer);
            algorithm.SetBuffer(3, "GrowDirectionBuffer", growDirectionBuffer);
            algorithm.SetBuffer(3, "IndexBuffer", indexBuffer);
            algorithm.SetBuffer(3, "VertexCounter", vertexCounter);
            algorithm.Dispatch(3, Constants.ATTRACTOR_COUNT / Constants.THREAD_COUNT, 1, 1);
        }

        Debug.Log("Tree Generation Complete.");

    }

    void AllocateMeshes() 
    {
        growDirectionBuffer = new ComputeBuffer(Constants.VERTEX_COUNT, 4 * 3, ComputeBufferType.Raw);
        closestVertexBuffer = new ComputeBuffer(Constants.ATTRACTOR_COUNT, 4, ComputeBufferType.Raw);
        AllocateTree();
        AllocateLeaves();
    }

    void AllocateTree() 
    {
        metadataBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        vertexCounter = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Counter);

        treeMesh = new Mesh();
        Vector3 dimensions = new Vector3(Constants.TREE_DIMENSIONS[0], Constants.TREE_DIMENSIONS[1], Constants.TREE_DIMENSIONS[2]);
        Vector3 center = new Vector3(Constants.TREE_CENTER[0], Constants.TREE_CENTER[1], Constants.TREE_CENTER[2]);
        treeMesh.bounds = new Bounds(center, dimensions);
        treeMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        treeMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        treeMesh.SetVertexBufferParams(Constants.VERTEX_COUNT, new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3));
        treeMesh.SetIndexBufferParams(Constants.VERTEX_COUNT * 2, IndexFormat.UInt32);
        treeMesh.SetSubMesh(0, new SubMeshDescriptor(0, Constants.VERTEX_COUNT, MeshTopology.Lines), MeshUpdateFlags.DontRecalculateBounds);
        
        vertexBuffer = treeMesh.GetVertexBuffer(0);
        indexBuffer = treeMesh.GetIndexBuffer();
        treeFilter.sharedMesh = treeMesh;
    }

    void AllocateLeaves() 
    {
        leafMesh = new Mesh();
        Vector3 dimensions = new Vector3(Constants.TREE_DIMENSIONS[0], Constants.TREE_DIMENSIONS[1], Constants.TREE_DIMENSIONS[2]);
        Vector3 center = new Vector3(Constants.TREE_CENTER[0], Constants.TREE_CENTER[1], Constants.TREE_CENTER[2]);
        leafMesh.bounds = new Bounds(center, dimensions);
        leafMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        leafMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        leafMesh.SetVertexBufferParams(Constants.ATTRACTOR_COUNT, new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3));
        leafMesh.SetIndexBufferParams(Constants.ATTRACTOR_COUNT, IndexFormat.UInt16);
        leafMesh.SetSubMesh(0, new SubMeshDescriptor(0, Constants.ATTRACTOR_COUNT, MeshTopology.Points), MeshUpdateFlags.DontRecalculateBounds);
        
        attractorBuffer = leafMesh.GetVertexBuffer(0);
        attractorIndexBuffer = leafMesh.GetIndexBuffer();
        leafFilter.sharedMesh = leafMesh;
    }

    void OnDestroy() 
    {
        vertexBuffer.Release();
        indexBuffer.Release();
        attractorBuffer.Release();
        attractorIndexBuffer.Release();
        growDirectionBuffer.Release();
        closestVertexBuffer.Release();

        metadataBuffer.Release();
        vertexCounter.Release();

        treeFilter.sharedMesh = null;
        leafFilter.sharedMesh = null;
        Object.Destroy(treeMesh);
        Object.Destroy(leafMesh);
    }
}

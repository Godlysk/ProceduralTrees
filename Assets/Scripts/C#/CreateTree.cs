using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CreateTree : MonoBehaviour
{
    public ComputeShader algorithm;
    public ComputeShader mesher;

    private Mesh branchMesh;
    private Mesh skeletonMesh;
    private Mesh leafMesh;

    public MeshFilter branchFilter;
    public MeshFilter skeletonFilter;
    public MeshFilter leafFilter;

    private GraphicsBuffer vertexBuffer;
    private GraphicsBuffer indexBuffer;
    private GraphicsBuffer attractorBuffer;
    private GraphicsBuffer attractorIndexBuffer;
    private ComputeBuffer growDirectionBuffer;
    private ComputeBuffer stemDirectionBuffer;
    private ComputeBuffer closestVertexBuffer;
    private GraphicsBuffer meshVertexBuffer;
    private GraphicsBuffer meshIndexBuffer;

    private ComputeBuffer metadataBuffer;
    private ComputeBuffer vertexCounter;
    private ComputeBuffer meshVertexCounter;
    private ComputeBuffer meshTriangleCounter;

    void Awake()
    {
        AllocateMeshes();
        RunAlgorithm();
    }

    void RunAlgorithm() 
    {
        vertexCounter.SetCounterValue(1);

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
        algorithm.SetInt("AttractorCount", Constants.ATTRACTOR_COUNT);

        algorithm.SetBuffer(0, "AttractorBuffer", attractorBuffer);
        algorithm.SetBuffer(0, "AttractorIndexBuffer", attractorIndexBuffer);
        algorithm.SetBuffer(0, "ClosestVertexBuffer", closestVertexBuffer);
        algorithm.SetBuffer(0, "VertexBuffer", vertexBuffer);
        algorithm.SetBuffer(0, "GrowDirectionBuffer", growDirectionBuffer);
        algorithm.SetBuffer(0, "StemDirectionBuffer", stemDirectionBuffer);
        algorithm.SetBuffer(0, "IndexBuffer", indexBuffer);
        algorithm.Dispatch(0, Constants.VERTEX_COUNT / Constants.THREAD_COUNT, 1, 1);

        algorithm.SetBuffer(1, "AttractorBuffer", attractorBuffer);
        algorithm.SetBuffer(1, "AttractorIndexBuffer", attractorIndexBuffer);
        algorithm.SetBuffer(1, "ClosestVertexBuffer", closestVertexBuffer);
        algorithm.SetBuffer(1, "VertexBuffer", vertexBuffer);
        algorithm.SetBuffer(1, "GrowDirectionBuffer", growDirectionBuffer);
        algorithm.SetBuffer(1, "StemDirectionBuffer", stemDirectionBuffer);
        algorithm.SetBuffer(1, "IndexBuffer", indexBuffer);
        algorithm.Dispatch(1, Constants.ATTRACTOR_COUNT / Constants.THREAD_COUNT, 1, 1);

        for (int i=0; i<Constants.ITERATIONS; i++) 
        {
            algorithm.SetBuffer(2, "AttractorBuffer", attractorBuffer);
            algorithm.SetBuffer(2, "AttractorIndexBuffer", attractorIndexBuffer);
            algorithm.SetBuffer(2, "ClosestVertexBuffer", closestVertexBuffer);
            algorithm.SetBuffer(2, "VertexBuffer", vertexBuffer);
            algorithm.SetBuffer(2, "GrowDirectionBuffer", growDirectionBuffer);
            algorithm.SetBuffer(2, "StemDirectionBuffer", stemDirectionBuffer);
            algorithm.SetBuffer(2, "IndexBuffer", indexBuffer);
            algorithm.Dispatch(2, Constants.ATTRACTOR_COUNT / Constants.THREAD_COUNT, 1, 1);

            algorithm.SetBuffer(3, "AttractorBuffer", attractorBuffer);
            algorithm.SetBuffer(3, "AttractorIndexBuffer", attractorIndexBuffer);
            algorithm.SetBuffer(3, "ClosestVertexBuffer", closestVertexBuffer);
            algorithm.SetBuffer(3, "VertexBuffer", vertexBuffer);
            algorithm.SetBuffer(3, "GrowDirectionBuffer", growDirectionBuffer);
            algorithm.SetBuffer(3, "StemDirectionBuffer", stemDirectionBuffer);
            algorithm.SetBuffer(3, "IndexBuffer", indexBuffer);
            algorithm.SetBuffer(3, "VertexCounter", vertexCounter);
            // algorithm.Dispatch(3, Constants.ATTRACTOR_COUNT / Constants.THREAD_COUNT, 1, 1);
            algorithm.Dispatch(3, Constants.VERTEX_COUNT / Constants.THREAD_COUNT, 1, 1);

            // ReadPointCount();
        }

        Debug.Log("Tree Generation Complete.");

        // meshing step has not been parallelized

        meshVertexCounter.SetCounterValue(0);
        meshTriangleCounter.SetCounterValue(0);

        mesher.SetInt("TreePolygon", Constants.TREE_POLY);
        mesher.SetInt("MeshVertexCount", Constants.MESH_VERTEX_COUNT);
        mesher.SetInt("MeshTriangleCount", Constants.MESH_TRIANGLE_COUNT);
        mesher.SetInt("SegmentCount", Constants.VERTEX_COUNT);
        mesher.SetFloat("BranchRadius", Constants.BRANCH_RADIUS);
        mesher.SetFloat("BranchLength", Constants.BRANCH_LENGTH);

        mesher.SetBuffer(0, "PointBuffer", vertexBuffer);
        mesher.SetBuffer(0, "SegmentBuffer", indexBuffer);
        mesher.SetBuffer(0, "VertexBuffer", meshVertexBuffer);
        mesher.SetBuffer(0, "IndexBuffer", meshIndexBuffer);
        mesher.SetBuffer(0, "VertexCounter", meshVertexCounter);
        mesher.SetBuffer(0, "TriangleCounter", meshTriangleCounter);
        mesher.Dispatch(0, 1, 1, 1);

        mesher.SetBuffer(1, "PointBuffer", vertexBuffer);
        mesher.SetBuffer(1, "SegmentBuffer", indexBuffer);
        mesher.SetBuffer(1, "VertexBuffer", meshVertexBuffer);
        mesher.SetBuffer(1, "IndexBuffer", meshIndexBuffer);
        mesher.SetBuffer(1, "VertexCounter", meshVertexCounter);
        mesher.SetBuffer(1, "PointCounter", vertexCounter);
        mesher.SetBuffer(1, "TriangleCounter", meshTriangleCounter);
        mesher.Dispatch(1, 1, 1, 1);

        Debug.Log("Tree Meshing Complete.");

    }

    private void ReadPointCount() {
        int[] count = { 0 };
        ComputeBuffer.CopyCount(vertexCounter, metadataBuffer, 0);
        metadataBuffer.GetData(count);
        Debug.Log(count[0]);
    }

    void AllocateMeshes() 
    {
        growDirectionBuffer = new ComputeBuffer(Constants.VERTEX_COUNT, 4 * 3, ComputeBufferType.Raw);
        stemDirectionBuffer = new ComputeBuffer(Constants.VERTEX_COUNT, 4 * 3, ComputeBufferType.Raw);
        closestVertexBuffer = new ComputeBuffer(Constants.ATTRACTOR_COUNT, 4, ComputeBufferType.Raw);
        AllocateLeaves();
        AllocateSkeleton();
        AllocateBranches();
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

    void AllocateSkeleton() 
    {
        metadataBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        vertexCounter = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Counter);

        skeletonMesh = new Mesh();
        Vector3 dimensions = new Vector3(Constants.TREE_DIMENSIONS[0], Constants.TREE_DIMENSIONS[1], Constants.TREE_DIMENSIONS[2]);
        Vector3 center = new Vector3(Constants.TREE_CENTER[0], Constants.TREE_CENTER[1], Constants.TREE_CENTER[2]);
        skeletonMesh.bounds = new Bounds(center, dimensions);
        skeletonMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        skeletonMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        skeletonMesh.SetVertexBufferParams(Constants.VERTEX_COUNT, new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3));
        skeletonMesh.SetIndexBufferParams(Constants.VERTEX_COUNT * 2, IndexFormat.UInt32);
        skeletonMesh.SetSubMesh(0, new SubMeshDescriptor(0, Constants.VERTEX_COUNT, MeshTopology.Lines), MeshUpdateFlags.DontRecalculateBounds);
        
        vertexBuffer = skeletonMesh.GetVertexBuffer(0);
        indexBuffer = skeletonMesh.GetIndexBuffer();
        skeletonFilter.sharedMesh = skeletonMesh;
    }

    void AllocateBranches()
    {
        meshVertexCounter = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Counter);
        meshTriangleCounter = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Counter);

        branchMesh = new Mesh();
        Vector3 dimensions = new Vector3(Constants.TREE_DIMENSIONS[0], Constants.TREE_DIMENSIONS[1], Constants.TREE_DIMENSIONS[2]);
        Vector3 center = new Vector3(Constants.TREE_CENTER[0], Constants.TREE_CENTER[1], Constants.TREE_CENTER[2]);
        branchMesh.bounds = new Bounds(center, dimensions);
        branchMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        branchMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        branchMesh.SetVertexBufferParams(Constants.MESH_VERTEX_COUNT, new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),  new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3));
        branchMesh.SetIndexBufferParams(Constants.MESH_TRIANGLE_COUNT * 3, IndexFormat.UInt32);
        branchMesh.SetSubMesh(0, new SubMeshDescriptor(0, Constants.MESH_VERTEX_COUNT), MeshUpdateFlags.DontRecalculateBounds);
        
        meshVertexBuffer = branchMesh.GetVertexBuffer(0);
        meshIndexBuffer = branchMesh.GetIndexBuffer();
        branchFilter.sharedMesh = branchMesh;
    }

    void OnDestroy() 
    {
        vertexBuffer.Release();
        indexBuffer.Release();
        attractorBuffer.Release();
        attractorIndexBuffer.Release();
        growDirectionBuffer.Release();
        stemDirectionBuffer.Release();
        closestVertexBuffer.Release();
        meshVertexBuffer.Release();
        meshIndexBuffer.Release();

        metadataBuffer.Release();
        vertexCounter.Release();
        meshVertexCounter.Release();
        meshTriangleCounter.Release();

        branchFilter.sharedMesh = null;
        skeletonFilter.sharedMesh = null;
        leafFilter.sharedMesh = null;
        Object.Destroy(branchMesh);
        Object.Destroy(skeletonMesh);
        Object.Destroy(leafMesh);
    }
}

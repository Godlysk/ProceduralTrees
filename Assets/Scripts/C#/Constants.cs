using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{

    // Tree Generation

    public const int VERTEX_COUNT = 10;
    public const float BRANCH_LENGTH = 1.50f;
    public const float BRANCH_RADIUS = 0.25f;

    // Mesh

    public const int TREE_POLY = 32;
    public const int MESH_VERTEX_COUNT = 5000;                      // VERTEX_COUNT * TREE_POLY;
    public const int MESH_TRIANGLE_COUNT = 4 * MESH_VERTEX_COUNT;

    // GPU

    public const int THREAD_COUNT = 512;      // one thread per tree


}

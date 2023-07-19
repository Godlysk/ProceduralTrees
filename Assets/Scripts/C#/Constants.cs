using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{ 

    public const float BRANCH_LENGTH = 0.20f;
    public const float BRANCH_RADIUS = 0.10f;
    public static float[] TREE_CENTER = new float[] { 0.0f, 0.0f, 5.0f }; // new float[] { 0.0f, 0.0f, 10.0f };
    public static float[] TREE_DIMENSIONS = new float[] { 5.0f, 5.0f, 5.0f }; // new float[] { 7.5f, 7.5f, 5.0f };
    public static int SEED_COUNT = 1;

    public const int TREE_POLY = 8;
    public const int MESH_VERTEX_COUNT = 50 * (TREE_POLY + 1) * VERTEX_COUNT;                      // VERTEX_COUNT * TREE_POLY;
    public const int MESH_TRIANGLE_COUNT = 5 * MESH_VERTEX_COUNT;
    
    public const int ATTRACTOR_COUNT = 4 * THREAD_COUNT;
    public const int VERTEX_COUNT = 10 * ATTRACTOR_COUNT;
    public const int THREAD_COUNT = 512;

    public const float ATTRACTION_RADIUS = 20.0f;
    public const float KILL_RADIUS = 0.50f;
    public const int ITERATIONS = 200;

}

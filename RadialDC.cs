using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class RadialDC_MT : MonoBehaviour
{
    //Voxel Generation Variables
    [System.NonSerialized] public static int worldDepthLimit;
    [System.NonSerialized] public static Vector3Int recipeBlockNum;
    [System.NonSerialized] public static Vector3Int recipeBlockVoxSize;
    [System.NonSerialized] public static Vector3 recipeBlockDecimalSize;
    [System.NonSerialized] public static Vector3Int recipeTotalVoxSize;
    [System.NonSerialized] public static Vector3 recipeTotalDecimalSize;
    [System.NonSerialized] public static RecipeDataLoaded recipeData;

    [System.NonSerialized] public static DCVoxel currentCenterVoxMT;
    [System.NonSerialized] public static Vector3 meshStart;
    [System.NonSerialized] public static Vector3 meshCenter;
    [System.NonSerialized] public static Vector3Int worldCenter;
    [System.NonSerialized] public static Vector3[,,] recenterPoints;
    [System.NonSerialized] public static int midRecenterPoint;

    [System.NonSerialized] public static CoreVoxCountData coreVoxelCountData;
    [System.NonSerialized] public static int coreVoxelCounter;
    [System.NonSerialized] public static Vector3Int coreVoxSize;
    [System.NonSerialized] public static CoreVoxelDataMT coreVoxelDataMT;

    [System.NonSerialized] public static Vector3 smallestVoxSize;
    [System.NonSerialized] public static Vector3[] voxSize;
    [System.NonSerialized] public static float3[] voxSizeF3;
    [System.NonSerialized] public static Vector3[] voxHalfSize;
    [System.NonSerialized] public static float3[] voxHalfSizeF3;

    [System.NonSerialized] public static int[] voxNum;
    [System.NonSerialized] public static int[] voxDepthPosMod;
    [System.NonSerialized] public static Vector3Int[] depthLimitsP;
    [System.NonSerialized] public static Vector3Int[] depthLimitsN;
    [System.NonSerialized] public static Vector3Int hDTotalVoxSize;
    [System.NonSerialized] public static Vector3 hDTotalDecimalSize;
    [System.NonSerialized] public static int[] depthDetail;
    [System.NonSerialized] public static float3 nullVector;

    //Voxel Density Hashing Varibales
    [System.NonSerialized] public static NativeHashMap<int, float> vertDensities;
    [System.NonSerialized] public static int cachCounterFound;
    [System.NonSerialized] public static int cachCounterNotFound;
    [System.NonSerialized] public static NativeArray<int> totalCross;

    //Outer Voxel Border Creation Variables
    [System.NonSerialized] public static NativeArray<DCSVoxel> outerVoxesI;
    [System.NonSerialized] public static NativeArray<DCSVoxel> outerVoxesO;
    [System.NonSerialized] public static int outerVoxOffsetI;
    [System.NonSerialized] public static int outerVoxOffsetO;
    [System.NonSerialized] public static int xBorderN;
    [System.NonSerialized] public static int xBorderP;
    [System.NonSerialized] public static int yBorderN;
    [System.NonSerialized] public static int yBorderP;
    [System.NonSerialized] public static int zBorderN;
    [System.NonSerialized] public static int zBorderP;
    [System.NonSerialized] public static int totalBorderSize;
    [System.NonSerialized] public static bool switchToDoubling;
    [System.NonSerialized] public static int outerVoxEscapeCount;

    //Multithread Jobs and Job Handles
    [System.NonSerialized] public static JobHandle createCoreVoxelJobHandle;
    [System.NonSerialized] public static CreateCoreVoxelsJob createCoreVoxelJob;
    [System.NonSerialized] public static JobHandle setVoxPositionCoreVoxelJobHandle;
    [System.NonSerialized] public static SetMeshVertexPositionJob meshPositionJob;
    [System.NonSerialized] public static JobHandle coreVoxMeshDataCreationJobHandle;
    [System.NonSerialized] public static CoreVoxMeshDataCreationJob coreVoxMeshDataCreationJob;

    [System.NonSerialized] public static JobHandle intitalCreateOuterVoxelJobHandle;
    [System.NonSerialized] public static JobHandle createOuterVoxelJobHandle;
    [System.NonSerialized] public static CreateOuterVoxelsJob createOuterVoxelJob;
    [System.NonSerialized] public static JobHandle intitalSetOuterVoxelMeshPositionJobHandle;
    [System.NonSerialized] public static JobHandle setOuterVoxelMeshPositionJobHandle;
    [System.NonSerialized] public static SetOuterVoxelMeshPositionJob setOuterVoxelMeshPositionJob;
    [System.NonSerialized] public static JobHandle outerVoxMeshDataCreationJobHandle;
    [System.NonSerialized] public static OuterVoxMeshDataCreationJob outerVoxMeshDataCreationJob;

    [System.NonSerialized] public static JobHandle createMeshJobHandle;
    [System.NonSerialized] public static CreateMeshJob createMeshJob;

    //Mulithread Flow Flags
    [System.NonSerialized] public static bool coreVoxCreationStarted;
    [System.NonSerialized] public static bool coreVoxCreationFinished;
    [System.NonSerialized] public static bool coreVoxPositionsStarted;
    [System.NonSerialized] public static bool coreVoxPositionsFinished;
    [System.NonSerialized] public static bool coreVoxMeshDataCreationStarted;
    [System.NonSerialized] public static bool coreVoxMeshDataCreationFinished;

    [System.NonSerialized] public static bool outerVoxSetupStarted;
    [System.NonSerialized] public static bool outerVoxSetupFinished;
    [System.NonSerialized] public static bool outerVoxInitMeshPosStarted;
    [System.NonSerialized] public static bool outerVoxInitMeshPosFinished;
    [System.NonSerialized] public static bool outerVoxCreationStartedInitial;
    [System.NonSerialized] public static bool outerVoxCreationStarted;
    [System.NonSerialized] public static bool outerVoxCreationFinished;
    [System.NonSerialized] public static bool outerVoxPositionsStarted;
    [System.NonSerialized] public static bool outerVoxPositionsFinished;
    [System.NonSerialized] public static bool outerVoxMeshDataCreationStarted;
    [System.NonSerialized] public static bool outerVoxMeshDataCreationFinished;
    [System.NonSerialized] public static bool outerVoxJobsStartedAll;
    [System.NonSerialized] public static bool outerVoxJobsFinishedAll;

    //Mesh Data Variables
    [System.NonSerialized] public static NativeList<float3> verticies;
    [System.NonSerialized] public static NativeList<int> triangles;
    [System.NonSerialized] public static NativeList<float2> uvs;
    [System.NonSerialized] public static NativeList<float3> normals;
    
    [System.NonSerialized] public static bool meshCreationStarted;
    [System.NonSerialized] public static bool meshCreationFinished;

    [System.NonSerialized] public static Mesh.MeshDataArray meshDataArray;
    [System.NonSerialized] public static Mesh.MeshData meshData;
    [NativeDisableContainerSafetyRestriction] static NativeArray<Stream0> stream0;
    [NativeDisableContainerSafetyRestriction] static NativeArray<ushort> streamTriangles;

    //Debug Variables in Outputs
    [System.NonSerialized] public static int debugCounter;
    [System.NonSerialized] public static GameObject innerVoxesGO;
    [System.NonSerialized] public static GameObject outerVoxesGO;
    [System.NonSerialized] public static GameObject innerVoxGO;
    [System.NonSerialized] public static GameObject outerVoxGO;
    [System.NonSerialized] public static GameObject creationGO;
    [System.NonSerialized] public static GameObject creationGO_D0;
    [System.NonSerialized] public static GameObject creationGO_D1;
    [System.NonSerialized] public static GameObject creationGO_D2;
    [System.NonSerialized] public static GameObject creationGO_D3;
    [System.NonSerialized] public static MeshGeneratorDebugGOs useDebugGOs;

    public enum Dir
    {
        x, y, z
    }

    //CoreVoxCountData is a container to store numbers used to create the correct number of voxel array positions
    public struct CoreVoxCountData
    {
        public int coreVoxTotal;
        public int outerVoxelCount;
        public int useCoreVoxTotal;
    }
    
    //DCVoxel is the primary data construct of voxel.
    //It including data for evaluating mesh vertex points and forming triangles from those points.
    public struct DCVoxel
    {
        public int index;

        public int depth;
        public int innerDepth;
        public bool depthEnd;

        public int3 pos;
        public int3 fullPos;
        public int3 baseNum;

        public float3 startPoint;
        public float3 centerPoint;
        public float3 endPoint;

        public bool anyCross;
        public float3 meshVertex;
        public float3 meshNormal;

        //These variables are used to point toward smaller voxels constructed from larger voxels.
        //This pointer in the form of an array index so it functions through unity's multicore job system.
        public int vNode000;
        public int vNode001;
        public int vNode010;
        public int vNode011;
        public int vNode100;
        public int vNode101;
        public int vNode110;
        public int vNode111;

        public int parentVox;

        public int detail;
        public DCSVoxel sVox;

        public bool voxCreated;
        public bool meshCreated;

        //These variables are used as part of 21 logical rules which allow the dual contouring system to proceed in radial manor.
        //They state which voxels are 0 or 1 in the radial formation (meaning which are moving outward from the center) and which are C labled for center bridges nessesary for the radial pattern.
        public bool x01;
        public bool y01;
        public bool z01;
        public bool xC;
        public bool yC;
        public bool zC;
    }

    //DCSVoxel is used for the outer voxels. These are paired down versions of the DCVoxel and contain many of the same logical components.
    public struct DCSVoxel
    {
        public int3 vert;
        public int baseVox;
        public int offset;

        public bool x01;
        public bool y01;
        public bool z01;
        public bool xC;
        public bool yC;
        public bool zC;

        public int xOffset;
        public int yOffset;
        public int zOffset;

        public float3 startPoint;
        public float3 centerPoint;
        public float3 endPoint;

        public bool anyCross;
        public float3 meshVertex;
        public float3 meshNormal;
    }

    //CoreVoxelDataMT contains the core voxel data as the current start/stop positions
    public struct CoreVoxelDataMT
    {
        public float3 coreVoxStartPos;
        public float3 coreVoxEndPos;
        public NativeArray<DCVoxel> coreVoxels;
    }

    //HermiteData is used to store intersections and gradients used to evaulate voxel vertex positions.
    public struct HermiteData
    {
        public List<float3> intersections;
        public List<float3> gradients;
    };

    //RecipeDataBlock is asingle block of recipe data containing density fuction recipes for evaluation
    public struct RecipeDataBlock
    {
        public Vector3 posOffset;
        public Vector3Int voxOffset;
        public DensityFunction.DFType DefaultDF;
    }

    //RecipeDataLoaded contains all currently loaded RecipeDataBlocks
    public struct RecipeDataLoaded
    {
        public RecipeDataBlock[,,] recipeDataBlocks;
        public Vector3 recipeLoadedStartPos;
        public Vector3 recipeLoadedEndPos;
    }


    //Main Functions
    //RecenterWorldMT is the primary functions used to create the world mesh.
    //In order to make use of unity's job parallelism it is written in 'flow gate' style.
    //This means it is meant to check on and, if ready, move the function to the next stage. This is done through a series of flags which get set as jobs are completed.
    //The function evaluates the Core Voxles (smaller voxel size, higher detail), and then the Outer Voxels (larger size, less detail)
    //For both Core and Outer Voxels, the process is split into 3 sections: 
    //(1) the creation of the voxels and there positions, (2) evaluation of voxel vertecies, (3) using voxel verticies to generate mesh data (including triangle arrays).
    public static bool RecenterWorldMT(ref Mesh worldMesh)
    {
        bool working = true;
        bool progressMessages = false;

        //Create Core Voxels
        if (!coreVoxCreationStarted)
        {
            meshStart = new Vector3(
                meshCenter.x - (((coreVoxSize.x) * voxSize[0].x) / 2) + voxSize[0].x,
                meshCenter.y - (((coreVoxSize.y) * voxSize[0].y) / 2) + voxSize[0].y,
                meshCenter.z - (((coreVoxSize.z) * voxSize[0].z) / 2) + voxSize[0].z);
            coreVoxelDataMT = new CoreVoxelDataMT();
            coreVoxelDataMT.coreVoxStartPos = meshStart;
            coreVoxelDataMT.coreVoxEndPos = new Vector3(meshStart.x + coreVoxSize.x * voxSize[0].x, meshStart.y + coreVoxSize.y * voxSize[0].y, meshStart.z + coreVoxSize.z * voxSize[0].z);

            coreVoxelCounter = 0;
            coreVoxelDataMT.coreVoxels = new NativeArray<DCVoxel>(coreVoxelCountData.coreVoxTotal, Allocator.Persistent);

            createCoreVoxelJob = new CreateCoreVoxelsJob
            {
                coreVoxelData = coreVoxelDataMT,
                debug = false
            };
            createCoreVoxelJobHandle = createCoreVoxelJob.Schedule();
            if (progressMessages) Debug.Log("MT Started Core Vox Creation Job");
            coreVoxCreationStarted = true;
        }
        if (coreVoxCreationStarted && !coreVoxCreationFinished)
        {
            if (createCoreVoxelJobHandle.IsCompleted)
            {
                createCoreVoxelJobHandle.Complete();

                coreVoxCreationFinished = true;
                if (progressMessages) Debug.Log("MT Completed Core Vox Creation Job");
            }
        }


        //Get Core Mesh Positions
        if (coreVoxCreationFinished && !coreVoxPositionsStarted)
        {
            totalCross = new NativeArray<int>(1, Allocator.Persistent);
            vertDensities = new NativeHashMap<int, float>(1000000, Allocator.Persistent);
            meshPositionJob = new SetMeshVertexPositionJob
            {
                voxDataMT = coreVoxelDataMT.coreVoxels,
                blockCenterPosMT = coreVoxelDataMT.coreVoxStartPos,
                vertDensities = vertDensities,
                totalCross = totalCross,
                debug = false
            };
            setVoxPositionCoreVoxelJobHandle = meshPositionJob.Schedule(coreVoxelDataMT.coreVoxels.Length, 16);

            coreVoxPositionsStarted = true;
            if (progressMessages) Debug.Log("MT Started Core Mesh Position Job");
        }
        if (coreVoxPositionsStarted && !coreVoxPositionsFinished)
        {
            if (setVoxPositionCoreVoxelJobHandle.IsCompleted)
            {
                setVoxPositionCoreVoxelJobHandle.Complete();
                coreVoxelDataMT.coreVoxels = meshPositionJob.voxDataMT;

                coreVoxPositionsFinished = true;
                if (progressMessages) Debug.Log("MT Completed Core Mesh Position Job");
            }
        }


        //Create Core Mesh Data
        if (coreVoxPositionsFinished && !coreVoxMeshDataCreationStarted)
        {
            verticies = new NativeList<float3>(totalCross[0] * 3, Allocator.Persistent);
            normals = new NativeList<float3>(totalCross[0] * 3, Allocator.Persistent);
            triangles = new NativeList<int>(totalCross[0], Allocator.Persistent);
            uvs = new NativeList<float2>(totalCross[0] * 3, Allocator.Persistent);

            coreVoxMeshDataCreationJob = new CoreVoxMeshDataCreationJob
            {
                voxData = coreVoxelDataMT,
                vertDensities = vertDensities,
                verticies = verticies,
                triangles = triangles,
                uvs = uvs,
                normals = normals,
                debug = false,
            };
            coreVoxMeshDataCreationJobHandle = coreVoxMeshDataCreationJob.Schedule();

            totalCross.Dispose();
            coreVoxMeshDataCreationStarted = true;
            if (progressMessages) Debug.Log("MT Started Create Core Mesh Data Job");
        }
        if (coreVoxMeshDataCreationStarted && !coreVoxMeshDataCreationFinished)
        {
            if (coreVoxMeshDataCreationJobHandle.IsCompleted)
            {
                coreVoxMeshDataCreationJobHandle.Complete();

                coreVoxMeshDataCreationFinished = true;
                if (progressMessages) Debug.Log("MT Completed Create Core Mesh Data Job");
            }
        }


        //Outer Voxel Jobs
        if (coreVoxMeshDataCreationFinished && !outerVoxJobsStartedAll && !outerVoxJobsFinishedAll)
        {
            //Setup + Create Initial Outer Voxels
            if (!outerVoxSetupStarted)
            {
                outerVoxEscapeCount = 0;
                outerVoxOffsetI = 1;
                outerVoxOffsetO = 2;
                xBorderN = 0;
                xBorderP = coreVoxSize.x;
                yBorderN = 0;
                yBorderP = coreVoxSize.y;
                zBorderN = 0;
                zBorderP = coreVoxSize.z;
                totalBorderSize = xBorderP - xBorderN;
                switchToDoubling = false;
                int useSize = totalBorderSize / outerVoxOffsetI - 1;
                int outerVoxSize =
                    (useSize * useSize * 6)//Faces
                    + (useSize * 12)//Edges
                    + 8;//Corners
                outerVoxesI = new NativeArray<DCSVoxel>(outerVoxSize, Allocator.Persistent);

                createOuterVoxelJob = new CreateOuterVoxelsJob
                {
                    coreVoxelData = coreVoxelDataMT,
                    outerVoxes = outerVoxesI,
                    xBorderN = xBorderN,
                    xBorderP = xBorderP,
                    yBorderN = yBorderN,
                    yBorderP = yBorderP,
                    zBorderN = zBorderN,
                    zBorderP = zBorderP,
                    offset = outerVoxOffsetI,
                    debug = false,
                };
                intitalCreateOuterVoxelJobHandle = createOuterVoxelJob.Schedule();

                outerVoxSetupStarted = true;
                if (progressMessages) Debug.Log("MT Started Setup Outer Voxel Job");
            }
            if (outerVoxSetupStarted && !outerVoxSetupFinished)
            {
                if (intitalCreateOuterVoxelJobHandle.IsCompleted)
                {
                    intitalCreateOuterVoxelJobHandle.Complete();

                    outerVoxSetupFinished = true;
                    if (progressMessages) Debug.Log("MT Completed Setup Outer Voxel Job");
                }
            }

            //Set Init Outer Voxel Mesh Posiions
            if (outerVoxSetupFinished && !outerVoxInitMeshPosStarted)
            {
                setOuterVoxelMeshPositionJob = new SetOuterVoxelMeshPositionJob
                {
                    outerVox = outerVoxesI,
                    blockCenterPos = coreVoxelDataMT.coreVoxStartPos,
                    vertDensities = vertDensities
                };
                intitalSetOuterVoxelMeshPositionJobHandle = setOuterVoxelMeshPositionJob.Schedule(outerVoxesI.Length, 16);

                outerVoxInitMeshPosStarted = true;
                if (progressMessages) Debug.Log("MT Started Init Set Core Mesh Position Job");
            }
            if (outerVoxInitMeshPosStarted && !outerVoxInitMeshPosFinished)
            {
                if (intitalSetOuterVoxelMeshPositionJobHandle.IsCompleted)
                {
                    intitalSetOuterVoxelMeshPositionJobHandle.Complete();

                    outerVoxInitMeshPosFinished = true;
                    outerVoxCreationStartedInitial = true;
                    if (progressMessages) Debug.Log("MT Completed Init Set Core Mesh Position Job");
                }
            }

            //Create Outer Voxels
            if (outerVoxInitMeshPosFinished && !outerVoxCreationStarted)
            {
                xBorderN -= outerVoxOffsetO;
                yBorderN -= outerVoxOffsetO;
                zBorderN -= outerVoxOffsetO;
                totalBorderSize = xBorderP - xBorderN;
                int useSize = totalBorderSize / outerVoxOffsetO - 1;
                int outerVoxSize =
                    (useSize * useSize * 6)//Faces
                    + (useSize * 12)//Edges
                    + 8;//Corners
                outerVoxesO = new NativeArray<DCSVoxel>(outerVoxSize, Allocator.Persistent);

                createOuterVoxelJob = new CreateOuterVoxelsJob
                {
                    coreVoxelData = coreVoxelDataMT,
                    outerVoxes = outerVoxesO,
                    xBorderN = xBorderN,
                    xBorderP = xBorderP,
                    yBorderN = yBorderN,
                    yBorderP = yBorderP,
                    zBorderN = zBorderN,
                    zBorderP = zBorderP,
                    offset = outerVoxOffsetO,
                    debug = false,
                };
                createOuterVoxelJobHandle = createOuterVoxelJob.Schedule();

                outerVoxCreationStartedInitial = false;
                outerVoxCreationStarted = true;
                if (progressMessages) Debug.Log("MT Started Create Outer Voxes Job");
            }
            if (outerVoxCreationStarted && !outerVoxCreationFinished)
            {
                if (createOuterVoxelJobHandle.IsCompleted)
                {
                    createOuterVoxelJobHandle.Complete();
                    xBorderP += outerVoxOffsetO;
                    yBorderP += outerVoxOffsetO;
                    zBorderP += outerVoxOffsetO;

                    outerVoxCreationFinished = true;
                    if (progressMessages) Debug.Log("MT Completed Create Outer Voxes Job");
                }
            }

            //Set Outer Voxel Mesh Positions
            if (outerVoxCreationFinished && !outerVoxPositionsStarted)
            {
                setOuterVoxelMeshPositionJob = new SetOuterVoxelMeshPositionJob
                {
                    outerVox = outerVoxesO,
                    blockCenterPos = coreVoxelDataMT.coreVoxStartPos,
                    vertDensities = vertDensities
                };
                setOuterVoxelMeshPositionJobHandle = setOuterVoxelMeshPositionJob.Schedule(outerVoxesO.Length, 16);

                outerVoxPositionsStarted = true;
                if (progressMessages) Debug.Log("MT Started Setting Outer Mesh Positions Job");
            }
            if (outerVoxPositionsStarted && !outerVoxPositionsFinished)
            {
                if (setOuterVoxelMeshPositionJobHandle.IsCompleted)
                {
                    setOuterVoxelMeshPositionJobHandle.Complete();

                    outerVoxPositionsFinished = true;
                    if (progressMessages) Debug.Log("MT Completed Setting Outer Mesh Positions Job");
                }
            }

            //Create Outer Voxel Mesh Data
            if (outerVoxPositionsFinished && !outerVoxMeshDataCreationStarted)
            {
                outerVoxMeshDataCreationJob = new OuterVoxMeshDataCreationJob
                {
                    coreVoxData = coreVoxelDataMT,
                    outerVoxI = outerVoxesI,
                    outerVoxO = outerVoxesO,
                    vertDensities = vertDensities,
                    verticies = verticies,
                    triangles = triangles,
                    uvs = uvs,
                    normals = normals,
                    innerOffset = outerVoxOffsetI,
                    debug = false,
                };
                outerVoxMeshDataCreationJobHandle = outerVoxMeshDataCreationJob.Schedule();

                outerVoxMeshDataCreationStarted = true;
                if (progressMessages) Debug.Log("MT Started Setting Outer Mesh Data Job");
            }
            if (outerVoxMeshDataCreationStarted && !outerVoxMeshDataCreationFinished)
            {
                if (outerVoxMeshDataCreationJobHandle.IsCompleted)
                {
                    outerVoxMeshDataCreationJobHandle.Complete();

                    ++outerVoxEscapeCount;
                    totalBorderSize = xBorderP - xBorderN;
                    outerVoxesI = outerVoxesO;
                    outerVoxOffsetI = outerVoxOffsetO;

                    if (!switchToDoubling && outerVoxOffsetO >= 8)
                    {
                        switchToDoubling = true;
                        outerVoxOffsetO = totalBorderSize / 2;
                    }
                    else if (outerVoxOffsetO < 40)
                        outerVoxOffsetO *= 2;

                    if ((xBorderP <= recipeTotalVoxSize.x + outerVoxOffsetO
                        || yBorderP <= recipeTotalVoxSize.y + outerVoxOffsetO
                        || zBorderP <= recipeTotalVoxSize.z + outerVoxOffsetO)
                        && outerVoxEscapeCount < 100)
                    {
                        outerVoxCreationStarted = false;
                        outerVoxCreationFinished = false;
                        outerVoxPositionsStarted = false;
                        outerVoxPositionsFinished = false;
                        outerVoxMeshDataCreationStarted = false;
                        outerVoxMeshDataCreationFinished = false;
                    }
                    else
                    {
                        outerVoxJobsFinishedAll = true;
                    }

                    ++debugCounter;
                    if (progressMessages) Debug.Log(debugCounter + " MT Completed Setting Outer Mesh Data Job. outerVoxOffsetO:" + outerVoxOffsetO + ", BorderP:" + xBorderP);
                    if (outerVoxJobsFinishedAll)
                    {
                        vertDensities.Dispose();
                        outerVoxesI.Dispose();

                        if (progressMessages) Debug.Log("MT Completed All Outer Mesh Jobs");
                    }
                }
            }
        }


        //Create Mesh
        if (outerVoxJobsFinishedAll && !meshCreationStarted)
        {
            meshDataArray = Mesh.AllocateWritableMeshData(1);
            meshData = meshDataArray[0];
            int mat = 0;
            createMeshJob = new CreateMeshJob
            {
                meshData = meshData,
                verticies = verticies,
                triangles = triangles,
                uvs = uvs,
                normals = normals,
                debug = false
            };
            createMeshJobHandle = createMeshJob.Schedule();

            meshCreationStarted = true;
            if (progressMessages) Debug.Log("MT Started Create Mesh Job");
        }
        if (meshCreationStarted && !meshCreationFinished)
        {
            if (createMeshJobHandle.IsCompleted)
            {
                createMeshJobHandle.Complete();
                Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, worldMesh);

                working = false;
                meshCreationFinished = true;
                if (progressMessages) Debug.Log("MT Completed Create Mesh Job");
            }
        }

        //Clean Up
        if (!working)
        {
            if (progressMessages) Debug.Log("MT Completed All Mesh Creations Jobs");
            coreVoxelDataMT.coreVoxels.Dispose();
            verticies.Dispose();
            triangles.Dispose();
            uvs.Dispose();
            normals.Dispose();

            for (int x = 0; x < 3; ++x)
            {
                float useX = meshCenter.x;
                if (x == 0) useX -= (hDTotalDecimalSize.x / 2f);
                else if (x == 2) useX += (hDTotalDecimalSize.x / 2f);
                for (int y = 0; y < 3; ++y)
                {
                    float useY = meshCenter.y;
                    if (y == 0) useY -= (hDTotalDecimalSize.y / 2f);
                    else if (y == 2) useY += (hDTotalDecimalSize.y / 2f);
                    for (int z = 0; z < 3; ++z)
                    {
                        float useZ = meshCenter.z;
                        if (z == 0) useZ -= (hDTotalDecimalSize.z / 2f);
                        else if (z == 2) useZ += (hDTotalDecimalSize.z / 2f);

                        recenterPoints[x, y, z] = new Vector3(useX, useY, useZ);
                    }
                }
            }
        }

        return working;
    }

    //RecenterWorld is the same as RecenterWorldMT, but all multithreaded compoanants have been commented out and replaced by non multithreaded variants.
    //This was done to keep the same procedural structure as the multithreaded version while still allowing various kinds of visual debugging not possible through the multithreaded execution.
    public static bool RecenterWorld(ref Mesh worldMesh)
    {
        if (!coreVoxCreationStarted)
        {
            useDebugGOs = GameObject.FindObjectOfType<MeshGeneratorDebugGOs>();
            if (GameObject.Find("CreationDebug") != null)
            {
                DestroyImmediate(GameObject.Find("CreationDebug"));
            }
            creationGO = new GameObject("CreationDebug");
            if (GameObject.Find("CreationDebug_D0") != null)
            {
                DestroyImmediate(GameObject.Find("CreationDebug_D0"));
            }
            creationGO_D0 = new GameObject("CreationDebug_D0");
            creationGO_D0.transform.parent = creationGO.transform;
            if (GameObject.Find("CreationDebug_D1") != null)
            {
                DestroyImmediate(GameObject.Find("CreationDebug_D1"));
            }
            creationGO_D1 = new GameObject("CreationDebug_D1");
            creationGO_D1.transform.parent = creationGO.transform;
            if (GameObject.Find("CreationDebug_D2") != null)
            {
                DestroyImmediate(GameObject.Find("CreationDebug_D2"));
            }
            creationGO_D2 = new GameObject("CreationDebug_D2");
            creationGO_D2.transform.parent = creationGO.transform;
            if (GameObject.Find("CreationDebug_D3") != null)
            {
                DestroyImmediate(GameObject.Find("CreationDebug_D3"));
            }
            creationGO_D3 = new GameObject("CreationDebug_D3");
            creationGO_D3.transform.parent = creationGO.transform;
            if (GameObject.Find("OuterVoxes") != null)
            {
                DestroyImmediate(GameObject.Find("OuterVoxes"));
            }
            outerVoxesGO = new GameObject("OuterVoxes");
        }

        bool working = true;
        bool progressMessages = false;

        //Create Core Voxels
        if (!coreVoxCreationStarted)
        {
            meshStart = new Vector3(
                meshCenter.x - (((coreVoxSize.x) * voxSize[0].x) / 2) + voxSize[0].x,
                meshCenter.y - (((coreVoxSize.y) * voxSize[0].y) / 2) + voxSize[0].y,
                meshCenter.z - (((coreVoxSize.z) * voxSize[0].z) / 2) + voxSize[0].z);
            Debug.Log("meshCenter:" + meshCenter + ", coreVoxSize:" + coreVoxSize + ", voxSize[0]:" + voxSize[0] + ", meshStart:" + meshStart);
            coreVoxelDataMT = new CoreVoxelDataMT();
            coreVoxelDataMT.coreVoxStartPos = meshStart;
            coreVoxelDataMT.coreVoxEndPos = new Vector3(meshStart.x + coreVoxSize.x * voxSize[0].x, meshStart.y + coreVoxSize.y * voxSize[0].y, meshStart.z + coreVoxSize.z * voxSize[0].z);

            coreVoxelCounter = 0;
            coreVoxelDataMT.coreVoxels = new NativeArray<DCVoxel>(coreVoxelCountData.coreVoxTotal, Allocator.Persistent);
            createCoreVoxelJob = new CreateCoreVoxelsJob
            {
                coreVoxelData = coreVoxelDataMT,
                debug = true
            };
            //createCoreVoxelJobHandle = createCoreVoxelJob.Schedule();
            createCoreVoxelJob.Execute();

            coreVoxCreationStarted = true;
            Debug.Log("Started Core Vox Creation Job");
        }
        if (coreVoxCreationStarted && !coreVoxCreationFinished)
        {
            if (true)//createCoreVoxelJobHandle.IsCompleted)
            {
                //createCoreVoxelJobHandle.Complete();
                //coreVoxelDataMT.coreVoxels = useCoreVoxelData;

                coreVoxCreationFinished = true;
                Debug.Log("Completed Core Vox Creation Job");
            }
        }


        //Get Core Mesh Positions
        if (coreVoxCreationFinished && !coreVoxPositionsStarted)
        {
            totalCross = new NativeArray<int>(1, Allocator.Persistent);
            vertDensities = new NativeHashMap<int, float>(1000000, Allocator.Persistent);
            meshPositionJob = new SetMeshVertexPositionJob
            {
                voxDataMT = coreVoxelDataMT.coreVoxels,
                blockCenterPosMT = coreVoxelDataMT.coreVoxStartPos,
                vertDensities = vertDensities,
                totalCross = totalCross,
                debug = false

            };
            //setVoxPositionCoreVoxelJobHandle = testJob.Schedule(voxArrayMT.Length, 16);
            for (int coreVox = 0; coreVox < coreVoxelDataMT.coreVoxels.Length; ++coreVox)
            {
                meshPositionJob.Execute(coreVox);
            }
            //handle.Complete();

            coreVoxPositionsStarted = true;
            Debug.Log("Started Core Mesh Position Job");
        }
        if (coreVoxPositionsStarted && !coreVoxPositionsFinished)
        {
            if (true)//setVoxPositionCoreVoxelJobHandle.IsCompleted)
            {
                //setVoxPositionCoreVoxelJobHandle.Complete();
                coreVoxelDataMT.coreVoxels = meshPositionJob.voxDataMT;

                coreVoxPositionsFinished = true;
                Debug.Log("Completed Core Mesh Position Job");
            }
        }


        //Create Core Mesh Data
        if (coreVoxPositionsFinished && !coreVoxMeshDataCreationStarted)
        {
            verticies = new NativeList<float3>(totalCross[0] * 3, Allocator.Persistent);
            normals = new NativeList<float3>(totalCross[0] * 3, Allocator.Persistent);
            triangles = new NativeList<int>(totalCross[0], Allocator.Persistent);
            uvs = new NativeList<float2>(totalCross[0] * 3, Allocator.Persistent);

            coreVoxMeshDataCreationJob = new CoreVoxMeshDataCreationJob
            {
                voxData = coreVoxelDataMT,
                vertDensities = vertDensities,
                verticies = verticies,
                triangles = triangles,
                uvs = uvs,
                normals = normals,
                debug = false,
            };
            //coreVoxMeshCreationJobHandle = coreVoxMeshCreationJob.Schedule();
            coreVoxMeshDataCreationJob.Execute();

            totalCross.Dispose();
            coreVoxMeshDataCreationStarted = true;
            Debug.Log("Started Create Core Mesh Job");
        }
        if (coreVoxMeshDataCreationStarted && !coreVoxMeshDataCreationFinished)
        {
            if (true)//(coreVoxMeshCreationJobHandle.IsCompleted)
            {
                //coreVoxMeshDataCreationJobHandle.Complete();

                coreVoxMeshDataCreationFinished = true;
                Debug.Log("Completed Create Core Mesh Job");
            }
        }


        //Outer Voxel Jobs
        if (coreVoxMeshDataCreationFinished && !outerVoxJobsStartedAll && !outerVoxJobsFinishedAll)
        {
            //Setup + Create Initial Outer Voxels
            if (!outerVoxSetupStarted)
            {
                outerVoxEscapeCount = 0;
                outerVoxOffsetI = 1;
                outerVoxOffsetO = 2;
                xBorderN = 0;
                xBorderP = coreVoxSize.x;
                yBorderN = 0;
                yBorderP = coreVoxSize.y;
                zBorderN = 0;
                zBorderP = coreVoxSize.z;
                totalBorderSize = xBorderP - xBorderN;
                switchToDoubling = false;
                int useSize = totalBorderSize / outerVoxOffsetI - 1;
                int outerVoxSize =
                    (useSize * useSize * 6)//Faces
                    + (useSize * 12)//Edges
                    + 8;//Corners
                outerVoxesI = new NativeArray<DCSVoxel>(outerVoxSize, Allocator.Persistent);

                createOuterVoxelJob = new CreateOuterVoxelsJob
                {
                    coreVoxelData = coreVoxelDataMT,
                    outerVoxes = outerVoxesI,
                    xBorderN = xBorderN,
                    xBorderP = xBorderP,
                    yBorderN = yBorderN,
                    yBorderP = yBorderP,
                    zBorderN = zBorderN,
                    zBorderP = zBorderP,
                    offset = outerVoxOffsetI,
                    debug = false,
                };
                //intitalCreateOuterVoxelJobHandle = createOuterVoxelJob.Schedule();
                createOuterVoxelJob.Execute();

                outerVoxSetupStarted = true;
                if (progressMessages) Debug.Log("Started Setup Outer Voxel Job");
            }
            if (outerVoxSetupStarted && !outerVoxSetupFinished)
            {
                if (true)//(intitalCreateOuterVoxelJobHandle.IsCompleted)
                {
                    //intitalCreateOuterVoxelJobHandle.Complete();

                    outerVoxSetupFinished = true;
                    if (progressMessages) Debug.Log("Completed Setup Outer Voxel Job");
                }
            }

            //Set Init Outer Voxel Mesh Posiions
            if (outerVoxSetupFinished && !outerVoxInitMeshPosStarted)
            {
                setOuterVoxelMeshPositionJob = new SetOuterVoxelMeshPositionJob
                {
                    outerVox = outerVoxesI,
                    blockCenterPos = coreVoxelDataMT.coreVoxStartPos,
                    vertDensities = vertDensities
                };
                //intitalSetOuterVoxelMeshPositionJobHandle = setOuterVoxelMeshPositionJob.Schedule(outerVoxesI.Length, 16);
                for (int outerVoxCount = 0; outerVoxCount < outerVoxesI.Length; ++outerVoxCount)
                {
                    setOuterVoxelMeshPositionJob.Execute(outerVoxCount);
                }

                outerVoxInitMeshPosStarted = true;
                if (progressMessages) Debug.Log("Started Init Set Core Mesh Position Job");
            }
            if (outerVoxInitMeshPosStarted && !outerVoxInitMeshPosFinished)
            {
                if (true)//(intitalSetOuterVoxelMeshPositionJobHandle.IsCompleted)
                {
                    intitalSetOuterVoxelMeshPositionJobHandle.Complete();

                    outerVoxInitMeshPosFinished = true;
                    outerVoxCreationStartedInitial = true;
                    if (progressMessages) Debug.Log("Completed Init Set Core Mesh Position Job");
                }
            }

            //Create Outer Voxels
            if (outerVoxInitMeshPosFinished && !outerVoxCreationStarted)
            {
                xBorderN -= outerVoxOffsetO;
                yBorderN -= outerVoxOffsetO;
                zBorderN -= outerVoxOffsetO;
                totalBorderSize = xBorderP - xBorderN;
                int useSize = totalBorderSize / outerVoxOffsetO - 1;
                int outerVoxSize =
                    (useSize * useSize * 6)//Faces
                    + (useSize * 12)//Edges
                    + 8;//Corners
                outerVoxesO = new NativeArray<DCSVoxel>(outerVoxSize, Allocator.Persistent);

                createOuterVoxelJob = new CreateOuterVoxelsJob
                {
                    coreVoxelData = coreVoxelDataMT,
                    outerVoxes = outerVoxesO,
                    xBorderN = xBorderN,
                    xBorderP = xBorderP,
                    yBorderN = yBorderN,
                    yBorderP = yBorderP,
                    zBorderN = zBorderN,
                    zBorderP = zBorderP,
                    offset = outerVoxOffsetO,
                    debug = false,
                };
                //createOuterVoxelJobHandle = createOuterVoxelJob.Schedule();
                createOuterVoxelJob.Execute();

                outerVoxCreationStartedInitial = false;
                outerVoxCreationStarted = true;
                if (progressMessages) Debug.Log("Started Create Outer Voxes Job");
            }
            if (outerVoxCreationStarted && !outerVoxCreationFinished)
            {
                if (true)//(createOuterVoxelJobHandle.IsCompleted)
                {
                    //createOuterVoxelJobHandle.Complete();
                    xBorderP += outerVoxOffsetO;
                    yBorderP += outerVoxOffsetO;
                    zBorderP += outerVoxOffsetO;

                    outerVoxCreationFinished = true;
                    if (progressMessages) Debug.Log("Completed Create Outer Voxes Job");
                }
            }

            //Set Outer Voxel Mesh Positions
            if (outerVoxCreationFinished && !outerVoxPositionsStarted)
            {
                setOuterVoxelMeshPositionJob = new SetOuterVoxelMeshPositionJob
                {
                    outerVox = outerVoxesO,
                    blockCenterPos = coreVoxelDataMT.coreVoxStartPos,
                    vertDensities = vertDensities
                };
                for (int outerVoxCount = 0; outerVoxCount < outerVoxesO.Length; ++outerVoxCount)
                {
                    setOuterVoxelMeshPositionJob.Execute(outerVoxCount);
                }

                outerVoxPositionsStarted = true;
                if (progressMessages) Debug.Log("Started Setting Outer Mesh Positions Job");
            }
            if (outerVoxPositionsStarted && !outerVoxPositionsFinished)
            {
                if (true)//(setOuterVoxelMeshPositionJobHandle.IsCompleted)
                {
                    //setOuterVoxelMeshPositionJobHandle.Complete();

                    outerVoxPositionsFinished = true;
                    if (progressMessages) Debug.Log("Completed Setting Outer Mesh Positions Job");
                }
            }

            //Create Outer Voxel Mesh Data
            if (outerVoxPositionsFinished && !outerVoxMeshDataCreationStarted)
            {
                outerVoxMeshDataCreationJob = new OuterVoxMeshDataCreationJob
                {
                    coreVoxData = coreVoxelDataMT,
                    outerVoxI = outerVoxesI,
                    outerVoxO = outerVoxesO,
                    vertDensities = vertDensities,
                    verticies = verticies,
                    triangles = triangles,
                    uvs = uvs,
                    normals = normals,
                    innerOffset = outerVoxOffsetI,
                    debug = true,
                };
                //outerVoxMeshDataCreationJobHandle = outerVoxMeshDataCreationJob.Schedule();
                outerVoxMeshDataCreationJob.Execute();

                outerVoxMeshDataCreationStarted = true;
                if (progressMessages) Debug.Log("Started Setting Outer Mesh Data Job");
            }
            if (outerVoxMeshDataCreationStarted && !outerVoxMeshDataCreationFinished)
            {
                if (true)//(outerVoxMeshDataCreationJobHandle.IsCompleted)
                {
                    //outerVoxMeshDataCreationJobHandle.Complete();

                    ++outerVoxEscapeCount;
                    totalBorderSize = xBorderP - xBorderN;
                    outerVoxesI = outerVoxesO;
                    outerVoxOffsetI = outerVoxOffsetO;

                    if (!switchToDoubling && outerVoxOffsetO >= 8)
                    {
                        switchToDoubling = true;
                        outerVoxOffsetO = totalBorderSize / 2;
                    }
                    else if (outerVoxOffsetO < 40)
                        outerVoxOffsetO *= 2;

                    if ((xBorderP <= recipeTotalVoxSize.x + outerVoxOffsetO
                        || yBorderP <= recipeTotalVoxSize.y + outerVoxOffsetO
                        || zBorderP <= recipeTotalVoxSize.z + outerVoxOffsetO)
                        && outerVoxEscapeCount < 100)
                    {
                        outerVoxCreationStarted = false;
                        outerVoxCreationFinished = false;
                        outerVoxPositionsStarted = false;
                        outerVoxPositionsFinished = false;
                        outerVoxMeshDataCreationStarted = false;
                        outerVoxMeshDataCreationFinished = false;
                    }
                    else
                    {
                        outerVoxJobsFinishedAll = true;
                    }

                    ++debugCounter;
                    if (progressMessages) Debug.Log(debugCounter + " MT Completed Setting Outer Mesh Data Job. outerVoxOffsetO:" + outerVoxOffsetO + ", BorderP:" + xBorderP);
                    if (outerVoxJobsFinishedAll)
                    {
                        vertDensities.Dispose();
                        outerVoxesO.Dispose();
                        outerVoxJobsStartedAll = true;

                        if (progressMessages) Debug.Log("Completed All Outer Mesh Jobs");
                    }
                }
            }
        }


        //Create Mesh
        if (outerVoxJobsFinishedAll && !meshCreationStarted)
        {
            meshDataArray = Mesh.AllocateWritableMeshData(1);
            meshData = meshDataArray[0];
            int mat = 0;
            stream0 = new NativeArray<Stream0>(verticies.Length, Allocator.Persistent);
            streamTriangles = new NativeArray<ushort>(verticies.Length * 3, Allocator.Persistent);
            createMeshJob = new CreateMeshJob
            {
                meshData = meshData,
                verticies = verticies,
                triangles = triangles,
                uvs = uvs,
                normals = normals,
                debug = false
            };
            //createMeshJobHandle = createMeshJob.Schedule();
            createMeshJob.Execute();

            meshCreationStarted = true;
            Debug.Log("MT Started Create Mesh Job");
        }
        if (meshCreationStarted && !meshCreationFinished)
        {
            if (true)//(createMeshJobHandle.IsCompleted)
            {
                //createMeshJobHandle.Complete();
                Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, worldMesh);

                working = false;
                meshCreationFinished = true;
                Debug.Log("MT Completed Create Mesh Job");
            }
        }


        //Clean Up
        if (!working)
        {
            coreVoxelDataMT.coreVoxels.Dispose();
            verticies.Dispose();
            triangles.Dispose();
            uvs.Dispose();
            normals.Dispose();
        }

        return working;
    }


    //Set Up Functions
    //SetDCVariables sets variables realted to minimum voxel size, detail area, and other world generation information.
    //This only needs to be run at system startup or when a key creation variable has been changed.
    public static void SetDCVariables(bool debugMessages = false)
    {
        recipeBlockNum = new Vector3Int(10, 10, 10);
        if (debugMessages) Debug.Log("recipeBlockNum:" + recipeBlockNum);
        recipeBlockVoxSize = new Vector3Int(50, 50, 50);
        if (debugMessages) Debug.Log("recipeBlockVoxSize:" + recipeBlockVoxSize);
        recipeTotalVoxSize = new Vector3Int(recipeBlockNum.x * recipeBlockVoxSize.x, recipeBlockNum.y * recipeBlockVoxSize.y, recipeBlockNum.z * recipeBlockVoxSize.z);
        if (debugMessages) Debug.Log("recipeTotalVoxSize:" + recipeTotalVoxSize);

        coreVoxSize = new Vector3Int(52, 52, 52);
        if (debugMessages) Debug.Log("coreVoxSize:" + coreVoxSize);

        nullVector = new Vector3(int.MinValue, int.MinValue, int.MinValue);

        //Set Vox Size per Depth
        worldDepthLimit = 3;
        smallestVoxSize = new Vector3(2f, 2f, 2f);
        Vector3 d = smallestVoxSize;
        voxSize = new Vector3[worldDepthLimit + 1];
        voxSizeF3 = new float3[worldDepthLimit + 1];
        voxHalfSize = new Vector3[worldDepthLimit + 1];
        voxHalfSizeF3 = new float3[worldDepthLimit + 1];
        voxNum = new int[worldDepthLimit + 1];
        voxSize[worldDepthLimit] = d;
        voxSizeF3[worldDepthLimit] = d;
        voxHalfSize[worldDepthLimit] = d * .5f;
        voxHalfSizeF3[worldDepthLimit] = d * .5f;
        for (int i = worldDepthLimit - 1; i >= 0; --i)
        {
            voxSize[i] = d * 2f;
            voxSizeF3[i] = d * 2f;
            voxHalfSize[i] = voxSize[i] * .5f;
            voxHalfSizeF3[i] = voxSizeF3[i] * .5f;
            d = voxSize[i];
            if (debugMessages) Debug.Log("i:" + i + ", d:" + d + ", voxSize[i]:" + voxSize[i] + ", voxHalfSize[i]:" + voxHalfSize[i]);
        }

        for (int i = worldDepthLimit; i >= 0; --i)
        {
            voxNum[i] = (int)Mathf.Pow(2f, (float)i);
            if (debugMessages) Debug.Log("i:" + i + ", voxNum[i]:" + voxNum[i]);
        }

        voxDepthPosMod = new int[worldDepthLimit + 1];
        voxDepthPosMod[0] = (int)Mathf.Pow(2f, worldDepthLimit);
        for (int i = 1; i <= worldDepthLimit; ++i)
        {
            voxDepthPosMod[i] = voxDepthPosMod[i - 1] / 2;
            if (debugMessages) Debug.Log("i:" + i + ", voxDepthPosMod[i]:" + voxDepthPosMod[i]);
        }

        depthDetail = new int[worldDepthLimit + 1];
        for (int i = worldDepthLimit; i >= 0; --i)
        {
            depthDetail[i] = i;
        }

        recipeBlockDecimalSize = new Vector3(coreVoxSize.x * voxSize[0].x, coreVoxSize.y * voxSize[0].y, coreVoxSize.z * voxSize[0].z);
        if (debugMessages) Debug.Log("recipeBlockDecimalSize:" + recipeBlockDecimalSize);
        recipeTotalDecimalSize = new Vector3(recipeBlockDecimalSize.x * recipeBlockNum.x, recipeBlockDecimalSize.y * recipeBlockNum.y, recipeBlockDecimalSize.z * recipeBlockNum.z);
        if (debugMessages) Debug.Log("recipeTotalDecimalSize:" + recipeTotalDecimalSize);

        Vector3Int coreCenter = new Vector3Int((recipeBlockVoxSize.x / 2) - 1, (recipeBlockVoxSize.y / 2) - 1, (recipeBlockVoxSize.z / 2) - 1);
        worldCenter = new Vector3Int(coreVoxSize.x / 2 - 1, coreVoxSize.y / 2 - 1, coreVoxSize.z / 2 - 1);
        if (debugMessages) Debug.Log("worldCenter:" + worldCenter);

        depthLimitsP = new Vector3Int[worldDepthLimit + 1];
        depthLimitsN = new Vector3Int[worldDepthLimit + 1];
        int hDayerWidth = 3;
        depthLimitsN[worldDepthLimit].x = coreCenter.x - (hDayerWidth - 1); depthLimitsN[worldDepthLimit].y = coreCenter.x - (hDayerWidth - 1); depthLimitsN[worldDepthLimit].z = coreCenter.x - (hDayerWidth - 1);
        depthLimitsP[worldDepthLimit].x = coreCenter.x + hDayerWidth; depthLimitsP[worldDepthLimit].y = coreCenter.x + hDayerWidth; depthLimitsP[worldDepthLimit].z = coreCenter.x + hDayerWidth;
        hDTotalVoxSize = new Vector3Int(depthLimitsP[worldDepthLimit].x - depthLimitsN[worldDepthLimit].x, depthLimitsP[worldDepthLimit].y - depthLimitsN[worldDepthLimit].y, depthLimitsP[worldDepthLimit].z - depthLimitsN[worldDepthLimit].z);
        hDTotalDecimalSize = new Vector3(hDTotalVoxSize.x * voxSize[0].x, hDTotalVoxSize.y * voxSize[0].y, hDTotalVoxSize.z * voxSize[0].z);
        midRecenterPoint = hDayerWidth - 1;
        recenterPoints = new Vector3[3, 3, 3];
        if (debugMessages) Debug.Log("depthLimitsN[worldDepthLimit]:" + depthLimitsN[worldDepthLimit] + ", depthLimitsP[worldDepthLimit]:" + depthLimitsP[worldDepthLimit] + ", hDTotalVoxSize:" + hDTotalVoxSize + ", hDTotalSize:" + hDTotalDecimalSize);

        int coreLayerSize = 2;
        for (int i = worldDepthLimit - 1; i >= 0; --i)
        {
            depthLimitsN[i].x = depthLimitsN[i + 1].x - coreLayerSize; depthLimitsN[i].y = depthLimitsN[i + 1].y - coreLayerSize; depthLimitsN[i].z = depthLimitsN[i + 1].z - coreLayerSize;
            depthLimitsP[i].x = depthLimitsP[i + 1].x + coreLayerSize; depthLimitsP[i].y = depthLimitsP[i + 1].y + coreLayerSize; depthLimitsP[i].z = depthLimitsP[i + 1].z + coreLayerSize;
            if (debugMessages) Debug.Log("i:" + i + ", depthLimitsN[i]:" + depthLimitsN[i] + ", depthLimitsP[i]:" + depthLimitsP[i]);
        }

        coreVoxelCountData = CountCoreVoxelsNeeded();
    }

    //SetCreationFlags sets the job creation flags. It needs to be run once before kicking off the mesh creation function.
    public static void SetCreationFlags()
    {
        coreVoxCreationStarted = false;
        coreVoxCreationFinished = false;
        coreVoxPositionsStarted = false;
        coreVoxPositionsFinished = false;
        coreVoxMeshDataCreationStarted = false;
        coreVoxMeshDataCreationFinished = false;

        outerVoxSetupStarted = false;
        outerVoxSetupFinished = false;
        outerVoxInitMeshPosStarted = false;
        outerVoxInitMeshPosFinished = false;
        outerVoxCreationStartedInitial = false;
        outerVoxCreationStarted = false;
        outerVoxCreationFinished = false;
        outerVoxPositionsStarted = false;
        outerVoxPositionsFinished = false;
        outerVoxMeshDataCreationStarted = false;
        outerVoxMeshDataCreationFinished = false;
        outerVoxJobsStartedAll = false;
        outerVoxJobsFinishedAll = false;

        meshCreationStarted = false;
        meshCreationFinished = false;

        cachCounterFound = 0;
        cachCounterNotFound = 0;
        outerVoxEscapeCount = 0;
    }

    //MakeRecipeBlocks creates the densisty evaluation data used to create the world.
    //This only needs to be run at startup or when the reciple blocks have changed.
    public static void MakeRecipeBlocks(Vector3 newMeshCenter, bool recipeDebug = false)
    {
        if (GameObject.Find("RecipeBlocks") != null)
        {
            DestroyImmediate(GameObject.Find("RecipeBlocks"));
        }
        GameObject recipeBlocks = new GameObject("RecipeBlocks");

        recipeData = new RecipeDataLoaded();
        recipeData.recipeDataBlocks = new RecipeDataBlock[recipeBlockNum.x, recipeBlockNum.y, recipeBlockNum.z];

        Vector3Int centerAdjust = new Vector3Int(recipeBlockNum.x / 2, recipeBlockNum.y / 2, recipeBlockNum.z / 2);
        meshCenter = newMeshCenter;
        Vector3 recipeBlocksStart = newMeshCenter;
        recipeBlocksStart -= new Vector3(recipeBlockDecimalSize.x * centerAdjust.x, recipeBlockDecimalSize.y * centerAdjust.y, recipeBlockDecimalSize.z * centerAdjust.z);
        recipeData.recipeLoadedStartPos = recipeBlocksStart;
        recipeData.recipeLoadedEndPos = new Vector3(recipeBlocksStart.x + recipeBlockDecimalSize.x * recipeBlockNum.x, recipeBlocksStart.y + recipeBlockDecimalSize.y * recipeBlockNum.y, recipeBlocksStart.z + recipeBlockDecimalSize.z * recipeBlockNum.z);
        if (recipeDebug) Debug.Log("worldBlocksStart:" + recipeBlocksStart);
        for (int x = 0; x < recipeData.recipeDataBlocks.GetLength(0); ++x)
        {
            for (int y = 0; y < recipeData.recipeDataBlocks.GetLength(1); ++y)
            {
                for (int z = 0; z < recipeData.recipeDataBlocks.GetLength(2); ++z)
                {
                    recipeData.recipeDataBlocks[x, y, z].DefaultDF = DensityFunction.DFType.simplex;
                    recipeData.recipeDataBlocks[x, y, z].voxOffset = new Vector3Int(x * recipeBlockVoxSize.x, y * recipeBlockVoxSize.y, z * recipeBlockVoxSize.z);
                    recipeData.recipeDataBlocks[x, y, z].posOffset = new Vector3(x * recipeBlockDecimalSize.x + recipeBlocksStart.x, y * recipeBlockDecimalSize.y + recipeBlocksStart.y, z * recipeBlockDecimalSize.z + recipeBlocksStart.z);
                }
            }
        }
    }


    //Core Voxel Creation Functions
    //CreateCoreVoxelsJob works as an Ijob meaning it run off the main thread to prevent interruptions.
    //The core voxels act as a virtual octree with the startng area subdvided into a field of voxels, and then each of those voxels subdivided into 8 smaller voxels, and then again, repeating for the desired voxel detail depth.
    //Note: Because pointers cannot be used in unity's job system, instead array indexes are stored to a place within a single voxel array
    public struct CreateCoreVoxelsJob : IJob
    {
        public CoreVoxelDataMT coreVoxelData;
        public bool debug;

        public void Execute()
        {
            Vector3 meshStart = new Vector3(meshCenter.x - ((coreVoxSize.x * voxSize[0].x) / 2f) + voxSize[0].x, meshCenter.y - ((coreVoxSize.y * voxSize[0].y) / 2) + voxSize[0].y, meshCenter.z - ((coreVoxSize.z * voxSize[0].z) / 2) + voxSize[0].z);
            coreVoxelData.coreVoxStartPos = meshStart;
            coreVoxelData.coreVoxEndPos = new Vector3(meshStart.x + coreVoxSize.x * voxSize[0].x, meshStart.y + coreVoxSize.y * voxSize[0].y, meshStart.z + coreVoxSize.z * voxSize[0].z);

            //Creating starting area of voxels
            int fullPosMod = voxDepthPosMod[0];
            for (int x = 0; x <= coreVoxSize.x; ++x)
            {
                for (int y = 0; y <= coreVoxSize.y; ++y)
                {
                    for (int z = 0; z <= coreVoxSize.z; ++z)
                    {
                        int useDepth = 0;
                        int detail = 0;

                        for (int checkLayer = worldDepthLimit; checkLayer >= 0; --checkLayer)
                        {
                            if (x >= depthLimitsN[checkLayer].x && x <= depthLimitsP[checkLayer].x && y >= depthLimitsN[checkLayer].y && y <= depthLimitsP[checkLayer].y && z >= depthLimitsN[checkLayer].z && z <= depthLimitsP[checkLayer].z)
                            {
                                useDepth = checkLayer;
                                detail = depthDetail[checkLayer];
                                break;
                            }
                        }

                        DCVoxel newVoxel = new DCVoxel();
                        newVoxel.voxCreated = true;

                        newVoxel.depth = 0;
                        newVoxel.innerDepth = useDepth;
                        newVoxel.detail = detail;

                        newVoxel.pos.x = x;
                        newVoxel.pos.y = y;
                        newVoxel.pos.z = z;
                        newVoxel.fullPos = newVoxel.pos * fullPosMod;
                        newVoxel.baseNum = new int3(x, y, z);

                        newVoxel.startPoint = coreVoxelData.coreVoxStartPos + new float3(x * voxSizeF3[0].x, y * voxSizeF3[0].y, z * voxSizeF3[0].z);
                        newVoxel.centerPoint = newVoxel.startPoint + voxHalfSizeF3[0];
                        newVoxel.endPoint = newVoxel.centerPoint + voxHalfSizeF3[0];

                        newVoxel.anyCross = false;

                        if (x >= worldCenter.x)
                            newVoxel.x01 = true;
                        if (y >= worldCenter.y)
                            newVoxel.y01 = true;
                        if (z >= worldCenter.z)
                            newVoxel.z01 = true;

                        if (x == worldCenter.x)
                            newVoxel.xC = true;
                        if (y == worldCenter.y)
                            newVoxel.yC = true;
                        if (z == worldCenter.z)
                            newVoxel.zC = true;

                        if (x == worldCenter.x && y == worldCenter.y && z == worldCenter.z)
                            currentCenterVoxMT = newVoxel;

                        newVoxel.parentVox = -1;

                        if (useDepth == 0)
                            newVoxel.depthEnd = true;

                        newVoxel.index = coreVoxelCounter;

                        coreVoxelData.coreVoxels[coreVoxelCounter] = newVoxel;

                        ++coreVoxelCounter;
                    }
                }
            }

            //Subdivide each voxel created into 8 smaller voxels and then again for desired level of depth
            int count1cv = 0;
            for (int checkDepth = 0; checkDepth < worldDepthLimit; ++checkDepth)
            {
                int useCheckDepth = checkDepth + 1;
                int dvFullPosMod = voxDepthPosMod[useCheckDepth];
                for (int cv = 0; cv < coreVoxelData.coreVoxels.Length; ++cv)
                {
                    if (coreVoxelData.coreVoxels[cv].voxCreated && coreVoxelData.coreVoxels[cv].depth == checkDepth && coreVoxelData.coreVoxels[cv].innerDepth > checkDepth)
                    {

                        for (int x = 0; x < 2; ++x)
                        {
                            for (int y = 0; y < 2; ++y)
                            {
                                for (int z = 0; z < 2; ++z)
                                {
                                    DCVoxel newVoxel = new DCVoxel();
                                    newVoxel.voxCreated = true;
                                    newVoxel.depth = useCheckDepth;
                                    newVoxel.innerDepth = coreVoxelData.coreVoxels[cv].innerDepth;
                                    if (newVoxel.depth == newVoxel.innerDepth)
                                        newVoxel.depthEnd = true;

                                    newVoxel.pos.x = x;
                                    newVoxel.pos.y = y;
                                    newVoxel.pos.z = z;
                                    newVoxel.fullPos.x = x * dvFullPosMod + coreVoxelData.coreVoxels[cv].fullPos.x;
                                    newVoxel.fullPos.y = y * dvFullPosMod + coreVoxelData.coreVoxels[cv].fullPos.y;
                                    newVoxel.fullPos.z = z * dvFullPosMod + coreVoxelData.coreVoxels[cv].fullPos.z;
                                    newVoxel.baseNum = coreVoxelData.coreVoxels[cv].baseNum;

                                    newVoxel.startPoint = coreVoxelData.coreVoxels[cv].startPoint + new float3(x * voxSizeF3[useCheckDepth].x, y * voxSizeF3[useCheckDepth].y, z * voxSizeF3[useCheckDepth].z);
                                    newVoxel.centerPoint = newVoxel.startPoint + voxHalfSizeF3[useCheckDepth];
                                    newVoxel.endPoint = newVoxel.centerPoint + voxHalfSizeF3[useCheckDepth];

                                    newVoxel.anyCross = false;
                                    newVoxel.meshVertex = nullVector;

                                    newVoxel.x01 = coreVoxelData.coreVoxels[cv].x01;
                                    newVoxel.y01 = coreVoxelData.coreVoxels[cv].y01;
                                    newVoxel.z01 = coreVoxelData.coreVoxels[cv].z01;

                                    if (coreVoxelData.coreVoxels[cv].xC && x == 0)
                                        newVoxel.xC = coreVoxelData.coreVoxels[cv].xC;
                                    if (coreVoxelData.coreVoxels[cv].yC && y == 0)
                                        newVoxel.yC = coreVoxelData.coreVoxels[cv].yC;
                                    if (coreVoxelData.coreVoxels[cv].zC && z == 0)
                                        newVoxel.zC = coreVoxelData.coreVoxels[cv].zC;

                                    newVoxel.parentVox = coreVoxelData.coreVoxels[cv].index;

                                    newVoxel.index = coreVoxelCounter;

                                    coreVoxelData.coreVoxels[coreVoxelCounter] = newVoxel;

                                    coreVoxelData.coreVoxels[cv] = SetVNode(coreVoxelData.coreVoxels[cv], x, y, z);

                                    if (debug)
                                    {
                                        if (newVoxel.depth == 0)
                                            Debug.Log("DV == 0... ?");
                                        else if (newVoxel.depth == 1)
                                        {
                                            GameObject testGO = Instantiate(useDebugGOs.vertexTestGreen, coreVoxelData.coreVoxels[coreVoxelCounter].centerPoint, Quaternion.identity);
                                            testGO.name = "" + coreVoxelData.coreVoxels[coreVoxelCounter].pos + "||D:" + coreVoxelData.coreVoxels[coreVoxelCounter].innerDepth;
                                            testGO.transform.parent = creationGO_D1.transform;
                                        }
                                        else if (newVoxel.depth == 2)
                                        {
                                            GameObject testGO = Instantiate(useDebugGOs.vertexTestYellow, coreVoxelData.coreVoxels[coreVoxelCounter].centerPoint, Quaternion.identity);
                                            testGO.name = "" + coreVoxelData.coreVoxels[coreVoxelCounter].pos + "||D:" + coreVoxelData.coreVoxels[coreVoxelCounter].innerDepth;
                                            testGO.transform.parent = creationGO_D2.transform;
                                        }
                                        else if (newVoxel.depth == 3)
                                        {
                                            GameObject testGO = Instantiate(useDebugGOs.vertexTestRed, coreVoxelData.coreVoxels[coreVoxelCounter].centerPoint, Quaternion.identity);
                                            testGO.name = "" + coreVoxelData.coreVoxels[coreVoxelCounter].pos + "||D:" + coreVoxelData.coreVoxels[coreVoxelCounter].innerDepth;
                                            testGO.transform.parent = creationGO_D3.transform;
                                        }
                                    }

                                    ++coreVoxelCounter;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    //SetVNode sets the voxel pointer of the DCVoxel to it's child octrees.
    public static DCVoxel SetVNode(DCVoxel vox, int x, int y, int z)
    {
        DCVoxel newVox = vox;

        if (x == 0 && y == 0 && z == 0)
            newVox.vNode000 = coreVoxelCounter;
        else if (x == 0 && y == 0 && z == 1)
            newVox.vNode001 = coreVoxelCounter;
        else if (x == 0 && y == 1 && z == 0)
            newVox.vNode010 = coreVoxelCounter;
        else if (x == 0 && y == 1 && z == 1)
            newVox.vNode011 = coreVoxelCounter;
        else if (x == 0 && y == 0 && z == 0)
            newVox.vNode100 = coreVoxelCounter;
        else if (x == 1 && y == 0 && z == 1)
            newVox.vNode101 = coreVoxelCounter;
        else if (x == 1 && y == 1 && z == 0)
            newVox.vNode110 = coreVoxelCounter;
        else if (x == 1 && y == 1 && z == 1)
            newVox.vNode111 = coreVoxelCounter;

        return newVox;
    }

    //CountCoreVoxelsNeeded computes the number of indices that will ne needed for the voxel array.
    public static CoreVoxCountData CountCoreVoxelsNeeded()
    {
        int xSize = coreVoxSize.x;
        int ySize = coreVoxSize.y;
        int zSize = coreVoxSize.z;

        int counter = 0;
        int outerCounter = 0;
        int useCounter = 0;

        for (int x = 0; x <= xSize; ++x)
        {
            for (int y = 0; y <= ySize; ++y)
            {
                for (int z = 0; z <= zSize; ++z)
                {
                    int useDepth = 0;

                    for (int checkLayer = worldDepthLimit; checkLayer >= 0; --checkLayer)
                    {
                        if (x >= depthLimitsN[checkLayer].x && x <= depthLimitsP[checkLayer].x && y >= depthLimitsN[checkLayer].y && y <= depthLimitsP[checkLayer].y && z >= depthLimitsN[checkLayer].z && z <= depthLimitsP[checkLayer].z)
                        {
                            useDepth = checkLayer;
                            break;
                        }
                        if (useDepth == 0)
                            ++outerCounter;
                    }

                    for (int i = 0; i <= useDepth; ++i)
                    {
                        counter += PowFunction(8, i);
                    }

                    useCounter += PowFunction(8, useDepth);
                }
            }
        }

        CoreVoxCountData countData = new CoreVoxCountData();
        countData.coreVoxTotal = counter;
        countData.outerVoxelCount = outerCounter;
        countData.useCoreVoxTotal = useCounter;

        return countData;
    }

    //PowFunction is a simple int based power function made to replace the slower Mathf pow function.
    public static int PowFunction(int basenum, int pownum)
    {
        int num = 1;
        for (int i = 0; i < pownum; ++i)
        {
            num *= basenum;
        }

        return num;
    }


    //Set Position Functions
    //SetMeshVertexPositionJob is meant to be run in parallel across as many processors as are available and set the mesh position and normal for each voxel.
    public struct SetMeshVertexPositionJob : IJobParallelFor
    {
        public NativeArray<DCVoxel> voxDataMT;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> vertDensities;
        [ReadOnly]
        public float3 blockCenterPosMT;
        public NativeArray<int> totalCross;
        public bool debug;

        public void Execute(int index)
        {
            if (voxDataMT[index].depthEnd)
            {
                DCVoxel voxMT = voxDataMT[index];

                HermiteData hermiteData = new HermiteData();
                hermiteData.intersections = new List<float3>();
                hermiteData.gradients = new List<float3>();

                int3 v000 = GetVertixFromVoxel(voxMT, 0, 0, 0);
                int3 v001 = GetVertixFromVoxel(voxMT, 0, 0, 1);
                int3 v010 = GetVertixFromVoxel(voxMT, 0, 1, 0);
                int3 v011 = GetVertixFromVoxel(voxMT, 0, 1, 1);
                int3 v100 = GetVertixFromVoxel(voxMT, 1, 0, 0);
                int3 v101 = GetVertixFromVoxel(voxMT, 1, 0, 1);
                int3 v110 = GetVertixFromVoxel(voxMT, 1, 1, 0);
                int3 v111 = GetVertixFromVoxel(voxMT, 1, 1, 1);

                CheckIntersection(voxMT.detail, blockCenterPosMT, v000, v100, Dir.x, hermiteData, vertDensities); //p000p100
                CheckIntersection(voxMT.detail, blockCenterPosMT, v000, v010, Dir.y, hermiteData, vertDensities); //p000p010
                CheckIntersection(voxMT.detail, blockCenterPosMT, v000, v001, Dir.z, hermiteData, vertDensities); //p000p001
                CheckIntersection(voxMT.detail, blockCenterPosMT, v100, v110, Dir.y, hermiteData, vertDensities); //p100p110
                CheckIntersection(voxMT.detail, blockCenterPosMT, v100, v101, Dir.z, hermiteData, vertDensities); //p100p101
                CheckIntersection(voxMT.detail, blockCenterPosMT, v010, v110, Dir.x, hermiteData, vertDensities); //p010p110
                CheckIntersection(voxMT.detail, blockCenterPosMT, v010, v011, Dir.z, hermiteData, vertDensities); //p010p011
                CheckIntersection(voxMT.detail, blockCenterPosMT, v001, v101, Dir.x, hermiteData, vertDensities); //p001p101
                CheckIntersection(voxMT.detail, blockCenterPosMT, v001, v011, Dir.y, hermiteData, vertDensities); //p001p011
                CheckIntersection(voxMT.detail, blockCenterPosMT, v110, v111, Dir.z, hermiteData, vertDensities); //p110p111
                CheckIntersection(voxMT.detail, blockCenterPosMT, v011, v111, Dir.x, hermiteData, vertDensities); //p011p111
                CheckIntersection(voxMT.detail, blockCenterPosMT, v101, v111, Dir.y, hermiteData, vertDensities); //p101p111

                if (hermiteData.intersections.Count > 0)
                {
                    voxMT.anyCross = true;
                    voxMT.meshVertex = SchmitzVertexFromHermiteData(hermiteData, .001f);

                    DensityFunction.DFType useDF = GetVertexDensityFunction(v000, blockCenterPosMT);
                    voxMT.meshVertex = VertexClamp(voxMT.meshVertex, voxMT);
                    voxMT.meshNormal = GetNormal(voxMT.meshVertex, useDF, voxMT.detail);
                }

                voxDataMT[index] = voxMT;
            }
        }
    }

    //GetVertixFromVoxel gets takes a voxle and x/y/z deltas and gets the associated vertex.
    public static int3 GetVertixFromVoxel(DCVoxel vox, int xD, int yD, int zD, int depthMod = 0, bool debug = false)
    {
        int powMod = voxDepthPosMod[vox.depth];
        int3 dPosMod = new int3(xD * powMod, yD * powMod, zD * powMod);
        int3 fullPosCalc = vox.fullPos + dPosMod;

        return fullPosCalc;
    }

    //CheckIntersection finds density intersections for voxel corners.
    //Note: The increments number can be scaled up or down to effect performance and exactness of vertex position data.
    public static void CheckIntersection(int detail, float3 centerPos, int3 v0, int3 v1, Dir dir, HermiteData hermiteData, NativeHashMap<int, float> vertDensities, bool debug = false)
    {
        float d0 = GetVertexDensity(v0, centerPos, vertDensities, detail);
        float d1 = GetVertexDensity(v1, centerPos, vertDensities, detail);

        if (CheckDensityCross(d0, d1))
        {
            DensityFunction.DFType useDF = GetVertexDensityFunction(v0, centerPos);
            float3 v0Pos = GetVertexPosition(centerPos, v0.x, v0.y, v0.z);
            float3 v1Pos = GetVertexPosition(centerPos, v1.x, v1.y, v1.z);
            float3 vecDist = v1Pos - v0Pos;
            float3 pos = v0Pos;

            if (d0 == 0f)
                ;
            else if (d1 == 0f)
                pos = v1Pos;
            else
            {
                int increments = 5;
                float3 incVec = new float3();
                if (dir == Dir.x)
                    incVec.x = vecDist.x / increments;
                else if (dir == Dir.y)
                    incVec.y = vecDist.y / increments;
                else
                    incVec.z = vecDist.z / increments;
                float3 p0 = v0Pos, p1;
                float dc0 = d0, dc1;
                bool searchingCross = true;

                int escapeCount = 0;
                while (searchingCross)
                {
                    p1 = p0 + incVec;
                    dc1 = DensityFunction.GetDensity(useDF, p1, detail);
                    if (CheckDensityCross(dc0, dc1))
                    {
                        float lerp = Mathf.InverseLerp(dc0, dc1, 0f);
                        pos = math.lerp(p0, p1, lerp);
                        searchingCross = false;
                    }
                    else
                    {
                        p0 = p1;
                        dc0 = dc1;
                    }

                    ++escapeCount;
                    if (escapeCount > increments)
                    {
                        searchingCross = false;
                    }
                }
            }

            hermiteData.intersections.Add(pos);
            float3 normal = GetNormal(pos, useDF, detail);
            hermiteData.gradients.Add(normal);
        }
    }

    //CheckDensityCross does a simple density cross over check
    public static bool CheckDensityCross(float a, float b)
    {
        if (a > 0f && b > 0f)
            return false;
        if (a < 0f && b < 0f)
            return false;
        else
            return true;
    }

    //GetVertexPosition takes in a vertex from a voxel field and returns it's position in world space.
    public static float3 GetVertexPosition(float3 startPos, float x, float y, float z)
    {
        float3 usePos = new float3(startPos.x + x * smallestVoxSize.x, startPos.y + y * smallestVoxSize.y, startPos.z + z * smallestVoxSize.z);

        if (usePos.x > recipeData.recipeLoadedEndPos.x)
            usePos.x = recipeData.recipeLoadedEndPos.x;
        else if (usePos.x < recipeData.recipeLoadedStartPos.x)
            usePos.x = recipeData.recipeLoadedStartPos.x;

        if (usePos.y > recipeData.recipeLoadedEndPos.y)
            usePos.y = recipeData.recipeLoadedEndPos.y;
        else if (usePos.y < recipeData.recipeLoadedStartPos.y)
            usePos.y = recipeData.recipeLoadedStartPos.y;

        if (usePos.z > recipeData.recipeLoadedEndPos.z)
            usePos.z = recipeData.recipeLoadedEndPos.z;
        else if (usePos.z < recipeData.recipeLoadedStartPos.z)
            usePos.z = recipeData.recipeLoadedStartPos.z;

        return usePos;
    }

    //GetVertexDensity gets the density at a given vertex position.
    //This uses the HashMap vertDensities to enable faster speed by catching recently used vertex positions.
    //Only if the vertex is not catched does it compute and then store it to the hash map.
    public static float GetVertexDensity(int3 vertPos, float3 centerPos, NativeHashMap<int, float> vertDensities, int detail = 0, bool debug = false)
    {
        int vertInt = VertToInt(vertPos);

        if (vertDensities.ContainsKey(vertInt))
        {
            return vertDensities[vertInt];
        }
        else
        {
            float3 pos = GetVertexPosition(centerPos, vertPos.x, vertPos.y, vertPos.z);
            Vector3Int worldBlock = GetWorldBlock(pos);
            DensityFunction.DFType df = recipeData.recipeDataBlocks[worldBlock.x, worldBlock.y, worldBlock.z].DefaultDF;

            float density = DensityFunction.GetDensity(df, pos, detail, true);
            vertDensities.AsParallelWriter().TryAdd(vertInt, density);

            return density;
        }

        return 0f;
    }

    //GetNormal takes the normal by getting the density tangent numerically (through a small offset) and normalizing the resulting vector.
    public static float3 GetNormal(float3 pos, DensityFunction.DFType dFtype, int detail = 0)
    {
        float offSet = .01f;
        float3 xOffset = new float3(offSet, 0f, 0f);
        float3 yOffset = new float3(0f, offSet, 0f);
        float3 zOffset = new float3(0f, 0f, offSet);
        float xNormal = DensityFunction.GetDensity(dFtype, pos + xOffset, detail) - DensityFunction.GetDensity(dFtype, pos - xOffset, detail);
        float yNormal = DensityFunction.GetDensity(dFtype, pos + yOffset, detail) - DensityFunction.GetDensity(dFtype, pos - yOffset, detail);
        float zNormal = DensityFunction.GetDensity(dFtype, pos + zOffset, detail) - DensityFunction.GetDensity(dFtype, pos - zOffset, detail);

        float3 normal = math.normalize(new float3(xNormal, yNormal, zNormal));
        return normal;
    }

    //GetVertexDensityFunction gets density function from the recipe block which corresponds to that area.
    public static DensityFunction.DFType GetVertexDensityFunction(int3 vertPos, float3 centerPos)
    {
        float3 pos = GetVertexPosition(centerPos, vertPos.x, vertPos.y, vertPos.z);
        Vector3Int worldBlock = GetWorldBlock(pos);
        DensityFunction.DFType df = recipeData.recipeDataBlocks[worldBlock.x, worldBlock.y, worldBlock.z].DefaultDF;
        return df;
    }

    //GetWorldBlock determines which reciple block is covering the position given.
    public static Vector3Int GetWorldBlock(Vector3 pos)
    {
        int useX = -1;
        for (int x = 0; x < recipeData.recipeDataBlocks.GetLength(0); ++x)
        {
            Vector3 rDBPos = recipeData.recipeDataBlocks[x, 0, 0].posOffset;
            if (pos.x >= rDBPos.x && pos.x <= rDBPos.x + recipeBlockDecimalSize.x)
            {
                useX = x;
                break;
            }
        }
        int useY = -1;
        for (int y = 0; y < recipeData.recipeDataBlocks.GetLength(1); ++y)
        {
            Vector3 rDBPos = recipeData.recipeDataBlocks[0, y, 0].posOffset;
            if (pos.y >= rDBPos.y && pos.y <= rDBPos.y + recipeBlockDecimalSize.y)
            {
                useY = y;
                break;
            }
        }
        int useZ = -1;
        for (int z = 0; z < recipeData.recipeDataBlocks.GetLength(2); ++z)
        {
            Vector3 rDBPos = recipeData.recipeDataBlocks[0, 0, z].posOffset;
            if (pos.z >= rDBPos.z && pos.z <= rDBPos.z + recipeBlockDecimalSize.z)
            {
                useZ = z;
                break;
            }
        }

        if (useX == -1 || useY == -1 || useZ == -1)
        {
            Debug.Log("Something went wrong:" + pos + " not in offset");
            return new Vector3Int(-1, -1, -1);
        }

        return new Vector3Int(useX, useY, useZ);
    }

    //SchmitzVertexFromHermiteData is a numerical method of computing vertex positions in an effcient matter.
    public static float3 SchmitzVertexFromHermiteData(HermiteData data, float threshold)
    {
        int MAX_ITERATIONS = 50;
        threshold *= threshold;

        List<float3> xPoints = data.intersections;
        List<float3> grads = data.gradients;
        int pointsCount = xPoints.Count;

        if (pointsCount == 0)
        {
            return nullVector;
        }

        float3 c = new float3();

        for (int i = 0; i < pointsCount; i++)
        {
            c += xPoints[i];
        }
        c /= pointsCount;

        for (int i = 0; i < MAX_ITERATIONS; i++)
        {
            Vector3 force = new Vector3();

            for (int j = 0; j < pointsCount; j++)
            {
                float3 xPoint = xPoints[j];
                float3 xNormal = grads[j];

                force += (Vector3)xNormal * -1f * (Vector3.Dot(xNormal, c - xPoint));
            }

            float damping = 1f - (float)i / MAX_ITERATIONS;
            c += (float3)(force * damping / pointsCount);

            if (force.sqrMagnitude * force.sqrMagnitude < threshold)
                break;
        }

        return c;
    }

    //VertexClamp is used to clamp the vertex withingthe voxel boundary as there is a small chance it may be outside the voxel creating strange shapes.
    public static float3 VertexClamp(float3 v, DCVoxel vox)
    {
        float3 voxMin = vox.startPoint;
        float3 voxMax = vox.endPoint;

        if (v.x < voxMin.x) v.x = voxMin.x;
        if (v.y < voxMin.y) v.y = voxMin.y;
        if (v.z < voxMin.z) v.z = voxMin.z;
        if (v.x > voxMax.x) v.x = voxMax.x;
        if (v.y > voxMax.y) v.y = voxMax.y;
        if (v.z > voxMax.z) v.z = voxMax.z;

        return v;
    }

    //VertToInt is a simple hashing function meant to run quickly. It converts each x, y, and z to a different place within a single integer.
    public static int VertToInt(int3 vert)
    {
        return 1000000 * vert.x + 1000 * vert.y + 1 * vert.z;
    }


    //Create Mesh Data
    //CoreVoxMeshDataCreationJob generates takes the computed vertex mesh positions and converts them to mesh array data (vertices tirangles, ivs, and normals)
    //This is done by checking each voxel vertex edge crossing and connecting it adjacent voxel mesh positions to form a polygon which is stred in the mesh data arrays.
    //Radial Dual Contouring follows 21 possible orientations of the voxel in order to ascertain whther it should be included in the raidal topography.
    //Various forms of visual debugging features are also included here as many issues in the process are best solved here.
    public struct CoreVoxMeshDataCreationJob : IJob
    {
        public CoreVoxelDataMT voxData;
        public NativeHashMap<int, float> vertDensities;
        public NativeList<float3> verticies;
        public NativeList<int> triangles;
        public NativeList<float2> uvs;
        public NativeList<float3> normals;
        public bool debug;

        public void Execute()
        {
            if (debug)
            {
                if (GameObject.Find("InnerDebug") != null)
                {
                    DestroyImmediate(GameObject.Find("InnerDebug"));
                }
                innerVoxesGO = new GameObject("InnerDebug");
            }

            NativeArray<DCVoxel> coreVoxes = voxData.coreVoxels;
            DCVoxel vox;
            for (int voxCount = 0; voxCount < coreVoxes.Length; ++voxCount)
            {
                if (coreVoxes[voxCount].anyCross)
                {
                    vox = coreVoxes[voxCount];
                    int offSetMod = voxNum[worldDepthLimit] / PowFunction(2, vox.depth);

                    if (debug)
                    {
                        innerVoxGO = Instantiate(useDebugGOs.vertexTestCube, vox.startPoint, Quaternion.identity);
                        string _01cString = "|";
                        if (!vox.x01) _01cString += "x0|"; else _01cString += "x1|";
                        if (!vox.y01) _01cString += "y0|"; else _01cString += "y1|";
                        if (!vox.z01) _01cString += "z0|"; else _01cString += "z1|";
                        if (vox.xC) _01cString += "xC|"; if (vox.yC) _01cString += "yC|"; if (vox.zC) _01cString += "zC|";

                        innerVoxGO.transform.parent = innerVoxesGO.transform;
                        innerVoxGO.name = "Vox|" + vox.baseNum + vox.pos + "|Depth:" + vox.depth + "||VoxStart:" + vox.startPoint + "||" + _01cString;


                        verticies = new NativeList<float3>(10, Allocator.Temp);
                        triangles = new NativeList<int>(10, Allocator.Temp);
                        uvs = new NativeList<float2>(10, Allocator.Temp);
                        normals = new NativeList<float3>(10, Allocator.Temp);
                    }

                    int3 v000 = GetVertixFromVoxel(vox, 0, 0, 0);
                    int3 v100 = GetVertexFromOffset(v000, offSetMod, 0, 0);
                    int3 v010 = GetVertexFromOffset(v000, 0, offSetMod, 0);
                    int3 v001 = GetVertexFromOffset(v000, 0, 0, offSetMod);
                    int3 v110 = GetVertexFromOffset(v000, offSetMod, offSetMod, 0);
                    int3 v101 = GetVertexFromOffset(v000, offSetMod, 0, offSetMod);
                    int3 v011 = GetVertexFromOffset(v000, 0, offSetMod, offSetMod);
                    int3 v111 = GetVertexFromOffset(v000, offSetMod, offSetMod, offSetMod);

                    float v000Density = GetVertexDensity(v000, voxData.coreVoxStartPos, vertDensities, vox.detail);
                    float v001Density = GetVertexDensity(v001, voxData.coreVoxStartPos, vertDensities, vox.detail);
                    float v010Density = GetVertexDensity(v010, voxData.coreVoxStartPos, vertDensities, vox.detail);
                    float v100Density = GetVertexDensity(v100, voxData.coreVoxStartPos, vertDensities, vox.detail);
                    float v110Density = GetVertexDensity(v110, voxData.coreVoxStartPos, vertDensities, vox.detail);
                    float v101Density = GetVertexDensity(v101, voxData.coreVoxStartPos, vertDensities, vox.detail);
                    float v011Density = GetVertexDensity(v011, voxData.coreVoxStartPos, vertDensities, vox.detail);
                    float v111Density = GetVertexDensity(v111, voxData.coreVoxStartPos, vertDensities, vox.detail);

                    bool thoroughCheck = true;

                    if (debug)
                    {
                        GameObject vertdense = new GameObject();
                        vertdense.transform.parent = innerVoxGO.transform;
                        vertdense.name = "VertexDensities";

                        GameObject v000go = Instantiate(useDebugGOs.vertexTestCube, GetVertexPosition(voxData.coreVoxStartPos, v000.x, v000.y, v000.z), Quaternion.identity);
                        v000go.transform.parent = vertdense.transform;
                        v000go.name = "v000:" + v000Density + "|" + v000 + "|Pos" + GetVertexPosition(voxData.coreVoxStartPos, v000.x, v000.y, v000.z);
                        v000go.transform.localScale = new Vector3(.2f, .2f, .2f);

                        GameObject v001go = Instantiate(useDebugGOs.vertexTestCube, GetVertexPosition(voxData.coreVoxStartPos, v001.x, v001.y, v001.z), Quaternion.identity);
                        v001go.transform.parent = vertdense.transform;
                        v001go.name = "v001:" + v001Density + "|" + v001 + "|Pos" + GetVertexPosition(voxData.coreVoxStartPos, v001.x, v001.y, v001.z);
                        v001go.transform.localScale = new Vector3(.2f, .2f, .2f);

                        GameObject v010go = Instantiate(useDebugGOs.vertexTestCube, GetVertexPosition(voxData.coreVoxStartPos, v010.x, v010.y, v010.z), Quaternion.identity);
                        v010go.transform.parent = vertdense.transform;
                        v010go.name = "v010:" + v010Density + "|" + v010 + "|Pos" + GetVertexPosition(voxData.coreVoxStartPos, v010.x, v010.y, v010.z);
                        v010go.transform.localScale = new Vector3(.2f, .2f, .2f);

                        GameObject v100go = Instantiate(useDebugGOs.vertexTestCube, GetVertexPosition(voxData.coreVoxStartPos, v100.x, v100.y, v100.z), Quaternion.identity);
                        v100go.transform.parent = vertdense.transform;
                        v100go.name = "v100:" + v100Density + "|" + v100 + "|Pos" + GetVertexPosition(voxData.coreVoxStartPos, v100.x, v100.y, v100.z);
                        v100go.transform.localScale = new Vector3(.2f, .2f, .2f);

                        GameObject v110go = Instantiate(useDebugGOs.vertexTestCube, GetVertexPosition(voxData.coreVoxStartPos, v110.x, v110.y, v110.z), Quaternion.identity);
                        v110go.transform.parent = vertdense.transform;
                        v110go.name = "v110:" + v110Density + "|" + v110 + "|Pos" + GetVertexPosition(voxData.coreVoxStartPos, v110.x, v110.y, v110.z);
                        v110go.transform.localScale = new Vector3(.2f, .2f, .2f);

                        GameObject v101go = Instantiate(useDebugGOs.vertexTestCube, GetVertexPosition(voxData.coreVoxStartPos, v101.x, v101.y, v101.z), Quaternion.identity);
                        v101go.transform.parent = vertdense.transform;
                        v101go.name = "v101:" + v101Density + "|" + v101 + "|Pos" + GetVertexPosition(voxData.coreVoxStartPos, v101.x, v101.y, v101.z);
                        v101go.transform.localScale = new Vector3(.2f, .2f, .2f);

                        GameObject v011go = Instantiate(useDebugGOs.vertexTestCube, GetVertexPosition(voxData.coreVoxStartPos, v011.x, v011.y, v011.z), Quaternion.identity);
                        v011go.transform.parent = vertdense.transform;
                        v011go.name = "v011:" + v011Density + "|" + v011 + "|Pos" + GetVertexPosition(voxData.coreVoxStartPos, v011.x, v011.y, v011.z);
                        v011go.transform.localScale = new Vector3(.2f, .2f, .2f);

                        GameObject v111go = Instantiate(useDebugGOs.vertexTestCube, GetVertexPosition(voxData.coreVoxStartPos, v111.x, v111.y, v111.z), Quaternion.identity);
                        v111go.transform.parent = vertdense.transform;
                        v111go.name = "v111:" + v111Density + "|" + v111 + "|Pos" + GetVertexPosition(voxData.coreVoxStartPos, v111.x, v111.y, v111.z);
                        v111go.transform.localScale = new Vector3(.2f, .2f, .2f);
                    }

                    //X-Crosses
                    //y0z0:X-Cross:000->100
                    if (vox.y01 == false && vox.z01 == false && CheckDensityCross(v000Density, v100Density))
                    {
                        DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, 0, -1);
                        DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, 0, -1, 0);
                        DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, 0, -1, -1);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = innerVoxGO.transform;
                            dp.name = "|XCross|000->100|" + "Voxes";

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.startPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.startPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.startPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.startPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                            dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                        }

                        if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                        {
                            if (v100Density < 0f)
                                AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //y0z1:X-Cross:001->101
                    if (vox.y01 == false && vox.z01 == true && CheckDensityCross(v001Density, v101Density))
                    {
                        DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, -1, 0);
                        DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, 0, 0, offSetMod);
                        DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, 0, -1, offSetMod);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = innerVoxGO.transform;
                            dp.name = "|XCross|001->101|" + "Voxes";

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                            dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                        }

                        if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                        {
                            if (v101Density < 0f)
                                AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //y1z0:X-Cross:010->110
                    if (vox.y01 == true && vox.z01 == false && CheckDensityCross(v010Density, v110Density))
                    {
                        DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, offSetMod, 0);
                        DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, 0, 0, -1);
                        DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, 0, offSetMod, -1);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = innerVoxGO.transform;
                            dp.name = "|XCross|010->110|" + "Voxes";

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                            dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                        }

                        if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                        {
                            if (v110Density < 0f)
                                AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //y1z1:X-Cross:011->111
                    if (vox.y01 == true && vox.z01 == true && CheckDensityCross(v011Density, v111Density))
                    {
                        DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, offSetMod, 0);
                        DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, 0, 0, offSetMod);
                        DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, 0, offSetMod, offSetMod);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = innerVoxGO.transform;
                            dp.name = "|XCross|011->111|" + "Voxes";

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.meshVertex, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.meshVertex, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.meshVertex, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.meshVertex, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                            dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                        }

                        if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                        {
                            if (v111Density < 0f)
                                AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                        }
                    }

                    //Y-Crosses
                    //x0z0:Y-Cross:000->010
                    if (vox.x01 == false && vox.z01 == false && CheckDensityCross(v000Density, v010Density))
                    {
                        DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, 0, -1);
                        DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, -1, 0, 0);
                        DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, -1, 0, -1);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();

                            dp.transform.parent = innerVoxGO.transform;
                            dp.transform.localPosition = new Vector3(0, 0, 0);
                            dp.name = "|YCross|000->010|" + "Voxes";

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox.meshVertex + "|BPos" + vox.baseNum + "|Pos:" + vox.pos;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex + "|BPos" + vox1.baseNum + "|Pos:" + vox1.pos;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex + "|BPos" + vox2.baseNum + "|Pos:" + vox2.pos;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex + "|BPos" + vox3.baseNum + "|Pos:" + vox3.pos;

                            dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                        }

                        if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                        {
                            if (v010Density < 0f)
                                AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //x0z1:Y-Cross:001->011
                    if (vox.x01 == false && vox.z01 == true && CheckDensityCross(v001Density, v011Density))
                    {
                        DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, 0, offSetMod);
                        DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, -1, 0, 0);
                        DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, -1, 0, offSetMod);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = innerVoxGO.transform;
                            dp.name = "|YCross|001->011|" + "Voxes";

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.meshVertex, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.meshVertex, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.meshVertex, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.meshVertex, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                            dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                        }

                        if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                        {
                            if (v011Density < 0f)
                                AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //x1z0:Y-Cross:100->110
                    if (vox.x01 == true && vox.z01 == false && CheckDensityCross(v100Density, v110Density))
                    {
                        DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, 0, -1);
                        DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, offSetMod, 0, 0);
                        DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, offSetMod, 0, -1);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = innerVoxGO.transform;
                            dp.name = "|YCross|100->110|" + "Voxes";

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.meshVertex, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.meshVertex, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.meshVertex, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.meshVertex, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                            dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                        }

                        if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                        {
                            if (v110Density < 0f)
                                AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //x1z1:Y-Cross:101->111
                    if (vox.x01 == true && vox.z01 == true && CheckDensityCross(v101Density, v111Density))
                    {
                        DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, 0, offSetMod);
                        DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, offSetMod, 0, 0);
                        DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, offSetMod, 0, offSetMod);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = innerVoxGO.transform;
                            dp.name = "|YCross|101->111|" + "Voxes";

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                            dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                        }

                        if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                        {
                            if (v111Density < 0f)
                                AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                        }
                    }

                    //Z-Crosses
                    //x0y0:Z-Cross:000->001
                    if (vox.x01 == false && vox.y01 == false && CheckDensityCross(v000Density, v001Density))
                    {
                        DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, -1, 0);
                        DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, -1, 0, 0);
                        DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, -1, -1, 0);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = innerVoxGO.transform;
                            dp.name = "|ZCross|000->001|" + "Voxes";

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.startPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.startPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.startPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.startPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                            dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                        }

                        if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                        {
                            if (v001Density < 0f)
                                AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //x0y1:Z-Cross:010->011
                    if (vox.x01 == false && vox.y01 == true && CheckDensityCross(v010Density, v011Density))
                    {
                        DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, offSetMod, 0);
                        DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, -1, 0, 0);
                        DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, -1, offSetMod, 0);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = innerVoxGO.transform;
                            dp.name = "|ZCross|010->011|" + "Voxes";

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.startPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.startPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.startPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.startPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                            dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                        }

                        if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                        {
                            if (v011Density < 0f)
                                AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //x1y0:Z-Cross:100->101
                    if (vox.x01 == true && vox.y01 == false && CheckDensityCross(v100Density, v101Density))
                    {
                        DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, -1, 0);
                        DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, offSetMod, 0, 0);
                        DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, offSetMod, -1, 0);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = innerVoxGO.transform;
                            dp.name = "|ZCross|100->101|" + "Voxes";

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.startPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.startPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.startPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.startPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                            dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                        }

                        if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                        {
                            if (v101Density < 0f)
                                AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //x1y1:Z-Cross:110->111
                    if (vox.x01 == true && vox.y01 == true && CheckDensityCross(v110Density, v111Density))
                    {
                        DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, offSetMod, 0);
                        DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, offSetMod, 0, 0);
                        DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, offSetMod, offSetMod, 0);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = innerVoxGO.transform;
                            dp.name = "|ZCross|110->111|" + "Voxes";

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.meshVertex, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.meshVertex, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.meshVertex, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.meshVertex, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                            dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                        }

                        if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                        {
                            if (v111Density < 0f)
                                AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }

                    //X-Center
                    if (vox.xC == true)
                    {
                        //xCy0:Z-Cross:000->001
                        if (vox.y01 == false && CheckDensityCross(v000Density, v001Density))
                        {
                            DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, -1, 0);
                            DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, -1, 0, 0);
                            DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, -1, -1, 0);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = innerVoxGO.transform;
                                dp.name = "|X-Center|ZCross|000->001|" + "Voxes";

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                                dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                            }

                            if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                            {
                                if (v001Density < 0f)
                                    AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //xCy1:Z-Cross:010->011
                        if (vox.y01 == true && CheckDensityCross(v010Density, v011Density))
                        {
                            DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, offSetMod, 0);
                            DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, -1, 0, 0);
                            DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, -1, offSetMod, 0);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = innerVoxGO.transform;
                                dp.name = "|X-Center|ZCross|010->011|" + "Voxes";

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                                dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                            }

                            if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                            {
                                if (v011Density < 0f)
                                    AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //xCz0:Y-Cross:000->010
                        if (vox.z01 == false && CheckDensityCross(v000Density, v010Density))
                        {
                            DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, 0, -1);
                            DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, -1, 0, 0);
                            DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, -1, 0, -1);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = innerVoxGO.transform;
                                dp.name = "|X-Center|YCross|000->010|" + "Voxes";

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.meshVertex, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.meshVertex, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.meshVertex, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.meshVertex, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                                dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                            }

                            if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                            {
                                if (v010Density < 0f)
                                    AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //xCz1:Y-Cross:001->011
                        if (vox.z01 == true && CheckDensityCross(v001Density, v011Density))
                        {
                            DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, 0, offSetMod);
                            DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, -1, 0, 0);
                            DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, -1, 0, offSetMod);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = innerVoxGO.transform;
                                dp.name = "|X-Center|YCross|001->011|" + "Voxes";

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                                dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                            }

                            if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                            {
                                if (v011Density < 0f)
                                    AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                    }

                    //Y-Center
                    if (vox.yC == true)
                    {
                        //yCx0:Z-Cross:000->001
                        if (vox.x01 == false && CheckDensityCross(v000Density, v001Density))
                        {
                            DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, -1, 0);
                            DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, -1, 0, 0);
                            DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, -1, -1, 0);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = innerVoxGO.transform;
                                dp.name = "|Y-Center|ZCross|000->001|" + "Voxes";

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                                dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                            }

                            if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                            {
                                if (v001Density < 0f)
                                    AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //yCx1:Z-Cross:100->101
                        if (vox.x01 == true && CheckDensityCross(v100Density, v101Density))
                        {
                            DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, -1, 0);
                            DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, offSetMod, 0, 0);
                            DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, offSetMod, -1, 0);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = innerVoxGO.transform;
                                dp.name = "|Y-Center|ZCross|100->101|" + "Voxes";

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                                dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                            }

                            if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                            {
                                if (v101Density < 0f)
                                    AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //yCz0:X-Cross:000->100
                        if (vox.z01 == false && CheckDensityCross(v000Density, v100Density))
                        {
                            DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, 0, -1);
                            DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, 0, -1, 0);
                            DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, 0, -1, -1);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = innerVoxGO.transform;
                                dp.name = "|Y-Center|XCross|000->100|" + "Voxes";

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                                dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                            }

                            if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                            {
                                if (v100Density < 0f)
                                    AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //yCz1:X-Cross:001->101
                        if (vox.z01 == true && CheckDensityCross(v001Density, v101Density))
                        {
                            DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, 0, offSetMod);
                            DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, 0, -1, 0);
                            DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, 0, -1, offSetMod);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = innerVoxGO.transform;
                                dp.name = "|Y-Center|YCross|000->101|" + "Voxes";

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                                dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                            }

                            if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                            {
                                if (v101Density < 0f)
                                    AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                    }

                    //Z-Center
                    if (vox.zC == true)
                    {
                        //zCx0:Y-Cross:000->010
                        if (vox.x01 == false && CheckDensityCross(v000Density, v010Density))
                        {
                            DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, 0, -1);
                            DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, -1, 0, 0);
                            DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, -1, 0, -1);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = innerVoxGO.transform;
                                dp.name = "|Z-Center|YCross|000->010|" + "Voxes";

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.meshVertex, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.meshVertex, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.meshVertex, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.meshVertex, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                                dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                            }

                            if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                            {
                                if (v010Density < 0f)
                                    AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //zCx1:Y-Cross:100->110
                        if (vox.x01 == true && CheckDensityCross(v100Density, v110Density))
                        {
                            DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, 0, -1);
                            DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, offSetMod, 0, 0);
                            DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, offSetMod, 0, -1);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = innerVoxGO.transform;
                                dp.name = "|X-Center|YCross|100->110|" + "Voxes";

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                                dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                            }

                            if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                            {
                                if (v110Density < 0f)
                                    AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //zCy0:X-Cross:000->100
                        if (vox.y01 == false && CheckDensityCross(v000Density, v100Density))
                        {
                            DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, 0, -1);
                            DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, 0, -1, 0);
                            DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, 0, -1, -1);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = innerVoxGO.transform;
                                dp.name = "|Z-Center|XCross|000->100|" + "Voxes";

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                                dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                            }

                            if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                            {
                                if (v100Density < 0f)
                                    AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //zCy1:X-Cross:010->110
                        if (vox.y01 == true && CheckDensityCross(v010Density, v110Density))
                        {
                            DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, 0, -1);
                            DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, 0, offSetMod, 0);
                            DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, 0, offSetMod, -1);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = innerVoxGO.transform;
                                dp.name = "|Z-Center|XCross|010->110|" + "Voxes";

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                                dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                            }

                            if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                            {
                                if (v110Density < 0f)
                                    AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                    }

                    // Double Centers
                    //X-Center + Y-Center 
                    //xCyC:Z-Cross:000->001
                    if (vox.xC == true && vox.yC == true && CheckDensityCross(v000Density, v001Density))
                    {
                        DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, -1, 0);
                        DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, -1, 0, 0);
                        DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, -1, -1, 0);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = innerVoxGO.transform;
                            dp.name = "|X-Center + Y-Center|ZCross|000->001|" + "Voxes";

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.startPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.startPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.startPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.startPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                            dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                        }

                        if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                        {
                            if (v001Density < 0f)
                                AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //X-Center + Z-Center
                    //xCzC:Y-Cross:000->010
                    if (vox.xC == true && vox.zC == true && CheckDensityCross(v000Density, v010Density))
                    {
                        DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, 0, -1);
                        DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, -1, 0, 0);
                        DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, -1, 0, -1);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = innerVoxGO.transform;
                            dp.name = "|X-Center + Z-Center|ZCross|000->010|" + "Voxes";

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.startPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.startPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.startPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.startPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                            dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                        }

                        if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                        {
                            if (v010Density < 0f)
                                AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //Y-Center + Z-Center
                    //yCzC:X-Cross:000->100
                    if (vox.yC == true && vox.zC == true && CheckDensityCross(v000Density, v100Density))
                    {
                        DCVoxel vox1 = GetVoxelFromVertexPos(voxData, v000, 0, 0, -1);
                        DCVoxel vox2 = GetVoxelFromVertexPos(voxData, v000, 0, -1, 0);
                        DCVoxel vox3 = GetVoxelFromVertexPos(voxData, v000, 0, -1, -1);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                vox1 = SetVoxelMeshPositionThorough(vox1, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                vox2 = SetVoxelMeshPositionThorough(vox2, voxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                vox3 = SetVoxelMeshPositionThorough(vox3, voxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = innerVoxGO.transform;
                            dp.name = "|Y-Center + Z-Center|XCross|000->100|" + "Voxes";

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox.startPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.startPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.startPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.startPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;

                            dp.transform.localScale = new Vector3(.2f, .2f, .2f);
                        }

                        if (vox.anyCross && vox1.anyCross && vox2.anyCross && vox3.anyCross)
                        {
                            if (v100Density < 0f)
                                AddQuadFromVox(vox, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromVox(vox, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }

                    if (debug)
                    {
                        CreateMesh(verticies, triangles, uvs, normals, 1, innerVoxGO);
                    }
                }
            }
        }
    }

    //GetVertexFromOffset is a simple fuctions to compute a vertex from a given offset.
    public static int3 GetVertexFromOffset(int3 vert, int xOff, int yOff, int zOff)
    {
        int useX = vert.x + xOff;
        int useY = vert.y + yOff;
        int useZ = vert.z + zOff;
        return new int3(useX, useY, useZ);
    }

    //GetVoxelFromVertexPos finds the voxel associated with a given vertex position and offset.
    //Because the core voxels are broken into virtual octrees, the function must move through the highest level via the child array indicies to the lowest detail voxel assocaited to that vertex.
    public static DCVoxel GetVoxelFromVertexPos(CoreVoxelDataMT voxData, int3 vertex, int dX, int dY, int dZ, bool debug = false)
    {
        int checkX = vertex.x + dX;
        int checkY = vertex.y + dY;
        int checkZ = vertex.z + dZ;
        if (debug) Debug.Log("checkX:" + checkX + ", checkY:" + checkY + ", checkZ:" + checkZ);

        int xWP = checkX / voxNum[worldDepthLimit];
        int yWP = checkY / voxNum[worldDepthLimit];
        int zWP = checkZ / voxNum[worldDepthLimit];

        if (xWP < 0) xWP = 0;
        if (yWP < 0) yWP = 0;
        if (zWP < 0) zWP = 0;

        int xIndex = xWP * (coreVoxSize.x + 1) * (coreVoxSize.x + 1);
        int yIndex = yWP * (coreVoxSize.y + 1);
        int zIndex = zWP;

        DCVoxel foundVox = voxData.coreVoxels[xIndex + yIndex + zIndex];
        int useDepth = foundVox.innerDepth;

        if (useDepth == 0)
        {
            if (debug) Debug.Log("Inner Depth = 0");
            return foundVox;
        }
        else
        {
            int useWorldDepth = worldDepthLimit;
            int xPos = 0;
            int yPos = 0;
            int zPos = 0;
            int xMod = checkX % voxNum[useWorldDepth];
            int yMod = checkY % voxNum[useWorldDepth];
            int zMod = checkZ % voxNum[useWorldDepth];
            while (useDepth > 0)
            {
                --useWorldDepth;
                --useDepth;

                int newBase = foundVox.vNode000;
                xPos = xMod / voxNum[useWorldDepth];
                yPos = yMod / voxNum[useWorldDepth];
                zPos = zMod / voxNum[useWorldDepth];
                xMod = xMod % voxNum[useWorldDepth];
                yMod = yMod % voxNum[useWorldDepth];
                zMod = zMod % voxNum[useWorldDepth];
                int3 modIndex = new int3(xPos * 2 * 2, yPos * 2, zPos);

                foundVox = voxData.coreVoxels[newBase + modIndex.x + modIndex.y + modIndex.z];
            }

            return foundVox;
        }

        return new DCVoxel();
    }

    //SetVoxelMeshPositionThorough is the same as SetVoxelMeshPosition, but is meant to ensure a mesh position is found.
    //This is useful in cases where we have a given vertex position to create a polygon from, but due to possibly slipping through certain details, it lacks neghboring verticies.
    //This is done by using more thorough (though somewhat slower) intersection checks, and if all else fails creating a vertex position in the center of the voxel.
    //Using the thorough check only when we know we have to find a vertex position is useful, but not otherwise, is done to increase effciency, letting the faster less effcient methods be used until the less effcient methods are required.
    public static DCVoxel SetVoxelMeshPositionThorough(DCVoxel vox, float3 blockCenterPos, NativeHashMap<int, float> vertDensities)
    {
        HermiteData hermiteData = new HermiteData();
        hermiteData.intersections = new List<float3>();
        hermiteData.gradients = new List<float3>();

        int3 v000 = GetVertixFromVoxel(vox, 0, 0, 0);
        int3 v001 = GetVertixFromVoxel(vox, 0, 0, 1);
        int3 v010 = GetVertixFromVoxel(vox, 0, 1, 0);
        int3 v011 = GetVertixFromVoxel(vox, 0, 1, 1);
        int3 v100 = GetVertixFromVoxel(vox, 1, 0, 0);
        int3 v101 = GetVertixFromVoxel(vox, 1, 0, 1);
        int3 v110 = GetVertixFromVoxel(vox, 1, 1, 0);
        int3 v111 = GetVertixFromVoxel(vox, 1, 1, 1);

        CheckIntersectionThorough(vox, blockCenterPos, v000, v100, Dir.x, hermiteData, vertDensities); //p000p100
        CheckIntersectionThorough(vox, blockCenterPos, v000, v010, Dir.y, hermiteData, vertDensities); //p000p010
        CheckIntersectionThorough(vox, blockCenterPos, v000, v001, Dir.z, hermiteData, vertDensities); //p000p001
        CheckIntersectionThorough(vox, blockCenterPos, v100, v110, Dir.y, hermiteData, vertDensities); //p100p110
        CheckIntersectionThorough(vox, blockCenterPos, v100, v101, Dir.z, hermiteData, vertDensities); //p100p101
        CheckIntersectionThorough(vox, blockCenterPos, v010, v110, Dir.x, hermiteData, vertDensities); //p010p110
        CheckIntersectionThorough(vox, blockCenterPos, v010, v011, Dir.z, hermiteData, vertDensities); //p010p011
        CheckIntersectionThorough(vox, blockCenterPos, v001, v101, Dir.x, hermiteData, vertDensities); //p001p101
        CheckIntersectionThorough(vox, blockCenterPos, v001, v011, Dir.y, hermiteData, vertDensities); //p001p011
        CheckIntersectionThorough(vox, blockCenterPos, v110, v111, Dir.z, hermiteData, vertDensities); //p110p111
        CheckIntersectionThorough(vox, blockCenterPos, v011, v111, Dir.x, hermiteData, vertDensities); //p011p111
        CheckIntersectionThorough(vox, blockCenterPos, v101, v111, Dir.y, hermiteData, vertDensities); //p101p111

        DCVoxel newVox = vox;
        if (hermiteData.intersections.Count > 0)
        {
            newVox.anyCross = true;
            newVox.meshVertex = SchmitzVertexFromHermiteData(hermiteData, .001f);

            DensityFunction.DFType useDF = GetVertexDensityFunction(v000, blockCenterPos);
            newVox.meshVertex = VertexClamp(newVox.meshVertex, newVox);
            newVox.meshNormal = GetNormal(newVox.meshVertex, useDF, newVox.detail);
        }
        else
        {
            newVox.anyCross = true;
            newVox.meshVertex = newVox.centerPoint;
            DensityFunction.DFType useDF = GetVertexDensityFunction(v000, blockCenterPos);
            newVox.meshNormal = GetNormal(newVox.meshVertex, useDF, newVox.detail);
        }

        return newVox;
    }

    //CheckIntersectionThorough is different from the standard CheckInteresction in that increases the increments to attempt to find any intersection cross that may have not been found originally.
    public static void CheckIntersectionThorough(DCVoxel vox, float3 centerPos, int3 v0, int3 v1, Dir dir, HermiteData hermiteData, NativeHashMap<int, float> vertDensities)
    {
        float d0 = GetVertexDensity(v0, centerPos, vertDensities, vox.detail);
        float d1 = GetVertexDensity(v1, centerPos, vertDensities, vox.detail);
        bool foundIntersection = false;

        DensityFunction.DFType useDF = GetVertexDensityFunction(v0, centerPos);
        Vector3 v0Pos = GetVertexPosition(centerPos, v0.x, v0.y, v0.z);
        Vector3 v1Pos = GetVertexPosition(centerPos, v1.x, v1.y, v1.z);
        Vector3 vecDist = v1Pos - v0Pos;

        Vector3 pos = v0Pos;

        if (d0 == 0f)
            ;
        else if (d1 == 0f)
            pos = v1Pos;
        else
        {
            int increments = 10;

            Vector3 incVec = new Vector3();
            if (dir == Dir.x)
                incVec.x = vecDist.x / increments;
            else if (dir == Dir.y)
                incVec.y = vecDist.y / increments;
            else
                incVec.z = vecDist.z / increments;

            Vector3 p0 = v0Pos, p1;
            float dc0 = d0, dc1;
            bool searchingCross = true;

            int escapeCount = 0;
            while (searchingCross)
            {
                p1 = p0 + incVec;
                dc1 = DensityFunction.GetDensity(useDF, p1, vox.detail);
                if (CheckDensityCross(dc0, dc1))
                {
                    float lerp = Mathf.InverseLerp(dc0, dc1, 0f);
                    foundIntersection = true;
                    pos = Vector3.Lerp(p0, p1, lerp);
                    searchingCross = false;
                }
                else
                {
                    p0 = p1;
                    dc0 = dc1;
                }
                ++escapeCount;
                if (escapeCount > increments)
                {
                    searchingCross = false;
                }
            }

        }

        if (foundIntersection)
        {
            hermiteData.intersections.Add(pos);
            float3 normal = GetNormal(pos, useDF, vox.detail);
            hermiteData.gradients.Add(normal);
        }
    }

    //AddQuadFromVox takes voxel data given to create a mesh quad or traingle.
    //Since it's possible some cases which include voxel size transitions will be traingles instead of quads, each triangle case is checked before a quad is created.
    //A final check is determined to be sure that is not a degnerate quad by the distance between b/c and a/d and using the alternative quad creation method if appropriate.
    public static void AddQuadFromVox(DCVoxel a, DCVoxel b, DCVoxel c, DCVoxel d, NativeList<float3> verticies, NativeList<int> triangles, NativeList<float2> uvs, NativeList<float3> normals)
    {
        if (math.all(a.meshVertex != nullVector) && math.all(b.meshVertex != nullVector) && math.all(c.meshVertex != nullVector) && math.all(d.meshVertex != nullVector))
        {
            if (math.all(a.meshVertex == b.meshVertex))
                AddTriangle(a.meshVertex, d.meshVertex, c.meshVertex, a.meshNormal, d.meshNormal, c.meshNormal, verticies, triangles, uvs, normals);
            else if (math.all(a.meshVertex == c.meshVertex))
                AddTriangle(a.meshVertex, b.meshVertex, d.meshVertex, a.meshNormal, b.meshNormal, d.meshNormal, verticies, triangles, uvs, normals);
            else if (math.all(a.meshVertex == d.meshVertex))
                AddTriangle(a.meshVertex, b.meshVertex, c.meshVertex, a.meshNormal, b.meshNormal, c.meshNormal, verticies, triangles, uvs, normals);
            else if (math.all(b.meshVertex == c.meshVertex))
                AddTriangle(a.meshVertex, b.meshVertex, d.meshVertex, a.meshNormal, b.meshNormal, d.meshNormal, verticies, triangles, uvs, normals);
            else if (math.all(b.meshVertex == d.meshVertex))
                AddTriangle(a.meshVertex, b.meshVertex, c.meshVertex, a.meshNormal, b.meshNormal, c.meshNormal, verticies, triangles, uvs, normals);
            else if (math.all(c.meshVertex == d.meshVertex))
                AddTriangle(a.meshVertex, b.meshVertex, c.meshVertex, a.meshNormal, b.meshNormal, c.meshNormal, verticies, triangles, uvs, normals);
            else
            {
                if (math.distance(b.meshVertex, c.meshVertex) < math.distance(a.meshVertex, d.meshVertex))
                    AddQuad(a.meshVertex, b.meshVertex, c.meshVertex, d.meshVertex, a.meshNormal, b.meshNormal, c.meshNormal, d.meshNormal, verticies, triangles, uvs, normals);
                else
                    AddQuadAlt(a.meshVertex, b.meshVertex, c.meshVertex, d.meshVertex, a.meshNormal, b.meshNormal, c.meshNormal, d.meshNormal, verticies, triangles, uvs, normals);
            }
        }
    }

    //AddTriangle adds relevant mesh traingle to the mesh generation arrays.
    public static void AddTriangle(float3 a, float3 b, float3 c, float3 aN, float3 bN, float3 cN, NativeList<float3> verticies, NativeList<int> triangles, NativeList<float2> uvs, NativeList<float3> normals)
    {
        int aIndex = verticies.Length;
        verticies.Add(a);
        normals.Add(aN);
        uvs.Add(new Vector2(0, 0));
        int bIndex = verticies.Length;
        verticies.Add(b);
        normals.Add(bN);
        uvs.Add(new Vector2(1, 0));
        int cIndex = verticies.Length;
        verticies.Add(c);
        normals.Add(cN);
        uvs.Add(new Vector2(0, 1));

        triangles.Add(aIndex);
        triangles.Add(cIndex);
        triangles.Add(bIndex);
    }

    //AddQuad adds relevant mesh quad to the mesh generation arrays.
    public static void AddQuad(float3 a, float3 b, float3 c, float3 d, float3 aN, float3 bN, float3 cN, float3 dN, NativeList<float3> verticies, NativeList<int> triangles, NativeList<float2> uvs, NativeList<float3> normals)
    {
        int aIndex = verticies.Length;
        verticies.Add(a);
        normals.Add(aN);
        uvs.Add(new Vector2(0, 0));
        int bIndex = verticies.Length;
        verticies.Add(b);
        normals.Add(bN);
        uvs.Add(new Vector2(1, 0));
        int cIndex = verticies.Length;
        verticies.Add(c);
        normals.Add(cN);
        uvs.Add(new Vector2(0, 1));
        int dIndex = verticies.Length;
        verticies.Add(d);
        normals.Add(dN);
        uvs.Add(new Vector2(1, 1));

        triangles.Add(aIndex);
        triangles.Add(cIndex);
        triangles.Add(bIndex);
        triangles.Add(bIndex);
        triangles.Add(cIndex);
        triangles.Add(dIndex);
    }

    //AddQuadAlt adds relevant mesh quad to the mesh generation arrays.
    //This differs from AddQuad as it uses the alternative dacdba to avoid degenerate traingles.
    public static void AddQuadAlt(float3 a, float3 b, float3 c, float3 d, float3 aN, float3 bN, float3 cN, float3 dN, NativeList<float3> verticies, NativeList<int> triangles, NativeList<float2> uvs, NativeList<float3> normals)
    {
        int cIndex = verticies.Length;
        verticies.Add(c);
        normals.Add(cN);
        uvs.Add(new Vector2(0, 0));
        int dIndex = verticies.Length;
        verticies.Add(d);
        normals.Add(dN);
        uvs.Add(new Vector2(1, 0));
        int aIndex = verticies.Length;
        verticies.Add(a);
        normals.Add(aN);
        uvs.Add(new Vector2(0, 1));
        int bIndex = verticies.Length;
        verticies.Add(b);
        normals.Add(bN);
        uvs.Add(new Vector2(1, 1));

        triangles.Add(dIndex);
        triangles.Add(aIndex);
        triangles.Add(cIndex);
        triangles.Add(dIndex);
        triangles.Add(bIndex);
        triangles.Add(aIndex);
    }


    //Outer Voxels
    //CreateOuterVoxelsJob creates a set of OuterVoxels ]. The for loops here are set to create a single outer voxel shell around the core voxels.
    //Note: the first layer has a size of 1 core voxel and uses the vertex data associated with that voxel (because the core voxels all have perimter voxels not used to create outward mesh quads)
    //After the first layer, a new voxel layer is created at set size in which is the mesh vertex position is computed at a later step.
    public struct CreateOuterVoxelsJob : IJob
    {
        public CoreVoxelDataMT coreVoxelData;
        public NativeArray<DCSVoxel> outerVoxes;
        public int xBorderP;
        public int xBorderN;
        public int yBorderP;
        public int yBorderN;
        public int zBorderP;
        public int zBorderN;
        public int offset;
        public bool debug;

        public void Execute()
        {
            int outerVoxCounter = 0;
            for (int xW = xBorderN; xW <= xBorderP; xW += offset)
            {
                for (int yW = yBorderN; yW <= yBorderP; yW += offset)
                {
                    for (int zW = zBorderN; zW <= zBorderP; zW += offset)
                    {
                        if ((xW == xBorderP || xW == xBorderN) && (yW <= yBorderP && yW >= yBorderN) && (zW <= zBorderP && zW >= zBorderN)
                            || (yW == yBorderP || yW == yBorderN) && (xW <= xBorderP && xW >= xBorderN) && (zW <= zBorderP && zW >= zBorderN)
                            || (zW == zBorderP || zW == zBorderN) && (xW <= xBorderP && xW >= xBorderN) && (yW <= yBorderP && yW >= yBorderN))
                        {
                            DCVoxel useVoxel = GetVoxelFromOffset(coreVoxelData, xW, yW, zW, 0, 0, 0);

                            DCSVoxel sVoxel = new DCSVoxel();
                            sVoxel.baseVox = useVoxel.index;
                            sVoxel.vert = GetVertixFromVoxel(useVoxel, 0, 0, 0, 0, true);
                            sVoxel.offset = offset;
                            sVoxel.meshVertex = nullVector;

                            sVoxel.startPoint = useVoxel.startPoint;
                            sVoxel.centerPoint = sVoxel.startPoint + new float3(offset * smallestVoxSize.x * 4, offset * smallestVoxSize.y * 4, offset * smallestVoxSize.z * 4);
                            sVoxel.endPoint = sVoxel.startPoint + new float3(offset * smallestVoxSize.x * 8, offset * smallestVoxSize.y * 8, offset * smallestVoxSize.z * 8);
                            sVoxel.xOffset = offset;
                            sVoxel.yOffset = offset;
                            sVoxel.zOffset = offset;

                            if (xW >= worldCenter.x)
                                sVoxel.x01 = true;
                            if (yW >= worldCenter.y)
                                sVoxel.y01 = true;
                            if (zW >= worldCenter.z)
                                sVoxel.z01 = true;

                            if (xW == worldCenter.x || (xW > worldCenter.x && xW - offset < worldCenter.x))
                                sVoxel.xC = true;
                            if (yW == worldCenter.y || (yW > worldCenter.y && yW - offset < worldCenter.y))
                                sVoxel.yC = true;
                            if (zW == worldCenter.z || (zW > worldCenter.z && zW - offset < worldCenter.z))
                                sVoxel.zC = true;

                            if (offset == 1)
                            {
                                sVoxel.anyCross = useVoxel.anyCross;
                                sVoxel.meshVertex = useVoxel.meshVertex;
                                sVoxel.meshNormal = useVoxel.meshNormal;
                            }
                            else
                                sVoxel.meshVertex = nullVector;

                            if (debug)
                            {
                                GameObject debugMeshVertex = null;
                                string xyz01String = "|";
                                if (sVoxel.x01)
                                    xyz01String += "x1|";
                                else
                                    xyz01String += "x0|";
                                if (sVoxel.y01)
                                    xyz01String += "y1|";
                                else
                                    xyz01String += "y0|";
                                if (sVoxel.z01)
                                    xyz01String += "z1|";
                                else
                                    xyz01String += "z0|";

                                string xyzCString = "|";
                                if (sVoxel.xC)
                                    xyzCString += "xC|";
                                if (sVoxel.yC)
                                    xyzCString += "yC|";
                                if (sVoxel.zC)
                                    xyzCString += "zC|";

                                debugMeshVertex = Instantiate(useDebugGOs.vertexTestCube, sVoxel.centerPoint, Quaternion.identity);
                                debugMeshVertex.name = "Offset:" + offset + "||" + xW + ":" + yW + ":" + zW + "!|" + xyz01String + xyzCString + "|startPos:" + sVoxel.startPoint;
                                debugMeshVertex.transform.parent = outerVoxesGO.transform;
                                if (offset == 1)
                                    debugMeshVertex.GetComponent<MeshRenderer>().material.color = Color.red;
                                else if (offset == 2)
                                    debugMeshVertex.GetComponent<MeshRenderer>().material.color = Color.blue;
                                else
                                    debugMeshVertex.GetComponent<MeshRenderer>().material.color = Color.black;
                            }

                            outerVoxes[outerVoxCounter] = sVoxel;
                            ++outerVoxCounter;
                        }
                    }
                }
            }
        }
    }

    //GetVoxelFromOffset finds or creates an associated core voxel structure to be used by the outer voxel for various functions
    public static DCVoxel GetVoxelFromOffset(CoreVoxelDataMT coreVoxelData, int xPos, int yPos, int zPos, int xOff, int yOff, int zOff)
    {
        bool voxInCore = true;
        int useX = xPos + xOff;
        int useY = yPos + yOff;
        int useZ = zPos + zOff;

        int3 newOffset = new int3(useX, useY, useZ);
        if (useX >= coreVoxSize.x)
        {
            voxInCore = false;
            newOffset.x = coreVoxSize.x + (useX - coreVoxSize.x);
        }
        else if (useX < 0)
        {
            voxInCore = false;
            newOffset.x = useX;
        }

        if (useY >= coreVoxSize.y)
        {
            voxInCore = false;
            newOffset.y = coreVoxSize.y + (useY - coreVoxSize.y);
        }
        else if (useY < 0)
        {
            voxInCore = false;
            newOffset.y = useY;
        }

        if (useZ >= coreVoxSize.z)
        {
            voxInCore = false;
            newOffset.z = coreVoxSize.z + (useZ - coreVoxSize.z);
        }
        else if (useZ < 0)
        {
            voxInCore = false;
            newOffset.z = useZ;
        }

        if (voxInCore)
        {
            int xIndex = useX * (coreVoxSize.x + 1) * (coreVoxSize.x + 1);
            int yIndex = useY * (coreVoxSize.y + 1);
            int zIndex = useZ;
            return coreVoxelData.coreVoxels[xIndex + yIndex + zIndex];
        }
        else
        {
            DCVoxel newVox = new DCVoxel();

            newVox.depth = 0;
            newVox.innerDepth = 0;
            newVox.detail = 0;

            newVox.pos.x = newOffset.x;
            newVox.pos.y = newOffset.y;
            newVox.pos.z = newOffset.z;
            newVox.fullPos = newVox.pos * voxDepthPosMod[0];
            newVox.baseNum = new int3(newOffset.x, newOffset.y, newOffset.z);

            newVox.startPoint = coreVoxelData.coreVoxStartPos + new float3(newOffset.x * voxSizeF3[0].x, newOffset.y * voxSizeF3[0].y, newOffset.z * voxSizeF3[0].z);
            newVox.centerPoint = newVox.startPoint + voxHalfSizeF3[0];
            newVox.endPoint = newVox.centerPoint + voxHalfSizeF3[0];

            newVox.anyCross = false;
            newVox.meshVertex = nullVector;

            return newVox;
        }
    }

    //SetOuterVoxelMeshPositionJob is mean to find the mesh position for an outer voxel.
    //This function is meant to run parallel on as many cores as possible.
    public struct SetOuterVoxelMeshPositionJob : IJobParallelFor
    {
        public NativeArray<DCSVoxel> outerVox;
        public float3 blockCenterPos;
        [NativeDisableParallelForRestriction]
        public NativeHashMap<int, float> vertDensities;

        public void Execute(int index)
        {
            if (outerVox[index].meshVertex.x == nullVector.x)
            {
                DCSVoxel sVox = outerVox[index];

                HermiteData hermiteData = new HermiteData();
                hermiteData.intersections = new List<float3>();
                hermiteData.gradients = new List<float3>();

                int3 v000 = GetVertexFromOffset(sVox.vert, 0, 0, 0);
                int3 v001 = GetVertexFromOffset(sVox.vert, 0, 0, sVox.zOffset * voxNum[worldDepthLimit]);
                int3 v010 = GetVertexFromOffset(sVox.vert, 0, sVox.yOffset * voxNum[worldDepthLimit], 0);
                int3 v011 = GetVertexFromOffset(sVox.vert, 0, sVox.yOffset * voxNum[worldDepthLimit], sVox.zOffset * voxNum[worldDepthLimit]);
                int3 v100 = GetVertexFromOffset(sVox.vert, sVox.xOffset * voxNum[worldDepthLimit], 0, 0);
                int3 v101 = GetVertexFromOffset(sVox.vert, sVox.xOffset * voxNum[worldDepthLimit], 0, sVox.zOffset * voxNum[worldDepthLimit]);
                int3 v110 = GetVertexFromOffset(sVox.vert, sVox.xOffset * voxNum[worldDepthLimit], sVox.yOffset * voxNum[worldDepthLimit], 0);
                int3 v111 = GetVertexFromOffset(sVox.vert, sVox.xOffset * voxNum[worldDepthLimit], sVox.yOffset * voxNum[worldDepthLimit], sVox.zOffset * voxNum[worldDepthLimit]);

                CheckIntersection(0, blockCenterPos, v000, v100, Dir.x, hermiteData, vertDensities); //p000p100
                CheckIntersection(0, blockCenterPos, v000, v010, Dir.y, hermiteData, vertDensities); //p000p010
                CheckIntersection(0, blockCenterPos, v000, v001, Dir.z, hermiteData, vertDensities); //p000p001
                CheckIntersection(0, blockCenterPos, v100, v110, Dir.y, hermiteData, vertDensities); //p100p110
                CheckIntersection(0, blockCenterPos, v100, v101, Dir.z, hermiteData, vertDensities); //p100p101
                CheckIntersection(0, blockCenterPos, v010, v110, Dir.x, hermiteData, vertDensities); //p010p110
                CheckIntersection(0, blockCenterPos, v010, v011, Dir.z, hermiteData, vertDensities); //p010p011
                CheckIntersection(0, blockCenterPos, v001, v101, Dir.x, hermiteData, vertDensities); //p001p101
                CheckIntersection(0, blockCenterPos, v001, v011, Dir.y, hermiteData, vertDensities); //p001p011
                CheckIntersection(0, blockCenterPos, v110, v111, Dir.z, hermiteData, vertDensities); //p110p111
                CheckIntersection(0, blockCenterPos, v011, v111, Dir.x, hermiteData, vertDensities); //p011p111
                CheckIntersection(0, blockCenterPos, v101, v111, Dir.y, hermiteData, vertDensities); //p101p111

                if (hermiteData.intersections.Count > 0)
                {
                    sVox.anyCross = true;
                    sVox.meshVertex = SchmitzVertexFromHermiteData(hermiteData, .001f);

                    DensityFunction.DFType useDF = GetVertexDensityFunction(v000, blockCenterPos);
                    sVox.meshVertex = SVertexClamp(sVox.meshVertex, sVox);
                    sVox.meshNormal = GetNormal(sVox.meshVertex, useDF);
                }

                outerVox[index] = sVox;
            }
        }
    }

    //SVertexClamp limits a mesh vertex position to the inside of an outer voxel in the case that it somehow is set to fall outside it.
    public static float3 SVertexClamp(float3 v, DCSVoxel vox)
    {
        Vector3 voxMin = vox.startPoint;
        Vector3 voxMax = vox.endPoint;

        if (v.x < voxMin.x) v.x = voxMin.x;
        if (v.y < voxMin.y) v.y = voxMin.y;
        if (v.z < voxMin.z) v.z = voxMin.z;
        if (v.x > voxMax.x) v.x = voxMax.x;
        if (v.y > voxMax.y) v.y = voxMax.y;
        if (v.z > voxMax.z) v.z = voxMax.z;

        return v;
    }

    //OuterVoxMeshDataCreationJob is creates the mesh data much like CoreVoxMeshDataCreationJob, but with some difference for the outer voxels.
    //One of thhose core differences, besides using DCSVoxel strucutres, is a changing offset used to find the correct neighboring voxel.
    public struct OuterVoxMeshDataCreationJob : IJob
    {
        public CoreVoxelDataMT coreVoxData;
        public NativeArray<DCSVoxel> outerVoxI;
        public NativeArray<DCSVoxel> outerVoxO;
        public NativeHashMap<int, float> vertDensities;
        public NativeList<float3> verticies;
        public NativeList<int> triangles;
        public NativeList<float2> uvs;
        public NativeList<float3> normals;
        public int innerOffset;
        public bool debug;

        public void Execute()
        {
            for (int outerVoxIndex = 0; outerVoxIndex < outerVoxI.Length; ++outerVoxIndex)
            {
                if (outerVoxI[outerVoxIndex].anyCross)
                {
                    if (debug)
                    {
                        verticies = new NativeList<float3>(Allocator.Temp);
                        triangles = new NativeList<int>(Allocator.Temp);
                        uvs = new NativeList<float2>(Allocator.Temp);
                        normals = new NativeList<float3>(Allocator.Temp);
                    }

                    DCSVoxel sVox = outerVoxI[outerVoxIndex];
                    DCVoxel newbase = coreVoxData.coreVoxels[sVox.baseVox];
                    int useInnerOffset = innerOffset * 8;

                    int3 v000 = sVox.vert;
                    int3 v100 = GetVertexFromOffset(v000, sVox.xOffset * voxNum[worldDepthLimit], 0, 0);
                    int3 v010 = GetVertexFromOffset(v000, 0, sVox.yOffset * voxNum[worldDepthLimit], 0);
                    int3 v001 = GetVertexFromOffset(v000, 0, 0, sVox.zOffset * voxNum[worldDepthLimit]);
                    int3 v110 = GetVertexFromOffset(v000, sVox.xOffset * voxNum[worldDepthLimit], sVox.yOffset * voxNum[worldDepthLimit], 0);
                    int3 v101 = GetVertexFromOffset(v000, sVox.xOffset * voxNum[worldDepthLimit], 0, sVox.zOffset * voxNum[worldDepthLimit]);
                    int3 v011 = GetVertexFromOffset(v000, 0, sVox.yOffset * voxNum[worldDepthLimit], sVox.zOffset * voxNum[worldDepthLimit]);
                    int3 v111 = GetVertexFromOffset(v000, sVox.xOffset * voxNum[worldDepthLimit], sVox.yOffset * voxNum[worldDepthLimit], sVox.zOffset * voxNum[worldDepthLimit]);

                    float v000Density = GetVertexDensity(v000, coreVoxData.coreVoxStartPos, vertDensities, newbase.detail);
                    float v001Density = GetVertexDensity(v001, coreVoxData.coreVoxStartPos, vertDensities, newbase.detail);
                    float v010Density = GetVertexDensity(v010, coreVoxData.coreVoxStartPos, vertDensities, newbase.detail);
                    float v100Density = GetVertexDensity(v100, coreVoxData.coreVoxStartPos, vertDensities, newbase.detail);
                    float v110Density = GetVertexDensity(v110, coreVoxData.coreVoxStartPos, vertDensities, newbase.detail);
                    float v101Density = GetVertexDensity(v101, coreVoxData.coreVoxStartPos, vertDensities, newbase.detail);
                    float v011Density = GetVertexDensity(v011, coreVoxData.coreVoxStartPos, vertDensities, newbase.detail);
                    float v111Density = GetVertexDensity(v111, coreVoxData.coreVoxStartPos, vertDensities, newbase.detail);

                    bool thoroughCheck = true;

                    if (debug)
                    {
                        outerVoxGO = Instantiate(useDebugGOs.vertexTestRed, sVox.centerPoint + new float3(0f, 0f, 0f), Quaternion.identity);
                        string sVoxddata;
                        outerVoxGO.name = newbase.pos.x + ":" + newbase.pos.y + ":" + newbase.pos.z + "|Center:" + sVox.centerPoint;// + superVoxelsInner[i].debugString;
                        outerVoxGO.transform.parent = outerVoxesGO.transform;
                        if (sVox.xC || sVox.xC || sVox.xC) outerVoxGO.GetComponent<MeshRenderer>().material.color = Color.blue;

                        string sidesCentersString = "|";
                        if (sVox.x01)
                            sidesCentersString += "x1|";
                        else
                            sidesCentersString += "x0|";
                        if (sVox.y01)
                            sidesCentersString += "y1|";
                        else
                            sidesCentersString += "y0|";
                        if (sVox.z01)
                            sidesCentersString += "z1|";
                        else
                            sidesCentersString += "z0|";
                        if (sVox.xC)
                            sidesCentersString += "xC|";
                        if (sVox.yC)
                            sidesCentersString += "yC|";
                        if (sVox.zC)
                            sidesCentersString += "zC|";

                        GameObject centers = new GameObject();
                        centers.transform.parent = outerVoxGO.transform;
                        centers.name = sidesCentersString;

                        GameObject vertdense = new GameObject();
                        vertdense.transform.parent = outerVoxGO.transform;
                        vertdense.name = "VertexDensities";

                        GameObject v000go = Instantiate(useDebugGOs.vertexTestCube, GetVertexPosition(coreVoxData.coreVoxStartPos, v000.x, v000.y, v000.z), Quaternion.identity);
                        v000go.transform.parent = vertdense.transform;
                        v000go.name = "v000:" + v000Density + GetVertexPosition(coreVoxData.coreVoxStartPos, v000.x, v000.y, v000.z);

                        GameObject v001go = Instantiate(useDebugGOs.vertexTestCube, GetVertexPosition(coreVoxData.coreVoxStartPos, v001.x, v001.y, v001.z), Quaternion.identity);
                        v001go.transform.parent = vertdense.transform;
                        v001go.name = "v001:" + v001Density + GetVertexPosition(coreVoxData.coreVoxStartPos, v001.x, v001.y, v001.z);

                        GameObject v010go = Instantiate(useDebugGOs.vertexTestCube, GetVertexPosition(coreVoxData.coreVoxStartPos, v010.x, v010.y, v010.z), Quaternion.identity);
                        v010go.transform.parent = vertdense.transform;
                        v010go.name = "v010:" + v010Density + GetVertexPosition(coreVoxData.coreVoxStartPos, v010.x, v010.y, v010.z);

                        GameObject v100go = Instantiate(useDebugGOs.vertexTestCube, GetVertexPosition(coreVoxData.coreVoxStartPos, v100.x, v100.y, v100.z), Quaternion.identity);
                        v100go.transform.parent = vertdense.transform;
                        v100go.name = "v100:" + v100Density + GetVertexPosition(coreVoxData.coreVoxStartPos, v100.x, v100.y, v100.z);

                        GameObject v110go = Instantiate(useDebugGOs.vertexTestCube, GetVertexPosition(coreVoxData.coreVoxStartPos, v110.x, v110.y, v110.z), Quaternion.identity);
                        v110go.transform.parent = vertdense.transform;
                        v110go.name = "v110:" + v110Density + GetVertexPosition(coreVoxData.coreVoxStartPos, v110.x, v110.y, v110.z);

                        GameObject v101go = Instantiate(useDebugGOs.vertexTestCube, GetVertexPosition(coreVoxData.coreVoxStartPos, v101.x, v101.y, v101.z), Quaternion.identity);
                        v101go.transform.parent = vertdense.transform;
                        v101go.name = "v101:" + v101Density + GetVertexPosition(coreVoxData.coreVoxStartPos, v101.x, v101.y, v101.z);

                        GameObject v011go = Instantiate(useDebugGOs.vertexTestCube, GetVertexPosition(coreVoxData.coreVoxStartPos, v011.x, v011.y, v011.z), Quaternion.identity);
                        v011go.transform.parent = vertdense.transform;
                        v011go.name = "v011:" + v011Density + GetVertexPosition(coreVoxData.coreVoxStartPos, v011.x, v011.y, v011.z);

                        GameObject v111go = Instantiate(useDebugGOs.vertexTestCube, GetVertexPosition(coreVoxData.coreVoxStartPos, v111.x, v111.y, v111.z), Quaternion.identity);
                        v111go.transform.parent = vertdense.transform;
                        v111go.name = "v111:" + v111Density + GetVertexPosition(coreVoxData.coreVoxStartPos, v111.x, v111.y, v111.z);
                    }

                    //X-Crosses
                    //y0z0:X-Cross:000->100
                    if (sVox.y01 == false && sVox.z01 == false && CheckDensityCross(v000Density, v100Density))
                    {
                        DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                        DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, -1, sVox.offset);
                        DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, 0, sVox.offset);
                        DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, -1, sVox.offset);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = outerVoxGO.transform;
                            dp.name = "|XCross|000->100|" + "SVoxes";

                            GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, newbase.startPoint, Quaternion.identity);
                            voxnbGO.transform.parent = dp.transform;
                            voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                        }

                        if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                            && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                        {
                            if (v100Density < 0f)
                                AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //y0z1:X-Cross:001->101
                    if (sVox.y01 == false && sVox.z01 == true && CheckDensityCross(v001Density, v101Density))
                    {
                        DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                        DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, 0, sVox.offset);
                        DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, useInnerOffset, sVox.offset);
                        DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, useInnerOffset, sVox.offset);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = outerVoxGO.transform;
                            dp.name = "|XCross|001->101|" + "SVoxes";

                            GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, newbase.startPoint, Quaternion.identity);
                            voxnbGO.transform.parent = dp.transform;
                            voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                        }

                        if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                            && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                        {
                            if (v101Density < 0f)
                                AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //y1z0:X-Cross:010->110
                    if (sVox.y01 == true && sVox.z01 == false && CheckDensityCross(v010Density, v110Density))
                    {
                        DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                        DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, useInnerOffset, 0, sVox.offset);
                        DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, -1, sVox.offset);
                        DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, useInnerOffset, -1, sVox.offset);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = outerVoxGO.transform;
                            dp.name = "|XCross|010->110|" + "SVoxes";

                            GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, newbase.startPoint, Quaternion.identity);
                            voxnbGO.transform.parent = dp.transform;
                            voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                        }

                        if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                            && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                        {
                            if (v110Density < 0f)
                                AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //y1z1:X-Cross:011->111
                    if (sVox.y01 == true && sVox.z01 == true && CheckDensityCross(v011Density, v111Density))
                    {
                        DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                        DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, useInnerOffset, 0, sVox.offset);
                        DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, useInnerOffset, sVox.offset);
                        DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, useInnerOffset, useInnerOffset, sVox.offset);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = outerVoxGO.transform;
                            dp.name = "|XCross|011->111|" + "SVoxes";

                            GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, sVox.centerPoint, Quaternion.identity);
                            voxnbGO.transform.parent = dp.transform;
                            voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                        }

                        if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                            && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                        {
                            if (v111Density < 0f)
                                AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                        }
                    }

                    //Y-Crosses
                    //x0z0:Y-Cross:000->010
                    if (sVox.x01 == false && sVox.z01 == false && CheckDensityCross(v000Density, v010Density))
                    {
                        DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                        DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, -1, sVox.offset);
                        DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, 0, sVox.offset);
                        DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, -1, sVox.offset);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = outerVoxGO.transform;
                            dp.name = "|YCross|000->010|" + "SVoxes" + "|MeshPos:" + sVox.meshVertex;

                            GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, newbase.startPoint, Quaternion.identity);
                            voxnbGO.transform.parent = dp.transform;
                            voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                        }

                        if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                            && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                        {
                            if (v010Density < 0f)
                                AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //x0z1:Y-Cross:001->011
                    if (sVox.x01 == false && sVox.z01 == true && CheckDensityCross(v001Density, v011Density))
                    {
                        DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                        DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, useInnerOffset, sVox.offset);
                        DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, 0, sVox.offset);
                        DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, useInnerOffset, sVox.offset);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = outerVoxGO.transform;
                            dp.name = "|YCross|001->011|" + "SVoxes";

                            GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, sVox.centerPoint, Quaternion.identity);
                            voxnbGO.transform.parent = dp.transform;
                            voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                        }

                        if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                            && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                        {
                            if (v011Density < 0f)
                                AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //x1z0:Y-Cross:100->110
                    if (sVox.x01 == true && sVox.z01 == false && CheckDensityCross(v100Density, v110Density))
                    {
                        DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                        DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, -1, sVox.offset);
                        DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, useInnerOffset, 0, 0, sVox.offset);
                        DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, useInnerOffset, 0, -1, sVox.offset);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = outerVoxGO.transform;
                            dp.name = "|YCross|100->110|" + "SVoxes";

                            GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, newbase.startPoint, Quaternion.identity);
                            voxnbGO.transform.parent = dp.transform;
                            voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                        }

                        if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                            && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                        {
                            if (v110Density < 0f)
                                AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //x1z1:Y-Cross:101->111
                    if (sVox.x01 == true && sVox.z01 == true && CheckDensityCross(v101Density, v111Density))
                    {
                        DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                        DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, useInnerOffset, sVox.offset);
                        DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, useInnerOffset, 0, 0, sVox.offset);
                        DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, useInnerOffset, 0, useInnerOffset, sVox.offset);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = outerVoxGO.transform;
                            dp.name = "|YCross|101->111|" + "SVoxes";

                            GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, sVox.centerPoint, Quaternion.identity);
                            voxnbGO.transform.parent = dp.transform;
                            voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                        }

                        if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                            && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                        {
                            if (v111Density < 0f)
                                AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                        }
                    }

                    //Z-Crosses
                    //x0y0:Z-Cross:000->001
                    if (sVox.x01 == false && sVox.y01 == false && CheckDensityCross(v000Density, v001Density))
                    {
                        DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                        DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, 0, sVox.offset);
                        DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, 0, sVox.offset);
                        DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, -1, 0, sVox.offset);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = outerVoxGO.transform;
                            dp.name = "|ZCross|000->001|" + "SVoxes";

                            GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, newbase.startPoint, Quaternion.identity);
                            voxnbGO.transform.parent = dp.transform;
                            voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                        }

                        if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                            && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                        {
                            if (v001Density < 0f)
                                AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //x0y1:Z-Cross:010->011
                    if (sVox.x01 == false && sVox.y01 == true && CheckDensityCross(v010Density, v011Density))
                    {
                        DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                        DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, useInnerOffset, 0, sVox.offset);
                        DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, 0, sVox.offset);
                        DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, useInnerOffset, 0, sVox.offset);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = outerVoxGO.transform;
                            dp.name = "|ZCross|010->011|" + "SVoxes";

                            GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, newbase.startPoint, Quaternion.identity);
                            voxnbGO.transform.parent = dp.transform;
                            voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                        }

                        if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                            && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                        {
                            if (v011Density < 0f)
                                AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //x1y0:Z-Cross:100->101
                    if (sVox.x01 == true && sVox.y01 == false && CheckDensityCross(v100Density, v101Density))
                    {
                        DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                        DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, 0, sVox.offset);
                        DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, useInnerOffset, 0, 0, sVox.offset);
                        DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, useInnerOffset, -1, 0, sVox.offset);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = outerVoxGO.transform;
                            dp.name = "|ZCross|100->101|" + "SVoxes";

                            GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, newbase.startPoint, Quaternion.identity);
                            voxnbGO.transform.parent = dp.transform;
                            voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                        }

                        if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                            && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                        {
                            if (v101Density < 0f)
                                AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //x1y1:Z-Cross:110->111
                    if (sVox.x01 == true && sVox.y01 == true && CheckDensityCross(v110Density, v111Density))
                    {
                        DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                        DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, useInnerOffset, 0, sVox.offset);
                        DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, useInnerOffset, 0, 0, sVox.offset);
                        DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, useInnerOffset, useInnerOffset, 0, sVox.offset);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = outerVoxGO.transform;
                            dp.name = "|ZCross|110->111|" + "SVoxes";

                            GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, newbase.startPoint, Quaternion.identity);
                            voxnbGO.transform.parent = dp.transform;
                            voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                        }

                        if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                            && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                        {
                            if (v111Density < 0f)
                                AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }

                    //X-Center
                    if (sVox.xC == true)
                    {
                        //xCy0:Z-Cross:000->001
                        if (sVox.y01 == false && CheckDensityCross(v000Density, v001Density))
                        {
                            DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                            DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, 0, sVox.offset);
                            DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, 0, sVox.offset);
                            DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, -1, 0, sVox.offset);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = outerVoxGO.transform;
                                dp.name = "|X-Center|ZCross|000->001|" + "SVoxes";

                                GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, sVox.centerPoint, Quaternion.identity);
                                voxnbGO.transform.parent = dp.transform;
                                voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                            }

                            if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                                && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                            {
                                if (v001Density < 0f)
                                    AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //xCy1:Z-Cross:010->011
                        if (sVox.y01 == true && CheckDensityCross(v010Density, v011Density))
                        {
                            DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                            DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, useInnerOffset, 0, sVox.offset);
                            DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, 0, sVox.offset);
                            DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, useInnerOffset, 0, sVox.offset);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = outerVoxGO.transform;
                                dp.name = "|X-Center|ZCross|010->011|" + "SVoxes";

                                GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, sVox.centerPoint, Quaternion.identity);
                                voxnbGO.transform.parent = dp.transform;
                                voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                            }

                            if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                                && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                            {
                                if (v011Density < 0f)
                                    AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //xCz0:Y-Cross:000->010
                        if (sVox.z01 == false && CheckDensityCross(v000Density, v010Density))
                        {
                            DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                            DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, -1, sVox.offset);
                            DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, 0, sVox.offset);
                            DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, -1, sVox.offset);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = outerVoxGO.transform;
                                dp.name = "|X-Center|YCross|000->010|" + "SVoxes";

                                GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, sVox.centerPoint, Quaternion.identity);
                                voxnbGO.transform.parent = dp.transform;
                                voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                            }

                            if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                                && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                            {
                                if (v010Density < 0f)
                                    AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //xCz1:Y-Cross:001->011
                        if (sVox.z01 == true && CheckDensityCross(v001Density, v011Density))
                        {
                            DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                            DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, useInnerOffset, sVox.offset);
                            DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, 0, sVox.offset);
                            DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, useInnerOffset, sVox.offset);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = outerVoxGO.transform;
                                dp.name = "|X-Center|YCross|001->011|" + "SVoxes";

                                GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, sVox.centerPoint, Quaternion.identity);
                                voxnbGO.transform.parent = dp.transform;
                                voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                            }

                            if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                                && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                            {
                                if (v011Density < 0f)
                                    AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                    }
                    //Y-Center
                    if (sVox.yC == true)
                    {
                        //yCx0:Z-Cross:000->001
                        if (sVox.x01 == false && CheckDensityCross(v000Density, v001Density))
                        {
                            DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                            DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, 0, sVox.offset);
                            DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, 0, sVox.offset);
                            DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, -1, 0, sVox.offset);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = outerVoxGO.transform;
                                dp.name = "|Y-Center|ZCross|000->001|" + "SVoxes";

                                GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, sVox.centerPoint, Quaternion.identity);
                                voxnbGO.transform.parent = dp.transform;
                                voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                            }

                            if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                                && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                            {
                                if (v001Density < 0f)
                                    AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //yCx1:Z-Cross:100->101
                        if (sVox.x01 == true && CheckDensityCross(v100Density, v101Density))
                        {
                            DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                            DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, 0, sVox.offset);
                            DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, useInnerOffset, 0, 0, sVox.offset);
                            DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, useInnerOffset, -1, 0, sVox.offset);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = outerVoxGO.transform;
                                dp.name = "|Y-Center|ZCross|100->101|" + "SVoxes";

                                GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, sVox.centerPoint, Quaternion.identity);
                                voxnbGO.transform.parent = dp.transform;
                                voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                            }

                            if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                                && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                            {
                                if (v101Density < 0f)
                                    AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //yCz0:X-Cross:000->100
                        if (sVox.z01 == false && CheckDensityCross(v000Density, v100Density))
                        {
                            DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                            DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, -1, sVox.offset);
                            DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, 0, sVox.offset);
                            DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, -1, sVox.offset);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = outerVoxGO.transform;
                                dp.name = "|Y-Center|XCross|000->100|" + "SVoxes";

                                GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, sVox.centerPoint, Quaternion.identity);
                                voxnbGO.transform.parent = dp.transform;
                                voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                            }

                            if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                                && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                            {
                                if (v100Density < 0f)
                                    AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //yCz1:X-Cross:001->101
                        if (sVox.z01 == true && CheckDensityCross(v001Density, v101Density))
                        {
                            DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                            DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, useInnerOffset, sVox.offset);
                            DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, 0, sVox.offset);
                            DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, useInnerOffset, sVox.offset);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = outerVoxGO.transform;
                                dp.name = "|Y-Center|YCross|000->101|" + "SVoxes";

                                GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, sVox.centerPoint, Quaternion.identity);
                                voxnbGO.transform.parent = dp.transform;
                                voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                            }

                            if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                                && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                            {
                                if (v101Density < 0f)
                                    AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                    }
                    //Z-Center
                    if (sVox.zC == true)
                    {
                        //zCx0:Y-Cross:000->010
                        if (sVox.x01 == false && CheckDensityCross(v000Density, v010Density))
                        {
                            DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                            DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, -1, sVox.offset);
                            DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, 0, sVox.offset);
                            DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, -1, sVox.offset);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = outerVoxGO.transform;
                                dp.name = "|Z-Center|YCross|000->010|" + "SVoxes";

                                GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, sVox.centerPoint, Quaternion.identity);
                                voxnbGO.transform.parent = dp.transform;
                                voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                            }

                            if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                                && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                            {
                                if (v010Density < 0f)
                                    AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //zCx1:Y-Cross:100->110
                        if (sVox.x01 == true && CheckDensityCross(v100Density, v110Density))
                        {
                            DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                            DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, -1, sVox.offset);
                            DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, useInnerOffset, 0, 0, sVox.offset);
                            DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, useInnerOffset, 0, -1, sVox.offset);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = outerVoxGO.transform;
                                dp.name = "|Z-Center|YCross|100->110|" + "SVoxes";

                                GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, sVox.centerPoint, Quaternion.identity);
                                voxnbGO.transform.parent = dp.transform;
                                voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                            }

                            if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                                && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                            {
                                if (v110Density < 0f)
                                    AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //zCy0:X-Cross:000->100
                        if (sVox.y01 == false && CheckDensityCross(v000Density, v100Density))
                        {
                            DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                            DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, -1, sVox.offset);
                            DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, 0, sVox.offset);
                            DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, -1, sVox.offset);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = outerVoxGO.transform;
                                dp.name = "|Z-Center|XCross|000->100|" + "SVoxes";

                                GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, sVox.centerPoint, Quaternion.identity);
                                voxnbGO.transform.parent = dp.transform;
                                voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                            }

                            if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                                && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                            {
                                if (v100Density < 0f)
                                    AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                        //zCy1:X-Cross:010->110
                        if (sVox.y01 == true && CheckDensityCross(v010Density, v110Density))
                        {
                            DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                            DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, -1, sVox.offset);
                            DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, useInnerOffset, 0, sVox.offset);
                            DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, useInnerOffset, -1, sVox.offset);

                            if (thoroughCheck)
                            {
                                if (math.all(vox1.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox2.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                                if (math.all(vox3.meshVertex == nullVector))
                                    SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                            }

                            if (debug)
                            {
                                GameObject dp = new GameObject();
                                dp.transform.parent = outerVoxGO.transform;
                                dp.name = "|Z-Center|XCross|010->110|" + "SVoxes";

                                GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, sVox.centerPoint, Quaternion.identity);
                                voxnbGO.transform.parent = dp.transform;
                                voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                                GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                                vox0GO.transform.parent = dp.transform;
                                vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                                GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                                vox1GO.transform.parent = dp.transform;
                                vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                                GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                                vox2GO.transform.parent = dp.transform;
                                vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                                GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                                vox3GO.transform.parent = dp.transform;
                                vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                            }

                            if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                                && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                            {
                                if (v110Density < 0f)
                                    AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                                else
                                    AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            }
                        }
                    }

                    // Double Centers
                    //X-Center + Y-Center 
                    //xCyC:Z-Cross:000->001
                    if (sVox.xC == true && sVox.yC == true && CheckDensityCross(v000Density, v001Density))
                    {
                        DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                        DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, 0, sVox.offset);
                        DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, 0, sVox.offset);
                        DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, -1, 0, sVox.offset);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = outerVoxGO.transform;
                            dp.name = "|X-Center + Y-Center|ZCross|000->001|" + "SVoxes";

                            GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, newbase.startPoint, Quaternion.identity);
                            voxnbGO.transform.parent = dp.transform;
                            voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                        }

                        if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                            && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                        {
                            if (v001Density < 0f)
                                AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //X-Center + Z-Center
                    //xCzC:Y-Cross:000->010
                    if (sVox.xC == true && sVox.zC == true && CheckDensityCross(v000Density, v010Density))
                    {
                        DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                        DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, -1, sVox.offset);
                        DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, 0, sVox.offset);
                        DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, -1, 0, -1, sVox.offset);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = outerVoxGO.transform;
                            dp.name = "|X-Center + Z-Center|ZCross|000->010|" + "SVoxes";

                            GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, newbase.startPoint, Quaternion.identity);
                            voxnbGO.transform.parent = dp.transform;
                            voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                        }

                        if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                            && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                        {
                            if (v010Density < 0f)
                                AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                        }
                    }
                    //Y-Center + Z-Center
                    //yCzC:X-Cross:000->100
                    if (sVox.yC == true && sVox.zC == true && CheckDensityCross(v000Density, v100Density))
                    {
                        DCSVoxel vox0 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, 0, sVox.offset);
                        DCSVoxel vox1 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, 0, -1, sVox.offset);
                        DCSVoxel vox2 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, 0, sVox.offset);
                        DCSVoxel vox3 = GetSVoxelFromVertexPos(coreVoxData, outerVoxI, outerVoxO, v000, 0, -1, -1, sVox.offset);

                        if (thoroughCheck)
                        {
                            if (math.all(vox1.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox1, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox2.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox2, coreVoxData.coreVoxStartPos, vertDensities);
                            if (math.all(vox3.meshVertex == nullVector))
                                SetSVoxelMeshPositionsThorough(vox3, coreVoxData.coreVoxStartPos, vertDensities);
                        }

                        if (debug)
                        {
                            GameObject dp = new GameObject();
                            dp.transform.parent = outerVoxGO.transform;
                            dp.name = "|Y-Center + Z-Center|XCross|000->100|" + "SVoxes";

                            GameObject voxnbGO = Instantiate(useDebugGOs.vertexTestCube, newbase.startPoint, Quaternion.identity);
                            voxnbGO.transform.parent = dp.transform;
                            voxnbGO.name = "voxnb" + ":" + newbase.meshVertex;

                            GameObject vox0GO = Instantiate(useDebugGOs.vertexTestCube, vox0.centerPoint, Quaternion.identity);
                            vox0GO.transform.parent = dp.transform;
                            vox0GO.name = "vox0" + ":" + vox0.meshVertex;

                            GameObject vox1GO = Instantiate(useDebugGOs.vertexTestCube, vox1.centerPoint, Quaternion.identity);
                            vox1GO.transform.parent = dp.transform;
                            vox1GO.name = "vox1" + ":" + vox1.meshVertex;

                            GameObject vox2GO = Instantiate(useDebugGOs.vertexTestCube, vox2.centerPoint, Quaternion.identity);
                            vox2GO.transform.parent = dp.transform;
                            vox2GO.name = "vox2" + ":" + vox2.meshVertex;

                            GameObject vox3GO = Instantiate(useDebugGOs.vertexTestCube, vox3.centerPoint, Quaternion.identity);
                            vox3GO.transform.parent = dp.transform;
                            vox3GO.name = "vox3" + ":" + vox3.meshVertex;
                        }

                        if (math.all(vox0.meshVertex != nullVector) && math.all(vox1.meshVertex != nullVector)
                            && math.all(vox2.meshVertex != nullVector) && math.all(vox3.meshVertex != nullVector))
                        {
                            if (v100Density < 0f)
                                AddQuadFromSVox(vox0, vox2, vox1, vox3, verticies, triangles, uvs, normals);
                            else
                                AddQuadFromSVox(vox0, vox1, vox2, vox3, verticies, triangles, uvs, normals);
                        }
                    }

                    if (debug && verticies.IsCreated)
                    {
                        CreateMesh(verticies, triangles, uvs, normals, 3, outerVoxGO);
                    }
                }
            }
        }
    }

    //GetSVoxelFromVertexPos is used to obtain a voxel from a vertex position through an area check.
    //Note: since the single size outer voxels are mostly found within the inner voxel shell and all other voxel types are mostly found in the outer voxel shell, the code runs them in different order depending on the offset for maximum effciency.
    public static DCSVoxel GetSVoxelFromVertexPos(CoreVoxelDataMT coreVoxelData, NativeArray<DCSVoxel> superVoxelsInner, NativeArray<DCSVoxel> superVoxelsOuter, int3 vertex, int dX, int dY, int dZ, int offset = 0, bool debug = false)
    {
        int checkX = vertex.x + dX;
        int checkY = vertex.y + dY;
        int checkZ = vertex.z + dZ;
        Vector3 checkPos = GetVertexPosition(coreVoxelData.coreVoxStartPos, checkX, checkY, checkZ);

        if (offset == 1)
        {
            for (int i = 0; i < superVoxelsInner.Length; ++i)
            {
                if (checkPos.x >= superVoxelsInner[i].startPoint.x && checkPos.x < superVoxelsInner[i].endPoint.x
                    && checkPos.y >= superVoxelsInner[i].startPoint.y && checkPos.y < superVoxelsInner[i].endPoint.y
                    && checkPos.z >= superVoxelsInner[i].startPoint.z && checkPos.z < superVoxelsInner[i].endPoint.z)
                {
                    return superVoxelsInner[i];
                }
            }
        }

        //Check for Vox in Outer Vox
        for (int i = 0; i < superVoxelsOuter.Length; ++i)
        {
            if (checkPos.x >= superVoxelsOuter[i].startPoint.x && checkPos.x < superVoxelsOuter[i].endPoint.x
                && checkPos.y >= superVoxelsOuter[i].startPoint.y && checkPos.y < superVoxelsOuter[i].endPoint.y
                && checkPos.z >= superVoxelsOuter[i].startPoint.z && checkPos.z < superVoxelsOuter[i].endPoint.z)
            {
                return superVoxelsOuter[i];
            }
        }

        //Check for Vox in Inner Vox
        for (int i = 0; i < superVoxelsInner.Length; ++i)
        {
            if (checkPos.x >= superVoxelsInner[i].startPoint.x && checkPos.x < superVoxelsInner[i].endPoint.x
                && checkPos.y >= superVoxelsInner[i].startPoint.y && checkPos.y < superVoxelsInner[i].endPoint.y
                && checkPos.z >= superVoxelsInner[i].startPoint.z && checkPos.z < superVoxelsInner[i].endPoint.z)
            {
                return superVoxelsInner[i];
            }
        }

        DCSVoxel emtpyVoxel = new DCSVoxel();
        emtpyVoxel.meshVertex = nullVector;
        emtpyVoxel.meshNormal = new Vector3(0f, 0f, 0f);
        return emtpyVoxel;
    }

    //SetSVoxelMeshPositionsThorough is a mesh position thorough function for the DCSVoxels.
    public static DCSVoxel SetSVoxelMeshPositionsThorough(DCSVoxel sVox, float3 blockCenterPos, NativeHashMap<int, float> vertDensities)
    {
        HermiteData hermiteData = new HermiteData();
        hermiteData.intersections = new List<float3>();
        hermiteData.gradients = new List<float3>();

        int3 v000 = GetVertexFromOffset(sVox.vert, 0, 0, 0);
        int3 v001 = GetVertexFromOffset(sVox.vert, 0, 0, sVox.zOffset * voxNum[worldDepthLimit]);
        int3 v010 = GetVertexFromOffset(sVox.vert, 0, sVox.yOffset * voxNum[worldDepthLimit], 0);
        int3 v011 = GetVertexFromOffset(sVox.vert, 0, sVox.yOffset * voxNum[worldDepthLimit], sVox.zOffset * voxNum[worldDepthLimit]);
        int3 v100 = GetVertexFromOffset(sVox.vert, sVox.xOffset * voxNum[worldDepthLimit], 0, 0);
        int3 v101 = GetVertexFromOffset(sVox.vert, sVox.xOffset * voxNum[worldDepthLimit], 0, sVox.zOffset * voxNum[worldDepthLimit]);
        int3 v110 = GetVertexFromOffset(sVox.vert, sVox.xOffset * voxNum[worldDepthLimit], sVox.yOffset * voxNum[worldDepthLimit], 0);
        int3 v111 = GetVertexFromOffset(sVox.vert, sVox.xOffset * voxNum[worldDepthLimit], sVox.yOffset * voxNum[worldDepthLimit], sVox.zOffset * voxNum[worldDepthLimit]);

        CheckIntersection(0, blockCenterPos, v000, v100, Dir.x, hermiteData, vertDensities); //p000p100
        CheckIntersection(0, blockCenterPos, v000, v010, Dir.y, hermiteData, vertDensities); //p000p010
        CheckIntersection(0, blockCenterPos, v000, v001, Dir.z, hermiteData, vertDensities); //p000p001
        CheckIntersection(0, blockCenterPos, v100, v110, Dir.y, hermiteData, vertDensities); //p100p110
        CheckIntersection(0, blockCenterPos, v100, v101, Dir.z, hermiteData, vertDensities); //p100p101
        CheckIntersection(0, blockCenterPos, v010, v110, Dir.x, hermiteData, vertDensities); //p010p110
        CheckIntersection(0, blockCenterPos, v010, v011, Dir.z, hermiteData, vertDensities); //p010p011
        CheckIntersection(0, blockCenterPos, v001, v101, Dir.x, hermiteData, vertDensities); //p001p101
        CheckIntersection(0, blockCenterPos, v001, v011, Dir.y, hermiteData, vertDensities); //p001p011
        CheckIntersection(0, blockCenterPos, v110, v111, Dir.z, hermiteData, vertDensities); //p110p111
        CheckIntersection(0, blockCenterPos, v011, v111, Dir.x, hermiteData, vertDensities); //p011p111
        CheckIntersection(0, blockCenterPos, v101, v111, Dir.y, hermiteData, vertDensities); //p101p111

        DCSVoxel newVox = sVox;
        if (hermiteData.intersections.Count > 0)
        {
            newVox.anyCross = true;
            newVox.meshVertex = SchmitzVertexFromHermiteData(hermiteData, .001f);

            DensityFunction.DFType useDF = GetVertexDensityFunction(v000, blockCenterPos);
            newVox.meshVertex = SVertexClamp(newVox.meshVertex, newVox);
            newVox.meshNormal = GetNormal(newVox.meshVertex, useDF, 0);

        }
        else
        {
            newVox.anyCross = true;
            newVox.meshVertex = newVox.centerPoint;
            DensityFunction.DFType useDF = GetVertexDensityFunction(v000, blockCenterPos);
            newVox.meshNormal = GetNormal(newVox.meshVertex, useDF, 0);
        }

        return newVox;
    }

    //AddQuadFromSVox is used to add quads to the mesh data arrays bit using the DCSVoxel data type as the parameters.
    public static void AddQuadFromSVox(DCSVoxel a, DCSVoxel b, DCSVoxel c, DCSVoxel d, NativeList<float3> verticies, NativeList<int> triangles, NativeList<float2> uvs, NativeList<float3> normals)
    {
        if (math.all(a.meshVertex == b.meshVertex))
            AddTriangle(a.meshVertex, d.meshVertex, c.meshVertex, a.meshNormal, d.meshNormal, c.meshNormal, verticies, triangles, uvs, normals);
        else if (math.all(a.meshVertex == c.meshVertex))
            AddTriangle(a.meshVertex, b.meshVertex, d.meshVertex, a.meshNormal, b.meshNormal, d.meshNormal, verticies, triangles, uvs, normals);
        else if (math.all(a.meshVertex == d.meshVertex))
            AddTriangle(a.meshVertex, b.meshVertex, c.meshVertex, a.meshNormal, b.meshNormal, c.meshNormal, verticies, triangles, uvs, normals);
        else if (math.all(b.meshVertex == c.meshVertex))
            AddTriangle(a.meshVertex, b.meshVertex, d.meshVertex, a.meshNormal, b.meshNormal, d.meshNormal, verticies, triangles, uvs, normals);
        else if (math.all(b.meshVertex == d.meshVertex))
            AddTriangle(a.meshVertex, b.meshVertex, c.meshVertex, a.meshNormal, b.meshNormal, c.meshNormal, verticies, triangles, uvs, normals);
        else if (math.all(c.meshVertex == d.meshVertex))
            AddTriangle(a.meshVertex, b.meshVertex, c.meshVertex, a.meshNormal, b.meshNormal, c.meshNormal, verticies, triangles, uvs, normals);
        else
        {
            if (Vector3.Distance(b.meshVertex, c.meshVertex) < Vector3.Distance(a.meshVertex, d.meshVertex))
                AddQuad(a.meshVertex, b.meshVertex, c.meshVertex, d.meshVertex, a.meshNormal, b.meshNormal, c.meshNormal, d.meshNormal, verticies, triangles, uvs, normals);
            else
                AddQuadAlt(a.meshVertex, b.meshVertex, c.meshVertex, d.meshVertex, a.meshNormal, b.meshNormal, c.meshNormal, d.meshNormal, verticies, triangles, uvs, normals);
        }
    }


    //Create Mesh
    //CreateMesh is the non-multithreaded function for using mesh data arrays to create meshes and is mostly used for bebugging purposes.
    public static GameObject CreateMesh(NativeList<float3> verticies, NativeList<int> triangles, NativeList<float2> uvs, NativeList<float3> normals, int mat, GameObject parentGO, string ds = "")
    {
        Mesh mesh;

        GameObject newMesh = new GameObject("Mesh" + ds);
        newMesh.layer = 9;
        newMesh.transform.parent = parentGO.transform;
        MeshRenderer meshRenderer = newMesh.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = useDebugGOs.testMaterial;
        if (mat == 0)
            meshRenderer.sharedMaterial = useDebugGOs.testMaterial;
        else if (mat == 1)
            meshRenderer.sharedMaterial = useDebugGOs.testMaterial2;
        else if (mat == 2)
            meshRenderer.sharedMaterial = useDebugGOs.testMaterial3;
        else if (mat == 3)
            meshRenderer.sharedMaterial = useDebugGOs.testMaterial4;
        MeshFilter meshFilter = newMesh.AddComponent<MeshFilter>();
        mesh = new Mesh();
        mesh.name = "New Mesh";

        Vector3[] verticiesFinal = new Vector3[verticies.Length];
        for (int i = 0; i < verticies.Length; ++i)
        {
            verticiesFinal[i] = verticies[i];
        }
        int[] trianglesFinal = new int[triangles.Length];
        for (int i = 0; i < triangles.Length; ++i)
        {
            trianglesFinal[i] = triangles[i];
        }
        Vector2[] uvFinal = new Vector2[uvs.Length];
        for (int i = 0; i < uvs.Length; ++i)
        {
            uvFinal[i] = uvs[i];
        }

        mesh.vertices = verticiesFinal;
        mesh.triangles = trianglesFinal;
        mesh.uv = uvFinal;

        Vector3[] normalsFinal = new Vector3[normals.Length];
        for (int i = 0; i < normals.Length; ++i)
        {
            normalsFinal[i] = normals[i];
        }
        mesh.normals = normalsFinal;
        meshFilter.mesh = mesh;

        return newMesh;
    }

    //GetBoundsMT is used to manually compute the bounds of the created world mesh so that it can have a proper mesh collision component.
    public static Bounds GetBoundsMT(NativeList<float3> verticies)
    {
        float minX = verticies[0].x;
        float maxX = verticies[0].x;
        float minY = verticies[0].y;
        float maxY = verticies[0].y;
        float minZ = verticies[0].z;
        float maxZ = verticies[0].z;
        for (int i = 1; i < verticies.Length; ++i)
        {
            if (verticies[i].x < minX) minX = verticies[i].x;
            if (verticies[i].x > maxX) maxX = verticies[i].x;
            if (verticies[i].y < minY) minY = verticies[i].y;
            if (verticies[i].y > maxY) maxY = verticies[i].y;
            if (verticies[i].z < minZ) minZ = verticies[i].z;
            if (verticies[i].z > maxZ) maxZ = verticies[i].z;
        }

        float3 boundSize = new float3(maxX - minX, maxY - minY, maxZ - minZ);
        float3 boundCenter = new float3(boundSize.x / 2, boundSize.y / 2, boundSize.z / 2);
        Bounds newBound = new Bounds(boundCenter, boundSize);

        return newBound;
    }

    //Stream0 is a single stream datasctruct used for unity's parallel mesh creation system
    //This a single stream which enforces strict memory order for the position, normal, and uv data.
    [StructLayout(LayoutKind.Sequential)]
    public struct Stream0
    {
        public float3 position, normal;
        public float2 texCoord0;
    }
    
    //CreateMeshJob moves through the meshdataarray stream to create the world mesh.
    public struct CreateMeshJob : IJob
    {
        public Mesh.MeshData meshData;
        public NativeList<float3> verticies;
        public NativeList<int> triangles;
        public NativeList<float2> uvs;
        public NativeList<float3> normals;
        public bool debug;

        public void Execute()
        {
            NativeArray<Stream0> stream0;
            NativeArray<ushort> streamTriangles;
            Bounds useBounds = GetBoundsMT(verticies);

            var descriptor = new NativeArray<VertexAttributeDescriptor>(
                3, Allocator.Temp, NativeArrayOptions.UninitializedMemory
            );
            descriptor[0] = new VertexAttributeDescriptor(dimension: 3);
            descriptor[1] = new VertexAttributeDescriptor(
                VertexAttribute.Normal, dimension: 3
            );
            descriptor[2] = new VertexAttributeDescriptor(
                VertexAttribute.TexCoord0, dimension: 2
            );

            int vertexCount = verticies.Length;
            meshData.SetVertexBufferParams(vertexCount, descriptor);
            descriptor.Dispose();
            int indexCount = triangles.Length;
            meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt16);

            meshData.subMeshCount = 1;
            meshData.SetSubMesh(
                0, new SubMeshDescriptor(0, indexCount)
                {
                    bounds = useBounds,
                    vertexCount = vertexCount
                },
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontValidateIndices
            );

            stream0 = meshData.GetVertexData<Stream0>();
            streamTriangles = meshData.GetIndexData<ushort>();//.Reinterpret<ProceduralMeshes.Streams.TriangleUInt16>(2);

            for (int vi = 0; vi < verticies.Length; ++vi)
            {
                stream0[vi] = new Stream0
                {
                    position = verticies[vi],
                    normal = normals[vi],
                    texCoord0 = uvs[vi]
                };
            }

            for (int ti = 0; ti < triangles.Length; ++ti)
            {
                streamTriangles[ti] = (ushort)triangles[ti];
            }
        }
    }
}

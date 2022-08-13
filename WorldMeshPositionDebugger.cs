using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;

public class WorldMeshPositionDebugger : MonoBehaviour
{
    [System.NonSerialized] int groundMask;

    [System.NonSerialized] bool worldStarted;
    [System.NonSerialized] Vector3 startingPosition;
    [System.NonSerialized] Vector3 currentCenterPosition;
    [System.NonSerialized] Vector3Int coreVoxCenter;

    [System.NonSerialized] bool generatingNewMesh;
    [System.NonSerialized] int counterRDCMT;
    [System.NonSerialized] float speed = 2.5f;
   
    [System.NonSerialized] GameObject currentCenterVoxGO;
    [System.NonSerialized] GameObject worldMeshGO;
    [System.NonSerialized] MeshFilter worldMeshFilter;
    [System.NonSerialized] Mesh meshRef;

    [System.NonSerialized] MeshGeneratorDebugGOs debugGOs;
    [System.NonSerialized] bool generatedDebugCubes;
    [System.NonSerialized] int debugTexture;
    System.Diagnostics.Stopwatch st;

    // Start is called before the first frame update
    void Start()
    {
        startingPosition = new Vector3(0f, 0f, 0f);
        groundMask = NodeUtil.groundBitMask;

        if (GameObject.Find("WorldMesh") != null)
        {
            DestroyImmediate(GameObject.Find("WorldMesh"));
        }
        worldMeshGO = new GameObject("WorldMesh");
        worldMeshFilter = worldMeshGO.AddComponent<MeshFilter>();
        worldMeshGO.AddComponent<MeshRenderer>();
        worldMeshGO.layer = 9;

        GameObject meshDebug = GameObject.Find("MeshGeneratorDebug");
        debugGOs = meshDebug.GetComponent<MeshGeneratorDebugGOs>();

        st = new System.Diagnostics.Stopwatch();
        meshRef = new Mesh();
        worldStarted = false;
        generatingNewMesh = true;
        counterRDCMT = 0;
        RadialDC_MT.SetDCVariables();
        RadialDC_MT.MakeRecipeBlocks(startingPosition);
        RadialDC_MT.SetCreationFlags();
        MakeNewMesh();
        worldMeshGO.AddComponent<MeshCollider>();

        transform.position = startingPosition;
        coreVoxCenter = new Vector3Int(coreVoxCenter.x, coreVoxCenter.y, coreVoxCenter.z);
        generatedDebugCubes = false;
        debugTexture = 1;
    }

    // Update is called once every frame
    void Update()
    {
        if (worldStarted)
        {
            //Simple player input controls for debugging
            Vector3 position = transform.position;
            if (Input.GetKey("a") || Input.GetKey(KeyCode.LeftArrow))
            {
                position.x -= speed * Time.deltaTime;
            }
            if (Input.GetKey("a") || Input.GetKey(KeyCode.RightArrow))
            {
                position.x += speed * Time.deltaTime;
            }
            if (Input.GetKey("s") || Input.GetKey(KeyCode.DownArrow))
            {
                position.z -= speed * Time.deltaTime;
            }
            if (Input.GetKey("w") || Input.GetKey(KeyCode.UpArrow))
            {
                position.z += speed * Time.deltaTime;
            }
            transform.position = position;
            Camera.main.transform.position = new Vector3(position.x, Camera.main.transform.position.y, position.z);

            //Simple 'gravity' for inclined terrain
            SetOnGround();
           
            //Either create new mesh or check if one needs to be created
            if (generatingNewMesh)
                MakeNewMesh();
            else
                CheckIfGeneratingNewMesh();
        }
        else
        {
            MakeNewMesh();
        }
    }

    //SetOnGround just repositions the player on the ground as he moves over inclines
    void SetOnGround()
    {
        RaycastHit hit;
        Vector3 scanPosition = new Vector3(transform.position.x, transform.position.y + 500f, transform.position.z);

        Debug.DrawRay(scanPosition, scanPosition + Vector3.down * 1000f, Color.red);

        if (Physics.Raycast(scanPosition, Vector3.down, out hit, Mathf.Infinity, groundMask))
        {
            transform.position = new Vector3(hit.point.x, hit.point.y + 1.0f, hit.point.z);
        }
        else
            Debug.Log("Nothing Hit for scan...");
    }

    //CheckIfGeneratingNewMesh checks if player is closed enough to the bounds of the high detail area to being gnerating a new mesh
    bool CheckIfGeneratingNewMesh()
    {
        Vector3[,,] possibleRecenterPoints = RadialDC_MT.recenterPoints;
        Vector3 currentPos = transform.position;
        Vector3 curentCenterPoint = possibleRecenterPoints[1, 1, 1];
        float minDist = Vector3.Distance(possibleRecenterPoints[1,1,1], currentPos);
        bool foundCloserVox = false;

        for(int x = 0; x < possibleRecenterPoints.GetLength(0); ++x)
        {
            for (int y = 0; y < possibleRecenterPoints.GetLength(1); ++y)
            {
                for (int z = 0; z < possibleRecenterPoints.GetLength(2); ++z)
                {
                    if (!(x == 1 && y == 1 && z == 1))
                    {
                        float checkMin = Vector3.Distance(possibleRecenterPoints[x, y, z], currentPos);
                        if (minDist > checkMin)
                        {
                            foundCloserVox = true;
                            curentCenterPoint = possibleRecenterPoints[x, y, z];
                            minDist = checkMin;
                        }
                    }
                }
            }
        }

        if (foundCloserVox)
        {
            ++debugTexture;
            if (debugTexture > 3)
                debugTexture = 0;
            Debug.Log("Generating new mesh...");
            st.Restart();
            generatedDebugCubes = false;

            currentCenterPosition = curentCenterPoint;

            generatingNewMesh = true;
            counterRDCMT = 0;

            RadialDC_MT.MakeRecipeBlocks(currentCenterPosition);
            RadialDC_MT.SetCreationFlags();

            if (GameObject.Find("CurrentCenter") != null)
            {
                DestroyImmediate(GameObject.Find("CurrentCenter"));
            }
            GameObject centerGO = Instantiate(debugGOs.vertexTestGreen, RadialDC_MT.worldCenter, Quaternion.identity);
            centerGO.name = "CurrentCenter";
        }

        return generatingNewMesh;
    }

    //MakeNewMesh access the RadialDC to create a new mesh. Once complete it sets into the current world mesh object.
    void MakeNewMesh()
    {
        if (generatingNewMesh)
        {
            generatingNewMesh = RadialDC_MT.RecenterWorldMT(ref meshRef);
            ++counterRDCMT;
            if (counterRDCMT > 10000)
                generatingNewMesh = false;
        }
        if (!generatingNewMesh)
        {
            st.Stop();
           
            worldStarted = true;
            generatingNewMesh = false;
            worldMeshFilter.sharedMesh = meshRef;
            worldMeshGO.GetComponent<MeshFilter>().mesh.RecalculateBounds();
            worldMeshGO.GetComponent<MeshCollider>().sharedMesh = worldMeshFilter.mesh;

            if (counterRDCMT > 10000)
                Debug.Log("RDCMT World Problem");
            else if (generatingNewMesh)
                Debug.Log("RDCMT World Created");
            Debug.Log("World created in " + st.Elapsed);
            st.Restart();

            MeshRenderer meshRenderer = worldMeshGO.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = debugGOs.testMaterial;
            if (debugTexture == 0)
                meshRenderer.sharedMaterial = debugGOs.testMaterial;
            else if (debugTexture == 1)
                meshRenderer.sharedMaterial = debugGOs.testMaterial2;
            else if (debugTexture == 2)
                meshRenderer.sharedMaterial = debugGOs.testMaterial3;
            else if (debugTexture == 3)
                meshRenderer.sharedMaterial = debugGOs.testMaterial4;

            ShowRecenterPoints(RadialDC_MT.recenterPoints);
        }
    }

    //ShowRecenterPoints shows the bounds of the high detail area for debugging purposes.
    void ShowRecenterPoints(Vector3[,,] possibleRecenterPoints)
    {
        if (GameObject.Find("PossibleRecenterPoints") != null)
        {
            DestroyImmediate(GameObject.Find("PossibleRecenterPoints"));
        }
        GameObject recenterPosGO = new GameObject("PossibleRecenterPoints");

        Color transparent = new Color(1.0f, 1.0f, 0.1f, 0.5f);

        for (int x = 0; x < possibleRecenterPoints.GetLength(0); ++x)
        {
            for (int y = 0; y < possibleRecenterPoints.GetLength(1); ++y)
            {
                for (int z = 0; z < possibleRecenterPoints.GetLength(2); ++z)
                {
                    if (!(x == 1 && y == 1 && z == 1))
                    {
                        GameObject possibleRecenterVoxGO = Instantiate(debugGOs.vertexTestCubeTransparent, possibleRecenterPoints[x, y, z], Quaternion.identity);
                        possibleRecenterVoxGO.name = "RecenterVox|" + x + "|" + y + "|" + z;
                        possibleRecenterVoxGO.transform.localScale = new Vector3(4f, 4f, 4f);
                        possibleRecenterVoxGO.transform.parent = recenterPosGO.transform;
                        possibleRecenterVoxGO.GetComponent<MeshRenderer>().material.color = transparent;
                    }
                }
            }
        }
    }
}
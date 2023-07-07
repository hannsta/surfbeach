using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TerrainGenerator : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    Vector3[] verticesCopy;
    int[] triangles;
    Color[] colors;
    Vector2[] uvs;
    public int xSize = 20;
    public int zSize = 20;
    [SerializeField] private AnimationCurve heightCurve;
    [SerializeField] private AnimationCurve clusterCurve;
    float minTerrainHeight;
    float maxTerrainHeight;
    public Gradient gradient;
    public int octaves = 4;
    public float lacunarity = 2f;
    public int seed = 0;
    public float scale = 10f; 
    public float defaultAmplitude = 10f;
    public float defaultFrequency = 1f;
    public float persistence = 0.5f;
    public GameObject water;
    public bool inverted = false;
    public GameObject palmTree;
    public GameObject pineTree;
    public GameObject oakTree;
    public GameObject[] rocks;
    public GameObject[] bushes;
    public float treeDensity = 0.5f;
    public float rockDensity = 0.5f;
    public float bushDensity = 0.5f;
    [SerializeField] private AnimationCurve rockCurve;
    [SerializeField] private AnimationCurve treeCurve;

    private GameObject[] landObjects;

    public Color sandColor = Color.yellow;

    public Color pathColor = Color.red;

    public GameObject player;

    public Texture2D heightTexture;

    private bool isRunning = false;
    // Start is called before the first frame update
    void Start()
    {

    }
    void Update()
    {
        if (isRunning && heightTexture == null && mesh != null && mesh.uv.Length > 0){
            GenerateTextures();
        }

    }
    public Vector3 GetRandomPoint(){
        OceanGenerator ocean = water.GetComponent<OceanGenerator>();
        float seaLevel = ocean.GetSeaLevel();
        Vector3 finalPoint = Vector3.zero;
        while (finalPoint == Vector3.zero){
            Vector3 randomPoint = vertices[Random.Range(0, vertices.Length)];
            Vector3 worldPoint = transform.TransformVector(randomPoint);     
            if (worldPoint.y > seaLevel){
                finalPoint = worldPoint;
            }
        }
        return finalPoint;
    }
    public void GenerateTerrain(){
        while (transform.childCount > 0) {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        mesh = new Mesh();
        mesh.MarkDynamic();
        mesh.Clear();

        GetComponent<MeshFilter>().mesh = mesh;
        CreateShape();  
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        WorldController world = GameObject.Find("World").GetComponent<WorldController>();
        if (!world.training){
            PopulateLandscape();
        }
        UpdateMesh();
        isRunning = true;
    }
    public void GenerateTextures()
    {
        Texture2D heightTexture2D = new Texture2D(xSize, xSize); 
        Vector2[] uvs = mesh.uv; // Retrieve the UV coordinates of the mesh
        float uvOffset = 0.001f; // Adjust the offset value as needed
        for (int i = 0; i < uvs.Length; i++)
        {
            Vector2 uv = uvs[i];
            Vector2 offsetUV = uv + new Vector2(uvOffset, uvOffset);
            int x = (int)(offsetUV.x * xSize);
            int y = (int)(offsetUV.y * xSize);
            heightTexture2D.SetPixel(x, y, new Color(0f, vertices[i].y/maxTerrainHeight, 0f, 1f));
        }

        heightTexture2D.filterMode = FilterMode.Trilinear;
        heightTexture2D.Apply(true);   

        heightTexture = heightTexture2D;

    }
    private void UpdateMesh(){

        ColorMesh();
        mesh.colors = colors;
        gameObject.transform.localScale = new Vector3(10, 10, 10);
        GetComponent<MeshCollider>().sharedMesh = mesh;
        mesh.UploadMeshData(false);
        //assign nav mesh area costs based on whether or not there is a path

        // NavMeshSurface navMeshSurface = gameObject.GetComponent<NavMeshSurface>();
        

        // navMeshSurface.BuildNavMesh();
    }
    public float getPointHeight(Vector3 worldPosition, float time){
        if (heightTexture == null){
            return 0f;
        }
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

        Color heightColor = heightTexture.GetPixel((int)(localPosition.x), (int)(localPosition.z));
        float coordHeight = heightColor.g * maxTerrainHeight;

        Vector3 finalPoint = new Vector3(localPosition.x, (float)coordHeight, localPosition.z);
        Vector3 worldPoint = transform.TransformPoint(finalPoint);

        return worldPoint.y;
    }

    public Mesh GetMesh(){
        return mesh;
    }
    public void HighlightVertex(int i, SelectedTool selectedTool){
        Color color = Color.white;
        if (selectedTool == SelectedTool.Path){
            color = pathColor;
        }else if (selectedTool == SelectedTool.RaiseGround){
            color = colors[i] * 1.4f;
        }else if (selectedTool == SelectedTool.LowerGround){
            color = colors[i] * 0.4f;
        }

        Color[] colorCopy = colors.Clone() as Color[];
        colorCopy[i] = color;
        mesh.SetColors(colorCopy);
        mesh.colors = colorCopy;
        GetComponent<MeshCollider>().sharedMesh = mesh;
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        mesh.UploadMeshData(false);
    }
    public void DeleteAtVertex(int i){
        if (landObjects[i] !=  null){
            Destroy(landObjects[i]);
            landObjects[i] = null;
        }
    }
    public void AddPathToVertex(int i){
        if (!mesh){
            mesh = GetComponent<MeshFilter>().mesh;
        }
        colors[i] = pathColor;
        mesh.SetColors(colors);
        mesh.colors = colors;
        GetComponent<MeshCollider>().sharedMesh = mesh;
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (landObjects != null && landObjects.Length > 0  && landObjects[i] !=  null){
            Destroy(landObjects[i]);
            landObjects[i] = null;
        }
        mesh.UploadMeshData(false);
    }
    public void RaiseVertex(int i, float amount){
        mesh = gameObject.GetComponent<MeshFilter>().mesh;

        vertices[i].y = vertices[i].y + amount;
        //Get surrounding verticies
        int[] surroundingVerticies = GetSurroundingVerticies(i);
        foreach (int j in surroundingVerticies){
            if (j >= 0 && j < vertices.Length){
                vertices[j].y = vertices[j].y + amount/2;
            }
        }
        
        mesh.vertices = vertices;
        if (landObjects != null && landObjects.Length > 0  && landObjects[i] !=  null){
          landObjects[i].transform.position  = transform.TransformPoint(vertices[i]);
        }
        mesh.SetVertices(vertices);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.UploadMeshData(false);
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    private int[] GetSurroundingVerticies(int i)
    {
        return new int[4] {i - 1, i + 1, i - xSize, i + xSize};

    }

    void ColorMesh()
    {
        colors = new Color[vertices.Length];
        if (inverted) return;

        MeshFilter waterMeshFilter = (MeshFilter)water.GetComponent("MeshFilter");
        Mesh waterMesh = waterMeshFilter.mesh;
        float seaLevel =   water.GetComponent<OceanGenerator>().GetSeaLevel();
        
        for (int i = 0, z = 0; z < vertices.Length; z++)
        {
            Vector3 normal = mesh.normals[i];
            float slopeAngle = Vector3.Angle(normal, Vector3.up);
            float worldHeight =  transform.TransformVector(vertices[i]).y;
            if (slopeAngle > 35){
                Color greenColor = Color.green;
                greenColor.a = 0.6f;
                colors[i] = greenColor;
            }else if (slopeAngle < 30 && worldHeight < seaLevel + 4f){
                Color blueColor = Color.blue;
                blueColor.a = 0.6f;
                colors[i] = blueColor;
            }  
            else{
                float height = Mathf.InverseLerp(minTerrainHeight, maxTerrainHeight, vertices[i].y);
                Color redColor = Color.red;
                redColor.a = 0.6f;
                colors[i] = redColor;
            }
            i++;
        }
    }
    public void PopulateLandscape(){
        if (inverted) return;

        landObjects = new GameObject[vertices.Length];

        MeshFilter waterMeshFilter = (MeshFilter)water.GetComponent("MeshFilter");
        OceanGenerator oceanGenerator = water.GetComponent<OceanGenerator>();
        Mesh waterMesh = waterMeshFilter.mesh;
        float seaLevel = oceanGenerator.GetSeaLevel();
        
        var localToWorld = transform.localToWorldMatrix;

        float treeSeed = Random.Range(0f, 100f);
        float rockSeed = Random.Range(0f, 100f);
        float bushSeed = Random.Range(0f, 100f);
        
        for (int i = 0, z = 0; z < vertices.Length; z++)
        {
            Vector3 normal = mesh.normals[i];
            float slopeAngle = Vector3.Angle(normal, Vector3.up);
            GameObject landObject = null;
            float rand = Random.Range(0f, 1f);

            float worldHeight =  transform.TransformVector(vertices[i]).y;
            GameObject treeToSpawn = palmTree;

            if (worldHeight > seaLevel + 10){
                treeToSpawn = rand < .2 ? oakTree : palmTree;
            }else if (worldHeight > seaLevel + 5){
                treeToSpawn = rand < .1 ? oakTree : palmTree;
            }
            

            float treePerlinValue = treeCurve.Evaluate(Mathf.PerlinNoise(vertices[i].x + 200*treeSeed, vertices[i].z + 200*treeSeed));
            float rockPerlinValue = rockCurve.Evaluate(Mathf.PerlinNoise(vertices[i].x + 200*rockSeed, vertices[i].z + 200*rockSeed));
            float bushPerlinValue = treeCurve.Evaluate(Mathf.PerlinNoise(vertices[i].x + 200*treeSeed, vertices[i].z + 200*treeSeed));

            if (worldHeight > seaLevel + 5 && slopeAngle < 35){
                if (rand < treeDensity * treePerlinValue){
                    landObject = Instantiate(treeToSpawn);
                }
                else if (rand < treeDensity * treePerlinValue + rockDensity * rockPerlinValue){
                    landObject = Instantiate(rocks[Random.Range(0, rocks.Length)]);
                }
                else if (rand < treeDensity * treePerlinValue + rockDensity * rockPerlinValue + bushDensity * bushPerlinValue){
                    landObject = Instantiate(bushes[Random.Range(0, bushes.Length)]);
                }
            }
            if (landObject != null){
                landObject.transform.position = transform.TransformPoint(vertices[i]);
                landObject.transform.Rotate(0, Random.Range(0, 360), 0, Space.Self);
                landObject.transform.parent = gameObject.transform;
                float scale = Random.Range(0.75f, 0.95f);
                landObject.transform.localScale = new Vector3(scale, scale, scale);
                landObjects[i] = landObject;
            }
            i++;
        }

        // bool playerSpawned = false;
        // while (!playerSpawned){
        //     int rand = Random.Range(0, vertices.Length);
        //     if (landObjects[rand] != null) continue;
        //     Vector3 normal = mesh.normals[rand];
        //     float slopeAngle = Vector3.Angle(normal, Vector3.up);
        //     float worldHeight =  transform.TransformVector(vertices[rand]).y;
        //     if (worldHeight > seaLevel && slopeAngle < 35){
        //         GameObject playerObj = Instantiate(player);
        //         playerObj.transform.position = transform.TransformPoint(vertices[rand]);
        //         playerObj.transform.Rotate(0, Random.Range(0, 360), 0, Space.Self);
        //         playerObj.transform.localScale = new Vector3(.05f, .05f, .05f);
        //         playerObj.transform.parent = gameObject.transform;
        //         player.tag = "Player";
        //         playerSpawned = true;
        //         GameObject.Find("World").GetComponent<InputHandler>().SetPlayer(playerObj);
        //     }
        // }



    }
    private Vector2[] GetOffsetSeed()
    {
        if (seed == 0){
            seed = Random.Range(0, 1000);
        }
        
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
                    
        for (int o = 0; o < octaves; o++) {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            octaveOffsets[o] = new Vector2(offsetX, offsetY);
        }
        return octaveOffsets;
    } 
    private void SetMinMaxHeights(float noiseHeight)
    {
        // Set min and max height of map for color gradient
        if (noiseHeight > maxTerrainHeight)
            maxTerrainHeight = noiseHeight;
        if (noiseHeight < minTerrainHeight)
            minTerrainHeight = noiseHeight;
    }
    private float GenerateHeight(float x, float z, Vector2[] octaveOffsets)
    {
        float amplitude = defaultAmplitude;
        float frequency = defaultFrequency;
        float noiseHeight = 0;

        // loop over octaves
        for (int y = 0; y < octaves; y++)
        {
            float mapZ = z / scale * frequency + octaveOffsets[y].y;
            float mapX = x / scale * frequency + octaveOffsets[y].x;

            //The *2-1 is to create a flat floor level
            float perlinValue = (Mathf.PerlinNoise(mapZ, mapX)) * 2 - 1;
            noiseHeight += heightCurve.Evaluate(perlinValue) * amplitude;
            noiseHeight *= clusterCurve.Evaluate(x/xSize) + clusterCurve.Evaluate(z/zSize);
            frequency *= lacunarity;
            amplitude *= persistence;
        }
        return noiseHeight;
    }
    void CreateShape()
    {
        Vector2[] octaveOffsets = GetOffsetSeed();
        

        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        int vertexOffset = (xSize + 1) * (zSize + 1);
        int vertexIndex = 0;
        uvs = new Vector2[vertices.Length];

        for (int z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float noiseHeight = GenerateHeight(x, z, octaveOffsets);
                if (noiseHeight > maxTerrainHeight)
                    maxTerrainHeight = noiseHeight;
                if (noiseHeight < minTerrainHeight)
                    minTerrainHeight = noiseHeight;
                SetMinMaxHeights(noiseHeight);
                // Front face vertices
                vertices[vertexIndex] = new Vector3(x, noiseHeight, z);
                uvs[vertexIndex] = new Vector2((float)x / xSize, (float)z / zSize);
                vertexIndex++;
            }
        }

        triangles = new int[xSize * zSize * 6];
        int triangleIndex = 0;
        int vert = 0;
        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                if (!inverted){
                    triangles[triangleIndex] = vert + 0;
                    triangles[triangleIndex + 1] = vert + xSize + 1;
                    triangles[triangleIndex + 2] = vert + 1;
                    triangles[triangleIndex + 3] = vert + 1;
                    triangles[triangleIndex + 4] = vert + xSize + 1;
                    triangles[triangleIndex + 5] = vert + xSize + 2;
                }else{
                    triangles[triangleIndex] = vert + 0;
                    triangles[triangleIndex + 1] = vert + 1;
                    triangles[triangleIndex + 2] = vert + xSize + 1;
                    triangles[triangleIndex + 3] = vert + 1;
                    triangles[triangleIndex + 4] = vert + xSize + 2;
                    triangles[triangleIndex + 5] = vert + xSize + 1;
                }


                vert++;
                triangleIndex += 6;
            }
            vert++;
        }

    }

    // Update is called once per frame

}

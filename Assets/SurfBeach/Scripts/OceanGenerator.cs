using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class OceanGenerator : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    Color[] colors;
    int[] triangles;
    public int xSize = 20;
    public int zSize = 20;
    Vector2[] uvs;
    public float amplitude = 20f; // Amplitude of the wave
    public float frequency = 1f; // Frequency of the wave
    public float speed = 1f; // Speed of the wave
    public Vector3 waveDirection = Vector3.right; // Direction of the wave
    public GameObject land;

    [SerializeField] private AnimationCurve resistanceCurve;

    private Mesh landMesh;
    private MeshFilter landMeshFilter;
    private float[] distances;
    private float[] lowerDistances;
    private float[] invertedDistances;
    private float[] depths;
    private float[] slopeAngles;
    private float maximumDepth = 0f;
    private float[] resistances;
    private int[] surfables;
    private float maxDistance = 0f;
    private Color[] resistanceColors;
    private bool isPaused = false;

    private List<int> corners;

    private int[] cornerArray;
    private int[] secondCornerArray;
    private List<float> cornerInfluences = new List<float>();

    private int[] startingPoints;
    private List<int> pointDirections;
    private int sampleNumber;
    
    float centerOutDistance = 2000f; // The distance from the center point
    float perpendicularDistance = 1000f; //

    public Material oceanMaterial;

    public Texture2D resistanceTexture;
    public Texture2D cornerTexture;
    public Texture2D depthTexture;
    public Texture2D slopeTexture;
    public Texture2D waveBreakTexture;
    private bool isRunning = false;

    public float GetSeaLevel(){
        return transform.TransformPoint(new Vector3(0f, 2f, 0f)).y - 10f;
    }
    // Start is called before the first frame update
    void Start()
    {

    }
    public void ResetObjects(){
        mesh = null;
        resistanceTexture = null;
        cornerTexture = null;
        depthTexture = null;
        slopeTexture = null;
        isRunning = false;
    }
    public void GenerateOcean(){
        mesh = new Mesh();
        landMeshFilter = (MeshFilter)land.GetComponent("MeshFilter");
        sampleNumber = 0;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        GetComponent<MeshFilter>().mesh = mesh;
        CreateShape();  
        startingPoints = new int[vertices.Length];
        surfables = new int[vertices.Length];
        corners = new List<int>();
        UpdateMesh();
        isRunning = true;
    }
    public void Update(){
        if (isRunning && slopeTexture == null && landMeshFilter.mesh != null && landMeshFilter.mesh.normals.Length > 0){
            CalculateWaveConstants();
        }
    }
    public void TogglePause(){
        isPaused = !isPaused;
    }
    public void CalculateWaveConstants(){
        SetCorners();
        SetDistances();
        GenerateTextures();
        GenerateWaveBreakTexture();
        oceanMaterial.SetFloat("_MaxDepth", maximumDepth);
    }
    public void GenerateTextures()
    {
        Texture2D texture2D = new Texture2D(xSize, xSize);
        Texture2D depthTexture2D = new Texture2D(xSize, xSize); 
        Texture2D slopeTexture2D = new Texture2D(xSize, xSize); 
        Texture2D cornerTexture2D = new Texture2D(xSize, xSize);

        Color[] colors = mesh.colors; // Cache mesh colors array for efficiency
        Vector2[] uvs = mesh.uv; // Retrieve the UV coordinates of the mesh
        float uvOffset = 0.001f; // Adjust the offset value as needed
        for (int i = 0; i < uvs.Length; i++)
        {
            Vector2 uv = uvs[i];
            Vector2 offsetUV = uv + new Vector2(uvOffset, uvOffset);
            int x = (int)(offsetUV.x * xSize);
            int y = (int)(offsetUV.y * xSize);

            //Resistance
            texture2D.SetPixel(x, y, resistanceColors[i]);
            //Depth
            depthTexture2D.SetPixel(x, y, new Color(0f, depths[i]/maximumDepth, 0f, 1f));
            //Slope
            slopeTexture2D.SetPixel(x, y, new Color(0f, slopeAngles[i] / 360, 0f, 1f));
            //Corner
            x = Mathf.Clamp((int)(offsetUV.x * xSize), 0, xSize - 1);
            y = Mathf.Clamp((int)(offsetUV.y * xSize), 0, xSize - 1);
            uv = uvs[cornerArray[i]];
            offsetUV = uv + new Vector2(uvOffset, uvOffset);
            if (cornerArray[i]!=0){
                cornerTexture2D.SetPixel(x, y, new Color(offsetUV.x, 0, offsetUV.y, 1f));
            }else{
                cornerTexture2D.SetPixel(x, y, new Color(0, 0, 0, 0));
            }

        }
        texture2D.filterMode = FilterMode.Trilinear;
        texture2D.Apply(true);

        depthTexture2D.filterMode = FilterMode.Trilinear;
        depthTexture2D.Apply(true);   

        slopeTexture2D.filterMode = FilterMode.Trilinear;
        slopeTexture2D.Apply(true);

        cornerTexture2D.filterMode = FilterMode.Point;
        cornerTexture2D.Apply(true);
        
        oceanMaterial.SetTexture("_Resistances", texture2D);
        oceanMaterial.SetTexture("_Depths", depthTexture2D);
        oceanMaterial.SetTexture("_Slopes", slopeTexture2D);
        oceanMaterial.SetTexture("_Corners", cornerTexture2D);

        resistanceTexture = texture2D;
        depthTexture = depthTexture2D;
        slopeTexture = slopeTexture2D;
        cornerTexture = cornerTexture2D;

    }
    public void GenerateWaveBreakTexture(){
        Texture2D waveBreakTexture2D = new Texture2D(xSize, xSize);
        Vector2[] uvs = mesh.uv; // Retrieve the UV coordinates of the mesh
        float uvOffset = 0.001f; // Adjust the offset value as needed
        for (int i = 0; i < uvs.Length; i++)
        {
            Vector2 uv = uvs[i];
            Vector2 offsetUV = uv + new Vector2(uvOffset, uvOffset);
            int x = (int)(offsetUV.x * xSize);
            int y = (int)(offsetUV.y * xSize);

            //Wave Break
            (float newHeight, bool isBreaking, float steepness, float waveHeight, float depth) = getPointHeight(transform.TransformPoint(vertices[i]),1);
            waveBreakTexture2D.SetPixel(x, y, new Color(0f, steepness, 0f, 1f));
        }
        waveBreakTexture2D.filterMode = FilterMode.Trilinear;
        waveBreakTexture2D.Apply(true);
        waveBreakTexture = waveBreakTexture2D;
    }

    private void SetDistances(){
        distances = new float[vertices.Length];
        lowerDistances = new float[vertices.Length];
        invertedDistances = new float[vertices.Length];
        depths = new float[vertices.Length];
        landMesh = landMeshFilter.mesh;
        slopeAngles = new float[vertices.Length];
        Vector3[] landVertices = landMesh.vertices;
        Vector3[] landNormals = landMesh.normals;

        for (int i = 0; i < vertices.Length; i++){
            //Raycast in the opposite direction of the wave
            RaycastHit hit;
            Vector3 currentVertex = vertices[i];
            Vector3 worldVertex = transform.TransformPoint(currentVertex);
            int landNormalIndex = ConvertVertexIndex(i);
            Vector3 landNormal = landNormals[landNormalIndex];
            float slopeAngle = Vector3.Angle(landNormal, Vector3.up);
            slopeAngles[i] = slopeAngle;

            if (Physics.Raycast(worldVertex, waveDirection, out hit)){
                //get the disntance from the vertex to the hit point
                float distance = Vector3.Distance(worldVertex, hit.point);
                if (hit.transform.gameObject == land){
                    distances[i] = distance;
                }else{
                    invertedDistances[i] = distance;
                }

                
                if (distance > maxDistance){
                    maxDistance = distance;
                }
            }else{
                distances[i] = 0f;
                invertedDistances[i] = 0f;
            }
            currentVertex.y = currentVertex.y - .7f*amplitude; ;
            worldVertex = transform.TransformPoint(currentVertex);
            float landHeight = land.transform.TransformPoint(landVertices[landNormalIndex]).y;

            depths[i] = worldVertex.y - landHeight;
            if (depths[i] > maximumDepth){
                maximumDepth = depths[i];
            }
            if (landHeight > worldVertex.y){
                lowerDistances[i] = 1f;
            }else{
                if (Physics.Raycast(worldVertex, -waveDirection, out hit)){
                    //get the disntance from the vertex to the hit point
                    float distance = Vector3.Distance(worldVertex, hit.point);
                    lowerDistances[i] = distance;
                }else{
                    lowerDistances[i] = 0f;
                }
            }

        }
        resistances = new float[vertices.Length];
        resistanceColors = new Color[vertices.Length];
        cornerArray = new int[vertices.Length];
        secondCornerArray  = new int[vertices.Length];
        float time = Time.time;
        Vector3 clockwise = new Vector3(waveDirection.z, 0, -waveDirection.x);

         
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];   
            float distance = distances[i];
            float invertedDistance = invertedDistances[i];
            float resistance = 1;         
            if (distance == 0){
                resistance = 1;
            }else if (distance > 100 && distance < 500){
                resistance = 0.05f;
            }else if (distance > 500){
                resistance = distance / maxDistance;
            }else{
                resistance = 1 - resistanceCurve.Evaluate(distance / 100);
            }
            
            float lowerDistance = lowerDistances[i];
            if (lowerDistance > 0){
                if (lowerDistances[i]==1){
                    resistance = resistance * .2f;
                }
            }

            resistances[i] = resistance;

            resistanceColors[i] = Color.Lerp(Color.white, Color.black, resistance);


            float nearestCornerDistance = 9000;
            int nearestCornerIndex = 0;
            float secondNearestCornerDistance = 9000;
            int secondNearestCornerIndex = 0;
            if (i != 0 && i != vertices.Length){
                for (int j = 0; j < corners.Count; j++){
                    //find distance to corner
                    int cornerIndex = corners[j];
                    Vector3 corner = vertices[cornerIndex];
                    float cornerDistance = Vector3.Distance(vertex, corner); 

                    float cornerThreshold = 100;
                    if (cornerInfluences[j] > 1000){
                        cornerThreshold = 100;
                    }else{
                        cornerThreshold = 40 + 60 * cornerInfluences[j]/1000;
                    }

                    float depth = depths[i];

                    float depthResistance = 1;
                    if (depth > 0){
                        depthResistance = (.8f + .2f * depth/maximumDepth);
                    }else{
                        depthResistance = .05f;
                    }
                    if (cornerDistance < cornerThreshold){
                        Vector3 cornerDirection = corner - vertex;
                        //get dot product of corner to vertex and wave direction
                        float dot = Vector3.Dot(cornerDirection.normalized, waveDirection.normalized);
                        float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
                        if (angle > 60){
                            angle = 60;
                        }
                        //if dot is positive, then vertex is on the same side of the wave as the corner
                        if (dot > .4){
                            float cornerDirectionDot = Vector3.Dot(cornerDirection, clockwise);
                            // || (pointDirections[j] == 0 && cornerDirectionDot < 0)
                            if ((pointDirections[j] == 1 && cornerDirectionDot < 0.1) || (pointDirections[j] == 0 && cornerDirectionDot > -0.1)){
                                if (cornerDistance < nearestCornerDistance){
                                    nearestCornerDistance = cornerDistance;
                                    nearestCornerIndex = cornerIndex;
                                    secondNearestCornerDistance = nearestCornerDistance;
                                    secondNearestCornerIndex = nearestCornerIndex;
                                }if (cornerDistance < secondNearestCornerDistance && cornerDistance > nearestCornerDistance){
                                    secondNearestCornerDistance = cornerDistance;
                                    secondNearestCornerIndex = cornerIndex;
                                }
                            }
                        }
                    }
                }
            }
            if (nearestCornerDistance < 9000){;
                cornerArray[i] = nearestCornerIndex;
            }   
        }


    }
    private void SetCorners(){
        //get center point of mesh
        Vector3 center = new Vector3(1000f, gameObject.transform.position.y+10, 1000f);
        Vector3 targetPoint = center + waveDirection.normalized * centerOutDistance;
        Vector3 perpendicularDirection = new Vector3(-waveDirection.z, 0f, waveDirection.x).normalized;
        Vector3 perpendicularPoint = targetPoint + perpendicularDirection * perpendicularDistance;

        float lastDistance = 0f;
        Vector3 lastPoint = perpendicularPoint;
        List<Vector3> points = new List<Vector3>();
        pointDirections = new List<int>();
        for (int i=0;i<perpendicularDistance*2;i++){
            Vector3 point = perpendicularPoint - perpendicularDirection * i;
            RaycastHit hit;
            float distance = 0f;
            if (Physics.Raycast(point, -waveDirection, out hit, 4000f)){
                //get the disntance from the vertex to the hit point
                distance = Vector3.Distance(point, hit.point);
                float change = Mathf.Abs(distance - lastDistance);
                if (Mathf.Abs(distance - lastDistance) > 100f){
                    if (lastDistance == 0f){
                        points.Add(hit.point);
                        pointDirections.Add(1);
                        cornerInfluences.Add(change);
                    }
                    if (distance > lastDistance){
                        points.Add(lastPoint);
                        pointDirections.Add(0);
                        cornerInfluences.Add(change);
                    }else{
                        points.Add(hit.point);
                        pointDirections.Add(1);
                        cornerInfluences.Add(change);
                    }
                }
                lastPoint = hit.point;
            }else{
                if (lastDistance > 0f){
                    points.Add(lastPoint);
                    pointDirections.Add(0);
                    cornerInfluences.Add(1000);
                }
            }
            lastDistance = distance;
        }
        //find nearest mesh vertex to hit
        for (int i=0;i<points.Count;i++){
            Vector3 point = points[i];
            float minDistance = 100000f;
            int minIndex = 0;
            for (int j=0;j<vertices.Length;j++){
                Vector3 vertex = vertices[j];
                //get world location of vertex
                Vector3 worldVertex = transform.TransformPoint(vertex);
                float distance = Vector3.Distance(point, worldVertex);
                if (distance < minDistance){
                    minDistance = distance;
                    minIndex = j;
                }
            }
            corners.Add(minIndex);
        }
    }
    private void UpdateMesh(){
        mesh.Clear();
        GetComponent<MeshFilter>().mesh = mesh;

        SetTriangles();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        //ColorMesh();
        mesh.colors = colors;

        //GetSurfables();

        mesh.UploadMeshData(false);
        gameObject.transform.localScale = new Vector3(5, 10, 5);

    }

    public int[] getSurroundingVertexIndecies(int index){

        int[] surroundingVertexIndecies = new int[8];

        surroundingVertexIndecies[0] = index - 1;
        surroundingVertexIndecies[1] = index + 1;
        surroundingVertexIndecies[2] = index + xSize;
        surroundingVertexIndecies[3] = index + xSize - 1;
        surroundingVertexIndecies[4] = index + xSize + 1;
        surroundingVertexIndecies[5] = index - xSize;
        surroundingVertexIndecies[6] = index - xSize - 1;
        surroundingVertexIndecies[7] = index - xSize + 1;
        
        return surroundingVertexIndecies;
    }
    
    void SetTriangles(){
        triangles = new int[xSize * zSize * 6];
        int vert = 0;
        int tris = 0;

        for (int z = 0; z < zSize; z++)
        {
            for(int x = 0; x < xSize; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }
    }
    int ConvertVertexIndex(int largerResolutionVertexIndex)
{
        int largerResolutionSize = 400;
        int smallerResolutionSize = 200;

        int largerResolutionX = largerResolutionVertexIndex % (largerResolutionSize + 1);
        int largerResolutionZ = largerResolutionVertexIndex / (largerResolutionSize + 1);

        int smallerResolutionX = largerResolutionX / 2;
        int smallerResolutionZ = largerResolutionZ / 2;

        int smallerResolutionVertexIndex = smallerResolutionZ * (smallerResolutionSize + 1) + smallerResolutionX;

        return smallerResolutionVertexIndex;
    }
    void CreateShape()
    { 
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        uvs = new Vector2[vertices.Length];
        float time = Time.time;
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for(int x = 0; x <= xSize; x++)
            {
                vertices[i] = new Vector3(x, 1, z);
                uvs[i] = new Vector2((float)x / xSize, (float)z / zSize);
                i++;
            }
        }
    }
    public (float, bool, float, float, float) getPointHeight(Vector3 worldPosition, float time){
        if (resistanceTexture == null){
            return (0f, false, 0f, 0f, 0f);
        }
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

        Color resistanceColor = resistanceTexture.GetPixel((int)(localPosition.x), (int)(localPosition.z));
        float coordResistance = 1f -  resistanceColor.r;

        
        Color depthColor = depthTexture.GetPixel((int)(localPosition.x), (int)(localPosition.z));

        float depthResistance = 1f;
        if (depthColor.g > 0){
            depthResistance = (.8f + .2f * depthColor.g);
        }else{
            depthResistance = .05f;
        }    

        float relativeX = (localPosition.x * frequency * waveDirection.x + (time * speed))  + (localPosition.z * frequency * waveDirection.z + time * speed) ;
        float totalHeight = 1f + coordResistance * depthResistance * (Mathf.Sin(relativeX) + .3f *Mathf.Abs(Mathf.Sin(relativeX))) * amplitude;
        float waveHeight = coordResistance * depthResistance * amplitude;
        Color cornerColor = cornerTexture.GetPixel((int)(localPosition.x), (int)(localPosition.z));
        if (cornerColor.a != 0){    
            Vector3 cornerPosition = new Vector3(cornerColor.r, 0, cornerColor.b);
            Vector3 normalizedPosition = new Vector3(localPosition.x / xSize, 0, localPosition.z / xSize);
            Vector3 cornerDirection = cornerPosition - normalizedPosition;
            float cornerDistance = Vector3.Distance(normalizedPosition, cornerPosition);

            float directionDot = Vector3.Dot(cornerDirection.normalized, waveDirection.normalized);
                        
            float angle = Mathf.Acos(directionDot) * Mathf.Rad2Deg;
            if (angle > 0 && angle < 70){
                float waveletX = (cornerDistance * xSize * frequency - (time  * speed * 2));                                        
                // totalHeight = cornerDistance * 50;
                totalHeight +=  depthResistance * (1 - angle / 70) * (Mathf.Sin(waveletX) + .3f * Mathf.Abs(Mathf.Sin(waveletX))) * amplitude;   
                waveHeight +=  depthResistance * (1 - angle / 70) * amplitude;                                
            }
        }else{
            
        }

        Color landSlope = slopeTexture.GetPixel((int)(localPosition.x), (int)(localPosition.z));
        float landSlopeAngle = landSlope.g * 360 * Mathf.Deg2Rad;
        float waveLength = speed / frequency;
        float depth = depthColor.g * maximumDepth;
 
        float steepness = waveHeight / depth;
        Vector3 finalPoint = new Vector3(localPosition.x, (float)totalHeight, localPosition.z);
        Vector3 worldPoint = transform.TransformPoint(finalPoint);

        bool isSurfable = false;
        bool isBreaking = false;
        bool isBroken = false;
        if (steepness > .14f){
            steepness= .5f;
            isBroken = true;
        }else if (steepness > .1f){
            steepness= .7f;
            isBreaking = true;
        }else if (steepness > .07f){
            steepness= 1;
            isSurfable = true;
        }else if (steepness > .05f){
            steepness= .5f;
        }else if(steepness > .03f){
            steepness = .3f;
        }else if(steepness > .01f){
            steepness = .3f;
        }else{
            steepness = 0;
        }
        if (waveHeight < .3f){
            steepness = steepness * waveHeight;
        }
        bool isRidable = false;
        if (isSurfable){
            if (worldPoint.y > worldPosition.y){
                if (waveHeight > .3f){
                    isRidable = true;
                }
            }
        }
        return (worldPoint.y, isRidable, steepness, waveHeight, depth);
    }
    // Update is called once per frame

}

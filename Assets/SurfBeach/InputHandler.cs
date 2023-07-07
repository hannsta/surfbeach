using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum SelectedTool{
    RaiseGround,
    LowerGround,
    Path,
    Delete,
    Dock,
    None
}

public class InputHandler : MonoBehaviour
{

    public float zoomSensitivity = .05f;
    public float panSensitivity = .75f;
    public float rotateSensitivity = 0.2f;
    private bool rightClickActive = false;
    private bool middleClickActive = false;

    private SelectedTool selectedTool = SelectedTool.None;
    private Vector3 focusPoint;

    public Color selectedColor = Color.red;
    public Color deselectedColor = Color.white;

    public GameObject UI;

    public GameObject[] UIButtons;

    private int currentHighlightedIndex = 0;

    private GameObject selectedGameObject;
    private GameObject hoverGameObject;

    public Color outlineColor = Color.red;
    private GameObject hoverPreview;
    private GameObject selectionPreview;

    private GameObject player = null;
    public GameObject playerMoveIcon = null;
    public GameObject dockPrefab;
    public GameObject previewObject;
    public OceanGenerator ocean;
    public float isRotating = 0f;
    private float rightClickStartTime = 0f;

    public GameObject mainCamera;
    public void Start(){
        SetFocus();
        //create input action
        InputAction myAction = new InputAction("MyAction");
        //bind to q and e on hold
        myAction.AddCompositeBinding("Axis").
            With("Positive", "<Keyboard>/e").
            With("Negative", "<Keyboard>/q");
        myAction.Enable();
    }
    public GameObject getSelectedGameObject(){
        return selectedGameObject;
    }
    public void SetPlayer(GameObject player){
        Debug.Log("Setting player");
        this.player = player;
    }
    private void SetFocus(){
        RaycastHit hit;
        if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit)){
            focusPoint = hit.point;
        }
    }
    public void SelectPlayer(){
        Debug.Log("here");
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null){
            SelectObject(player);
        }
    }
    public void BindCameraBounds(){
        if (mainCamera.transform.position.y < 50){
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, 50, mainCamera.transform.position.z);
        }
        if (mainCamera.transform.position.y > 2500){
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, 2500, mainCamera.transform.position.z);
        }
        if (mainCamera.transform.position.x < -1000){
            mainCamera.transform.position = new Vector3(-1000, mainCamera.transform.position.y, mainCamera.transform.position.z);
        }
        if (mainCamera.transform.position.x > 3000){
            mainCamera.transform.position = new Vector3(3000, mainCamera.transform.position.y, mainCamera.transform.position.z);
        }
        if (mainCamera.transform.position.z < -1000){
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, -1000);
        }
        if (mainCamera.transform.position.z > 3000){
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, 3000);
        }

    }
    public void DoMouseMove(InputAction.CallbackContext context){
        if (rightClickActive){
            if (focusPoint == Vector3.zero){
                SetFocus();
            }
            Vector2 mouseDelta = context.ReadValue<Vector2>();
            Debug.DrawLine(focusPoint, focusPoint + Vector3.up * 100, Color.red, 100f);

            Vector3 clockwise = Vector3.Cross(mainCamera.transform.forward, Vector3.up);

            //rotate the camera around the focus point
            mainCamera.transform.RotateAround(focusPoint, Vector3.up, mouseDelta[0] * rotateSensitivity);
            mainCamera.transform.RotateAround(focusPoint, clockwise, mouseDelta[1] * rotateSensitivity);
            mainCamera.transform.LookAt(focusPoint);

        }
        if (middleClickActive){
            Vector2 mouseDelta = context.ReadValue<Vector2>();
            mainCamera.transform.position += -1f * mainCamera.transform.right * mouseDelta[0] * panSensitivity;
            mainCamera.transform.position += -1f * mainCamera.transform.up * mouseDelta[1] * panSensitivity;
            
            SetFocus();
            //mainCamera.transform.LookAt(focusPoint);
        }
        BindCameraBounds();
            //get mouse position
            Camera canvasCamera = GameObject.Find("CanvasCamera").GetComponent<Camera>();
            Ray ray = Camera.main.ScreenPointToRay( Mouse.current.position.ReadValue() );
            RaycastHit hit;
            if( Physics.Raycast( ray, out hit) )
            {
                GameObject obj = hit.collider.gameObject;

                if (selectedTool == SelectedTool.None && obj.name != "Map"){
                    return;
                }
                
                if (obj.name == "Map" && selectedTool != SelectedTool.None){
                    TerrainGenerator terrain = obj.GetComponent<TerrainGenerator>();
                    
                    Mesh mesh = terrain.GetMesh();
                    int index = GetClosestVertex(hit, mesh.triangles);
                    if (index == currentHighlightedIndex){
                        return;
                    }
                    else{
                        if (selectedTool == SelectedTool.Dock && hoverPreview != null){
                                if (hoverPreview.GetComponent<Outline>() == null){
                                    hoverPreview.AddComponent<Outline>();
                                }
                                hoverPreview.transform.position =  hit.point;
                            
                            float seaLevel = ocean.GetSeaLevel();
                            if (hit.point.y > seaLevel && hit.point.y < seaLevel + 3.0f){
                                hoverPreview.GetComponent<Outline>().OutlineColor = Color.green;
                            }else{
                                hoverPreview.GetComponent<Outline>().OutlineColor = Color.red;
                            }

                        }else{
                            terrain.HighlightVertex(index, selectedTool);
                            currentHighlightedIndex = index;
                        }


                    }
                }
            }
        
    }   
    public void DoZoom(InputAction.CallbackContext context){
        //move game object in the opposite direction of the camera
        // Debug.Log("Zooming");


        Vector2 zoomLevel = context.ReadValue<Vector2>();
        mainCamera.transform.position += mainCamera.transform.forward * zoomLevel[1]  * zoomSensitivity;
        BindCameraBounds();

    }
    public void DoRightClick(InputAction.CallbackContext context){
        if (context.started){
            rightClickActive = true;
            rightClickStartTime = Time.time;
        }
        if (context.canceled){
            if (Time.time - rightClickStartTime < 1f){
                if (selectedGameObject != null && selectedGameObject == player){
                    //move player navmesh agent to the clicked location
                    Ray ray = Camera.main.ScreenPointToRay( Mouse.current.position.ReadValue() );
                    RaycastHit hit;
                    if( Physics.Raycast( ray, out hit, 3000f, LayerMask.GetMask("Terrain")) )
                    {
                        GameObject obj = hit.collider.gameObject;
                        if (obj.name == "Map"){
                            player.GetComponent<NavMeshAgent>().SetDestination(hit.point);
                            GameObject moveIcon = Instantiate(playerMoveIcon, hit.point, Quaternion.identity, obj.transform);
                            
                            //play moveIcon animation
                            moveIcon.GetComponent<Animation>().Play();

                            Destroy(moveIcon, .5f);
                        }
                    }
                }
            }
            rightClickActive = false;
        }
    }
    public void DoCameraMove(InputAction.CallbackContext context){

    }
    public void SelectObject(GameObject obj){
        if (selectedGameObject != null){
            Destroy(selectedGameObject.GetComponent<Outline>());
        }
        GameObject selectionMenu = GameObject.Find("SelectionMenu");
        if (selectionPreview != null){
            Destroy(selectionPreview);
        }
        if (obj.name.Contains("bush") || obj.name.Contains("rock")){
            selectionPreview = Instantiate(obj, selectionMenu.transform);   
            selectionPreview.transform.localScale = new Vector3(70,70,70);
            selectionPreview.transform.localPosition = new Vector3(-65,-60,-43);
        }
        else if (obj.name.Contains("Man")){
            selectionPreview = Instantiate(obj);   
            Destroy(selectionPreview.GetComponent<NavMeshAgent>());
            selectionPreview.transform.parent = selectionMenu.transform;
            selectionPreview.transform.localScale = new Vector3(5,5,5);
            selectionPreview.transform.localPosition = new Vector3(-65,-90,-43);

        }else{
            selectionPreview = Instantiate(obj, selectionMenu.transform);   
            selectionPreview.transform.localScale = new Vector3(35,35,35);
            selectionPreview.transform.localPosition = new Vector3(-65,-90,-43);
        }
        selectionPreview.layer = 5;
        selectedGameObject = obj;
        if (selectedGameObject.GetComponent<Outline>() == null){
            selectedGameObject.AddComponent<Outline>();
        }
        selectedGameObject.GetComponent<Outline>().OutlineColor = outlineColor;
        selectedGameObject.GetComponent<Outline>().OutlineWidth = 5f;
        selectedGameObject.GetComponent<Outline>().OutlineColor = selectedColor;
        selectedGameObject.GetComponent<Outline>().enabled = true;

        GameObject selectionLabel = GameObject.Find("SelectionLabel");
        selectionLabel.GetComponent<TextMeshProUGUI>().text = obj.name.Replace("(Clone)","").Trim();;
    }
    public void DoLeftClick(InputAction.CallbackContext context){
        if (context.started){

            // get the camera called canvascamera
            
            Camera canvasCamera = GameObject.Find("CanvasCamera").GetComponent<Camera>();

            Ray UIray = canvasCamera.ScreenPointToRay( Mouse.current.position.ReadValue() );


            RaycastHit UIhit;
            
            if( Physics.Raycast( UIray, out UIhit) )
            {
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay( Mouse.current.position.ReadValue() );

            RaycastHit hit;
            //user terrain mask unless its delete, then do not mask raycast layers
            int layerMask = LayerMask.GetMask("Terrain");
            if (selectedTool == SelectedTool.Delete || selectedTool == SelectedTool.None){
                layerMask = Physics.AllLayers;
            }
            if( Physics.Raycast( ray, out hit, 3000f, layerMask) )
            {
                GameObject obj = hit.collider.gameObject;
                if (selectedTool == SelectedTool.None && obj.name != "Map"){
                    SelectObject(obj);
                }
                if (obj.name == "Map"){
                    TerrainGenerator terrain = obj.GetComponent<TerrainGenerator>();
                    Mesh mesh = terrain.GetMesh();
                    int index = GetClosestVertex(hit, mesh.triangles);
                    
                    if (selectedTool == SelectedTool.RaiseGround){
                        terrain.RaiseVertex(index, .25f);
                    }
                    if (selectedTool == SelectedTool.LowerGround){
                        terrain.RaiseVertex(index, -.25f);
                    }
                    if (selectedTool == SelectedTool.Path){
                        terrain.AddPathToVertex(index);
                    }
                    if (selectedTool == SelectedTool.Delete){
                        terrain.DeleteAtVertex(index);
                    }
                    if (selectedTool == SelectedTool.Dock){
                        if (hoverPreview.gameObject.transform.position.y > ocean.GetSeaLevel() && hoverPreview.gameObject.transform.position.y < ocean.GetSeaLevel() + 3.0f){
                            Destroy(hoverPreview.GetComponent<Outline>());
                            hoverPreview.GetComponent<QuestObject>().isActive = true;
                            hoverPreview = null;
                            selectedTool = SelectedTool.None;
                        }

                    }
                    if (selectedGameObject != null){
                        Destroy(selectedGameObject.GetComponent<Outline>());
                    }
                    selectedGameObject = null;
                    Destroy(selectionPreview);
                    GameObject selectionLabel = GameObject.Find("SelectionLabel");
                    selectionLabel.GetComponent<TextMeshProUGUI>().text = "Select an object";

                }
                else{
                    Debug.Log("Hit " + obj.name);
                }

                
            }
        }
    }
    private void DeselectAll(){
        if (hoverPreview != null){
            Destroy(hoverPreview);
        }
        foreach (GameObject button in UIButtons){
            button.GetComponentInChildren<Image>().color = deselectedColor;
        }
    }
    public void ToggleDelete(){
        DeselectAll();
        if (selectedTool == SelectedTool.Delete){
            selectedTool = SelectedTool.None;
            return;
        }
        selectedTool = SelectedTool.Delete;
        GameObject.Find("DeleteToggle").GetComponentInChildren<Image>().color = selectedColor;
    }
    public void ToggleLowerGround(){
        DeselectAll();
        if (selectedTool == SelectedTool.LowerGround){
            selectedTool = SelectedTool.None;
            return;
        }
        selectedTool = SelectedTool.LowerGround;
        GameObject.Find("LowerGroundToggle").GetComponentInChildren<Image>().color = selectedColor;
    }
    public void ToggleRaiseGround(){
        DeselectAll();
        if (selectedTool == SelectedTool.RaiseGround){
            selectedTool = SelectedTool.None;
            return;
        }
        selectedTool = SelectedTool.RaiseGround;
        GameObject.Find("RaiseGroundToggle").GetComponentInChildren<Image>().color = selectedColor;
    }
    public void ToggleDock(){
        DeselectAll();
        if (selectedTool == SelectedTool.Dock){
            selectedTool = SelectedTool.None;
            return;
        }
        selectedTool = SelectedTool.Dock;
        GameObject.Find("DockToggle").GetComponentInChildren<Image>().color = selectedColor;
        hoverPreview = Instantiate(dockPrefab);
    }
    public void TogglePath(){
        DeselectAll();
        if (selectedTool == SelectedTool.Path){
            selectedTool = SelectedTool.None;
            return;
        }
        selectedTool = SelectedTool.Path;
        GameObject.Find("PathToggle").GetComponentInChildren<Image>().color = selectedColor;

    }
     public static int GetClosestVertex(RaycastHit aHit, int[] aTriangles)
        {
            var b = aHit.barycentricCoordinate;
            int index = aHit.triangleIndex * 3;
            if (aTriangles == null || index < 0 || index + 2 >= aTriangles.Length)
                return -1;
            if (b.x > b.y)
            {
                if (b.x > b.z)
                    return aTriangles[index]; // x
                else
                    return aTriangles[index + 2]; // z
            }
            else if (b.y > b.z)
                return aTriangles[index + 1]; // y
            else
                return aTriangles[index + 2]; // z
        }

    public void DoMiddleClick(InputAction.CallbackContext context){
        if (context.started){
            middleClickActive = true;
        }
        if (context.canceled){
            middleClickActive = false;
        }
    }

    public void Update(){
        if (Keyboard.current.qKey.isPressed){
            hoverPreview.transform.rotation = hoverPreview.transform.rotation * Quaternion.Euler(0f, 0f, 1f);
        }
        if (Keyboard.current.eKey.isPressed){
            hoverPreview.transform.rotation = hoverPreview.transform.rotation * Quaternion.Euler(0f, 0f, -1f);
        }
        // // Debug.Log(moving);
        // // Debug.Log(movement);
        
        // if (moving){
        //     m_character.HandleCharacterMovement(movement,jumping, sprinting);
        // }else{
        //     m_character.HandleCharacterMovement(new Vector3(0f,0f,0f), jumping, sprinting);
        // }
        // if (looking){
        //     m_character.doLookHorizontal(LookDelta.x);
        //     m_character.doLookVertical(-LookDelta.y);
        // }
        // if (jumping) {
        //     jumping = false;
        // }
    }

}

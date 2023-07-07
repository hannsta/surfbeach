using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Floater : MonoBehaviour
{
    private OceanGenerator ocean;
    private TerrainGenerator terrain;
    public bool isRiding = false;
    public int rideStreak = 0;

    public float currentHeight = 0f;
    public float lastHeight = 0f;
    public float steepness = 0f;
    public float waveHeight = 0f;
    public float depth = 0f;

    public float strength = 100f;
    // Start is called before the first frame update
    void Start()
    {
        //get ocean generator
        ocean = GameObject.Find("Ocean").GetComponent<OceanGenerator>();
        terrain = GameObject.Find("Map").GetComponent<TerrainGenerator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (ocean == null){
            ocean = GameObject.Find("Ocean").GetComponent<OceanGenerator>();
            terrain = GameObject.Find("Map").GetComponent<TerrainGenerator>();
        }else{


            lastHeight = currentHeight;
            (currentHeight, isRiding, steepness, waveHeight, depth) = ocean.getPointHeight(gameObject.transform.position, Time.timeSinceLevelLoad);
            float landHeight = terrain.getPointHeight(gameObject.transform.position, Time.timeSinceLevelLoad);
            if (depth<=0f){

                gameObject.transform.position = new Vector3(gameObject.transform.position.x, landHeight, gameObject.transform.position.z);

            }else{
                
                gameObject.transform.position = new Vector3(gameObject.transform.position.x, currentHeight, gameObject.transform.position.z);

            }
            if (isRiding){
                rideStreak++;
                gameObject.GetComponent<MeshRenderer>().material.color = Color.green;
            }else{
                rideStreak = 0;
                gameObject.GetComponent<MeshRenderer>().material.color = Color.gray;
            }

          
        }
    }
}

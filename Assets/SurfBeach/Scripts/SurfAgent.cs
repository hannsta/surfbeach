using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;
public class SurfAgent : Agent
{
    public Floater floater;
    public OceanGenerator ocean;
    public TerrainGenerator terrain;
    public WorldController world;
    private int stepCount = 0;
    private int episodeCount = 0;

    private float personalScore = 0;
    private InputAction m_MoveUpAction;
    private InputAction m_MoveLeftAction;
    public void Start(){
        loadRelatives();
    }
    private void loadRelatives(){
        floater = gameObject.GetComponent<Floater>();
        ocean = GameObject.Find("Ocean").GetComponent<OceanGenerator>();
        terrain = GameObject.Find("Map").GetComponent<TerrainGenerator>();
        world = GameObject.Find("World").GetComponent<WorldController>();
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(gameObject.transform.position);
        sensor.AddObservation(ocean.waveBreakTexture);
        sensor.AddObservation(ocean.waveDirection);
        sensor.AddObservation(floater.isRiding);
        sensor.AddObservation(floater.waveHeight);
        sensor.AddObservation(floater.depth);
        sensor.AddObservation(floater.steepness);
        sensor.AddObservation(floater.currentHeight);
        sensor.AddObservation(floater.lastHeight);   
        sensor.AddObservation(floater.strength); 
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        int moveX = actions.DiscreteActions[1];
        int moveZ = actions.DiscreteActions[0];

        switch (moveX)
        {
            case 0: moveX = 0; break;
            case 1: moveX = 1; break;
            case 2: moveX = -1; break;
        }
        switch (moveZ)
        {
            case 0: moveZ = 0; break;
            case 1: moveZ = 1; break;
            case 2: moveZ = -1; break;
        }

        bool isFloating = floater.depth > 0f;

        float moveSpeed = 20f;
        if (isFloating){
            moveSpeed = 5f;
        }
        Vector3 position = gameObject.transform.position;

        gameObject.transform.position += new Vector3(-moveX, 0f, moveZ) * Time.deltaTime * moveSpeed;

        manageRewards(position, isFloating);
        

    }
    public void manageRewards(Vector3 position, bool isFloating){
        if (position.x < 0 || position.x > 2000 || position.z > 2000 || position.z < 0){
            AddReward(-500f);
            Debug.Log("End Episode - out of bounds");
            EndEpisode();
        }
        if (position.x < 100 || position.x > 1900 || position.z > 1900 || position.z < 100){
            AddReward(-1f);
        }
        if (floater.depth > 15f){
            AddReward(-0.1f);
        }
        floater.strength = isFloating ? floater.strength - 0.1f : floater.strength + 0.1f;
        if (floater.strength < 0f){
            AddReward(-200f);
            Debug.Log("End Episode - strength");
            EndEpisode();
        }
        if (floater.strength > 500f){
            floater.strength = 500f;
        }
        if (floater.isRiding){
            float reward = 3f * floater.waveHeight;
            if (floater.rideStreak > 0){
                AddReward(+reward*reward * floater.rideStreak);
                world.score += reward * floater.rideStreak * Time.deltaTime;
                personalScore += reward * floater.rideStreak * Time.deltaTime;
            }else{
                AddReward(+reward*reward);
                world.score += reward * Time.deltaTime;
                personalScore += reward * Time.deltaTime;
            }
            float totalReward = GetCumulativeReward();
            if (personalScore > 1000){
                Debug.Log("End Episode - total reward");
                EndEpisode();
            }
        }
    
        if (this.stepCount > 600){
            Debug.Log("End Episode - step count");
            EndEpisode();
        }
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 0;
        discreteActions[1] = 0;
        if (Keyboard.current.wKey.isPressed){
            discreteActions[1] = 1;
        }
        if (Keyboard.current.sKey.isPressed){
            discreteActions[1] = 2;
        }
        if (Keyboard.current.aKey.isPressed){
            discreteActions[0] = 2;
        }
        if (Keyboard.current.dKey.isPressed){
            discreteActions[0] = 1;
        }
    }
    public override void OnEpisodeBegin()
    {

        //random integer etween 500 and 1500
        // float x  = Random.Range(350, 1650);
        // float y  = Random.Range(350, 1650);
        if (!terrain || !ocean || !floater || !world){
            loadRelatives();
        }
        if (world.training){
            gameObject.transform.position = terrain.GetRandomPoint();
        }else{
            gameObject.transform.position = world.spawnPoint;
        }
        this.SetReward(0f);
        floater.strength = 500f;
        floater.isRiding = false;

        stepCount = 0;
        world.episodeCount++;
    }
}

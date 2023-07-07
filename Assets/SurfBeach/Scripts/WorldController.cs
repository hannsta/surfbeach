using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldController : MonoBehaviour
{
    public float speed = 1f;
    public float amplitude = 1f;
    public float frequency = 1f;
    public float score = 0;
    public int surfers = 0;

    public Vector3 waveDirection = new Vector3(1f, 0f, 1f);
    public int seed;

    public TerrainGenerator terrain;
    public TerrainGenerator invertedTerrain;
    public OceanGenerator ocean;
    public Material oceanMaterial;

    public bool training = false;

    public int episodeCount = 0;
    
    private float lastGenTime = 0f;


    public GameObject surferPrefab;
    public float spawnRate = 1f;
    public float lastSpawnTime = 0f;

    public Vector3 spawnPoint;

    public void Generate(bool generateTerrain){
        //if last gen time within 1 sec of current time do nothing
        if (lastGenTime > 0 && Time.timeSinceLevelLoad - lastGenTime < 1f){
            return;
        }
        lastGenTime = Time.timeSinceLevelLoad;
        ocean.speed = speed;
        ocean.amplitude = amplitude;
        ocean.frequency = frequency;
        ocean.waveDirection = waveDirection;

        oceanMaterial.SetFloat("_Speed", speed);
        oceanMaterial.SetFloat("_Amplitude", amplitude);
        oceanMaterial.SetFloat("_Frequency", frequency);
        oceanMaterial.SetVector("_WaveDirection", waveDirection);

        terrain.seed = seed;
        invertedTerrain.seed = seed;

        if (generateTerrain){
            terrain.GenerateTerrain();
            invertedTerrain.GenerateTerrain();
        }
        ocean.GenerateOcean();
    }
    public void ReGenerate(bool generateTerrain){
        speed = Random.Range(0.4f, 0.6f);
        amplitude = Random.Range(0.4f, 0.6f);
        frequency = Random.Range(0.6f, .8f);

        waveDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        waveDirection.Normalize();
        
        seed = Random.Range(0, 1000000);
        ocean.ResetObjects();
        Generate(generateTerrain);
    }
    public void UpdateOcean(){
        ocean.ResetObjects();
        Generate(false);
    }

    
    // Update is called once per frame
    void Start()
    {

        Generate(true);

        if (training){
            return;
        }

        Quests quests = GetComponent<Quests>();
        quests.DockQuest();

    }

    void Update()
    {
        if (training){
            if (episodeCount % 100 == 0){
                Debug.Log("Episode: " + episodeCount);
            }
            if (episodeCount >= 500){
                episodeCount = 0;
                ReGenerate(true);
                episodeCount++;
            }else if (episodeCount % 100 == 0){
                ReGenerate(false);
                episodeCount++;
            }
        }else{
            
            if (spawnPoint != null && spawnPoint != Vector3.zero){
                if (Time.timeSinceLevelLoad - lastSpawnTime > 45f){
                    lastSpawnTime = Time.timeSinceLevelLoad;
                    GameObject surfer = Instantiate(surferPrefab, spawnPoint, Quaternion.identity);
                    surfer.transform.parent = transform;
                    Destroy(surfer, 360f);
                    surfers++;
                    
                }
            }

        }

    }
}

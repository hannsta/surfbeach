using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quests : MonoBehaviour
{
    Quest dockQuest;
    public GameObject dock;
    public WorldController world;

    // Start is called before the first frame update
    void Start()
    {
        world = GameObject.Find("World").GetComponent<WorldController>();
    }
    public void OnDockComplete(GameObject dock){
        dockQuest.CompleteQuest();
        world.spawnPoint = dock.transform.position;
    }
        
    public void DockQuest(){
        dockQuest = gameObject.AddComponent<Quest>();
        dockQuest.questName = "Place a Dock on the Beach";
        dockQuest.StartQuest();
    }
    // Update is called once per frame
    void Update()
    {
        if (dockQuest && !dockQuest.isComplete && GameObject.FindGameObjectsWithTag("Dock").Length > 0){
            GameObject dock = GameObject.FindGameObjectsWithTag("Dock")[0];
            QuestObject questObject = dock.GetComponent<QuestObject>();
            if (questObject!=null && questObject.isActive){
                OnDockComplete(dock);
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quest : MonoBehaviour
{
    // Start is called before the first frame update
    public string questName;
    public string description;
    public string completeText;
    public bool isComplete = false;
    public bool isActive = false;
    void Start()
    {

    }
    public void StartQuest(){
        UIHandler ui = GameObject.Find("UI").GetComponent<UIHandler>();
        ui.QuestPopup(questName,15f);
    }
    public void CompleteQuest(){
        UIHandler ui = GameObject.Find("UI").GetComponent<UIHandler>();
        ui.QuestPopup(questName+": Completed!",15f);
        isComplete = true;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}

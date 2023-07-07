using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIHandler : MonoBehaviour
{
    public TextMeshProUGUI speed;
    public TextMeshProUGUI amplitude;
    public TextMeshProUGUI frequency;
    public TextMeshProUGUI score;
    public TextMeshProUGUI surfers;
    public GameObject pointer;

    public GameObject questPopup;

    public WorldController world;
        // Start is called before the first frame update
    void Start()
    {
        
    }
    public void ReGenerate(){
        world.ReGenerate(true);
    }
    public void UpdateWeather(){
        world.ReGenerate(false);
    }
    public void UpdateOcean(){
        world.UpdateOcean();
    }
    public void QuestPopup(string text, float openTime){
        questPopup.SetActive(true);
        questPopup.GetComponentInChildren<TextMeshProUGUI>().text = text;
        Invoke("CloseQuestPopup", openTime);
    }
    public void CloseQuestPopup(){
        questPopup.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
        speed.text = world.speed.ToString() + " ft/s";
        amplitude.text = world.amplitude.ToString() + " ft";
        frequency.text = world.frequency.ToString() + " s";
        score.text = world.score.ToString();
        surfers.text = world.surfers.ToString();

        float waveAngle = Vector3.Angle(world.waveDirection, Vector3.forward);        
        pointer.transform.rotation = Quaternion.Euler(0f,0f,waveAngle);
    }
}

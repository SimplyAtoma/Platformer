using UnityEngine;
using TMPro;

public class TimerController : MonoBehaviour
{
    public TMP_Text timerText;
    float timerValue = 100;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timerValue -= Time.deltaTime;
        if (timerValue <= 0)
        {
            
            Debug.Log("Player has run out of time!");
        }
        timerText.text = $"TIME\n{((int)timerValue).ToString()}";
    }
}

using UnityEngine;
using TMPro;

public class TimerController : MonoBehaviour
{
    public TMP_Text timerText;
    float timerValue = 500;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timerValue -= Time.deltaTime;
        timerText.text = $"TIME\n{((int)timerValue).ToString()}";
    }
}

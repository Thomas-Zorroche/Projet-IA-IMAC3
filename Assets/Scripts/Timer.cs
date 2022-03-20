using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer : MonoBehaviour
{

    public Color TimerColor;
    public Color LastMinuteColor;
    public UnityEngine.UI.Image Wheel;
    public UnityEngine.UI.Text Text;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setTimer(float value, int maxValue)
	{
        Wheel.fillAmount = value / maxValue;
        Text.text = Mathf.FloorToInt(value).ToString();

        if ( value < maxValue/4 && Wheel.color != LastMinuteColor)
		{
            Wheel.color = LastMinuteColor;
            Text.color = LastMinuteColor;
		} else if ( value > maxValue/4 && Wheel.color == LastMinuteColor)
		{
            Wheel.color = TimerColor;
            Text.color = TimerColor;
		}

    }
}

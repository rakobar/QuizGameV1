using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimeController : MonoBehaviour
{
    public TMP_Text[] timerText;

    public float hideTime;

    private float timeOnSec = 0f;
    private bool cdIsActive;
    private float sIncrement;
    private float sDecrement;
    private int styleTime;
    private bool timeStop;

    // Update is called once per frame
    void Update()
    {
        if (cdIsActive == true)
        {
            int days, hours, minutes, seconds;
            sDecrement += Time.deltaTime;
            timeStop = false;
            
            //atur tampilan
            if(styleTime == 0)
            {
                days = Mathf.FloorToInt(timeOnSec / 86400);
                hours = Mathf.FloorToInt(timeOnSec / 3600); // set Time Hours
                minutes = Mathf.FloorToInt((timeOnSec % 3600) / 60); // set Time Minutes
                seconds = Mathf.FloorToInt(timeOnSec % 60); // set Time Seconds

                timerText[0].text = days.ToString("00") + ":" + hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
            }
            else if(styleTime == 1)
            {
                hours = Mathf.FloorToInt(timeOnSec / 3600); // set Time Hours
                minutes = Mathf.FloorToInt((timeOnSec % 3600) / 60); // set Time Minutes
                seconds = Mathf.FloorToInt(timeOnSec % 60); // set Time Seconds

                timerText[0].text = hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
            }
            else if(styleTime == 2)
            {
                minutes = Mathf.FloorToInt(timeOnSec / 60); // set Time Minutes
                seconds = Mathf.FloorToInt(timeOnSec % 60); // set Time Seconds

                timerText[0].text = minutes.ToString("00") + ":" + seconds.ToString("00");
            }

            if(timeStop == false) {

                if (sDecrement >= 1)
                {
                    timeOnSec--;
                    sDecrement = 0;
                }
            }

            if(timeOnSec == 0)
            {
                timeStop = true;
            }
        }
        else
        {
            sIncrement += Time.deltaTime;
            //hideTime = (int)sIncrement;
            hideTime = sIncrement;
            Debug.Log("Increment Time :" + hideTime);
        }

    }
    public void TimeSet(bool countDownIsActive = false)
    {
        cdIsActive = countDownIsActive;
        sIncrement = 0f;
    }

    public void TimeSet(bool countDownIsActive, int timeModel, float timeOnMinutes)
    {
        cdIsActive = countDownIsActive;
        styleTime = timeModel;
        timeOnSec = timeOnMinutes * 60f;
    }
    public void TimeAdd(float OnSec)
    {
        timeOnSec = OnSec;
    }
    public void TimeStop(bool isActive)
    {
        timeStop = isActive; 
    }

    public void DisplayCDAlertTime(bool isActive, float timeSet)
    {

    }
}

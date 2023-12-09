using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimeController : MonoBehaviour
{
    public TMP_Text[] timerText;

    public int hideTime;

    private float timeOnSec = 0f;
    private bool cdIsActive;
    private float s;

    // Update is called once per frame
    void Update()
    {
        s += Time.deltaTime;
        int days = Mathf.FloorToInt(timeOnSec / 86400);
        int hours = Mathf.FloorToInt(timeOnSec / 3600); // set Time Hours
        int minutes = Mathf.FloorToInt((timeOnSec % 3600) / 60); // set Time Minutes
        int seconds = Mathf.FloorToInt(timeOnSec % 60); // set Time Seconds

        if (cdIsActive == true)
        {
            bool timeStop = false;
            //atur tampilan
            timerText[0].text = hours.ToString("00") + ":" +minutes.ToString("00") + ":" + seconds.ToString("00");

            if(timeStop == false) {

                if (s >= 1)
                {
                    timeOnSec--;
                    s = 0;
                }
            }

            if(timeOnSec == 0)
            {
                timeStop = true;
            }
        }
        else
        {
            hideTime = (int)s;
            //timeOnSec = s;
        }

    }

    public void timeSet(bool countDownIsActive, float timeOnMinutes)
    {
        cdIsActive = countDownIsActive;
        timeOnSec = timeOnMinutes * 60;
    }
}

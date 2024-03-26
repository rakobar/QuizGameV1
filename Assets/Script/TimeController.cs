using System;
using System.Collections;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimeController : MonoBehaviour
{
    [Tooltip("Set Child Obj start on, 0 : Time Text, 1 : Timer Add")]
    public GameObject TimeContainer;
    [SerializeField] private Image TimeStopImage;

    private float timeOnSec = 0f;
    private float timeStored = 0f;
    private bool cdIsActive;
    //private float sIncrement = 3600f;
    private float sDecrement;
    private int styleTime;
    [SerializeField]private bool timeStop = false;
    private bool timeStopped = false;
    private float timeStopTimer, decrementtimeStopTimer = 0;
    //private bool anim = true;
    private float animationTime = 0.3f;
    private Vector3 curPos;
    private float defcurPos;
    private float setcurPos = 100f; //set koodinat y untuk di luar layar.

    private Color defaultColor;
    private bool skillstat;
    private TMP_Text[] timerText;

    private string filePath;

    //AES Key & IV 16 byte
    private static readonly byte[] key = Encoding.UTF8.GetBytes("AzraRakobarReinz"); // Ganti dengan kunci rahasia Anda
    private static readonly byte[] iv = Encoding.UTF8.GetBytes("0721200007212024"); // Ganti dengan initial vector Anda

    // Update is called once per frame
    private void Start()
    {
        var childObj = TimeContainer.transform.childCount;
        timerText = new TMP_Text[childObj];

        for (int i = 0; i < childObj; i++)
        {
            timerText[i] = TimeContainer.transform.GetChild(i).gameObject.GetComponent<TMP_Text>();

        }
        timerText[1].gameObject.SetActive(false);
        TimeStopImage.transform.gameObject.SetActive(false);

        defaultColor = TimeStopImage.color;
        defaultColor.a = 0f;
        TimeStopImage.color = defaultColor;

        curPos = TimeContainer.transform.GetChild(1).transform.localPosition;
        defcurPos = curPos.y; //ambil nilai Y bawaan objek
        curPos.y = setcurPos; //ubah nilai Y ke nilai yang di simpan di setcurPos.
        TimeContainer.transform.GetChild(1).transform.localPosition = curPos; //atur nilai kordinat yang telah di sesuaikan dan atur ke objek;

    }
    void Update()
    {

        int days, hours, minutes, seconds;

        if (cdIsActive == true)
        {
            sDecrement += Time.deltaTime;

            if (timeOnSec <= 1)
            {
                timeStop = true;
            }

            if (!timeStop) 
            {
                if (sDecrement >= 1)
                {
                    timeOnSec--;
                    sDecrement = 0;
                }

                //if (skillstat)
                //{
                //    File.WriteAllText(filePath, EDProcessing.Encrypt((timeOnSec / 60).ToString(), key, iv));
                //}
            }
        }

        //else
        //{
        //    sIncrement += Time.deltaTime;
        //    //hideTime = (int)sIncrement;
        //    hideTime = sIncrement;
        //    Debug.Log("Increment Time :" + hideTime);
        //}

        if (timeStopped)
        {
            TimeStop(timeStopped);
            if (timeStopTimer <= 1 && timeStopped)
            {
                timeStopped = false;
                TimeStop(timeStopped);
            }

            if(timeStopTimer != 0 || timeStopTimer > 0)
            {
                decrementtimeStopTimer += Time.deltaTime;
                if(decrementtimeStopTimer >= 1)
                {
                    timeStopTimer--;
                    decrementtimeStopTimer = 0;
                }
            }
        }

        //atur tampilan
        if (styleTime == 0)
        {
            days = Mathf.FloorToInt(timeOnSec / 86400);
            hours = Mathf.FloorToInt(timeOnSec / 3600); // set Time Hours
            minutes = Mathf.FloorToInt((timeOnSec % 3600) / 60); // set Time Minutes
            seconds = Mathf.FloorToInt(timeOnSec % 60); // set Time Seconds

            timerText[0].text = days.ToString("00") + ":" + hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
        }
        else if (styleTime == 1)
        {
            hours = Mathf.FloorToInt(timeOnSec / 3600); // set Time Hours
            minutes = Mathf.FloorToInt((timeOnSec % 3600) / 60); // set Time Minutes
            seconds = Mathf.FloorToInt(timeOnSec % 60); // set Time Seconds

            timerText[0].text = hours.ToString("00") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
        }
        else if (styleTime == 2)
        {
            minutes = Mathf.FloorToInt(timeOnSec / 60); // set Time Minutes
            seconds = Mathf.FloorToInt(timeOnSec % 60); // set Time Seconds

            timerText[0].text = minutes.ToString("00") + ":" + seconds.ToString("00");
        }

    }
    //public void TimeSet(bool countDownIsActive = false)
    //{
    //    cdIsActive = countDownIsActive;
    //    sIncrement = 0f;
    //}

    public void TimeSet(bool countDownIsActive, int timeModel, float timeOnSeconds, bool skillstat = false, string filePath = "")
    {
        cdIsActive = countDownIsActive;
        styleTime = timeModel;
        timeOnSec = timeOnSeconds;
        timeStored = timeOnSeconds;
        this.skillstat = skillstat;
        this.filePath = filePath;



    }
    public void TimeAdd(bool timeAdd, float OnSec)
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath) && skillstat)
        {
            var CurrentEndDate = EDProcessing.Decrypt(File.ReadAllText(filePath), key, iv);

            if (timeAdd)
            {
                timeOnSec += OnSec;
                timerText[1].text = $"+ {OnSec}";
                StartCoroutine(AnimatedTime());
                File.WriteAllText(filePath, EDProcessing.Encrypt(DateTime.Parse(CurrentEndDate).AddSeconds(OnSec).ToString(), key, iv));
            }
            else
            {
                timeOnSec -= OnSec;
                timerText[1].text = $"- {OnSec}";
                StartCoroutine(AnimatedTime());
                File.WriteAllText(filePath, EDProcessing.Encrypt(DateTime.Parse(CurrentEndDate).AddSeconds(-OnSec).ToString(), key, iv));
            }
        }
        else
        {
            Debug.LogError("Skill Tidak Di Izinkan.");
        }
    }

    public void TimeStop(float time)
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath) && skillstat)
        {
            var CurrentEndDate = EDProcessing.Decrypt(File.ReadAllText(filePath), key, iv);
            timeStopTimer = time;
            timeStopped = true;
            File.WriteAllText(filePath, EDProcessing.Encrypt(DateTime.Parse(CurrentEndDate).AddSeconds(time).ToString(), key, iv));
        }
    }

    public void TimeStop(bool isActive, bool wAnim = true)
    {
        timeStop = isActive;
        if (wAnim)
        {
            StartCoroutine(AnimatedFade(isActive));
        }
    }

    public bool getActiveState()
    {
        return !timeStop;
    }

    public float getTime()
    {
        return timeOnSec;
    }
    public float getTimeElapse()
    {
        //getting time Lapse (totalTime - remainTime)
        var timelapse = timeStored - timeOnSec;

        return timelapse;
    }

    //Animation
    private IEnumerator AnimatedTime()
    {

        timerText[1].gameObject.SetActive(true);
        timerText[1].transform.LeanMoveLocal(new Vector2(curPos.x, defcurPos), animationTime).setEaseOutQuart();
        yield return new WaitUntil(() => Mathf.Approximately(timerText[1].transform.localPosition.y, defcurPos));
        timerText[1].transform.LeanScale(Vector3.zero, animationTime);
        yield return new WaitUntil(() => timerText[1].transform.localScale == Vector3.zero);
        timerText[1].transform.LeanMoveLocal(new Vector2(curPos.x, curPos.y), animationTime);
        yield return new WaitUntil(() => Mathf.Approximately(timerText[1].transform.localPosition.y, curPos.y));
        timerText[1].transform.LeanScale(Vector3.one, animationTime);
        timerText[1].gameObject.SetActive(false);

    }

    private IEnumerator AnimatedFade(bool animActive)
    {
        if (animActive)
        {
            if (TimeStopImage.color.a <= 0f)
            {
                TimeStopImage.transform.gameObject.SetActive(true);
                LeanTween.value(TimeStopImage.gameObject, ImgAlphaUpdate, 0f, 0.35f, animationTime);
            }
        }
        else
        {
            if(TimeStopImage.color.a >= 0.35f)
            {
                LeanTween.value(TimeStopImage.gameObject, ImgAlphaUpdate, 0.35f, 0f, animationTime);
                yield return new WaitUntil(() => TimeStopImage.color == defaultColor);
                TimeStopImage.transform.gameObject.SetActive(true);
            }
        }
    }

    void ImgAlphaUpdate(float alpha)
    {
        var color = TimeStopImage.color;
        color.a = alpha;
        TimeStopImage.color = color;
    }

}

using UnityEngine;
using UnityEngine.UI;

public class AbilityController : MonoBehaviour
{
    [SerializeField] TimeController timeController;
    [SerializeField] QuizRDB quizController;
    [SerializeField] AlertController alertController;
    [Header("Ability Property Set")]
    [Tooltip("Length Btn And Rate Must Same")]
    [SerializeField] Button[] AbilityBtn;
    [SerializeField] float[] ratesShowBtn;

    float sec;
    float secAbility;
    int limitActivedBtn = 3;
    int ActiveBtn = 0;
    bool AbilityAllowed = false;
    //bool timeStopped = false;
    //public float timeStopTimerSec;

    private bool StartWAbility;
    private float timer = 0f;
    private float spawnIntervalPercentage = 0.3f; //mosal 0.2 = 20%, maka setia

    private void Start()
    {

    }

    private void Update()
    {
        if (StartWAbility)
        {
            secAbility += Time.deltaTime;

        }

        //if (timeStopped)
        //{
        //    timeController.TimeStop(timeStopped);

        //    if (timeStopTimerSec <= 1 && timeStopped)
        //    {
        //        timeStopped = false;
        //        timeController.TimeStop(timeStopped);
        //    }

        //    if (timeStopTimerSec != 0f || timeStopTimerSec > 0f)
        //    {
        //        sec += Time.deltaTime;

        //        if (sec >= 1)
        //        {
        //            timeStopTimerSec--;
        //            sec = 0;
        //        }
        //    }
            
        //}
        //else
        //{
        //    if (timeStopped)
        //    {
        //        timeController.TimeStop(false);
        //    }
        //}
    }

    public void AbillitySet(bool Active)
    {
        AbilityAllowed = Active;
    }

    private void CallAbility()
    {
        if(AbilityAllowed)
        {
            float totalRate = 0;
            if (ActiveBtn == limitActivedBtn)
            {
                Debug.LogWarning("Ability : Maxed Active Btn.");
            }
            else
            {
                //cek panjang array btn dan rateShowBtn;
                if (AbilityBtn.Length != ratesShowBtn.Length)
                {
                    Debug.LogError("Rate System : Error length Ability not same as rate length.");
                }
                else
                {
                    //menghitung total persentase kemunculan semua gambar.
                    foreach (float rate in ratesShowBtn)
                    {
                        totalRate += rate;
                    }

                    //memilih skill yang akan ditampilkan berdasarkan persentase
                    float randVal = Random.Range(0f, totalRate);
                    float cumulativeRate = 0;

                    //
                    for (int i = 0; i < AbilityBtn.Length; i++)
                    {
                        //tambah nilai cumulativeRate dari rateShowBtn
                        cumulativeRate += ratesShowBtn[i];

                        //randVal kurang dari atau sama dengan cumulativeRate;
                        if (randVal <= cumulativeRate)
                        {
                            AbilityBtn[i].gameObject.SetActive(true);
                            ActiveBtn++;
                            break;
                        }
                    }
                }
            }
        }
        
    }

    //added class
    public void Decrease15S()
    {
        TimeDecrease(15);
    }
    public void Add15S()
    {
        TimeExtend(15);
    }
    public void Add30S()
    {
        TimeExtend(30);
    }

    public void TimeStopI()
    {
        TimeStop(10);
    }

    public void AutoCorrect()
    {
        if (AbilityAllowed)
        {
            quizController.AutoCorrect();
            ActiveBtn--;
        }
        else
        {
            alertController.AlertSet("Skill Tidak Diizinkan.", true);
        }
    }
    public void BoostScoreI()
    {
        ScoreMultiplier(true, 0.25);
    }
    public void BoostScoreII()
    {
        ScoreMultiplier(false, 2);
    }
    public void BoostScoreWithRiskMinusPoint()
    {
        ScoreMultiplier(false, 4);
        FalseQuestScoring(true, 0.5);
    }
    public void FalseRemoveI()
    {
        FalseRemove(1);
    }

    public void FalseRemoveII()
    {
        FalseRemove(2);
    }
    
    public void FalseRemoveIII()
    {
        FalseRemove(3);
    }

    //added Advance primary function
    private void TimeExtend(int TimeinSeconds)
    {
        if (AbilityAllowed)
        {
            timeController.TimeAdd(true, TimeinSeconds);
            ActiveBtn--;
        }
        else
        {
            alertController.AlertSet("Skill Tidak Diizinkan.",true);
        }
    }
    private void TimeDecrease(int TimeinSeconds)
    {
        if (AbilityAllowed)
        {
            timeController.TimeAdd(false, TimeinSeconds);
            ActiveBtn--;
        }
        else
        {
            alertController.AlertSet("Skill Tidak Diizinkan.", true);
        }
    }

    private void TimeStop(bool timeStopped) //holding by answer
    {
        if (AbilityAllowed)
        {
            timeController.TimeStop(timeStopped);
        }
        else
        {
            alertController.AlertSet("Skill Tidak Diizinkan.", true);
        }

    }

    private void TimeStop(float timeSec) //byTime
    {
        if (AbilityAllowed)
        {
            timeController.TimeStop(timeSec);
            ActiveBtn--;
        }
        else
        {
            alertController.AlertSet("Skill Tidak Diizinkan.", true);
        }
    }

    private void FalseRemove(int objToRemove)
    {
        if (AbilityAllowed)
        {
            quizController.FalseRemover(objToRemove);
            ActiveBtn--;
        }
        else
        {
            alertController.AlertSet("Skill Tidak Diizinkan.", true);
        }

    }
    private void ScoreMultiplier(bool isPercent, double addVal)
    {
        if (AbilityAllowed)
        {
            quizController.pointTrueQuizMultiplier(true, isPercent, addVal);
            ActiveBtn--;
        }
        else
        {
            alertController.AlertSet("Skill Tidak Diizinkan.", true);
        }

    }

    private void FalseQuestScoring(bool isMinus,double percent)
    {
        if (AbilityAllowed)
        {
            if (!isMinus)
            {
                quizController.PointFalseAddScore(true, isMinus, percent);
            }
            else
            {
                quizController.PointFalseAddScore(true, isMinus, percent);
            }

            ActiveBtn--;
        }
        else
        {
            alertController.AlertSet("Skill Tidak Diizinkan.", true);
        }

    }

    //end of primary function
}

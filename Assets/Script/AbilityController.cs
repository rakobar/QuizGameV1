using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbilityController : MonoBehaviour
{
    [SerializeField] TimeController timeController;
    [SerializeField] QuizRDB quizController;
    [SerializeField] Button[] AbilityBtn;

    string[] abilityType = { "Time_Extend", "Time_Stop", "Time_Slow", "False_Display", "Display_TrueAnswer"/*, "Retry", "Skip", "Hints"*/, "Score_Multiplier", "2x, false 2x", "imune descore"};
    int timeSkillSpawn;
    float sec;

    public void TimeExtend()
    {
        timeController.TimeAdd(30);
    }
    public void TimeStop(float timeSec)
    {
        sec += Time.deltaTime;

        if (sec >= 1)
        {
            timeSec--;
            sec = 0;
        }

        if (timeSec == 0)
        {
            timeController.TimeStop(false);
        }
        else
        {
            timeController.TimeStop(true);
        }
        
    }
    public void FalseRemoveI()
    {
        quizController.FalseRemover(1);
    }
    public void FalseRemoveII()
    {
        quizController.FalseRemover(2);
    }
    public void FiftyFifty()
    {
        quizController.FalseRemover(3);
    }

    public void AutoCorrectSkipQuestion()
    {
    }
    void ScoreMultiplier(float intMultiplier)
    {
        //pending
    }
}

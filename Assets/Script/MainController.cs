using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MainController : MonoBehaviour
{
    DatabaseReference dbref;

    [Header("Ui Component Needed")]
    [SerializeField]Button[] quizBtn;
    [SerializeField]TMP_Text[] quizText; // Textdesc1, textdesc2
    [SerializeField] TMP_InputField quizKeyInput;

    [Header("Obj Component Needed")]
    [SerializeField]GameObject[] uiComponent; //inputQuiz, startQuiz, quizPanel, skillUI

    [Header("Important Component Needed")]
    [SerializeField] BackgroundScroller BackgroundController;
    [SerializeField] TimeController timeController;
    [SerializeField] QuizRDB QuizController;

    //table reference name
    string targetTable = "data_quizkey";

    //komponen quiz data
    string idQuiz;
    string descQuiz;
    string skillStats;
    string qPoint;
    int qSize;
    int timeToSet; //on minutes

    // Start is called before the first frame update
    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
                FirebaseApp app = FirebaseApp.DefaultInstance;
                //FirebaseApp.Create();
                // Set a flag here to indicate whether Firebase is ready to use by your app.
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        });


    }

    // Update is called once per frame
    void Update()
    {
        BackgroundController.BGAnimSet(1,1,0.05f);
    }

    public void QuizMenuBtn(int btnCallType)
    {
        //for btn Quiz
        if (btnCallType == 1)
        {
            var qText = quizKeyInput.text.ToUpper();

            if (string.IsNullOrEmpty(qText) || string.IsNullOrWhiteSpace(qText))
            {
                Debug.Log("Kosong!");
            }
            else
            {
                StartCoroutine(findQuizKey(qText));
            }

            
            
        }//for btn start
        else if(btnCallType == 2)
        {
            timeController.TimeSet(true, 2, timeToSet);
            uiActiveType(3);
        }
        else if(btnCallType == 3)
        {
            uiActiveType(1);
            setQuizData(false);
        }
        
    }
    IEnumerator findQuizKey(string quizKey)
    {
        dbref = FirebaseDatabase.DefaultInstance.RootReference;
        var fetchData = dbref.Child(targetTable).GetValueAsync();
        yield return new WaitUntil(() => fetchData.IsCompleted);
        
        if (fetchData.Exception != null)
        {
            Debug.LogError(fetchData.Exception);
            yield break;
        }

        DataSnapshot snapshot = fetchData.Result;
        var idList = snapshot.Children.Select(child => child.Key).ToList();

        if (idList.Any(id => id == quizKey))
        {
            StartCoroutine(getQuizData(quizKey));
            quizKeyInput.text = null;
        }

    }

    IEnumerator getQuizData(string quizKey) //function mengabil data quiz
    {
        dbref = FirebaseDatabase.DefaultInstance.RootReference;
        
        var fetchData = dbref.Child(targetTable).Child(quizKey).OrderByKey().GetValueAsync();
        yield return new WaitUntil(() => fetchData.IsCompleted);

        if(fetchData.Exception != null)
        {
            Debug.LogError(fetchData.Exception);
            yield break;
        }

        //set data ke variabel global.
        DataSnapshot snapshot = fetchData.Result;
        idQuiz = snapshot.Child("quiz_id").Value.ToString();
        descQuiz = snapshot.Child("quiz_desc").Value.ToString();
        skillStats = snapshot.Child("quiz_skillActive").Value.ToString();
        qPoint = snapshot.Child("quiz_questionPoint").Value.ToString();
        qSize = int.Parse(snapshot.Child("quiz_maxsoaldisplay").Value.ToString());
        timeToSet = int.Parse(snapshot.Child("quiz_timeSet").Value.ToString());

        

        uiActiveType(2);
        setQuizData(true);
        QuizController.getQuestData("0", idQuiz, qSize);

        if(skillStats == "true")
        {
            uiComponent[3].SetActive(true);
        }
        else
        {
            uiComponent[3].SetActive(false);
        }

        quizBtn[1].interactable = true;
    }

    void setQuizData(bool isActive)
    {
        if(isActive == true)
        {
            string stat;

            if (skillStats == "true")
            {
                stat = "Tersedia";
            }
            else
            {
                stat = "Tidak Tersedia";
            }

            quizText[0].text = idQuiz;
            quizText[1].text = descQuiz;
            quizText[2].text = "Waktu Pengerjaan : " + timeToSet.ToString() + " Menit";
            quizText[3].text = "Jumlah Soal : " + qSize.ToString() + " Soal";
            quizText[4].text = "Penggunaan Skill : " + stat;
        }
        else
        {
            for(int i = 0; i < quizText.Length; i++)
            {
                quizText[i].text = null;
            }
        }
        

    }

    void uiActiveType(int typ)
    {
        if(typ == 0)
        {
            for(int i = 0; i < uiComponent.Length; i++)
            {
                uiComponent[i].SetActive(false);
            }
        }
        //input container
        else if( typ == 1)
        {
            uiComponent[0].SetActive(true);
            uiComponent[1].SetActive(false);
            uiComponent[2].SetActive(false);
            quizBtn[1].interactable = false;
        }
        //display desc container
        else if (typ == 2)
        {
            uiComponent[0].SetActive(false);
            uiComponent[1].SetActive(true);
            uiComponent[2].SetActive(false);
        }
        //display quiz
        else if(typ == 3)
        {
            uiComponent[0].SetActive(false);
            uiComponent[1].SetActive(false);
            uiComponent[2].SetActive(true);
        }
    }

}

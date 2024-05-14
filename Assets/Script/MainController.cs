using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainController : MonoBehaviour
{
    DatabaseReference dbref;

    [Header("Ui Component Needed")]
    [Tooltip("Quiz Btn, Start btn, Back btn")]
    [SerializeField] Button[] ActionBtn;
    [Tooltip("0:QuizKeyHeader, 1:Desc Quiz, 2:Time Requirement, 3:Max Questions, 4:Abillity Status")]
    [SerializeField] TMP_Text[] quizMenuText; // Textdesc1, textdesc2
    [SerializeField] TMP_InputField quizKeyInput;

    [Header("Obj Component Needed")]
    [Tooltip("0:inputQuizObj, 1:Quiz Menu Desc Obj, 2:Quiz Panel Obj, 3:Ability UI, 4:Menu Obj, 5:Student Info Obj, 6:Login Obj")]
    [SerializeField] GameObject[] uiComponent; //inputQuiz, startQuiz, quizPanel, skillUI

    [Header("Important Component Needed")]
    [SerializeField] BackgroundScroller BackgroundController;
    [SerializeField] TimeController timeController;
    [SerializeField] QuizRDB QuizController;
    [SerializeField] AlertController AlertController;
    [SerializeField] LoginRDB LoginController;
    [SerializeField] AbilityController abilityController;
    [SerializeField] LoadingController loadingController;

    //table reference name
    string targetTable = "data_quizkey";
    string resultTable = "data_quizresult"; //di akses untuk mengecek jawaban.

    //komponen quiz data
    string uid;
    string idQuiz;
    string descQuiz;
    bool skillStats;
    string dateStart, dateEnd;
    int qPoint;
    int qSize;
    int timeToSet; //on seconds
    [SerializeField] float intervalSpawnSkillTime;
    int correctStreak = 3;
    int limitActiveBtn = 3;

    private double tmpTime;
    private string[] filePath = new string[2];

    private string UrlSurvey;

    float animatedTime = 0.30f; //0.25

    //AES Key & IV 16 byte
    private static readonly byte[] key = Encoding.UTF8.GetBytes("AzraRakobarReinz"); // Ganti dengan kunci rahasia Anda
    private static readonly byte[] iv = Encoding.UTF8.GetBytes("0721200007212024"); // Ganti dengan initial vector Anda

    // Start is called before the first frame update
    void Start()
    {
        // Limit the framerate to 60
        Application.targetFrameRate = 60;

        //FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        //{
        //    var dependencyStatus = task.Result;
        //    if (dependencyStatus == Firebase.DependencyStatus.Available)
        //    {
        //        // Create and hold a reference to your FirebaseApp,
        //        // where app is a Firebase.FirebaseApp property of your application class.


        //        //FirebaseApp.Create();
        //        // Set a flag here to indicate whether Firebase is ready to use by your app.
        //    }
        //    else
        //    {
        //        UnityEngine.Debug.LogError(System.String.Format(
        //          "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
        //        // Firebase Unity SDK is not safe to use here.
        //    }
        //});

        FirebaseDatabase.DefaultInstance.PurgeOutstandingWrites();
        //FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);
        FirebaseApp app = FirebaseApp.DefaultInstance;

        var folderPath = Path.Combine(Application.persistentDataPath, EDProcessing.HashSHA384("sessionQuizData"));
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }

        //btn quiz,start,exit

        //foreach(var btn in ActionBtn)
        //{
        //    btn.onClick.RemoveAllListeners();
        //}

        ActionBtn[0].onClick.AddListener(btnQuiz);
        ActionBtn[1].onClick.AddListener(btnStart);
        ActionBtn[1].interactable = false;
        ActionBtn[2].onClick.AddListener(btnBack);
        ActionBtn[3].onClick.AddListener(btnExit);
        ActionBtn[4].onClick.AddListener(btnBackToInput);
        ActionBtn[5].onClick.AddListener(btnEnd);
        ActionBtn[5].gameObject.SetActive(false);
        ActionBtn[7].gameObject.SetActive(false);
        ActionBtn[8].onClick.AddListener(openUrl);
        ActionBtn[9].onClick.AddListener(openUrl);

        //initial animated
        uiAnimatedActiveType(1);
        uiComponent[2].transform.localScale = Vector3.zero;
        uiComponent[1].transform.localScale = Vector3.zero;
        uiComponent[8].transform.localScale = Vector3.zero;

        AudioController.Instance.PlayAudioBGM("BGM0");
        ProceessURlSurvey();
    }

    // Update is called once per frame
    void Update()
    {
        BackgroundController.BGAnimSet(1, 1, 0.05f);
        CheckInternetAvailability();
        if (timeController.getTime() <= 0.00001 && uiComponent[2].activeSelf == true)
        {
            //timeController.TimeStop(true, false);
            AlertController.AlertSet("Waktu Telah Habis.", true, TextAlignmentOptions.Center, false, EndQuiz);
        }

        if (skillStats)
        {
            ActionBtn[5].gameObject.SetActive(false);
            if (QuizController.HasAnswerTracking() >= qSize)
            {
                if (uiComponent[2].activeSelf)
                {
                    btnEnd();
                }
                
            }
        }
        else
        {
            if (uiComponent[2].activeSelf)
            {
                ActiveEndBtn(qSize == QuizController.HasAnswerTracking() ? true : false);
            }
            
        }

        if (uiComponent[4].activeSelf)
        {
            if (!AudioController.Instance.bgmSource.isPlaying)
            {
                AudioController.Instance.PlayAudioBGM("BGM0");
            }
        }

        if (uiComponent[2].activeSelf)
        {
            if (!AudioController.Instance.bgmSource.isPlaying)
            {
                AudioController.Instance.RandomPlayAudioBGM();
            }
        }

    }
    void CheckInternetAvailability()
    {
        // Mengecek ketersediaan koneksi internet
        NetworkReachability reachability = Application.internetReachability;

        // Memeriksa hasil dan memberikan respons sesuai
        switch (reachability)
        {
            case NetworkReachability.NotReachable:

                if (uiComponent[6].activeSelf)
                {
                    AlertController.AlertSet("Tidak Ada Koneksi, Silahkan Cek Jaringan Anda", true, TextAlignmentOptions.Center, false, LoginController.CheckLoginStatus);
                }
                else
                {
                    AlertController.AlertSet("Tidak Ada Koneksi, Silahkan Cek Jaringan Anda", true, TextAlignmentOptions.Center);
                }

                break;

                //case NetworkReachability.ReachableViaCarrierDataNetwork :
                //    //Debug.Log("Terhubung melalui jaringan data operator seluler.");
                //    break;

                //case NetworkReachability.ReachableViaLocalAreaNetwork:
                //    //Debug.Log("Terhubung melalui jaringan lokal (Wi-Fi atau Ethernet).");
                //    break;
        }

    }

    private IEnumerator ActiveEndBtn(bool status)
    {
        if (status)
        {
            if (!ActionBtn[5].gameObject.activeSelf)
            {
                ActionBtn[5].gameObject.SetActive(true);
                ActionBtn[5].transform.LeanScale(Vector3.one, animatedTime);
            }
        }
        else
        {
            if (ActionBtn[5].gameObject.activeSelf)
            {
                ActionBtn[5].transform.LeanScale(Vector3.zero, animatedTime).setEaseInBack();
                yield return new WaitUntil(() => ActionBtn[5].transform.localScale == Vector3.zero);
                ActionBtn[5].gameObject.SetActive(false);
            }
        }
    }

    private void ProceessURlSurvey()
    {
        StartCoroutine(getUrl());
    }

    private IEnumerator getUrl()
    {
        dbref = FirebaseDatabase.DefaultInstance.RootReference;
        var query = dbref.Child("data_gameSurvey").GetValueAsync();
        yield return new WaitUntil(()=> query.IsCompleted);

        if(query.Exception != null)
        {
            AlertController.AlertSet($"Error getting URL Data : \n{query.Exception.InnerExceptions}");
        }

        DataSnapshot snapshot = query.Result;

        if (snapshot.Child("Url").Value != null && snapshot.Child("BtnActive").Value != null)
        {
            UrlSurvey = snapshot.Child("Url").Value.ToString();
            ActionBtn[8].gameObject.SetActive(bool.Parse(snapshot.Child("BtnActive").Value.ToString()));
            ActionBtn[9].gameObject.SetActive(bool.Parse(snapshot.Child("BtnActive").Value.ToString()));
        }
        else
        {
            UrlSurvey = null;
            ActionBtn[8].gameObject.SetActive(false);
            ActionBtn[9].gameObject.SetActive(false);
        }
    }

    private void openUrl()
    {
        Application.OpenURL(UrlSurvey);
    }

    private void btnQuiz() //btnQuiz
    {
        AudioController.Instance.PlayAudioSFX("ButtonClick");
        var qText = quizKeyInput.text.ToUpper();
        if (string.IsNullOrEmpty(qText) || string.IsNullOrWhiteSpace(qText))
        {
            AlertController.AlertSet("Tidak boleh kosong atau ada spasi di dalamnya. ", true, TextAlignmentOptions.Center);
        }
        else
        {
            StartCoroutine(findQuizKey(qText));
            ActionBtn[1].gameObject.GetComponentInChildren<TMP_Text>().text = "Loading..";
        }
    }

    private void btnStart()
    {
        AudioController.Instance.PlayAudioSFX("ButtonClick");
        StartCoroutine(StartProgress());
    }

    private IEnumerator StartProgress()
    {
        
        var curTime = timeToSet * 60;
        var maxQuestions = QuizController.getMaxQuestionData();

        if(maxQuestions != qSize)
        {
            AlertController.AlertSet("Jumlah Soal Tidak Valid,\nSilahkan Hubungi Guru atau Pengelola!,\nJika Sudah Ada Perubahan Tunggu 1-3 Menit, Lalu Masuk Kembali.", true, TextAlignmentOptions.Left);
            quizMenuText[3].text = $"{qSize} Soal (Invalid)";
        }
        else
        {
            dbref = FirebaseDatabase.DefaultInstance.RootReference;
            var fetchData = dbref.Child(resultTable).Child(uid).GetValueAsync();
            yield return new WaitUntil(() => fetchData.IsCompleted);

            if (fetchData.Exception != null)
            {
                Debug.LogError(fetchData.Exception);
                yield break;
            }

            DataSnapshot snapshot = fetchData.Result;

            var idList = new List<string>();
            if (snapshot.Children.Select(child => child.Key) != null)
            {
                idList = snapshot.Children.Select(child => child.Key).ToList();

                if (skillStats)
                {
                    StartFun(curTime);
                }
                else
                {
                    if (idList.Any(id => id == idQuiz))
                    {
                        AlertController.AlertSet("Kamu Telah Mengerjakan Kuis Ini.", true, TextAlignmentOptions.Center);
                    }
                    else
                    {
                        StartNormal(curTime);
                    }
                }
            }
            else
            {
                if (skillStats)
                {
                    StartFun(curTime);
                }
                else
                {
                    StartNormal(curTime);
                }
            }
        }

    }

    private void StartNormal(int timeSet)
    {
        var StartDate = DateTime.Now;
        //Tanggal waktu sudah di depan atau sama dengan dateStart.
        if (StartDate >= DateTime.Parse(dateStart))
        {
            if (StartDate <= DateTime.Parse(dateEnd))
            {
                var elapseTime = StartDate - DateTime.Parse(dateStart);
                var maxTime = (DateTime.Parse(dateEnd) - DateTime.Parse(dateStart)).TotalSeconds;
                int elapseSeconds = (int)elapseTime.TotalSeconds;
                int remainTime = (int)maxTime - elapseSeconds;

                if (File.Exists(filePath[0]))
                {
                    if(remainTime > timeSet)
                    {
                        var DateData = EDProcessing.Decrypt(File.ReadAllText(filePath[0]), key, iv);
                        //var FullDate = DateData.Split(",");
                        var remainingTimeOnFile = DateTime.Parse(DateData).Subtract(StartDate).TotalSeconds;

                        if((int)remainingTimeOnFile == 0 || (int)remainingTimeOnFile <= 1)
                        {
                            AlertController.AlertSet("Waktu Telah Habis !");
                            //QuizController.resetQuizData();
                        }
                        else
                        {
                            //timeController.TimeSet(true, 2, (int)remainingTimeOnFile);
                            tmpTime = remainingTimeOnFile;
                            AlertController.AlertSet("Ingat, Yang kamu kerjakan di perangkat ini, tidak dapat di bawa ke perangkat lain!", false, TextAlignmentOptions.Center, true, processStart);
                        }
                    }
                    else
                    {
                        //timeController.TimeSet(true, 2, remainTime);
                        tmpTime = remainTime;
                        AlertController.AlertSet("Ingat, Yang kamu kerjakan di perangkat ini, tidak dapat di bawa ke perangkat lain!", false, TextAlignmentOptions.Center, true, processStart);
                    }
                }
                else
                {
                    if (elapseSeconds < maxTime) //kurang dari
                    {
                        if (remainTime > timeSet)
                        {
                            //Initialize Start Date & End Date to DateData.
                            //var EndDate = StartDate.AddSeconds(timeSet);
                            //var DateData = EndDate.ToString();

                            //File.WriteAllText(filePath, EDProcessing.Encrypt(DateData, key, iv));
                            tmpTime = timeSet;
                            //timeController.TimeSet(true, 2, timeSet);
                        }
                        else
                        {
                            tmpTime = remainTime;
                            //timeController.TimeSet(true, 2, remainTime);
                        }
                        File.WriteAllText(filePath[1], EDProcessing.Encrypt(DateTime.Now.ToString(), key, iv));
                        AlertController.AlertSet("Ingat, Yang kamu kerjakan di perangkat ini, tidak dapat di bawa ke perangkat lain!", false, TextAlignmentOptions.Center, true, processStart);
                    }
                }
            }
            else
            {
                AlertController.AlertSet("Waktu Telah Habis !");
                QuizController.DeleteData();
                //QuizController.resetQuizData();
            }
        }
        else
        {
            var waitingToStart = DateTime.Parse(dateStart) - DateTime.Now;
            var waitingOnMinute = (int)waitingToStart.TotalMinutes;
            var waitingOnSecond = (int)waitingToStart.TotalSeconds;

            if (waitingOnMinute != 0)
            {
                AlertController.AlertSet($"Waktu Quiz Belum Mulai.\nTersisa {waitingOnMinute} Menit Sampai Memulai.", false, TextAlignmentOptions.Center);
            }
            else
            {
                AlertController.AlertSet($"Waktu Quiz Belum Mulai.\nTersisa {waitingOnSecond} Detik Sampai Memulai.", false, TextAlignmentOptions.Center);
            }
        }
    }

    private void StartFun(int timeSet)
    {
        var StartDate = DateTime.Now;
        if (StartDate >= DateTime.Parse(dateStart))
        {
            if (File.Exists(filePath[0]))
            {
                //var timeData = File.ReadAllText(filePath);
                //var TimeFromFile = float.Parse(EDProcessing.Decrypt(timeData, key, iv)) * 60;

                //if(TimeFromFile == 0 || TimeFromFile <= 1)
                //{
                //    AlertController.AlertSet("Waktu Telah Habis !", false, TextAlignmentOptions.Center);
                //    DeleteData();
                //}
                //else
                //{
                //    timeController.TimeSet(true, 2, TimeFromFile, skillStats);
                //}

                var DateData = EDProcessing.Decrypt(File.ReadAllText(filePath[0]), key, iv);
                //var FullDate = DateData.Split(",");
                var remainingTimeOnFile = DateTime.Parse(DateData).Subtract(StartDate).TotalSeconds;
                if((int)remainingTimeOnFile == 0 || (int)remainingTimeOnFile <= 1)
                {
                    AlertController.AlertSet("Waktu Telah Habis, Semua Soal & Jawaban Telah Di Reset!");
                    QuizController.DeleteData();
                    StartCoroutine(findQuizKey(idQuiz));
                    //QuizController.resetQuizData();
                }
                else
                {
                    //timeController.SetTimeStopped(true);
                    //timeController.TimeSet(true, 2, (int)remainingTimeOnFile, skillStats, filePath);
                    tmpTime = remainingTimeOnFile;
                    AlertController.AlertSet("Ingat, Yang kamu kerjakan di perangkat ini, tidak dapat di bawa ke perangkat lain!", false, TextAlignmentOptions.Center, true, processStart);
                }
            }
            else
            {
                //var EndDate = StartDate.AddSeconds(timeSet);
                //var DateData = EndDate.ToString();
                //File.WriteAllText(filePath, EDProcessing.Encrypt((timeSet / 60).ToString(), key, iv));
                //timeController.SetTimeStopped(true);
                //timeController.TimeSet(true, 2, timeSet, skillStats, filePath);
                tmpTime = timeSet;
                File.WriteAllText(filePath[1], EDProcessing.Encrypt(DateTime.Now.ToString(), key, iv));
                AlertController.AlertSet("Ingat, Yang kamu kerjakan di perangkat ini, tidak dapat di bawa ke perangkat lain!", false, TextAlignmentOptions.Center, true, processStart);
                //File.WriteAllText(filePath, EDProcessing.Encrypt(DateData, key, iv));
            }
            
        }
        else
        {
            var waitingToStart = DateTime.Parse(dateStart) - DateTime.Now;
            var waitingOnMinute = (int)waitingToStart.TotalMinutes;
            var waitingOnSecond = (int)waitingToStart.TotalSeconds;

            if (waitingOnMinute != 0)
            {
                AlertController.AlertSet($"Waktu Quiz Belum Mulai.\nTersisa {waitingOnMinute} Menit Sampai Memulai.", false, TextAlignmentOptions.Center);
            }
            else
            {
                AlertController.AlertSet($"Waktu Quiz Belum Mulai.\nTersisa {waitingOnSecond} Detik Sampai Memulai.", false, TextAlignmentOptions.Center);
            }

        }
    }

    //private void DeleteData()
    //{
    //    string[] fileName = { 
    //        $"{uid}_{idQuiz}_{skillStats}_QD", 
    //        $"{uid}_{idQuiz}_{skillStats}_AD", 
    //        $"{uid}_{idQuiz}_{skillStats}_ID", 
    //        $"{uid}_{idQuiz}_{skillStats}_QTD",
    //        $"{uid}_{idQuiz}_{skillStats}_QSD",
    //        $"{uid}_{idQuiz}_{skillStats}_SD0", 
    //        $"{uid}_{idQuiz}_{skillStats}_SD1", 
    //        $"{uid}_{idQuiz}_{skillStats}_SD2"}; //contoh : 10011232_QUIZ03_Q

    //    foreach (var nameFile in fileName)
    //    {
    //        var path = Path.Combine(Application.persistentDataPath, EDProcessing.HashSHA384("sessionQuizData"), EDProcessing.HashSHA384(nameFile));
    //        if (File.Exists(path))
    //        {
    //            //menghapus file yang ditetapkan.
    //            File.Delete(path);
    //        }
    //    }
    //}

    private void processStart()
    {
        //timeController.SetTimeStopped(false);
        double timeElapse = 0;

        if (File.Exists(filePath[1]))
        {
            var DateElapse = EDProcessing.Decrypt(File.ReadAllText(filePath[1]), key, iv);
            timeElapse = (DateTime.Now - DateTime.Parse(DateElapse)).TotalSeconds;
        }

        if (skillStats)
        {
            timeController.TimeSet(true, 2, (int)tmpTime, (int)timeElapse, skillStats, filePath[0]);
            abilityController.AbillitySet(skillStats, intervalSpawnSkillTime, correctStreak, limitActiveBtn, uid, idQuiz);
        }
        else
        {
            timeController.TimeSet(true, 2, (int)tmpTime, (int)timeElapse);
        }

        StartCoroutine(uiAnimatedActiveType(6));

        timeController.TimeStop(false);

        var EndDate = DateTime.Now.AddSeconds(tmpTime);
        var DateData = EndDate.ToString();
        File.WriteAllText(filePath[0], EDProcessing.Encrypt(DateData, key, iv));
        StartCoroutine(uiAnimatedActiveType(3));
    }

    private void btnBack()
    {
        AudioController.Instance.PlayAudioSFX("ButtonClick");
        StartCoroutine(uiAnimatedActiveType(1));
        setQuizData(false);
    }
    private void btnBackToInput()
    {
        AudioController.Instance.PlayAudioSFX("ButtonClick");
        StartCoroutine(uiAnimatedActiveType(5));
    }

    private void btnEnd()
    {
        if (skillStats)
        {
            AlertController.AlertSet("Quiz Telah Selesai !", false, TextAlignmentOptions.Center, false, EndQuiz);
        }
        else
        {
            AudioController.Instance.PlayAudioSFX("ButtonClick");
            AlertController.AlertSet("Apakah kamu yakin ingin langsung menyelesaikannya ?", false, TextAlignmentOptions.Center, true, EndQuiz);
        }
        abilityController.AbillitySet();
    }

    private void btnExit()
    {
        AudioController.Instance.PlayAudioSFX("ButtonClick");
        AlertController.AlertSet("Apakah kamu yakin ingin keluar ?", false, TextAlignmentOptions.Center, true, Application.Quit);
    }

    private void EndQuiz()
    {
        QuizController.ResultQuiz();
        timeController.setTimeElapse(false);
        timeController.TimeStop(true);
        abilityController.AbillitySet(); //default false.
        StartCoroutine(uiAnimatedActiveType(4));
        //AudioController.Instance.bgmSource.Stop();
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

        var idList = new List<string>();
        if(snapshot.Children.Select(child => child.Key) == null)
        {
            AlertController.AlertSet("Kode Salah Atau Tidak Ditemukan.", true, TextAlignmentOptions.Center);
        }
        else
        {
            idList = snapshot.Children.Select(child => child.Key).ToList();

            if (idList.Any(id => string.Equals(id, quizKey, StringComparison.OrdinalIgnoreCase)))
            {
                StartCoroutine(getQuizData(quizKey));
            }
            else
            {
                AlertController.AlertSet("Kode Salah Atau Tidak Ditemukan.", true, TextAlignmentOptions.Center);
            }
        }
        yield return new WaitForSeconds(animatedTime);
        quizKeyInput.text = null;
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
        skillStats = bool.Parse(snapshot.Child("quiz_skillActive").Value.ToString());
        qPoint = int.Parse(snapshot.Child("quiz_questionPoint").Value.ToString());
        qSize = int.Parse(snapshot.Child("quiz_maxsoaldisplay").Value.ToString());
        dateStart = snapshot.Child("quiz_timeStart").Value.ToString()/*.Replace(" AM", string.Empty).Replace(" PM", string.Empty)*/;
        dateEnd = snapshot.Child("quiz_timeEnd").Value.ToString()/*.Replace(" AM", string.Empty).Replace(" PM", string.Empty)*/;
        timeToSet = int.Parse(snapshot.Child("quiz_timeSet").Value.ToString());

        uid = uiComponent[5].transform.GetChild(2).gameObject.GetComponent<TMP_Text>().text;

        //atur formating agar terbaca oleh game
        if (DateTime.TryParseExact(dateStart, "dd/MM/yyyy HH:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime startDate) && DateTime.TryParseExact(dateEnd, "dd/MM/yyyy HH:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime endDate))
        {
            //masukan dan timpa tgl yang di dapat dari server dengan tgl yang telah di formating.
            dateStart = startDate.ToString();
            dateEnd = endDate.ToString();
        }

        //parsing ulang agar tanggal tidak error
        dateStart = DateTime.Parse(dateStart).ToString();
        dateEnd = DateTime.Parse(dateEnd).ToString();

        var dateUIStart = uiComponent[7].transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
        var dateUIEnd = uiComponent[7].transform.GetChild(2).gameObject.GetComponent<TMP_Text>();

        uiComponent[3].SetActive(skillStats);
        //uiComponent[7].SetActive(!skillStats);
        uiComponent[7].transform.GetChild(1).gameObject.SetActive(!skillStats);
        uiComponent[7].transform.GetChild(2).gameObject.SetActive(!skillStats);
        ActionBtn[7].gameObject.SetActive(skillStats);

        try
        {
            //var maxTime = DateTime.Parse(dateEnd) - DateTime.Parse(dateStart);

            if (!skillStats) // false
            {
                dateUIStart.text = dateStart;
                dateUIEnd.text = dateEnd;
                //var MaxTime = DateTime.Parse(dateEnd) - DateTime.Parse(dateStart);
                //timeToSet = (int)MaxTime.TotalMinutes;
            }
            else
            {
                dateUIStart.text = dateStart;
            }
        }
        catch (Exception e)
        {
            AlertController.AlertSet(e.Message, true, TextAlignmentOptions.Center);
        }

        try
        {
            QuizController.getQuestData(uid, idQuiz, qSize, qPoint, skillStats);
            filePath[0] = Path.Combine(Application.persistentDataPath, EDProcessing.HashSHA384("sessionQuizData"), EDProcessing.HashSHA384($"{uid}_{idQuiz}_{skillStats}_QTD"));
            filePath[1] = Path.Combine(Application.persistentDataPath, EDProcessing.HashSHA384("sessionQuizData"), EDProcessing.HashSHA384($"{uid}_{idQuiz}_{skillStats}_QTDE"));
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        StartCoroutine(uiAnimatedActiveType(2));
        setQuizData(true);

    }

    void setQuizData(bool isActive)
    {
        if(isActive == true)
        {
            string stat;

            if (skillStats)
            {
                stat = "Tersedia";
            }
            else
            {
                stat = "Tidak Tersedia";
            }

            quizMenuText[0].text = idQuiz;
            quizMenuText[1].text = descQuiz;
            quizMenuText[2].text = timeToSet.ToString() + (skillStats ? $" Menit Permulaan" : " Menit");
            quizMenuText[3].text = qSize.ToString() + " Soal";
            quizMenuText[4].text = "Skill " + stat;
        }
        else
        {
            for(int i = 0; i < quizMenuText.Length; i++)
            {
                quizMenuText[i].text = null;
            }
        }
        
    }

    private IEnumerator uiAnimatedActiveType(int typ)
    {
        if(typ == 0)
        {
            for(int i = 0; i < uiComponent.Length; i++)
            {
                uiComponent[i].SetActive(false);
            }
        }
        //display input container
        else if( typ == 1)
        {
            if(uiComponent[4].transform.localScale == Vector3.one)
            {
                uiComponent[1].transform.LeanScale(Vector3.zero, animatedTime);
                yield return new WaitUntil(() => uiComponent[1].transform.localScale == Vector3.zero);
                uiComponent[1].SetActive(false);

                if (!uiComponent[1].activeSelf)
                {
                    
                    uiComponent[0].SetActive(true);
                    uiComponent[0].transform.LeanScale(Vector3.one, animatedTime);
                }

            }
            else
            {
                uiComponent[1].SetActive(false);
                uiComponent[0].SetActive(true);
            }

            ActionBtn[1].interactable = false;
        }
        //display desc container
        else if (typ == 2)
        {
            if(uiComponent[4].transform.localScale == Vector3.one)
            {
                uiComponent[0].transform.LeanScale(Vector3.zero, animatedTime);
                yield return new WaitUntil(() => uiComponent[0].transform.localScale == Vector3.zero);
                uiComponent[0].SetActive(false);
                uiComponent[1].SetActive(true);
                uiComponent[1].transform.LeanScale(Vector3.one, animatedTime);
            }
            else
            {
                uiComponent[0].SetActive(false);
                uiComponent[1].SetActive(true);
            }
            
        }
        //display quiz container
        else if(typ == 3)
        {
            uiComponent[4].transform.LeanScale(Vector3.zero, animatedTime).setEaseInBack();
            yield return new WaitUntil(() => uiComponent[4].transform.localScale == Vector3.zero);
            uiComponent[4].SetActive(false);
            //uiComponent[1].SetActive(false);
            //uiComponent[0].SetActive(true);
            uiComponent[2].SetActive(true);
            uiComponent[2].transform.LeanScale(Vector3.one, animatedTime).setEaseOutBack();
            uiComponent[1].transform.LeanScale(Vector3.zero, animatedTime);
            ActionBtn[3].transform.LeanScale(Vector3.zero, animatedTime);
            ActionBtn[6].transform.LeanScale(Vector3.zero, animatedTime);
            AudioController.Instance.bgmAudioFadeOut(animatedTime);
        }
        //display menu container + result
        else if(typ == 4)
        {
            uiComponent[0].SetActive(false);
            uiComponent[1].SetActive(false);

            uiComponent[2].transform.LeanScale(Vector3.zero, animatedTime).setEaseInBack();
            yield return new WaitUntil(() => uiComponent[2].transform.localScale == Vector3.zero);
            uiComponent[2].SetActive(false);
            uiComponent[4].SetActive(true);
            uiComponent[4].transform.LeanScale(Vector3.one, animatedTime).setEaseOutBack();
            yield return new WaitUntil(() => uiComponent[4].transform.localScale == Vector3.one);
            uiComponent[8].SetActive(true);
            uiComponent[8].transform.LeanScale(Vector3.one, animatedTime);
            ActionBtn[3].transform.LeanScale(Vector3.one, animatedTime);
            ActionBtn[6].transform.LeanScale(Vector3.one, animatedTime);
            AudioController.Instance.bgmAudioFadeOut(animatedTime);

        }
        // result => input
        else if(typ == 5)
        {
            if (uiComponent[4].transform.localScale == Vector3.one)
            {
                uiComponent[8].transform.LeanScale(Vector3.zero, animatedTime);
                yield return new WaitUntil(() => uiComponent[8].transform.localScale == Vector3.zero);
                uiComponent[8].SetActive(false);

                if (!uiComponent[8].activeSelf)
                {

                    uiComponent[0].SetActive(true);
                    uiComponent[0].transform.LeanScale(Vector3.one, animatedTime);
                }

            }
            else
            {
                uiComponent[8].SetActive(false);
                uiComponent[0].SetActive(true);
            }

            ActionBtn[1].interactable = false;
        }
        else if(typ == 6)
        {
            //control setting ui & ability guide ui
            if (uiComponent[9].activeSelf)
            {
                uiComponent[9].transform.LeanScale(Vector3.zero, animatedTime).setEaseInBack();
                yield return new WaitUntil(()=> uiComponent[9].transform.localScale == Vector3.zero);
                uiComponent[9].SetActive(false);
            }

        }
    }
}

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
    private double tmpTime;
    private string filePath;

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

        foreach(var btn in ActionBtn)
        {
            btn.onClick.RemoveAllListeners();
        }

        ActionBtn[0].onClick.AddListener(btnQuiz);
        ActionBtn[1].onClick.AddListener(btnStart);
        ActionBtn[1].interactable = false;
        ActionBtn[2].onClick.AddListener(btnBack);
        ActionBtn[3].onClick.AddListener(btnExit);
        ActionBtn[4].onClick.AddListener(btnBackToInput);
        ActionBtn[5].onClick.AddListener(btnEnd);

        //initial animated
        uiAnimatedActiveType(1);
        uiComponent[2].transform.localScale = Vector3.zero;
        uiComponent[1].transform.localScale = Vector3.zero;
        uiComponent[8].transform.localScale = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        BackgroundController.BGAnimSet(1, 1, 0.05f);
        CheckInternetAvailability();

        var timeRemain = timeController.getTime();
        if (timeRemain <= 1 && timeController.getActiveState() == true && uiComponent[2].activeSelf == true)
        {
            timeController.TimeStop(true, false);
            AlertController.AlertSet("Waktu Telah Habis.", true, TextAlignmentOptions.Center, false, EndQuiz);
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

                if (uiComponent[6].activeSelf == true)
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

    private void btnQuiz() //btnQuiz
    {
        var qText = quizKeyInput.text.ToUpper();

        if (string.IsNullOrEmpty(qText) || string.IsNullOrWhiteSpace(qText))
        {
            AlertController.AlertSet("Tidak boleh kosong atau ada spasi di dalamnya. ", true, TextAlignmentOptions.Center);
        }
        else
        {
            StartCoroutine(findQuizKey(qText));
        }
    }

    private void btnStart()
    {
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

                if (File.Exists(filePath))
                {
                    if(remainTime > timeSet)
                    {
                        var DateData = EDProcessing.Decrypt(File.ReadAllText(filePath), key, iv);
                        //var FullDate = DateData.Split(",");
                        var remainingTimeOnFile = DateTime.Parse(DateData).Subtract(StartDate).TotalSeconds;

                        if((int)remainingTimeOnFile == 0 || (int)remainingTimeOnFile <= 1)
                        {
                            AlertController.AlertSet("Waktu Telah Habis !");
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

                        AlertController.AlertSet("Ingat, Yang kamu kerjakan di perangkat ini, tidak dapat di bawa ke perangkat lain!", false, TextAlignmentOptions.Center, true, processStart);
                    }
                }
            }
            else
            {
                AlertController.AlertSet("Waktu Telah Habis !");
                DeleteData();
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
            if (File.Exists(filePath))
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

                var DateData = EDProcessing.Decrypt(File.ReadAllText(filePath), key, iv);
                //var FullDate = DateData.Split(",");
                var remainingTimeOnFile = DateTime.Parse(DateData).Subtract(StartDate).TotalSeconds;
                if((int)remainingTimeOnFile == 0 || (int)remainingTimeOnFile <= 1)
                {
                   AlertController.AlertSet("Waktu Telah Habis, Semua Soal & Jawaban Telah Di Reset!");
                   DeleteData();
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

    private void DeleteData()
    {
        string[] fileName = { $"{uid}_{idQuiz}_{skillStats}_QD", $"{uid}_{idQuiz}_{skillStats}_AD", $"{uid}_{idQuiz}_{skillStats}_ID", $"{uid}_{idQuiz}_{skillStats}_QTD" }; //contoh : 10011232_QUIZ03_Q
        foreach (var nameFile in fileName)
        {
            var path = Path.Combine(Application.persistentDataPath, EDProcessing.HashSHA384("sessionQuizData"), EDProcessing.HashSHA384(nameFile));
            if (File.Exists(path))
            {
                //menghapus file yang ditetapkan.
                File.Delete(path);
            }
        }
    }

    private void processStart()
    {
        //timeController.SetTimeStopped(false);

        if (skillStats)
        {
            timeController.TimeSet(true, 2, (int)tmpTime, skillStats, filePath);
        }
        else
        {
            timeController.TimeSet(true, 2, (int)tmpTime);
        }

        var EndDate = DateTime.Now.AddSeconds(tmpTime);
        var DateData = EndDate.ToString();
        File.WriteAllText(filePath, EDProcessing.Encrypt(DateData, key, iv));
        StartCoroutine(uiAnimatedActiveType(3));
    }

    private void btnBack()
    {
        StartCoroutine(uiAnimatedActiveType(1));
        setQuizData(false);
    }
    private void btnBackToInput()
    {
        StartCoroutine(uiAnimatedActiveType(5));
    }

    private void btnEnd()
    {
        AlertController.AlertSet("Apakah kamu yakin ingin langsung menyelesaikannya ?", false, TextAlignmentOptions.Center, true, EndQuiz);
    }

    private void btnExit()
    {
        AlertController.AlertSet("Apakah kamu yakin ingin keluar ?", false, TextAlignmentOptions.Center, true, Application.Quit);
    }

    private void EndQuiz()
    {
        QuizController.ResultQuiz();
        timeController.TimeStop(true, false);
        StartCoroutine(uiAnimatedActiveType(4));
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

            if (idList.Any(id => id == quizKey))
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
        abilityController.AbillitySet(skillStats);

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
            filePath = Path.Combine(Application.persistentDataPath, EDProcessing.HashSHA384("sessionQuizData"), EDProcessing.HashSHA384($"{uid}_{idQuiz}_{skillStats}_QTD"));
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
            quizMenuText[2].text = timeToSet.ToString() + " Menit";
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
                uiComponent[0].transform.LeanScale(Vector3.zero, animatedTime).setEaseInBack();
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
            uiComponent[4].transform.LeanScale(Vector3.zero, animatedTime);
            yield return new WaitUntil(() => uiComponent[4].transform.localScale == Vector3.zero);
            uiComponent[4].SetActive(false);
            //uiComponent[1].SetActive(false);
            //uiComponent[0].SetActive(true);
            uiComponent[2].SetActive(true);
            uiComponent[2].transform.LeanScale(Vector3.one, animatedTime);
            uiComponent[1].transform.LeanScale(Vector3.zero, animatedTime);
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
            uiComponent[4].transform.LeanScale(Vector3.one, animatedTime);
            yield return new WaitUntil(() => uiComponent[4].transform.localScale == Vector3.one);
            uiComponent[8].SetActive(true);
            uiComponent[8].transform.LeanScale(Vector3.one, animatedTime);
            
        }
        // result => input
        else if(typ == 5)
        {
            if (uiComponent[4].transform.localScale == Vector3.one)
            {
                uiComponent[8].transform.LeanScale(Vector3.zero, animatedTime).setEaseInBack();
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
    }
}

using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public class QuizRDB : MonoBehaviour
{
    DatabaseReference dbref;
    //FirebaseStorage fs;

    //UI Text Component for Quiz Data.
    [Header("Ui Component Needed")]
    [Tooltip("Inputfield Answer Essay Type")]
    private TMP_InputField descAnswerText;
    
    [Header("Ui Component Needed (Actions)")]
    [Tooltip("0 = text, 1 = img")]
    public GameObject[] QuestContainer; // 0 = text, 1 = img
    //[Tooltip("0 = multi, 1 = essay, 2 = multiimgd, 3 = multidimg,4 = multiimgf,5 = essayimgd")]
    [Tooltip("0 = multi, 1 = essay")]
    public GameObject[] AnswerContainer; // 0 = multi, 1 = essay
    [Tooltip("max 5 multiple answer btn & first obj child must text, and seconds obj child must img")]
    public Button[] btnMultiAnswers;
    [Tooltip("0 = Next, 1 = Prev, 2 = End")]
    public Button[] btnNavigator;
    [Tooltip("For Displaying Index Questions")]
    public Button btnIndexQuest;

    [Header("Button Menu Quiz")]
    public Button btnStartQuiz;
    private TMP_Text btnStartText;

    [Header("Result UI Needed")]
    [Tooltip("Text Component : 0 = QuizKeyResult, 1 = Point(Alphabet), 2 = Point(Number), 3 = MaxQuestion, 4 = NoAnswer, 5 = CorrectAnswer, 6 = InCorrectAnswer, 7 = TimeRemaining ")]
    public TMP_Text[] ResultComponentText;

    [Header("ScoreUI")]
    public GameObject ScoreObj;
    private Vector3 ScorePopupPos;
    private float ScorePopupYPos = 100f;
    private float ScorePopupDefYPos;

    [Header("Game Object Needed")]
    [SerializeField] AlertController alertController;
    [SerializeField] TimeController timeController;

    //Deklarasi Firebase
    [Header("Firebase Require Variable")]
    //private protected string fsUrl = "gs://theaswerqmaster.appspot.com"; //firebase storage reference url
    private protected string tableQuizName = "data_quizsoal"; // table name from firebase.
    private protected string tableResultName = "data_quizresult"; // table name from firebase.
    private protected string tableQuestionsDataRecordName = "data_quizrecords"; // table name from firebase.
    private protected string tableResultRank = "data_quizrank"; // table name from firebase.
    private string uID; 
    private string quizkey; //future implemented to automatic get by input user (on progress...)
    private protected int questSize; //max soal to load. in future this value fixed get from firebase. (on progress...)
    private bool skillstat;

    //String for store another Quiz Data
    private protected string questType;
    private protected List<string> correctAnswer = new List<string>();
    private protected int pointQuest; //nilai point quest jika skill diaktifkan.
    private protected double pointQuestTrue; //nilai point quest jika skill diaktifkan.
    private protected double pointQuestFalse = 0; //nilai point quest jika skill diaktifkan.


    int curpageIndex = 0; //initial page
    const int pageSize = 1; //1 quest per page. default is 1
    int countingdownload = 0;
    int faildownload = 0;

    private float animatedTime = 0.25f;
    private string[] fileName;

    //untuk pendataan soal dengan jawaban benar, salah & tidak terisi.
    private int truePoint, falsePoint, hasAnswer, noAnswer;
    private double totalScore;
    private string pointAlphabet;

    //list string untuk kelola data lokal.
    private List<string> ids = new List<string>(); //for store id soal
    private List<string> currentIds = new List<string>(); //for current page selected
    private List<string> answers = new List<string>(); //for store answer
    List<AnswersData> jawabanSoalList = new List<AnswersData>();
    List<QuestionsData> dataSoalList = new List<QuestionsData>(); //for store questions data

    DateTime curDate;

    //AES Key & IV 16 byte
    private static readonly byte[] key = Encoding.UTF8.GetBytes("AzraRakobarReinz"); // Ganti dengan kunci rahasia Anda
    private static readonly byte[] iv = Encoding.UTF8.GetBytes("0721200007212024"); // Ganti dengan initial vector Anda

    // Start is called before the first frame update
    void Start()
    {
        //FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        //{
        //    var dependencyStatus = task.Result;
        //    if (dependencyStatus == DependencyStatus.Available)
        //    {
                
        //        // Create and hold a reference to your FirebaseApp,
        //        // where app is a Firebase.FirebaseApp property of your application class.
        //        //FirebaseApp app = FirebaseApp.DefaultInstance;
        //        //fs = FirebaseStorage.DefaultInstance;
                
        //        //FirebaseApp.Create();
        //    }
        //    else
        //    {
        //        UnityEngine.Debug.LogError(System.String.Format(
        //          "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
        //        // Firebase Unity SDK is not safe to use here.
        //    }
        //});

        //FirebaseDatabase.DefaultInstance.PurgeOutstandingWrites();
        //FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);
        dbref = FirebaseDatabase.DefaultInstance.RootReference;

        pointQuestTrue = pointQuest;

        btnNavigator[0].onClick.AddListener(NextPage);
        btnNavigator[1].onClick.AddListener(PreviousPage);
        btnNavigator[2].gameObject.SetActive(false);

        btnStartText = btnStartQuiz.transform.GetComponentInChildren<TMP_Text>();
        descAnswerText = AnswerContainer[1].transform.GetComponentInChildren<TMP_InputField>();


        ScoreObj.transform.GetChild(1).transform.gameObject.SetActive(false);
        ScorePopupPos = ScoreObj.transform.GetChild(1).transform.localPosition;
        ScorePopupDefYPos = ScorePopupPos.y;
        ScorePopupPos.y = ScorePopupYPos;
        ScoreObj.transform.GetChild(1).transform.localPosition = ScorePopupPos;

    }

    private void Update()
    {
        curDate = DateTime.Now;
        StartCoroutine(ActiveBtnEnd(hasAnswer == questSize ? true : false));
    }

    public void getQuestData(string uid, string qkey, int questionSize, int questionPoint, bool skillstat)
    {
        //reset Data.
        hasAnswer = 0;
        noAnswer = 0;

        curpageIndex = 0;
        ids.Clear();
        currentIds.Clear();
        dataSoalList.Clear();
        jawabanSoalList.Clear();


        this.skillstat = skillstat;
        questSize = questionSize;
        quizkey = qkey;
        uID = uid;
        pointQuest = questionPoint;

        btnNavigator[0].gameObject.SetActive(!skillstat);
        btnNavigator[1].gameObject.SetActive(!skillstat);
        ScoreObj.SetActive(skillstat);

        StartCoroutine(getQuestData(tableQuizName, qkey));
    }

    public int getMaxQuestionData()
    {
        return ids.Count;
    }

    IEnumerator ActiveBtnEnd(bool status)
    {
        if (status)
        {
            if (!btnNavigator[2].gameObject.activeSelf)
            {
                btnNavigator[2].gameObject.SetActive(true);
                btnNavigator[2].transform.LeanScale(Vector3.one, animatedTime);
            }
        }
        else
        {
            if (btnNavigator[2].gameObject.activeSelf)
            {
                btnNavigator[2].transform.LeanScale(Vector3.zero, animatedTime).setEaseInBack();
                yield return new WaitUntil(()=> btnNavigator[2].transform.localScale == Vector3.zero);
                btnNavigator[2].gameObject.SetActive(false);
            }
        }
    }

    IEnumerator getQuestData(string tableReference, string quizKeyReference)
    {
        //dbref = FirebaseDatabase.DefaultInstance.RootReference;
        // Set query & Get data dari Firebase

        //pembersihan data quiz
        
        var matchedID = new List<string>();
        var matchedData = new List<QuestionsData>();
        var matchedAvaliData = new List<QuestionsData>();
        var matchedDifID = new List<string>();

        var query = dbref.Child(tableReference).Child(quizKeyReference).OrderByKey();
        var fetchDataTask = query.GetValueAsync();
        yield return new WaitUntil(() => fetchDataTask.IsCompleted); //tunggu hingga pengambilan data selesai.

        if(fetchDataTask.IsFaulted || fetchDataTask.IsCanceled)
        {
            if (fetchDataTask.Exception != null)
            {
                // Handling error
                //Debug.LogError(fetchDataTask.Exception);
                yield return fetchDataTask.Exception.InnerException.Message;
            }
        }
        else
        {
            DataSnapshot snapshot = fetchDataTask.Result;

            //ambil semua data soal (id,desc,tipe,opsi,jawaban_benar) dan simpan ke class string DataSoal.
            var dataSoal = snapshot.Children.Select(child => new QuestionsData
            {
                QuestionID = child.Key,
                QuestionDescription = child.Child("soal_desc").Value.ToString(),
                QuestionIMGURL = child.HasChild("files") ? child.Child("files").Value.ToString() : string.Empty,
                QuestionType = child.Child("soal_type").Value.ToString(),
                QuestionOptions = Enumerable.Range(1, btnMultiAnswers.Length) // Membuat urutan 1 sampai (ukuran listMultiAnswerText.Length)
                .Select(i => child.Child($"option_{i}").Value.ToString()) // Mengambil nilai dari option_1, option_2, ..., option_5
                .ToArray(),
                //QuestionRightAnswer = child.Child("true_answ").Value.ToString()
                QuestionRightAnswer = child.Child("true_answ").HasChildren ? child.Child("true_answ").Children.Select(snapshot => snapshot.Value.ToString()).ToArray() : new string[] { child.Child("true_answ").Value.ToString() }


            }).ToList();

            SattoloShuffle(dataSoal); //randomisasi dengan algoritma Sattolo Shuffle.
            var dataSoalTaken = dataSoal.Take(questSize).ToList(); //ambil dataSoal yang sudah dirandomifikasi dengan jumlah yang sudah diatur pada questSize.

            //inisiasi penamaan dan alamat file untuk data soal, jawaban, dan index soal.
            
            fileName = new string[] { $"{uID}_{quizkey}_{skillstat}_QD", $"{uID}_{quizkey}_{skillstat}_AD", $"{uID}_{quizkey}_{skillstat}_ID" , $"{uID}_{quizkey}_{skillstat}_QTD" }; //contoh : 10011232_QUIZ03_Q //contoh : 10011232_QUIZ03_Q

            var filePath = new List<string>(); //wadah untuk path file yang di Hashing
            foreach (var nameFile in fileName)
            {
                filePath.Add($"{Path.Combine(Application.persistentDataPath, EDProcessing.HashSHA384("sessionQuizData"), EDProcessing.HashSHA384(nameFile))}");
            }

            //cek apakah ada data soal pada penyimpanan internal
            if (File.Exists(filePath[0]))
            {
                //proses memasukan data pada soal dalam file ke dalam objek list localDataSoal
                int found = 0;
                string readData = File.ReadAllText(filePath[0]);
                var decryptQuestionsData = EDProcessing.Decrypt(readData, key, iv);
                var loadFromFile = decryptQuestionsData.Split('\n');
                var localDataSoal = new List<QuestionsData>();
                foreach (var localSoal in loadFromFile)
                {
                    var valSoal = JsonUtility.FromJson<QuestionsData>(localSoal);
                    localDataSoal.Add(valSoal);
                }

                //inisialisasi data soal yang sudah di load di penyimpanan internal, lalu melakukan pengecekan data.
                foreach (var localData in localDataSoal)
                {
                    //melakukan pencarian data yang sama.
                    var findMatchItem = dataSoal.Find(data =>
                    data.QuestionID == localData.QuestionID &&
                    data.QuestionDescription == localData.QuestionDescription &&
                    data.QuestionIMGURL == localData.QuestionIMGURL &&
                    data.QuestionType == localData.QuestionType &&
                    string.Join("|", data.QuestionOptions) == string.Join("|", localData.QuestionOptions) &&
                    string.Join("|", data.QuestionRightAnswer) == string.Join("|", localData.QuestionRightAnswer));

                    if (findMatchItem != null)
                    {
                        found++; //hitung data yang sama
                        matchedData.Add(findMatchItem);
                        matchedID.Add(findMatchItem.QuestionID);
                    }

                }

                foreach (var localData in localDataSoal)
                {
                    //melakukan pencarian data yang tidak sama berdasarkan ID.
                    var findMatchItem = dataSoal.Find(data =>
                    data.QuestionID == localData.QuestionID && (
                    data.QuestionDescription != localData.QuestionDescription ||
                    data.QuestionIMGURL != localData.QuestionIMGURL ||
                    data.QuestionType != localData.QuestionType ||
                    string.Join("|", data.QuestionOptions) != string.Join("|", localData.QuestionOptions) ||
                    string.Join("|", data.QuestionRightAnswer) != string.Join("|", localData.QuestionRightAnswer)));

                    if (findMatchItem != null)
                    {
                        matchedDifID.Add(findMatchItem.QuestionID); //tambahkan data yang tidak sama ke list lokal matchedDifData.
                    }

                }

                //pengambilan data soal yang tidak sama dengan data lokal
                foreach (var localData in localDataSoal)
                {
                    //melakukan pencarian data yang tidak sama.
                    var findMatchItem = dataSoal.Find(data =>
                    data.QuestionID != localData.QuestionID &&
                    data.QuestionDescription != localData.QuestionDescription &&
                    data.QuestionIMGURL != localData.QuestionIMGURL &&
                    data.QuestionType != localData.QuestionType &&
                    string.Join("|", data.QuestionOptions) != string.Join("|", localData.QuestionOptions) &&
                    string.Join("|", data.QuestionRightAnswer) != string.Join("|", localData.QuestionRightAnswer));

                    if (findMatchItem != null)
                    {
                        matchedAvaliData.Add(findMatchItem); //tambahkan data yang tidak sama ke list lokal matchedDifData.
                    }

                }


                //jika jumlah data yang sama sesuai dengan pengaturan dari questSize, maka eksekusi data soal yang berada di penyimpanan internal.
                if (found == questSize)
                {
                    ids.AddRange(localDataSoal.Select(entry => entry.QuestionID).ToList()); //menyimpan data id soal pada variabel global ids.
                    dataSoalList.AddRange(localDataSoal); //simpan data soal yang sudah di load sebelumnya. 

                    if (File.Exists(filePath[1])) //pengecekan file jawaban.
                    {
                        var fileAnswerData = File.ReadAllText(filePath[1]); //baca data jawaban.
                        var decryptAnswerData = EDProcessing.Decrypt(fileAnswerData, key, iv); //mendeskripsikan data soal
                                                                                               //proses memasukan data jawaban ke dalam sesi game
                        var dataAnswerLines = decryptAnswerData.Split('\n');
                        foreach (var dataAnswer in dataAnswerLines)
                        {
                            var jsonAnswerData = JsonUtility.FromJson<AnswersData>(dataAnswer);
                            jawabanSoalList.Add(jsonAnswerData);
                        }

                    }
                    else
                    {
                        Debug.Log("Tidak Terdeteksi Adanya record file tipe AD, Tidak ada proses Load.");
                        NoAnswerAdd(ids);
                    }

                    if (File.Exists(filePath[2])) //pengecekan file untuk last index
                    {
                        string fileIndexData = File.ReadAllText(filePath[2]); //baca data index soal;

                        curpageIndex = int.Parse(EDProcessing.Decrypt(fileIndexData, key, iv));
                    }
                    else
                    {
                        curpageIndex = 0; //load by default 0
                    }

                    PrepairingDownload(localDataSoal);
                }
                else
                {
                    //hapus file data yang tidak sesuai.
                    //File.Delete(filePath[0]); //data soal
                    //File.Delete(filePath[2]); //data index

                    //blok proses perubahan data soal.
                    var remainQDataNeeded = questSize - found;
                    var rfDataQuestions = new List<QuestionsData>();
                    rfDataQuestions.AddRange(matchedData);

                    //pengecekan data berdasarkan id soal, jika di temukan tambahkan ke lokal list rfDataQuestions.
                    foreach(var QuestionID in matchedDifID)
                    {
                        var findMatchDataByID = dataSoal.Find(data => data.QuestionID == QuestionID);

                        //jika findMatchDataByID tidak kosong
                        if (findMatchDataByID != null)
                        {
                            //tambahkan data soal ke rfDataQuestions
                            rfDataQuestions.Add(findMatchDataByID);
                            remainQDataNeeded--; //kurangi index data soal yang dibutuhkan
                        }
                        else
                        {
                            Debug.Log("tidak ada data soal yang tersedia berdasarkan id");
                        }
                    }

                    //jika belum mencapai 0, lakukan penambahan data soal secara random.
                    if (remainQDataNeeded != 0)
                    {
                        if(matchedAvaliData != null)
                        {
                            SattoloShuffle(matchedAvaliData); //lakukan randomifikasi ulang.
                            rfDataQuestions.AddRange(matchedAvaliData.Take(remainQDataNeeded).ToList());
                            remainQDataNeeded = 0;
                        }
                        else
                        {
                            Debug.Log("tidak ada data soal yang tersedia");
                        }
                        
                    }
                    ids.AddRange(rfDataQuestions.Select(entry => entry.QuestionID).ToList()); //menyimpan data id soal pada variabel global ids.
                    dataSoalList.AddRange(rfDataQuestions); //simpan data soal yang sudah di load sebelumnya. 
                    //akhir blok.

                    //blok proses peresetan data jawaban 
                    if (File.Exists(filePath[1])) //pengecekan file jawaban.
                    {
                        var answerData = new List<AnswersData>(); 
                        string fileAnswerData = File.ReadAllText(filePath[1]); //baca data jawaban.

                        var decryptAnswerData = EDProcessing.Decrypt(fileAnswerData, key, iv); //mendeskripsikan data soal
                        //proses memasukan data jawaban ke dalam sesi game
                        var dataAnswerLines = decryptAnswerData.Split('\n');
                        foreach (var dataAnswer in dataAnswerLines)
                        {
                            var jsonAnswerData = JsonUtility.FromJson<AnswersData>(dataAnswer);
                            answerData.Add(jsonAnswerData);
                        }
                        //cari dan ambil data yang id nya terdapat di matchedID.
                        var findSameIDQuestion = answerData.Where(answer => matchedID.Contains(answer.QuestionID)).ToList();
                        string[] targetTipeSoalM = { "Multi", "Multidimg", "Multiimgd", "Multiimgf" };
                        string[] targetTipeSoalE = { "Essay", "Essayimgd" };
                        //cek tipe soal dan jawaban, jika tidak terdapat data yang sama lakukan reset.

                        //if(findSameIDQuestion.Any(data => targetTipeSoalM.Contains(data.QuestionType)))
                        //{

                        //}

                        //ambil data yang idnya tidak ada di findSameIDQuestion.
                        var exceptIDQuestion = answerData.Except(findSameIDQuestion).ToList();
                        answerData.Clear();
                        answerData.AddRange(findSameIDQuestion);

                        //jika id dengan data yang berbeda tidak ditemukan,tambahkan wadah jawaban untuk soal baru.
                        if(exceptIDQuestion.Count != 0)
                        {
                            //proses reset data jawaban...
                            foreach (var difData in exceptIDQuestion)
                            {
                                difData.QuestionType = 0;
                                difData.AnswerDescription = string.Empty;
                                difData.AnswerStatus = false;
                                difData.AnswerEssayPoint = 0;
                                difData.QuestionHasAnswer = false;
                                difData.QuestionTimeTake = 0;

                                answerData.Add(difData);
                            }
                        }                     

                        foreach (var adata in answerData)
                        {
                            Debug.Log($"{adata.QuestionID}, {adata.QuestionType}, {adata.AnswerDescription}, {adata.AnswerStatus}");
                        }

                        jawabanSoalList.AddRange(answerData);
                        answerData.Clear();
                    }

                    if (File.Exists(filePath[2])) //pengecekan file untuk last index
                    {
                        string fileIndexData = File.ReadAllText(filePath[2]); //baca data index soal;
                        curpageIndex = int.Parse(EDProcessing.Decrypt(fileIndexData, key, iv));
                    }
                    else
                    {
                        curpageIndex = 0;
                    }

                    PrepairingDownload(localDataSoal);
                }
            }
            else
            {
                LoadAndSaveDataQuest(dataSoalTaken, filePath[0]);
            }
            filePath.Clear();
        }

    }

    private void LoadAndSaveDataQuest(List<QuestionsData> dataQuests, string filePath)
    {
        //var idList = dataQuests.Select(entry => entry.QuestionID).ToList(); // ambil id soal pada dataQuests.
        ids.AddRange(dataQuests.Select(entry => entry.QuestionID).ToList()); //Menyimpan idList(idsoal) ke variabel list global 'ids'
        dataSoalList.AddRange(dataQuests); //Menyimpan dataSoal ke variabel list 'dataSoalList'
        NoAnswerAdd(ids);
        //simpan data soal dengan format json ke lokal dengan identifikiasi id quiz dan id siswa
        var dataList = new List<string>();
        foreach (var data in dataQuests)
        {
            string jsonData = JsonUtility.ToJson(data);
            dataList.Add(jsonData);
        }
        string encryptedSoalData = EDProcessing.Encrypt(string.Join("\n", dataList), key, iv);
        File.WriteAllText(filePath, encryptedSoalData);

        dataList.Clear();
        PrepairingDownload(dataQuests);
    }

    private void PrepairingDownload(List<QuestionsData> dataQuests)
    {
        //function download
        //metode sementara untuk pengecekan soal yang memiliki data untuk di download.
        string[] targetTipeSoal = { "Multidimg", "Multiimgd", "Multiimgf", "Essayimgd" };
        if (dataQuests.Any(data => targetTipeSoal.Contains(data.QuestionType)))
        {
            string[] subTargetTipeSoal = { "Multidimg", "Multiimgf" };

            var _descImg = dataQuests.Where(data => targetTipeSoal.Contains(data.QuestionType) && data.QuestionIMGURL != string.Empty)
                .Select(data => data.QuestionIMGURL).ToList();

            var _options = dataQuests.Where(data => subTargetTipeSoal.Contains(data.QuestionType))
                .SelectMany(data => data.QuestionOptions).ToList();

            var allUrl = _descImg.Concat(_options).ToList();
            _descImg.Clear();
            _options.Clear();

            foreach (var url in allUrl)
            {
                StartCoroutine(DownloadFile(url, allUrl.Count));
            }
            allUrl.Clear();

            //var _img = dataSoalTaken
            //    .Where(data => targetTipeSoal.Contains(data.QType))
            //    .Select(data => data.IdQ).ToList();
            //Debug.Log("Quest id yang memiliki data untuk di download :" + _img.Count);
            //SearchData(_img); //cari data sekaligus download berdasarkan id soal.
            //memanggil fungsi DisplayCurPage untuk menampilkan data soal berdasarkan idsoal ke UI.
        }
        else if (dataQuests.All(data => !targetTipeSoal.Contains(data.QuestionType)))
        {
            DisplayCurPage();
            btnStartQuiz.interactable = true;
            btnStartText.text = "Mulai";
        }
        else
        {
            //var _img = dataSoalTaken
            //    .Where(data => !targetTipeSoal.Contains(data.QType))
            //    .Select(data => data.IdQ).ToList();
            //Debug.Log("Quest id yang tidak memiliki data untuk di download :" + _img.Count);
            //Debug.Log("Semua quest id tidak memiliki data untuk di download");
        }

        //Debug.Log("Total Questions fetched : " + dataSoalTaken.Count); //mengecek apakah data soal sudah di dapat.
    }

    IEnumerator DownloadFile(string Url, int MaxUrlSelected)
    {
        var fileName = DecodeURL(Url);
        var folderName = $"/qData/{quizkey}/";
        
        bool wError = false;

        //string folderPath = Path.Combine(Application.persistentDataPath, folderName);
        string folderPath = Application.persistentDataPath + folderName ;
        //membuat folder jika tidak ada.
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string localFilePath = Application.persistentDataPath + folderName + fileName; // saving path : (local)/qData/

        if (!File.Exists(localFilePath))
        {
            using (UnityWebRequest www = UnityWebRequest.Get(Url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    // Menyimpan data file yang diunduh ke dalam berkas lokal
                    File.WriteAllBytes(localFilePath, www.downloadHandler.data);

                    //Debug.Log("File downloaded and saved to: " + localFilePath);
                    countingdownload++;
                }
                else
                {
                    Debug.LogError("Download failed: " + www.error);
                }
            }
        }
        else
        {
            long localFileSize = new FileInfo(localFilePath).Length;

            UnityWebRequest sizeRequest = UnityWebRequest.Head(Url);
            yield return sizeRequest.SendWebRequest();

            if (sizeRequest.result == UnityWebRequest.Result.Success)
            {
                // Dapatkan ukuran file di URL
                long urlFileSize = long.Parse(sizeRequest.GetResponseHeader("Content-Length"));

                // Bandingkan ukuran file lokal dengan ukuran file di URL
                if (localFileSize != urlFileSize)
                {
                    //Debug.Log("Ukuran file lokal berbeda dengan ukuran file di URL. Memulai pengunduhan ulang...");

                    // Mulai unduh file baru
                    using (UnityWebRequest www = UnityWebRequest.Get(Url))
                    {
                        yield return www.SendWebRequest();

                        if (www.result == UnityWebRequest.Result.Success)
                        {
                            // Menyimpan data file yang diunduh ke dalam berkas lokal
                            File.WriteAllBytes(localFilePath, www.downloadHandler.data);

                            //Debug.Log("File downloaded and saved to: " + localFilePath);
                            countingdownload++;
                        }
                        else
                        {
                            Debug.LogError("Download failed: " + www.error);
                        }
                    }
                }
                else
                {
                    //Debug.Log("Ukuran file lokal sama dengan ukuran file di URL. Tidak perlu pengunduhan ulang.");
                    countingdownload++;
                }
            }
            else
            {
                Debug.LogError("Gagal mendapatkan ukuran file di URL. Error: " + sizeRequest.error);
                faildownload++;
                wError = true;
            }
        }

        if(countingdownload == MaxUrlSelected && !wError)
        {
            //Debug.Log("Semua File Telah Di download");
            DisplayCurPage();
            countingdownload = 0;
            btnStartText.text = "Mulai";
            btnStartQuiz.interactable = true;
        }
        else if (countingdownload + faildownload == MaxUrlSelected && wError)
        {
            //Debug.Log("terdownload :" + countingdownload + ", tidak terdownload : " + faildownload + ", dikarenakan error");
            alertController.AlertSet("Terdapat kegagalan dalam memproses data soal\nTampilan soal tidak lengkap.", false, TextAlignmentOptions.Center);
            btnStartText.text = $"Mulai {countingdownload}/{MaxUrlSelected}";
            countingdownload = 0;
            DisplayCurPage();
            btnStartQuiz.interactable = true;
            faildownload = 0;
        }
        else
        {
            var loadingProgress = countingdownload / MaxUrlSelected * 100;
            btnStartText.text = $"Downloading: {loadingProgress} %";
            //Debug.Log("Terdownload : " + countingdownload + "dari" + MaxUrlSelected);
        }
    }

    string DecodeURL(string url)
    {

        // Mencari indeks awal dan akhir nama file dalam URL
        int startIndex = url.LastIndexOf("/") + 1;
        int endIndex = url.LastIndexOf("?");

        // Mengambil substring yang berisi nama file

        string brokenfileName = url.Substring(startIndex, endIndex - startIndex);
        string decodedUrl = UnityWebRequest.UnEscapeURL(brokenfileName);

        string[] fileName = decodedUrl.Split("/");

        string lastpart = fileName[fileName.Length - 1];

        return lastpart;
    }
    public void ResultQuiz()
    {
        try
        {
            double cResult = 0;

            if (skillstat)
            {
                var maxScore = pointQuest * questSize;
                cResult = totalScore;

                switch (cResult)
                {
                    case var _ when cResult > maxScore:
                        pointAlphabet = "S+";
                        break;
                    case var _ when cResult >= (maxScore * 0.95) && cResult <= maxScore:
                        pointAlphabet = "S";
                        break;
                    case var _ when cResult >= (maxScore * 0.90) && cResult <= (maxScore * 0.94):
                        pointAlphabet = "A+";
                        break;
                    case var _ when cResult >= (maxScore * 0.85) && cResult <= (maxScore * 0.89):
                        pointAlphabet = "A";
                        break;
                    case var _ when cResult >= (maxScore * 0.80) && cResult <= (maxScore * 0.84):
                        pointAlphabet = "A-";
                        break;
                    case var _ when cResult >= (maxScore * 0.75) && cResult <= (maxScore * 0.79):
                        pointAlphabet = "B+";
                        break;
                    case var _ when cResult >= (maxScore * 0.70) && cResult <= (maxScore * 0.74):
                        pointAlphabet = "B";
                        break;
                    case var _ when cResult >= (maxScore * 0.65) && cResult <= (maxScore * 0.69):
                        pointAlphabet = "B-";
                        break;
                    case var _ when cResult >= (maxScore * 0.60) && cResult <= (maxScore * 0.64):
                        pointAlphabet = "C+";
                        break;
                    case var _ when cResult >= (maxScore * 0.55) && cResult <= (maxScore * 0.59):
                        pointAlphabet = "C";
                        break;
                    case var _ when cResult >= (maxScore * 0.50) && cResult <= (maxScore * 0.54):
                        pointAlphabet = "C-";
                        break;
                    case var _ when cResult >= (maxScore * 0.40) && cResult <= (maxScore * 0.49):
                        pointAlphabet = "D";
                        break;
                    case var _ when cResult <= (maxScore * 0.40):
                        pointAlphabet = "E";
                        break;

                    default:
                        pointAlphabet = "NaN";
                        break;
                }
            }
            else
            {
                cResult = (double)truePoint / (double)questSize * 100;
                switch (cResult)
                {
                    case var _ when cResult >= 90:
                        pointAlphabet = "A+";
                        break;
                    case var _ when cResult >= 85 && cResult <= 89:
                        pointAlphabet = "A";
                        break;
                    case var _ when cResult >= 80 && cResult <= 84:
                        pointAlphabet = "A-";
                        break;
                    case var _ when cResult >= 75 && cResult <= 79:
                        pointAlphabet = "B+";
                        break;
                    case var _ when cResult >= 70 && cResult <= 74:
                        pointAlphabet = "B";
                        break;
                    case var _ when cResult >= 65 && cResult <= 69:
                        pointAlphabet = "B-";
                        break;
                    case var _ when cResult >= 60 && cResult <= 64:
                        pointAlphabet = "C+";
                        break;
                    case var _ when cResult >= 55 && cResult <= 59:
                        pointAlphabet = "C";
                        break;
                    case var _ when cResult >= 50 && cResult <= 54:
                        pointAlphabet = "C-";
                        break;
                    case var _ when cResult >= 40 && cResult <= 49:
                        pointAlphabet = "D";
                        break;
                    case var _ when cResult <= 40:
                        pointAlphabet = "E";
                        break;
                    default:
                        pointAlphabet = "NaN";
                        break;
                }
            }
            
            var timeLapse = timeController.getTimeElapse();
            var timeInSecs = Mathf.FloorToInt(timeLapse % 60);
            var timeInMinutes = Mathf.FloorToInt(timeLapse / 60);

            ResultComponentText[0].text = quizkey;
            ResultComponentText[1].text = pointAlphabet;
            ResultComponentText[2].text = cResult.ToString("F2");
            ResultComponentText[3].text = questSize.ToString();
            ResultComponentText[4].text = noAnswer.ToString();
            ResultComponentText[5].text = truePoint.ToString();
            ResultComponentText[6].text = falsePoint.ToString();
            ResultComponentText[7].text = $"{timeInMinutes:00} : {timeInSecs:00} Menit";
            //StartCoroutine(ActiveBtnEnd(false));
            StartCoroutine(sendDataResult(uID, quizkey, pointAlphabet, double.Parse(cResult.ToString("F2")), truePoint, falsePoint, noAnswer, skillstat ? "Waktu Tidak Terbaca" : $"{timeInMinutes:00} : {timeInSecs:00}"));
        }
        catch(Exception e)
        {
            Debug.Log(e.Message);
        }
        
    }

    private IEnumerator sendDataForRank(string uid, string qKey,string charpoint, int points, int trueAnswer, int falseAnswer, int noAnswer)
    {
        ResultStore dataHasil = new(charpoint, points, trueAnswer, falseAnswer, noAnswer);

        string json = JsonUtility.ToJson(dataHasil);

        var query = dbref.Child(tableResultRank).Child(qKey).Child(uid).SetRawJsonValueAsync(json);//sending data to firebase
        yield return new WaitUntil(() => query.IsCompleted); //tunggu hingga penyimpanan data selesai.

        if (query.Exception != null)
        {
            // Handling error
            Debug.LogError(query.Exception);
            yield break;
        }
    }

    private IEnumerator sendDataResult(string uid, string qKey, string charpoint, double points, int trueAnswer, int falseAnswer, int noAnswer, string timeLapse)
    {
        string dateFormat = curDate.ToString("dd/MM/yyyy HH:mm:ss tt");

        var queryDate = dbref.Child(tableResultName).Child(uid).Child(qKey).GetValueAsync();
        yield return new WaitUntil(() => queryDate.IsCompleted); //tunggu hingga pengambilan data selesai.
        if (queryDate.Exception != null)
        {
            Debug.LogError(queryDate.Exception);
            yield break;
        }

        DataSnapshot snapshot = queryDate.Result;

        var checkdate = snapshot.Child("quiz_dateadded").Value;

        ResultStore dataHasil;
        string json;
        Task query;

        if (checkdate != null)
        {
            dataHasil = new(qKey, charpoint, points, trueAnswer, falseAnswer, noAnswer, timeLapse, checkdate.ToString(), dateFormat);
            json = JsonUtility.ToJson(dataHasil);
            query = dbref.Child(tableResultName).Child(uid).Child(qKey).SetRawJsonValueAsync(json);//sending data to firebase
            yield return new WaitUntil(() => query.IsCompleted);//tunggu hingga penyimpanan data selesai.
            if (query.Exception != null)
            {
                // Handling error
                Debug.LogError(query.Exception);
                yield break;
            }
            
        }
        else
        {
            dataHasil = new(qKey, charpoint, points, trueAnswer, falseAnswer, noAnswer, timeLapse, dateFormat, dateFormat);
            json = JsonUtility.ToJson(dataHasil);
            query = dbref.Child(tableResultName).Child(uid).Child(qKey).SetRawJsonValueAsync(json);//sending data to firebase
            yield return new WaitUntil(() => query.IsCompleted);//tunggu hingga penyimpanan data selesai.
            if (query.Exception != null)
            {
                // Handling error
                Debug.LogError(query.Exception);
                yield break;
            }
        }

        ids.Clear();
        currentIds.Clear();
        dataSoalList.Clear();
        jawabanSoalList.Clear();
        curpageIndex = 0;

        //inisiasi penamaan dan alamat file untuk data soal, jawaban, dan index soal.
        //string[] fileName = { $"{uID}_{quizkey}_{skillstat}_QD", $"{uID}_{quizkey}_{skillstat}_AD", $"{uID}_{quizkey}_{skillstat}_ID", $"{uID}_{quizkey}_{skillstat}_QTD" }; //contoh : 10011232_QUIZ03_Q
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

    //private IEnumerator quizDataRecorded(string idQ, string DescQ, string DescImg, string QType, string[] options, string Trueanswer)
    //{
    //    DateTime curDate = DateTime.Now;

    //    var questionData = new(idQ, DescQ, DescImg, QType, options[], Trueanswer);

    //    string formatedDate = curDate.ToString("ddmmyyyy");

    //    var query = dbref.Child(tableQuestionsDataRecordName).Child(uID).Child(quizkey).Child(formatedDate).SetRawJsonValueAsync(questionData);
    //    yield return new WaitUntil(() => query.IsCompleted); //tunggu hingga penyimpanan data selesai.

    //    if (query.Exception != null)
    //    {
    //        // Handling error
    //        Debug.LogError(query.Exception);
    //        yield break;
    //    }
    //}

    private void indexQuestRT(int index)
    {
        var DateRT = curDate.ToString("dd/MM/YYYY");
        var query = dbref.Child(tableQuestionsDataRecordName).Child(uID).Child(quizkey).Child(DateRT).SetRawJsonValueAsync(index.ToString());

        if (query.Exception != null)
        {
            Debug.LogError(query.Exception);
            return;
        }
    }

    public void checkQuestionStatus() //cek apakah soal yang ditampilkan sudah memiliki jawaban atau tidak.
    {
        foreach(var idQuest in currentIds)
        {
            var matchAnswer = jawabanSoalList.Find(j => j.QuestionID == idQuest);
            if (matchAnswer != null)
            {
                //Debug.Log($"Data Jawaban : {matchAnswer.QuestionID}, {matchAnswer.QuestionHasAnswer}, {matchAnswer.AnswerDescription}, {matchAnswer.AnswerStatus}, {matchAnswer.QuestionType}, {matchAnswer.AnswerEssayPoint}");

                //fitur sementara untuk mengecek soal yang telah di jawab
                if (matchAnswer.QuestionType == 1)
                {
                    //diset untuk soal tipe pilihan ganda.
                    try
                    {
                        foreach (var btnAnswer in btnMultiAnswers)
                        {
                            var answerMulti = btnAnswer.transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
                            var answerIMG = btnAnswer.GetComponent<Image>();

                            answerIMG.color = new Color32(255, 255, 255, 255);

                            if (matchAnswer.QuestionHasAnswer == true)
                            {
                                if (answerMulti.text == matchAnswer.AnswerDescription && (matchAnswer.AnswerDescription != null || matchAnswer.AnswerDescription != string.Empty))
                                {
                                    answerIMG.color = new Color32(255, 255, 0, 255);
                                }
                                else
                                {
                                    answerIMG.color = new Color32(255, 255, 255, 255);
                                }
                            }
                            else
                            {
                                answerIMG.color = new Color32(255, 255, 255, 255);
                            }

                            if (!btnAnswer.IsInteractable())
                            {
                                btnAnswer.interactable = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e.Message);
                    }

                    //for (int i = 0; i < btnMultiAnswers.Length; i++)
                    //{
                    //    var answerMulti = btnMultiAnswers[i].transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
                    //    if (matchAnswer.QuestionHasAnswer)
                    //    {
                    //        if (answerMulti.text == matchAnswer.AnswerDescription)
                    //        {
                    //            btnMultiAnswers[i].GetComponent<Image>().color = Color.yellow;
                    //        }
                    //        else
                    //        {
                    //            btnMultiAnswers[i].GetComponent<Image>().color = Color.white;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        btnMultiAnswers[i].GetComponent<Image>().color = Color.white;
                    //    }

                    //    if (!btnMultiAnswers[i].IsInteractable())
                    //    {
                    //        btnMultiAnswers[i].interactable = true;
                    //    }

                    //}
                }
                else if (matchAnswer.QuestionType == 2)
                {
                    var answerEssay = AnswerContainer[1].transform.GetChild(0).transform.GetComponent<TMP_InputField>();

                    if (matchAnswer.QuestionHasAnswer)
                    {
                        answerEssay.text = matchAnswer.AnswerDescription;
                    }
                }
                else
                {
                    if (matchAnswer.QuestionType != 0)
                    {
                        alertController.AlertSet($"Terdapat Error Pada Soal '{matchAnswer.QuestionID}', Silahkan Hubungi Guru atau Pengelola.", true, TextAlignmentOptions.Center);
                    }
                }

            }
            else
            {
                Debug.Log("No entry found for ID :" + idQuest);
            }
        }

        truePoint = jawabanSoalList.Count(j => j.AnswerStatus); // ambil total jawaban benar dari jawabanSoalList
        falsePoint = jawabanSoalList.Count(j => !j.AnswerStatus); // ambil total jawaban salah dari jawabanSoalList
        //noAnswer = questSize - (truePoint + falsePoint); // jumlah soal - jawaban benar & salah
        noAnswer = jawabanSoalList.Count(j => !j.QuestionHasAnswer); // jumlah HasAnswer != true / yang tidak memiliki jawaban.
        hasAnswer = jawabanSoalList.Count(j => j.QuestionHasAnswer); // jumlah HasAnswer = true / yang memiliki jawaban
        totalScore = jawabanSoalList.Sum(j => j.AnswerScorePoint);

        Debug.Log("Data list jawaban saat ini : " + hasAnswer + " jawaban, dari " + questSize + " soal.");
    }

    public void btnAnswer(int answerTyp)
    {
        foreach (string currentIdSoal in currentIds)
        {
            var newAnswer = new AnswersData(); //buat objek baru untuk soal yang aktif

            newAnswer.QuestionID = currentIdSoal; // Set ID soal yang sedang aktif

            //1 = multiply
            if (answerTyp == 1)
            {
                TMP_Text currentJwb = EventSystem.current.currentSelectedGameObject.transform.GetChild(0).GetComponent<TMP_Text>(); //ambil komponen teks dari object yang terpilih.
                newAnswer.QuestionType = answerTyp;
                newAnswer.AnswerDescription = currentJwb.text; // Set jawaban yang dipilih
                newAnswer.QuestionHasAnswer = true; // Set true ketika sudah dijawab.
                newAnswer.AnswerEssayPoint = 0; //default data if question essay point essay.

                if (newAnswer.AnswerDescription == correctAnswer[0]) //jika jawaban sama dengan kunci jawaban.
                {
                    newAnswer.AnswerStatus = true; // Set true ketika jawaban benar.
                    newAnswer.AnswerScorePoint = skillstat ? pointQuestTrue : 0;
                }
                else
                {
                    newAnswer.AnswerStatus = false; // Set false ketika jawaban salah.
                    newAnswer.AnswerScorePoint = skillstat ? pointQuestFalse : 0;
                }
                

            }
            //2 = essay, becareful this isn't recommended for long answer....
            else if (answerTyp == 2)
            {
                var currentJwb = descAnswerText.text;
                newAnswer.QuestionType = answerTyp;
                newAnswer.AnswerDescription = currentJwb;
                newAnswer.QuestionHasAnswer = true;

                //jaro-winkler performed on here...
                if(string.IsNullOrEmpty(newAnswer.AnswerDescription))
                {
                    newAnswer.AnswerStatus = false;
                    newAnswer.AnswerEssayPoint = 0;
                    newAnswer.AnswerScorePoint = skillstat ? pointQuestFalse : 0;
                }
                else
                {
                    // lakukan pemrosesan teks pada kedua string yang akan di cocokan.
                    string[] userInput = ProcessText(currentJwb);
                    var historySimilarity = new List<double>();

                    for(int i = 0; i < correctAnswer.Count; i++)
                    {
                        if(correctAnswer[i] != null || !string.IsNullOrEmpty(correctAnswer[i]))
                        {
                            string[] keyAnswer = ProcessText(correctAnswer[i]);

                            double similarityScore = CalculateJaroWinklerScore(userInput, keyAnswer); //hitung kemiripan antara input pengguna & kunci jawaban.
                            historySimilarity.Add(similarityScore); //masukan hasil perhitungan kemiripan ke historySimilarity.

                            
                        }
                    }

                    double threshold = 0.70; //batas poin untuk kemiripan string, atur sesuai kebutuhan,

                    //mengambil nilai tertinggi dari historySimilarity dan jika diatas poin threshold maka status jawaban akan benar, jika di bawah maka status jawaban salah.
                    if (historySimilarity.Max() >= threshold)
                    {
                        newAnswer.AnswerStatus = true;
                        newAnswer.AnswerEssayPoint = historySimilarity.Max();
                        newAnswer.AnswerScorePoint = skillstat ? pointQuestTrue : 0;

                    }
                    else
                    {
                        newAnswer.AnswerStatus = false;
                        newAnswer.AnswerEssayPoint = historySimilarity.Max();
                        newAnswer.AnswerScorePoint = skillstat ? pointQuestFalse : 0;

                    }

                    //var similarityPoints = new List<double>();
                    //foreach (var dataAnswers in answers) 
                    //{
                    //    string[] keyAnswer = ProcessText(dataAnswers);
                    //    double similarityScore = CalculateJaroWinklerScore(userInput, keyAnswer);
                    //    similarityPoints.Add(similarityScore);
                    //}

                    //double maxSimilarity = similarityPoints.Max();
                    //if(maxSimilarity >= threshold)
                    //{
                    //    newAnswer.AnswerStatus = true;
                    //    newAnswer.AnswerEssayPoint = maxSimilarity;
                    //}
                    //else
                    //{
                    //    newAnswer.AnswerStatus = false;
                    //    newAnswer.AnswerEssayPoint = maxSimilarity;
                    //}
                    
                }
            }
            else
            {
                alertController.AlertSet("AnswerTyp : Undefined type " + answerTyp + " isn't implemented!", true,TextAlignmentOptions.Center);
            }

            //fitur pengecekan jawaban, jika data jawaban ada maka akan dilakukan proses update, jika tidak ada jawaban akan dilakukan penambahan data.
            if (newAnswer.QuestionHasAnswer)
            {
                //pencarian data jawaban pada jawabanSoalList.
                var matchAnswer = jawabanSoalList.Find(j => j.QuestionID == currentIdSoal);

                /**jika data tidak kosong, akan dilakukan pengecekan data dengan membandingkan jawaban yang sudah ada dengan jawaban baru 
                 * kondisi perubahan akan terjadi jika jawaban lama != jawaban baru. **/

                if (matchAnswer != null)
                {
                    if (matchAnswer.AnswerDescription != newAnswer.AnswerDescription || 
                        matchAnswer.QuestionHasAnswer != newAnswer.QuestionHasAnswer)
                    {
                        matchAnswer.QuestionType = answerTyp;
                        matchAnswer.AnswerDescription = newAnswer.AnswerDescription;
                        matchAnswer.QuestionHasAnswer = newAnswer.QuestionHasAnswer;
                        matchAnswer.AnswerStatus = newAnswer.AnswerStatus;
                        matchAnswer.AnswerEssayPoint = newAnswer.AnswerEssayPoint;
                        matchAnswer.AnswerScorePoint = newAnswer.AnswerScorePoint;

                        Debug.Log($"Data diperbarui untuk ID: {currentIdSoal}");
                    }
                    else
                    {
                        Debug.Log($"Data tidak berubah untuk ID: {currentIdSoal}");
                    }
                }
                //else
                //{
                //    Debug.Log("Tidak ada entri ditemukan untuk ID :" + idQuest + ", Menambahkan Ke list Soal yang dijawab.");

                //    var storedAnswer = new JawabanSoal();

                //    storedAnswer.IdSoal = idQuest;
                //    storedAnswer.SoalType = newAnswer.SoalType;
                //    storedAnswer.Jawaban = newAnswer.Jawaban;
                //    storedAnswer.HasAnswer = newAnswer.HasAnswer;
                //    storedAnswer.Status = newAnswer.Status;
                //    storedAnswer.PointEssay = newAnswer.PointEssay;

                //    jawabanSoalList.Add(storedAnswer);
                //}
            }
            //else
            //{
            //    Debug.Log("ID : " + newAnswer.IdSoal + "Tidak Memiliki Jawaban");
            //}
            
        }
        NextPage();
        //simpan ke storage lokal
        //string fileName = $"{uID}_{quizkey}_{skillstat}_AD";
        string filePath = Path.Combine(Application.persistentDataPath, EDProcessing.HashSHA384("sessionQuizData"), EDProcessing.HashSHA384(fileName[1]));

        var dataList = new List<string>();
        foreach (var data in jawabanSoalList)
        {
            var jsonData = JsonUtility.ToJson(data);
            dataList.Add(jsonData);
        }

        string encryptedSoalData = EDProcessing.Encrypt(string.Join("\n", dataList), key, iv);
        //string dbugData = string.Join("\n", dataList);
        File.WriteAllText(filePath, encryptedSoalData);
        dataList.Clear();
    }
    
    private void NoAnswerAdd(List<string> id) //inisialisasi jawaban kosong dari soal yang telah di list
    {
        string[] targetTipeEssay = { "Essay", "Essayimgd" };
        foreach (string currentIdSoal in id)
        {
            var initialListAnswer = new AnswersData();

            initialListAnswer.QuestionID = currentIdSoal; // Set ID soal yang sedang aktif

            var data = dataSoalList.Find(q => q.QuestionID == currentIdSoal);

            foreach(var typeQuestions in targetTipeEssay)
            {
                if (data.QuestionType == typeQuestions)
                {
                    initialListAnswer.QuestionType = 2;
                }
                else
                {
                    initialListAnswer.QuestionType = 1;
                }
            }

            initialListAnswer.AnswerDescription = string.Empty;
            initialListAnswer.AnswerStatus = false;
            initialListAnswer.AnswerEssayPoint = 0;
            initialListAnswer.AnswerScorePoint = 0;
            initialListAnswer.QuestionHasAnswer = false; //set status jawaban ke false
            initialListAnswer.QuestionTimeTake = 0;

            jawabanSoalList.Add(initialListAnswer);
        }
    }

    void GenerateQuestLocalTemp(string qid)
    {
        var dataSoalToDisplay = dataSoalList.FirstOrDefault(dataSoal => dataSoal.QuestionID == qid); //cocokan data id soal yang di panggil dengan id soal yang ada di data soal.
        GenerateQuestLocalTemp(dataSoalToDisplay); // panggil data soal yang sesuai dengan id soal.
    }
    void GenerateQuestLocalTemp(QuestionsData data)
    {
        string imgPath = Path.Combine(Application.persistentDataPath, $"qData/{quizkey}/");
        string[] targetTipeSoal = { "Multidimg", "Multiimgf" };
        string[] targetTipeEssay = { "Essay", "Essayimgd" };
        //string idQ = $"{data.IdQ}";
        var questText = QuestContainer[0].transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
        questText.text = $"{data.QuestionDescription}";
        questType = $"{data.QuestionType}";
        //bersihkan penambung jawaban benar;
        correctAnswer.Clear();
        //jika tipe soal terdapat pada targetTipeSoal (jawaban benar) berbentuk URL akan di lakukan dekoding untuk mengambil nama file
        if (questType.Any(data => targetTipeSoal.Contains(questType)))
        {
            var decodedURLcorrectAnswer = DecodeURL($"{data.QuestionRightAnswer[0]}");
            correctAnswer.Add(decodedURLcorrectAnswer);
        }
        else
        {
            if (questType.Any(data => targetTipeEssay.Contains(questType)))
            {
                for (int i = 0; i < data.QuestionRightAnswer.Length; i++)
                {
                    correctAnswer.Add(data.QuestionRightAnswer[i]);
                }
            }
            else
            {
                correctAnswer.Add(data.QuestionRightAnswer[0]); 
            }
        }

        if (data.QuestionIMGURL != string.Empty)
        {
            QuestContainer[0].SetActive(true);
            QuestContainer[1].SetActive(true);

            var soal_img_url = $"{data.QuestionIMGURL}";
            var soal_img_name = DecodeURL(soal_img_url);

            string imgQPath = imgPath + soal_img_name;

            if (File.Exists(imgQPath))
            {
                // Baca byte dari file
                byte[] fileData = File.ReadAllBytes(imgQPath);

                // Buat objek Texture2D dan muat data gambar
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);

                // Atur gambar pada RawImage
                var QuestImg = QuestContainer[1].transform.GetChild(0).gameObject.GetComponent<RawImage>();
                QuestImg.texture = texture;
            }
            else
            {
                Debug.Log(imgQPath + ", Tidak Ditemukan");
            }
        }
        else
        {
            QuestContainer[0].SetActive(true);
            QuestContainer[1].SetActive(false);
        }

        if (questType == "Multi") //full text
        {
            AnswerContainer[0].SetActive(true);
            AnswerContainer[1].SetActive(false);

            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                var options = $"{data.QuestionOptions[i]}";
                answers.Add(options); // tambahkan ke list answer
            }

            SattoloShuffle(answers); //randomifikasi menggunakan sattolo

            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {


                var answerMulti = btnMultiAnswers[i].transform.GetChild(0).gameObject;
                var answerImg = btnMultiAnswers[i].transform.GetChild(1).gameObject;
                answerMulti.GetComponent<TMP_Text>().text = answers[i];
                answerImg.SetActive(false);
                answerMulti.SetActive(true);
                //listMultiAnswerText[i].text = answers[i]; //masukan & tampilkan hasil randomifikasi ke UI
            }
            answers.Clear(); //bersihkan data jawaban untuk soal selanjutnya.
        }
        else if (questType == "Multiimgd") // questions text+img + answer text
        {
            AnswerContainer[0].SetActive(true);
            AnswerContainer[1].SetActive(false);

            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                var options = $"{data.QuestionOptions[i]}";
                answers.Add(options); // tambahkan ke list answer
            }

            SattoloShuffle(answers); //randomifikasi menggunakan sattolo

            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                var answerMulti = btnMultiAnswers[i].transform.GetChild(0).gameObject;
                var answerImg = btnMultiAnswers[i].transform.GetChild(1).gameObject;
                answerMulti.GetComponent<TMP_Text>().text = answers[i];
                answerImg.SetActive(false);
                answerMulti.SetActive(true);
                //listMultiAnswerText[i].text = answers[i]; //masukan & tampilkan hasil randomifikasi ke UI
            }
            answers.Clear(); //bersihkan data jawaban untuk soal selanjutnya.

        }
        else if (questType == "Multidimg") // questions text + answer img
        {
            AnswerContainer[0].SetActive(true);
            AnswerContainer[1].SetActive(false);

            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                var options = $"{data.QuestionOptions[i]}";
                var decodedUrl = DecodeURL(options);
                answers.Add(decodedUrl); // tambahkan ke list answer
            }

            SattoloShuffle(answers); //randomifikasi menggunakan sattolo

            //set img dari file yang telah di download.
            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                string imgAPath = imgPath + answers[i];

                if (File.Exists(imgAPath))
                {
                    // Baca byte dari file
                    byte[] fileData = File.ReadAllBytes(imgAPath);

                    // Buat objek Texture2D dan muat data gambar
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(fileData);

                    // Atur gambar pada RawImage
                    var answerMultiImg = btnMultiAnswers[i].transform.GetChild(1).gameObject.GetComponent<RawImage>();
                    answerMultiImg.texture = texture;
                }
                else
                {
                    Debug.Log(imgAPath + ", Tidak Ditemukan");
                }
            }

            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                var answerMulti = btnMultiAnswers[i].transform.GetChild(0).gameObject;
                var answerImg = btnMultiAnswers[i].transform.GetChild(1).gameObject;
                answerMulti.GetComponent<TMP_Text>().text = answers[i];
                answerImg.SetActive(true);
                answerMulti.SetActive(false);
                
                //listMultiAnswerText[i].text = answers[i]; //masukan & tampilkan hasil randomifikasi ke UI
            }

            answers.Clear(); //bersihkan data jawaban untuk soal selanjutnya.

        }
        else if (questType == "Multiimgf") // questions text+img + answer img
        {

            AnswerContainer[0].SetActive(true);
            AnswerContainer[1].SetActive(false);

            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                var options = $"{data.QuestionOptions[i]}";
                var decodedUrl = DecodeURL(options);
                answers.Add(decodedUrl); // tambahkan ke list answer
            }

            SattoloShuffle(answers);

            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                string imgAPath = imgPath + answers[i];

                if (File.Exists(imgAPath))
                {
                    // Baca byte dari file
                    byte[] fileData = File.ReadAllBytes(imgAPath);

                    // Buat objek Texture2D dan muat data gambar
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(fileData);

                    // Atur gambar pada RawImage
                    var answerMultiImg = btnMultiAnswers[i].transform.GetChild(1).gameObject.GetComponent<RawImage>();
                    answerMultiImg.texture = texture;
                }
                else
                {
                    Debug.Log(imgAPath + ", Tidak Ditemukan");
                }
            }

            //atur text yang sudah berada di variabel answers ke ui text.
            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                var answerMulti = btnMultiAnswers[i].transform.GetChild(0).gameObject;
                var answerImg = btnMultiAnswers[i].transform.GetChild(1).gameObject;
                answerMulti.GetComponent<TMP_Text>().text = answers[i];
                answerImg.SetActive(true);
                answerMulti.SetActive(false);
                //listMultiAnswerText[i].text = answers[i]; //masukan & tampilkan hasil randomifikasi ke UI
            }
            answers.Clear(); //bersihkan data jawaban untuk soal selanjutnya.
        }
        else if (questType == "Essay" || questType == "Essayimgd")
        {
            AnswerContainer[0].SetActive(false);
            AnswerContainer[1].SetActive(true);
        }
        else
        {
            alertController.AlertSet("Error Quest Type :" + questType + "isn't Implemented yet!", true, TextAlignmentOptions.Center);

            for(int i = 0; i < AnswerContainer.Length; i++)
            {
                AnswerContainer[i].SetActive(false);
            }
        }
    }

    public void DisplayCurPage()
    {
        int start = curpageIndex * pageSize;
        int end = Mathf.Min(start + pageSize, questSize);
        var currentPageIds = ids.GetRange(start, end - start); // indeks satu soal ke halaman
        currentIds = currentPageIds; //simpan currentPageIds ke variable list global 'currentIds'
        descAnswerText.text = null;
        //GenerateQuest(currentPageIds);// GenerateQuest untuk menampilkan data soal berdasarkan currentPageIds (ver. Online)

        //GenerateQuest untuk menampilkan data soal berdasarkan currentPageIds (ver. Offline)
        foreach (var ids in currentPageIds)
        {
            GenerateQuestLocalTemp(ids);
            int index = curpageIndex + 1;

            btnIndexQuest.transform.GetChild(1).transform.GetComponent<TMP_Text>().text = index.ToString();

            //control button navigation
            if (index == 1)
            {
                btnNavigator[0].interactable = true;
                btnNavigator[1].interactable = false;
            }
            else if(index == questSize)
            {
                btnNavigator[0].interactable = false;
                btnNavigator[1].interactable = true;
            }
            else
            {
                btnNavigator[0].interactable = true;
                btnNavigator[1].interactable = true;
            }
        }

        //StartCoroutine(ActiveBtnEnd(hasAnswer == questSize ? true : false));
        checkQuestionStatus();

        if (skillstat)
        {
            var ScoreUI = ScoreObj.transform.GetChild(0).transform.GetComponent<TMP_Text>();
            ScoreUI.text = totalScore.ToString();
            pointTrueQuizMultiplier(!skillstat, !skillstat, 0);
            PointFalseAddScore(!skillstat, !skillstat, 0);
        }
        
        //simpan data indeks soal
        //string fileName = $"{uID}_{quizkey}_{skillstat}_ID";
        string filePath = Path.Combine(Application.persistentDataPath, EDProcessing.HashSHA384("sessionQuizData"), EDProcessing.HashSHA384(fileName[2]));

        File.WriteAllText(filePath, EDProcessing.Encrypt(curpageIndex.ToString(),key, iv));
    }

    public void NextPage() //function next page
    {
        
        if ((curpageIndex + 1) * pageSize < questSize) //example curpage is 0 + 1 = 1 and then 1 * page size have 1 and then < Size of Quest.
        {
            curpageIndex++;
            DisplayCurPage();
        }
        else
        {
            checkQuestionStatus();

            if (skillstat)
            {
                var ScoreUI = ScoreObj.transform.GetChild(0).transform.GetComponent<TMP_Text>();
                ScoreUI.text = totalScore.ToString();
                pointTrueQuizMultiplier(!skillstat, !skillstat, 0);
                PointFalseAddScore(!skillstat, !skillstat, 0);
            }
        }
    }

    public void PreviousPage()
    {

        if (curpageIndex > 0)
        {
            curpageIndex--;
            DisplayCurPage();
        }
        else
        {
            checkQuestionStatus();

            if (skillstat) 
            {
                var ScoreUI = ScoreObj.transform.GetChild(0).transform.GetComponent<TMP_Text>();
                ScoreUI.text = totalScore.ToString();
                pointTrueQuizMultiplier(!skillstat, !skillstat, 0);
                PointFalseAddScore(!skillstat, !skillstat, 0);
            }

        }
    }

    //baris untuk ability bantuan....
    public void FalseRemover(int answertoRemove)
    {
        /**
         * Skill : False Remover (jumlah tombol dengan string salah yang akan di nonaktifkan), misalkan max pilihan ganda adalah 5 maka 4 tombol yang memiliki string yang salah dapat dimatikan.
         */
        if (skillstat)
        {
            // Mengonfirmasi bahwa answertoRemove berada dalam rentang yang valid
            if (answertoRemove >= 0 && answertoRemove <= btnMultiAnswers.Length - 1)
            {
                int countToDisable = 0;

                // Loop melalui tombol-tombol
                for (int i = 0; i < btnMultiAnswers.Length; i++)
                {
                    string buttonText = btnMultiAnswers[i].transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text;

                    // Jika nilai teks tombol tidak sama dengan correctAnswer
                    if (buttonText != correctAnswer[0])
                    {
                        // Nonaktifkan tombol dan hitung jumlah yang telah dinonaktifkan
                        btnMultiAnswers[i].interactable = false;
                        btnMultiAnswers[i].GetComponent<Image>().color = Color.red;
                        countToDisable++;

                        // Keluar dari loop jika sudah menonaktifkan sejumlah yang diinginkan
                        if (countToDisable == answertoRemove)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Invalid answertoRemove value. Ensure it is within the range of 0 to btnMultiAnswers.Length - 1.");
            }
        }
    }

    public void pointTrueQuizMultiplier(bool active, bool isPercent, double addVal)
    {
        if (skillstat)
        {
            if (active)
            {
                if (isPercent)
                {
                    var percentofPointQuest = pointQuest * addVal;
                    pointQuestTrue = pointQuest + percentofPointQuest;
                }
                else
                {
                    pointQuestTrue = pointQuest * addVal;
                }

            }
            else
            {
                pointQuestTrue = pointQuest;
            }
        }
        
    }
    public void PointFalseAddScore(bool active,bool isMinus, double percent)
    {
        if (skillstat)
        {
            if (active)
            {
                if (isMinus)
                {
                    var minus = pointQuest * percent;
                    pointQuestFalse = -minus;
                }
                else
                {
                    pointQuestFalse = pointQuest * percent;
                }

            }
            else
            {
                pointQuestFalse = 0;
            }
        }
        
    }
    public void AutoCorrect()
    {
        if (skillstat)
        {
            foreach (var idQuest in currentIds)
            {
                //pencarian data jawaban pada jawabanSoalList.
                var matchAnswer = jawabanSoalList.Find(j => j.QuestionID == idQuest);
                var matchQuestions = dataSoalList.Find(Q => Q.QuestionID == idQuest);
                string[] targetTipeSoalE = { "Essay", "Essayimgd" };
                /**jika data tidak kosong, akan dilakukan pengecekan data dengan membandingkan jawaban yang sudah ada dengan jawaban baru 
                 * kondisi perubahan akan terjadi jika jawaban lama != jawaban baru. **/

                if (matchAnswer != null)
                {
                    matchAnswer.AnswerDescription = "AnswerPass";
                    matchAnswer.AnswerStatus = true;
                    matchAnswer.QuestionHasAnswer = true;
                    matchAnswer.AnswerEssayPoint = pointQuestTrue;


                    foreach (var TypeQuestion in targetTipeSoalE)
                    {
                        matchAnswer.QuestionType = matchQuestions.QuestionType == TypeQuestion ? 2 : 1;
                    }

                    matchAnswer.AnswerEssayPoint = 1;

                    Debug.Log($"Jawaban diperbarui untuk ID: {idQuest}");
                }
                else
                {
                    Debug.Log("Tidak Ada Jawaban");
                }
            }

            NextPage();
        }
    }

    //sub blok handle animation
    private IEnumerator AnimatedScorePopupTime()
    {
        var ScorePopup = ScoreObj.transform.GetChild(1).transform.gameObject;
        var animationTime = 0.3f;

        ScorePopup.SetActive(true);
        ScorePopup.transform.LeanMoveLocal(new Vector2(ScorePopupPos.x, ScorePopupDefYPos), animationTime).setEaseOutQuart();
        yield return new WaitUntil(() => Mathf.Approximately(ScorePopup.transform.localPosition.y, ScorePopupDefYPos));
        ScorePopup.transform.LeanScale(Vector3.zero, animationTime);
        yield return new WaitUntil(() => ScorePopup.transform.localScale == Vector3.zero);
        ScorePopup.transform.LeanMoveLocal(new Vector2(ScorePopupPos.x, ScorePopupPos.y), animationTime);
        yield return new WaitUntil(() => Mathf.Approximately(ScorePopup.transform.localPosition.y, ScorePopupPos.y));
        ScorePopup.transform.LeanScale(Vector3.one, animationTime);
        ScorePopup.SetActive(false);

    }

    //akhir dari baris ability bantuan....

    // Sattolo Shuffle
    void SattoloShuffle<T>(List<T> list)
    {
        int i = list.Count; // Mengambil jumlah elemen dalam daftar
        while (i > 1) // Loop hingga hanya ada satu elemen yang tersisa
        {
            i--; // Mengurangi nilai i setiap kali loop dieksekusi untuk menggerakkan pointer ke elemen sebelumnya
            int j = UnityEngine.Random.Range(0, i/* + 1*/); // Mengambil indeks acak dari 0 hingga i-1 (tidak termasuk i), membatasi rentang pengambilan indeks agar tidak mencakup elemen terakhir
            T tmp = list[j]; // Menyimpan nilai sementara dari elemen yang dipilih secara acak
            list[j] = list[i]; // Menukar nilai elemen yang dipilih dengan elemen ke-i (elemen yang dipilih secara acak tidak akan pernah menjadi elemen terakhir)
            list[i] = tmp; // Memindahkan nilai yang disimpan ke posisi yang dipilih secara acak, menyelesaikan pertukaran
        }
    } // end of Sattolo Shuffle

    //Jaro Winkler Procedure
    string[] ProcessText(string text)
    {
        // Pemrosesan teks: menghapus karakter khusus dan tokenisasi
        string cleanedText = new string(text.Where(c => Char.IsLetterOrDigit(c) || Char.IsWhiteSpace(c)).ToArray());
        return cleanedText.ToLower().Split(' '); // Ubah ke huruf kecil dan tokenisasi menggunakan spasi
    }

    double CalculateJaroWinklerScore(string[] words1, string[] words2)
    {
        double jaroScore = CalculateJaroScore(words1, words2);
        double prefixLength = GetCommonPrefixLength(words1, words2);

        // Nilai kons tanpa pembobotan
        const double scalingFactor = 0.1;
        double prefixScaling = Math.Min(scalingFactor * prefixLength, 0.25);

        // Hitung skor Jaro-Winkler
        return jaroScore + prefixScaling * (1 - jaroScore);
    }

    double CalculateJaroScore(string[] words1, string[] words2)
    {
        int matchingWindow = Math.Max(words1.Length, words2.Length) / 2 - 1;
        int matches = CountMatches(words1, words2, matchingWindow);

        if (matches == 0)
            return 0;

        int transpositions = CountTranspositions(words1, words2);
        return (matches / (double)words1.Length + matches / (double)words2.Length + (matches - transpositions) / (double)matches) / 3.0;
    }

    int CountMatches(string[] words1, string[] words2, int matchingWindow)
    {
        int matches = 0;
        bool[] used2 = new bool[words2.Length];

        for (int i = 0; i < words1.Length; i++)
        {
            for (int j = Math.Max(0, i - matchingWindow); j < Math.Min(words2.Length, i + matchingWindow + 1); j++)
            {
                if (!used2[j] && words1[i] == words2[j])
                {
                    used2[j] = true;
                    matches++;
                    break;
                }
            }
        }

        return matches;
    }

    int CountTranspositions(string[] words1, string[] words2)
    {
        int transpositions = 0;
        int index2 = 0;

        for (int i = 0; i < words1.Length; i++)
        {
            if (index2 < words2.Length && words1[i] == words2[index2])
            {
                index2++;
            }
            else
            {
                transpositions++;
            }
        }

        return transpositions / 2;
    }

    int GetCommonPrefixLength(string[] words1, string[] words2)
    {
        int minLength = Math.Min(words1.Length, words2.Length);
        int commonPrefix = 0;

        for (int i = 0; i < minLength; i++)
        {
            if (words1[i] == words2[i])
            {
                commonPrefix++;
            }
            else
            {
                break;
            }
        }

        return commonPrefix;
    }// end of line jaro-winkler
}

using Firebase;
using Firebase.Database;
using Firebase.Extensions;
//using Firebase.Storage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    [Tooltip("Questions Raw IMG")]
    public RawImage QuestImg;
    [Tooltip("Inputfield Answer Essay Type")]
    public TMP_InputField descAnswerText;
    
    [Header("Ui Component Needed (Actions)")]
    [Tooltip("0 = text, 1 = img")]
    public GameObject[] QuestContainer; // 0 = text, 1 = img
    //[Tooltip("0 = multi, 1 = essay, 2 = multiimgd, 3 = multidimg,4 = multiimgf,5 = essayimgd")]
    [Tooltip("0 = multi, 1 = essay")]
    public GameObject[] AnswerContainer; // 0 = multi, 1 = essay
    [Tooltip("max 5 multiple answer btn & first obj child must text, and seconds obj child must img")]
    public Button[] btnMultiAnswers;
    [Tooltip("0 = Next, 1 = Prev")]
    public Button[] btnNavigator;
    
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

    //String for store another Quiz Data
    private protected string questType;
    private protected string correctAnswer;
    private protected string pointQuest; //bobot nilai pertanyaan (pending....)

    int curpageIndex = 0; //initial page
    const int pageSize = 1; //1 quest per page. default is 1
    int countingdownload = 0;
    int faildownload = 0;

    //untuk pendataan soal dengan jawaban benar, salah & tidak terisi.
    private int truePoint, falsePoint, hasAnswer, noAnswer;

    //[Header("Dummy Data")]
    ////dummy data img
    //public string linkSample;
    public Image imgSample;

    //list string untuk kelola data lokal.
    private List<string> ids = new List<string>(); //for store id soal
    private List<string> currentIds = new List<string>(); //for current page selected
    private List<string> answers = new List<string>(); //for store answer
    List<AnswersData> jawabanSoalList = new List<AnswersData>();
    List<QuestionsData> dataSoalList = new List<QuestionsData>(); //for store questions data

    DateTime curDate = DateTime.Now;
    string formatedDateNow;

    private void Awake()
    {
        formatedDateNow = curDate.ToString("ddMMyyyy");
    }
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
                //FirebaseApp app = FirebaseApp.DefaultInstance;
                //fs = FirebaseStorage.DefaultInstance;
                dbref = FirebaseDatabase.DefaultInstance.RootReference;
                //FirebaseApp.Create();

                //GetRandQuestID();


                //StartCoroutine(imgLoader());
                //StartCoroutine(getQuestData(tableQuizName,quizkey));
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

    public void getQuestData(string uid, string qkey, int questionSize)
    {
        questSize = questionSize;
        quizkey = qkey;
        uID = uid;

        StartCoroutine(getQuestData(tableQuizName, qkey));

    }

    IEnumerator getQuestData(string tableReference, string quizKeyReference)
    {
        //dbref = FirebaseDatabase.DefaultInstance.RootReference;
        // Set query & Get data dari Firebase

        var matchedID = new List<string>();
        var query = dbref.Child(tableReference).Child(quizKeyReference).OrderByKey();
        var fetchDataTask = query.GetValueAsync();
        yield return new WaitUntil(() => fetchDataTask.IsCompleted); //tunggu hingga pengambilan data selesai.

        if (fetchDataTask.Exception != null)
        {
            // Handling error
            Debug.LogError(fetchDataTask.Exception);
            yield break;
        }

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
            QuestionRightAnswer = child.Child("true_answ").Value.ToString()
        }).ToList();

        SattoloShuffle(dataSoal); //randomisasi dengan algoritma Sattolo Shuffle.
        var dataSoalTaken = dataSoal.Take(questSize).ToList(); //ambil dataSoal yang sudah dirandomifikasi dengan jumlah yang sudah diatur pada questSize.

        //inisiasi penamaan dan alamat file untuk data soal, jawaban, dan index soal.
        string[] fileName = { $"{uID}_{quizkey}_QD", $"{uID}_{quizkey}_AD", $"{uID}_{quizkey}_ID" }; //contoh : 10011232_QUIZ03_Q
        string[] filePath = { $"{Path.Combine(Application.persistentDataPath, fileName[0])}",
                           $"{Path.Combine(Application.persistentDataPath, fileName[1])}",
                           $"{Path.Combine(Application.persistentDataPath, fileName[2])}"};


        //cek apakah ada data soal pada penyimpanan internal
        if (File.Exists(filePath[0]))
        {
            //proses memasukan data pada soal dalam file ke dalam objek list localDataSoal
            int found = 0;
            string readData = File.ReadAllText(filePath[0]);
            var loadFromFile = readData.Split('\n');
            var localDataSoal = new List<QuestionsData>();
            foreach (var localSoal in loadFromFile)
            {
                var valSoal = JsonUtility.FromJson<QuestionsData>(localSoal) ;
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
                data.QuestionRightAnswer == localData.QuestionRightAnswer);

                if (findMatchItem != null)
                {
                    found++; //hitung data yang sama
                    matchedID.Add(findMatchItem.QuestionID) ;
                }

            }
            //jika jumlah data yang sama sesuai dengan pengaturan dari questSize, maka eksekusi data soal yang berada di penyimpanan internal.
            if(found == questSize)
            {
                ids.AddRange(localDataSoal.Select(entry => entry.QuestionID).ToList()); //menyimpan data id soal pada variabel global ids.
                dataSoalList.AddRange(localDataSoal); //simpan data soal yang sudah di load sebelumnya. 

                if (File.Exists(filePath[1])) //pengecekan file jawaban.
                {
                    string fileAnswerData = File.ReadAllText(filePath[1]); //baca data jawaban.

                    //proses memasukan data jawaban ke dalam sesi game
                    var dataAnswerLines = fileAnswerData.Split('\n');
                    foreach (var dataAnswer in dataAnswerLines)
                    {
                        var jsonAnswerData = JsonUtility.FromJson<AnswersData>(dataAnswer);
                        jawabanSoalList.Add(jsonAnswerData);
                    }

                }
                else
                {
                    Debug.Log("Tidak Terdeteksi Adanya record file tipe AD, Tidak ada proses Load.");
                }

                if (File.Exists(filePath[2])) //pengecekan file untuk last index
                {
                    string fileIndexData = File.ReadAllText(filePath[2]); //baca data index soal;
                    curpageIndex = int.Parse(fileIndexData);
                }
                else
                {
                    Debug.Log("Tidak Terdeteksi Adanya record file tipe ID, Tidak ada proses Load.");
                }

                PrepairingDownload(localDataSoal);
            }
            else
            {
                //hapus file data yang tidak sesuai.
                File.Delete(filePath[0]); //data soal
                File.Delete(filePath[2]); //data index

                //implement diferent questions for answer...
                if (File.Exists(filePath[1])) //pengecekan file jawaban.
                {
                    string fileAnswerData = File.ReadAllText(filePath[1]); //baca data jawaban.
                    var answerData = new List<AnswersData>();
                    //proses memasukan data jawaban ke dalam sesi game
                    var dataAnswerLines = fileAnswerData.Split('\n');
                    foreach (var dataAnswer in dataAnswerLines)
                    {
                        var jsonAnswerData = JsonUtility.FromJson<AnswersData>(dataAnswer);
                        answerData.Add(jsonAnswerData);
                    }
                    var findSameIDQuestion = answerData.Where(answer => matchedID.Contains(answer.QuestionID)).ToList();
                    var exceptIDQuestion = answerData.Except(findSameIDQuestion).ToList();
                    answerData.Clear();
                    answerData.AddRange(findSameIDQuestion);

                    //proses reset data jawaban...
                    foreach(var difData in exceptIDQuestion)
                    {
                        difData.QuestionType = 0;
                        difData.AnswerDescription = null;
                        difData.AnswerStatus = false;
                        difData.AnswerEssayPoint = 0;
                        difData.QuestionHasAnswer = false;
                        difData.QuestionTimeTake = 0;

                        answerData.Add(difData);
                    }

                    foreach(var adata in answerData)
                    {
                        Debug.Log($"{adata.QuestionID}, {adata.QuestionType}, {adata.AnswerDescription}, {adata.AnswerStatus}");
                    }

                    jawabanSoalList.AddRange(answerData);
                    answerData.Clear();
                }

                LoadAndSaveDataQuest(dataSoalTaken, filePath[0]);
            }
        }
        else
        {
            Debug.Log("Fresh Load Data");
            LoadAndSaveDataQuest(dataSoalTaken, filePath[0]);
        }
    }

    private void LoadAndSaveDataQuest(List<QuestionsData> dataQuests, string filePath)
    {
        //var idList = dataQuests.Select(entry => entry.QuestionID).ToList(); // ambil id soal pada dataQuests.
        ids.AddRange(dataQuests.Select(entry => entry.QuestionID).ToList()); //Menyimpan idList(idsoal) ke variabel list global 'ids'
        NoAnswerAdd(ids);
        dataSoalList.AddRange(dataQuests); //Menyimpan dataSoal ke variabel list 'dataSoalList'

        //simpan data soal dengan format json ke lokal dengan identifikiasi id quiz dan id siswa
        var dataList = new List<string>();
        foreach (var data in dataQuests)
        {
            string jsonData = JsonUtility.ToJson(data);
            dataList.Add(jsonData);
        }
        string dataString = string.Join("\n", dataList);
        File.WriteAllText(filePath, dataString);
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
                    System.IO.File.WriteAllBytes(localFilePath, www.downloadHandler.data);

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
                            System.IO.File.WriteAllBytes(localFilePath, www.downloadHandler.data);

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
                countingdownload++;
                faildownload++;
                wError = true;
            }
        }

        if(countingdownload == MaxUrlSelected && wError == false)
        {
            Debug.Log("Semua File Telah Di download");
            DisplayCurPage();
            countingdownload = 0;
        }
        else if (countingdownload == MaxUrlSelected && wError == true)
        {
            Debug.Log("terdownload :" + countingdownload + ", tidak terdownload : " + faildownload + ", dikarenakan error");
            countingdownload = 0;
            DisplayCurPage();
            faildownload = 0;
        }
        else
        {
            Debug.Log("Terdownload : " + countingdownload + "dari" + MaxUrlSelected);
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
        float cResult = ((float)truePoint / questSize) * 100;

        //Debug.Log("Nilai :" + (int)cResult);
        //Debug.Log("Total Soal :" + questSize);
        //Debug.Log("Jawaban Benar : " + truePoint);
        //Debug.Log("Jawaban Salah : " + falsePoint);
        //Debug.Log("Tidak Dijawab : " + noAnswer);

        StartCoroutine(sendDataResult(uID, quizkey, (int)cResult, truePoint, falsePoint, noAnswer));

    }

    private IEnumerator sendDataForRank(string uid, string idQ, int points, int trueAnswer, int falseAnswer, int noAnswer)
    {
        ResultStore dataHasil = new(points, trueAnswer, falseAnswer, noAnswer);

        string json = JsonUtility.ToJson(dataHasil);

        var query = dbref.Child(tableResultRank).Child(uid).Child(idQ).SetRawJsonValueAsync(json);//sending data to firebase
        yield return new WaitUntil(() => query.IsCompleted); //tunggu hingga penyimpanan data selesai.

        if (query.Exception != null)
        {
            // Handling error
            Debug.LogError(query.Exception);
            yield break;
        }
    }

    private IEnumerator sendDataResult(string uid, string idQ, int points, int trueAnswer, int falseAnswer, int noAnswer)
    {
        string dateadded = curDate.ToString("yyyyMMdd"), dateupdated = curDate.ToString("yyyyMMdd");

        var queryDate = dbref.Child(tableResultName).Child(uid).Child(idQ).GetValueAsync();
        yield return new WaitUntil(() => queryDate.IsCompleted); //tunggu hingga pengambilan data selesai.
        if (queryDate.Exception != null)
        {
            Debug.LogError(queryDate.Exception);
            yield break;
        }

        DataSnapshot snapshot = queryDate.Result;

        var checkdate = snapshot.Child("dateadded").Value.ToString();

        ResultStore dataHasil;
        string json;
        Task query;

        if (checkdate != null || checkdate != dateadded)
        {
            dataHasil = new(points, trueAnswer, falseAnswer, noAnswer, checkdate, dateupdated);
            json = JsonUtility.ToJson(dataHasil);
            query = dbref.Child(tableResultName).Child(uid).Child(idQ).SetRawJsonValueAsync(json);//sending data to firebase
        }
        else
        {
            dataHasil = new(points, trueAnswer, falseAnswer, noAnswer, dateadded, dateupdated);
            json = JsonUtility.ToJson(dataHasil);
            query = dbref.Child(tableResultName).Child(uid).Child(idQ).SetRawJsonValueAsync(json);//sending data to firebase

        }
        yield return new WaitUntil(() => query.IsCompleted); //tunggu hingga penyimpanan data selesai.

        if (query.Exception != null)
        {
            // Handling error
            Debug.LogError(query.Exception);
            yield break;
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
        var query = dbref.Child(tableQuestionsDataRecordName).Child(uID).Child(quizkey).Child(formatedDateNow).SetRawJsonValueAsync(index.ToString());

        if (query.Exception != null)
        {
            Debug.LogError(query.Exception);
            return;
        }
    }

    //IEnumerator GenerateIMGdisplay(string url)
    //{
    //    using UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
    //    //yield return req.SendWebRequest();

    //    if (req.isNetworkError || req.isHttpError)
    //    {
    //        Debug.LogError(req.error);
    //    }

    //    DownloadHandlerTexture handler = (DownloadHandlerTexture)req.downloadHandler;
    //    Texture2D sampleImg = handler.texture;

    //    Rect rect = new Rect(0, 0, sampleImg.width, sampleImg.height);
    //    Vector2 pivot = new Vector2(0.5f, 0.5f);

    //    Sprite newTexture = Sprite.Create(sampleImg, rect, pivot);

    //    imgSample.sprite = newTexture;
    //    yield return newTexture;

    //}

    //IEnumerator imgLoaderv2()
    //{
    //    // Start a download of the given URL
    //    using (WWW www = new WWW(linkSample))
    //    {
    //        // Wait for download to complete
    //        yield return www;

    //        // assign texture
    //        Renderer renderer = GetComponent<Renderer>();
    //        renderer.material.mainTexture = www.textureNonReadable;
    //    }
    //}

    void CheckInternetAvailability()
    {
        // Mengecek ketersediaan koneksi internet
        NetworkReachability reachability = Application.internetReachability;

        // Memeriksa hasil dan memberikan respons sesuai
        switch (reachability)
        {
            case NetworkReachability.NotReachable:
                //Debug.Log("Tidak ada koneksi internet.");
                break;

            case NetworkReachability.ReachableViaCarrierDataNetwork:
                //Debug.Log("Terhubung melalui jaringan data operator seluler.");
                break;

            case NetworkReachability.ReachableViaLocalAreaNetwork:
                //Debug.Log("Terhubung melalui jaringan lokal (Wi-Fi atau Ethernet).");
                break;
        }
    }

    public void checkQuestionStatus() //cek apakah soal yang ditampilkan sudah memiliki jawaban atau tidak.
    {
        foreach (var idQuest in currentIds)
        {
            var matchAnswer = jawabanSoalList.Find(j => j.QuestionID == idQuest);

            if (matchAnswer != null)
            {
                Debug.Log($"Data Jawaban : {matchAnswer.QuestionID}, {matchAnswer.QuestionHasAnswer}, {matchAnswer.AnswerDescription}, {matchAnswer.AnswerStatus}, {matchAnswer.QuestionType}, {matchAnswer.AnswerEssayPoint}, {matchAnswer.QuestionTimeTake}");

                //fitur sementara untuk mengecek soal yang telah di jawab

                //diset untuk soal tipe pilihan ganda.
                for (int i = 0; i < btnMultiAnswers.Length; i++)
                {
                    var answerMulti = btnMultiAnswers[i].transform.GetChild(0).gameObject.GetComponent<TMP_Text>();

                    if (matchAnswer.QuestionHasAnswer == true && (matchAnswer.QuestionType == 1) && answerMulti.text == matchAnswer.AnswerDescription)
                    {
                        var btnImg = btnMultiAnswers[i].GetComponent<Image>();
                        btnImg.color = Color.yellow;
                    }
                    else
                    {
                        var btnImg = btnMultiAnswers[i].GetComponent<Image>();
                        btnImg.color = Color.white;
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

                if (newAnswer.AnswerDescription == correctAnswer) //jika jawaban sama dengan kunci jawaban.
                {
                    newAnswer.AnswerStatus = true; // Set true ketika jawaban benar.
                }
                else
                {
                    newAnswer.AnswerStatus = false; // Set false ketika jawaban salah.
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

                if(currentJwb == string.Empty || currentJwb == "")
                {
                    newAnswer.AnswerStatus = false;
                    newAnswer.AnswerEssayPoint = -1;
                }
                else
                {
                    // lakukan pemrosesan teks pada kedua string yang akan di cocokan.
                    string[] userInput = ProcessText(currentJwb);
                    string[] keyAnswer = ProcessText(correctAnswer);

                    double similarityScore = CalculateJaroWinklerScore(userInput, keyAnswer); //hitung kemiripan antara input pengguna & kunci jawaban.
                    double threshold = 0.75; //batas poin untuk kemiripan string, atur sesuai kebutuhan,

                    if (similarityScore >= threshold) //jika diatas poin threshold maka status jawaban akan benar, jika di bawah maka status jawaban salah.
                    {
                        newAnswer.AnswerStatus = true;
                        newAnswer.AnswerEssayPoint = similarityScore;
                    }
                    else
                    {
                        newAnswer.AnswerStatus = false;
                        newAnswer.AnswerEssayPoint = similarityScore;
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
                Debug.LogError("AnswerTyp : Undefined type " + answerTyp + " isn't implemented!");
            }

            //fitur pengecekan jawaban, jika data jawaban ada maka akan dilakukan proses update, jika tidak ada jawaban akan dilakukan penambahan data.
            if (newAnswer.QuestionHasAnswer)
            {
                foreach (var idQuest in currentIds)
                {
                    //pencarian data jawaban pada jawabanSoalList.
                    var matchAnswer = jawabanSoalList.Find(j => j.QuestionID == idQuest);

                    /**jika data tidak kosong, akan dilakukan pengecekan data dengan membandingkan jawaban yang sudah ada dengan jawaban baru 
                     * kondisi perubahan akan terjadi jika jawaban lama != jawaban baru. **/

                    if (matchAnswer != null)
                    {
                        if (matchAnswer.AnswerDescription != newAnswer.AnswerDescription)
                        {
                            matchAnswer.QuestionType = answerTyp;
                            matchAnswer.AnswerDescription = newAnswer.AnswerDescription;
                            matchAnswer.QuestionHasAnswer = newAnswer.QuestionHasAnswer;
                            matchAnswer.AnswerStatus = newAnswer.AnswerStatus;
                            matchAnswer.AnswerEssayPoint = newAnswer.AnswerEssayPoint;

                            Debug.Log($"Data diperbarui untuk ID: {idQuest}");
                        }
                        else
                        {
                            Debug.Log($"Data tidak berubah untuk ID: {idQuest}");
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
            }
            //else
            //{
            //    Debug.Log("ID : " + newAnswer.IdSoal + "Tidak Memiliki Jawaban");
            //}
        }

        //simpan ke storage lokal
        string fileName = $"{uID}_{quizkey}_AD";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        var dataList = new List<string>();
        foreach (var data in jawabanSoalList)
        {
            var jsonData = JsonUtility.ToJson(data);
            dataList.Add(jsonData);
        }

        string dataString = string.Join("\n", dataList);
        File.WriteAllText(filePath, dataString);
        dataList.Clear();
        NextPage();
    }
    
    private void NoAnswerAdd(List<string> id) //inisialisasi jawaban kosong dari soal yang telah di list
    {

        foreach (string currentIdSoal in id)
        {
            var initialListAnswer = new AnswersData();

            initialListAnswer.QuestionID = currentIdSoal; // Set ID soal yang sedang aktif
            initialListAnswer.QuestionType = 0;
            initialListAnswer.AnswerDescription = null;
            initialListAnswer.AnswerStatus = false;
            initialListAnswer.AnswerEssayPoint = 0;
            initialListAnswer.QuestionHasAnswer = false; //set status jawaban ke false
            initialListAnswer.QuestionTimeTake = 0;

            jawabanSoalList.Add(initialListAnswer);
        }
    }
    public void FalseRemover(int answertoRemove)
    {
        /**
         * Skill : False Remover (jumlah tombol dengan string salah yang akan di nonaktifkan), misalkan max pilihan ganda adalah 5 maka 4 tombol yang memiliki string yang salah dapat dimatikan.
         */
        
        // Mengonfirmasi bahwa answertoRemove berada dalam rentang yang valid
        if (answertoRemove >= 0 && answertoRemove <= btnMultiAnswers.Length - 1)
        {
            int countToDisable = 0;

            // Loop melalui tombol-tombol
            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                string buttonText = btnMultiAnswers[i].transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text;

                // Jika nilai teks tombol tidak sama dengan correctAnswer
                if (buttonText != correctAnswer)
                {
                    // Nonaktifkan tombol dan hitung jumlah yang telah dinonaktifkan
                    btnMultiAnswers[i].interactable = false;
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

    //void GenerateQuest(List<string> qid) // generate quest
    //{
    //    dbref = FirebaseDatabase.DefaultInstance.RootReference;
    //    foreach (string idList in qid)
    //    {
    //        // Running Coroutine & call function FetchQuestData to show question data.
    //        StartCoroutine(FetchQuestData(idList));
    //        //FetchQuestData(idList);
    //    }

    //}

    //IEnumerator FetchQuestData(string qid)
    //{

    //    // Get data soal from Firebase by quiz key and quest id
    //    var query = dbref.Child(tableName).Child(quizkey).Child(qid);
    //    var fetchDataTask = query.GetValueAsync();
    //    yield return new WaitUntil(() => fetchDataTask.IsCompleted);

    //    if (fetchDataTask.Exception != null)
    //    {
    //        // Handling error
    //        Debug.LogError(fetchDataTask.Exception);
    //        yield break;
    //    }

    //    Debug.Log("Success retrieved data.");
    //    DataSnapshot snapshot = fetchDataTask.Result;

    //    // Set data soal from Firebase
    //    QuestText.text = snapshot.Child("soal_desc").Value.ToString();
    //    questType = snapshot.Child("soal_type").Value.ToString();
    //    correctAnswer = snapshot.Child("true_answ").Value.ToString();

    //    if (questType == "Multi")
    //    {
    //        AnswerContainer[0].SetActive(true);
    //        AnswerContainer[1].SetActive(false);

    //        for (int i = 0; i < listMultiAnswerText.Length; i++)
    //        {
    //            string optionKey = "option_" + (i + 1);
    //            string answer = snapshot.Child(optionKey).Value.ToString();
    //            answers.Add(answer);
    //        }

    //        SattoloShuffle(answers);

    //        for (int i = 0; i < listMultiAnswerText.Length; i++)
    //        {
    //            listMultiAnswerText[i].text = answers[i];
    //        }
    //        answers.Clear();

    //    }
    //    else if (questType == "Essay")
    //    {
    //        AnswerContainer[0].SetActive(false);
    //        AnswerContainer[1].SetActive(true);

    //        // Implement concentrate correct answer in the future.
    //        //for (int i = 0; i < listAnswerText.Length; i++)
    //        //{
    //        //    listAnswerText[i].text = "Essay";
    //        //}
    //    }
    //    else
    //    {
    //        AnswerContainer[0].SetActive(false);
    //        AnswerContainer[1].SetActive(false);

    //        Debug.LogError("Error Quest Type :" + questType + "isn't Implemented yet!");
    //        //for (int i = 0; i < listAnswerText.Length; i++)
    //        //{
    //        //    listAnswerText[i].text = "Error Get Answer Data";
    //        //}
    //    }
    //}

    void GenerateQuestLocalTemp(string qid)
    {
        var dataSoalToDisplay = dataSoalList.FirstOrDefault(dataSoal => dataSoal.QuestionID == qid); //cocokan data id soal yang di panggil dengan id soal yang ada di data soal.
        GenerateQuestLocalTemp(dataSoalToDisplay); // panggil data soal yang sesuai dengan id soal.
    }
    void GenerateQuestLocalTemp(QuestionsData data)
    {
        string imgPath = Path.Combine(Application.persistentDataPath, $"qData/{quizkey}/");
        string[] targetTipeSoal = { "Multidimg", "Multiimgf" };
        //string idQ = $"{data.IdQ}";
        var questText = QuestContainer[0].transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
        questText.text = $"{data.QuestionDescription}";
        questType = $"{data.QuestionType}";

        //jika tipe soal terdapat pada targetTipeSoal (jawaban benar) berbentuk URL akan di lakukan dekoding untuk mengambil nama file
        if (questType.Any(data => targetTipeSoal.Contains(questType)))
        {
            var decodedURLcorrectAnswer = DecodeURL($"{data.QuestionRightAnswer}");
            correctAnswer = decodedURLcorrectAnswer;
        }
        else
        {
            correctAnswer = $"{data.QuestionRightAnswer}";
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
            Debug.LogError("Error Quest Type :" + questType + "isn't Implemented yet!");

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
            //Debug.Log($"Halaman {index}: {ids}");

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
        checkQuestionStatus();

        //simpan data indeks soal
        string fileName = $"{uID}_{quizkey}_ID";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        File.WriteAllText(filePath, curpageIndex.ToString());
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
            Debug.Log("Ini adalah halaman terakhir.");
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
            Debug.Log("Ini adalah halaman pertama.");
        }
    }
    // Sattolo Shuffle
    void SattoloShuffle<T>(List<T> list)
    {
        int i = list.Count; // Mendapatkan jumlah elemen dalam list
        while (i > 1)
        {
            i--; // Mengurangi indeks i
            int j = UnityEngine.Random.Range(0, i + 1); // Memilih indeks acak j, 0 <= j <= i
            T tmp = list[j]; // Menyimpan elemen di indeks j ke dalam variabel sementara tmp
            list[j] = list[i]; // Memindahkan elemen di indeks i ke indeks j
            list[i] = tmp; // Memindahkan elemen yang disimpan di tmp ke indeks i
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

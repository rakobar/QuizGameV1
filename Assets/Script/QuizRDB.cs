using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Storage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public class QuizRDB : MonoBehaviour
{
    DatabaseReference dbref;
    FirebaseStorage fs;

    //UI Text Component for Quiz Data.
    [Header("Ui Component Needed")]
    [Tooltip("Questions Raw IMG")]
    public RawImage QuestImg;
    [Tooltip("Inputfield Answer Essay Type")]
    public TMP_InputField descAnswerText;
    
    [Header("Ui Component Needed (Actions)")]
    [Tooltip("0 = text, 1 = img")]
    public GameObject[] QuestContainer; // 0 = text, 1 = img
    [Tooltip("0 = multi, 1 = essay, 2 = multiimgd, 3 = multidimg,4 = multiimgf,5 = essayimgd")]
    public GameObject[] AnswerContainer; // 0 = multi, 1 = essay
    [Tooltip("max 5 multiple answer btn & first obj child must text, and seconds obj child must img")]
    public Button[] btnMultiAnswers;
    [Tooltip("0 = Next, 1 = Prev")]
    public Button[] btnNavigator;
    
    //Deklarasi Firebase
    [Header("Firebase Require Variable")]
    private protected string fsUrl = "gs://theaswerqmaster.appspot.com"; //firebase storage reference url
    private protected string tableQuizName = "data_quizsoal"; // table name from firebase.
    private protected string tableResultName = "data_quizresult"; // table name from firebase.
    private string uID; 
    private string quizkey; //future implemented to automatic get by input user (on progress...)
    private protected int questSize; //max soal to load. in future this value fixed get from firebase. (on progress...)

    //String for store another Quiz Data
    private protected string questType;
    private protected string correctAnswer;
    private protected string pointQuest; //bobot nilai pertanyaan (pending....)

    int curpageIndex = 0; //initial page
    const int pageSize = 1; //1 quest per page. default is 1

    //untuk pendataan soal dengan jawaban benar, salah & tidak terisi.
    public int truePoint, falsePoint, hasAnswer, noAnswer;

    //[Header("Dummy Data")]
    ////dummy data img
    //public string linkSample;
    public Image imgSample;

    //list string untuk kelola data lokal.
    private List<string> ids = new List<string>(); //for store id soal
    private List<string> currentIds = new List<string>(); //for current page selected
    private List<string> answers = new List<string>(); //for store answer
    List<JawabanSoal> jawabanSoalList = new List<JawabanSoal>(); //for store picked answer
    List<DataSoal> dataSoalList = new List<DataSoal>(); //for store questions data

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
                fs = FirebaseStorage.DefaultInstance;
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

        string[] fileName = { $"{uID}_{quizkey}_Q.json", $"{uID}_{quizkey}_A.json", $"{uID}_{quizkey}_D.txt" }; //contoh : 10011232_QUIZ03_Q

        string filePath0 = Path.Combine(Application.persistentDataPath, fileName[0]); // Path lengkap menuju berkas di internal storage
        string filePath1 = Path.Combine(Application.persistentDataPath, fileName[1]); // Path lengkap menuju berkas di internal storage
        string filePath2 = Path.Combine(Application.persistentDataPath, fileName[2]); // Path lengkap menuju berkas di internal storage

        if (File.Exists(filePath0) && File.Exists(filePath1) && File.Exists(filePath2))
        {
            string jsonData0 = File.ReadAllText(filePath0);
            string jsonData1 = File.ReadAllText(filePath1);
            string textData0 = File.ReadAllText(filePath2);

            // Mengonversi teks JSON menjadi objek DataSoal dan DataJawaban
            List<DataSoal> loadedDataQuests = JsonUtility.FromJson<List<DataSoal>>(jsonData0);
            List<JawabanSoal> loadedDataAnswers = JsonUtility.FromJson<List<JawabanSoal>>(jsonData1);

            var idList = loadedDataQuests.Select(entry => entry.IdQ).ToList(); // ambil id soal pada loadedData.
            jawabanSoalList.AddRange(loadedDataAnswers); //menyimpan data jawaban
            ids.AddRange(idList); //Menyimpan idList(idsoal) ke variabel list global 'ids'
            dataSoalList.AddRange(loadedDataQuests); //Menyimpan dataSoal ke variabel list 'dataSoalList'
            curpageIndex = int.Parse(textData0); //set index yang terakhir kali aktif.
        }
        else
        {
            StartCoroutine(getQuestData(tableQuizName, qkey));
        }

        //string formattedTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        //Debug.Log(formattedTimestamp);
    }

    IEnumerator getQuestData(string tableReference, string quizKeyReference)
    {
        dbref = FirebaseDatabase.DefaultInstance.RootReference;
        // Set query & Get data dari Firebase
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
        var dataSoal = snapshot.Children.Select(child => new DataSoal
        {
            IdQ = child.Key,
            DescQ = child.Child("soal_desc").Value.ToString(),
            DescImg = child.Key + "_soal_img",
            QType = child.Child("soal_type").Value.ToString(),
            Options = Enumerable.Range(1, btnMultiAnswers.Length) // Membuat urutan 1 sampai (ukuran listMultiAnswerText.Length)
            .Select(i => child.Child($"option_{i}").Value.ToString()) // Mengambil nilai dari option_1, option_2, ..., option_5
            .ToArray(),
            TrueAnswer = child.Child("true_answ").Value.ToString()
        }).ToList();

        SattoloShuffle(dataSoal); //randomisasi dengan algoritma Sattolo Shuffle.
         
        //ambil dataSoal yang sudah dirandomifikasi dengan jumlah yang sudah diatur pada questSize.
        var dataSoalTaken = dataSoal.Take(questSize).ToList();
        var idList = dataSoalTaken.Select(entry => entry.IdQ).ToList(); // ambil id soal pada dataSoalTaken.
        ids.AddRange(idList); //Menyimpan idList(idsoal) ke variabel list global 'ids'
        NoAnswerAdd(ids);
        dataSoalList.AddRange(dataSoalTaken); //Menyimpan dataSoal ke variabel list 'dataSoalList'


        //simpan data soal ke lokal dengan identifikiasi id quiz dan id siswa

        string fileName = $"{uID}_{quizkey}_Q.json"; //contoh : 10011232_QUIZ03_Q
        string filePath = Path.Combine(Application.persistentDataPath, fileName); // Path lengkap menuju berkas di internal storage
        string jsonData = JsonUtility.ToJson(dataSoalTaken);// Mengubah data soal ke dalam bentuk JSON
        File.WriteAllText(filePath, jsonData);// Menyimpan data JSON ke berkas

        //function download
        //metode sementara untuk pengecekan soal yang memiliki data untuk di download.
        //string[] targetTipeSoal = { "Multiimgd", "Multidimg", "Multiimgd", "Multiimgf", "Essayimgd" };
        //if (dataSoalTaken.Any(data => targetTipeSoal.Contains(data.QType)))
        //{

        //    var _img = dataSoalTaken
        //        .Where(data => targetTipeSoal.Contains(data.QType))
        //        .Select(data => data.IdQ).ToList();
        //    Debug.Log("Quest id yang memiliki data untuk di download :" + _img.Count);

        //    SearchData(_img); //cari data sekaligus download berdasarkan id soal.
        //}
        //else
        //{
        //    var _img = dataSoalTaken
        //        .Where(data => !targetTipeSoal.Contains(data.QType))
        //        .Select(data => data.IdQ).ToList();
        //    Debug.Log("Quest id yang tidak memiliki data untuk di download :" + _img.Count);
        //    Debug.Log("Semua quest id tidak memiliki data untuk di download");
        //}

        //Debug.Log("Total Questions fetched : " + dataSoalTaken.Count); //mengecek apakah data soal sudah di dapat.
        DisplayCurPage(); //memanggil fungsi DisplayCurPage untuk menampilkan data soal berdasarkan idsoal ke UI.
    }



    void SearchData<T>(List<T> prefixName) //pencarian file berdasarkan prefixName
    {
        string[] extensionList = { ".jpg", ".jpeg", ".png", ".gif"};
        string[] variableName = { "_soalimg", "_option1img", "_option2img", "_option3img", "_option4img", "_option5img"};

        foreach (var prefix in prefixName)
        {
            foreach (var variable in variableName)
            {
                foreach (var ext in extensionList)
                {
                    var fileName = prefix + variable + ext; //example combination : Q1_soalimg.png
                    StorageReference fileRef = fs.GetReferenceFromUrl(fsUrl)
                        .Child("/" + tableQuizName + "/" + quizkey + "/" + fileName); // path : (link_bucket)/data_quizsoal/(quizkey)/(fileName)
                    
                    //ambil meta data pada firebase untuk pengecekan file, jika ada lakukan download.
                    fileRef.GetMetadataAsync().ContinueWithOnMainThread(task => {
                        if (task.IsCompleted && !task.IsFaulted && task.Result != null)
                        {
                            //Debug.Log("File dengan nama " + fileName + " ada!");
                            DownloadFile(fileRef);
                        }
                        //else
                        //{
                        //    Debug.Log("File dengan nama "+fileName+" tidak ada.");
                        //}
                    });
                }
            }
        }
    }
    private void DownloadFile(StorageReference fileRef)
    {
        string localFilePath = Application.persistentDataPath + "/qData/" + fileRef.Name; // saving path : (local)/qData/

        fileRef.GetFileAsync(localFilePath).ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError($"Failed to download file {fileRef.Name}");
            }
            else
            {
                Debug.Log($"File {fileRef.Name} downloaded successfully to {localFilePath}");
            }
        });
    }

    string DecodeURL(string url)
    {

        // Mencari indeks awal dan akhir nama file dalam URL
        int startIndex = url.LastIndexOf("/") + 1;
        int endIndex = url.LastIndexOf("?");

        // Mengambil substring yang berisi nama file

        string brokenfileName = url.Substring(startIndex, endIndex - startIndex);
        string decodedUrl = UnityWebRequest.EscapeURL(brokenfileName);

        string[] fileName = decodedUrl.Split('/');
        string lastpart = fileName[fileName.Length - 1];

        return lastpart;
    }
    private void ResultQuiz()
    {
        float cResult = ((float)truePoint / questSize) * 100;

        Debug.Log("Nilai :" + (int)cResult);
        Debug.Log("Total Soal :" + questSize);
        Debug.Log("Jawaban Benar : " + truePoint);
        Debug.Log("Jawaban Salah : " + falsePoint);
        Debug.Log("Tidak Dijawab : " + noAnswer);

        StartCoroutine(sendDataResult("debugid", quizkey, (int)cResult, truePoint, falsePoint, noAnswer));

    }

    private IEnumerator sendDataResult(string uid, string idQ, int points, int trueAnswer, int falseAnswer, int noAnswer)
    {
        //sending data to firebase
        ResultStore dataHasil = new(points, trueAnswer, falseAnswer, noAnswer);

        string json = JsonUtility.ToJson(dataHasil);

        var query = dbref.Child(tableResultName).Child(uid).Child(idQ).SetRawJsonValueAsync(json);
        yield return new WaitUntil(() => query.IsCompleted); //tunggu hingga penyimpanan data selesai.

        if (query.Exception != null)
        {
            // Handling error
            Debug.LogError(query.Exception);
            yield break;
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
            var matchAnswer = jawabanSoalList.Find(j => j.IdSoal == idQuest);

            if (matchAnswer != null)
            {
                Debug.Log($"Data Jawaban : {matchAnswer.IdSoal}, {matchAnswer.HasAnswer}, {matchAnswer.Jawaban}, {matchAnswer.Status}, {matchAnswer.SoalType}, {matchAnswer.PointEssay}");

                //fitur sementara untuk mengecek soal yang telah di jawab

                //diset untuk soal tipe pilihan ganda.
                for (int i = 0; i < btnMultiAnswers.Length; i++)
                {
                    var answerMulti = btnMultiAnswers[i].transform.GetChild(0).gameObject.GetComponent<TMP_Text>();

                    if (matchAnswer.HasAnswer == true && (matchAnswer.SoalType == 1) && answerMulti.text == matchAnswer.Jawaban)
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
        truePoint = jawabanSoalList.Count(j => j.Status); // ambil total jawaban benar dari jawabanSoalList
        falsePoint = jawabanSoalList.Count(j => !j.Status); // ambil total jawaban salah dari jawabanSoalList
        //noAnswer = questSize - (truePoint + falsePoint); // jumlah soal - jawaban benar & salah
        noAnswer = jawabanSoalList.Count(j => !j.HasAnswer); // jumlah HasAnswer != true / yang tidak memiliki jawaban.
        hasAnswer = jawabanSoalList.Count(j => j.HasAnswer); // jumlah HasAnswer = true / yang memiliki jawaban
        Debug.Log("Data list jawaban saat ini : " + hasAnswer + " jawaban, dari " + questSize + " soal.");
    }

    public void btnAnswer(int answerTyp)
    {
        foreach (string currentIdSoal in currentIds)
        {
            var newAnswer = new JawabanSoal(); //buat objek baru untuk soal yang aktif

            newAnswer.IdSoal = currentIdSoal; // Set ID soal yang sedang aktif

            //1 = multiply
            if (answerTyp == 1)
            {
                TMP_Text currentJwb = EventSystem.current.currentSelectedGameObject.transform.GetChild(0).GetComponent<TMP_Text>(); //ambil komponen teks dari object yang terpilih.
                newAnswer.SoalType = answerTyp;
                newAnswer.Jawaban = currentJwb.text; // Set jawaban yang dipilih
                newAnswer.HasAnswer = true; // Set true ketika sudah dijawab.
                newAnswer.PointEssay = 0; //default data if question essay point essay.

                if (newAnswer.Jawaban == correctAnswer) //jika jawaban sama dengan kunci jawaban.
                {
                    newAnswer.Status = true; // Set true ketika jawaban benar.
                }
                else
                {
                    newAnswer.Status = false; // Set false ketika jawaban salah.
                }

            }
            //2 = essay, becareful this isn't recommended for long answer....
            else if (answerTyp == 2)
            {
                var currentJwb = descAnswerText.text;
                newAnswer.SoalType = answerTyp;
                newAnswer.Jawaban = currentJwb;
                newAnswer.HasAnswer = true;


                //jaro-winkler performed on here...

                // lakukan pemrosesan teks pada kedua string yang akan di cocokan.
                string[] userInput = ProcessText(currentJwb);
                string[] keyAnswer = ProcessText(correctAnswer);

                double similarityScore = CalculateJaroWinklerScore(userInput, keyAnswer); //hitung kemiripan antara input pengguna & kunci jawaban.
                double threshold = 0.75; //batas poin untuk kemiripan string, atur sesuai kebutuhan,

                if (similarityScore >= threshold) //jika diatas poin threshold maka status jawaban akan benar, jika di bawah maka status jawaban salah.
                {
                    newAnswer.Status = true;
                    newAnswer.PointEssay = similarityScore;
                }
                else
                {
                    newAnswer.Status = false;
                    newAnswer.PointEssay = similarityScore;
                }

            }
            else if (answerTyp == 3)
            {
                Debug.Log("multiimgd");
            }
            else
            {
                Debug.LogError("AnswerTyp : Undefined type " + answerTyp + " isn't implemented!");
            }

            //fitur pengecekan jawaban, jika data jawaban ada maka akan dilakukan proses update, jika tidak ada jawaban akan dilakukan penambahan data.
            if (newAnswer.HasAnswer == true)
            {
                foreach (var idQuest in currentIds)
                {
                    //pencarian data jawaban pada jawabanSoalList.
                    var matchAnswer = jawabanSoalList.Find(j => j.IdSoal == idQuest);

                    /**jika data tidak kosong, akan dilakukan pengecekan data dengan membandingkan jawaban yang sudah ada dengan jawaban baru 
                     * kondisi perubahan akan terjadi jika jawaban lama != jawaban baru. **/

                    if (matchAnswer != null)
                    {
                        if (matchAnswer.Jawaban != newAnswer.Jawaban)
                        {
                            matchAnswer.SoalType = answerTyp;
                            matchAnswer.Jawaban = newAnswer.Jawaban;
                            matchAnswer.HasAnswer = newAnswer.HasAnswer;
                            matchAnswer.Status = newAnswer.Status;
                            matchAnswer.PointEssay = newAnswer.PointEssay;

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
        string fileName = $"{uID}_{quizkey}_A.json";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        string jsonData = JsonUtility.ToJson(jawabanSoalList);

        File.WriteAllText(filePath, jsonData);

        NextPage();
    }
    
    private void NoAnswerAdd(List<string> id) //inisialisasi jawaban kosong dari soal yang telah di list
    {

        foreach (string currentIdSoal in id)
        {
            var initialListAnswer = new JawabanSoal();

            initialListAnswer.IdSoal = currentIdSoal; // Set ID soal yang sedang aktif
            initialListAnswer.SoalType = 0;
            initialListAnswer.Jawaban = null;
            initialListAnswer.Status = false;
            initialListAnswer.PointEssay = 0;
            initialListAnswer.HasAnswer = false; //set status jawaban ke false

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

    void GenerateQuestLocalTemp(String qid)
    {
        var dataSoalToDisplay = dataSoalList.FirstOrDefault(dataSoal => dataSoal.IdQ == qid); //cocokan data id soal yang di panggil dengan id soal yang ada di data soal.
        StartCoroutine(GenerateQuestLocalTemp(dataSoalToDisplay)) ; // panggil data soal yang sesuai dengan id soal.
    }
    IEnumerator GenerateQuestLocalTemp(DataSoal data)
    {
        string[] targetTipeSoal = { "Multidimg", "Multiimgf" };
        //string idQ = $"{data.IdQ}";
        var questText = QuestContainer[0].transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
        questText.text = $"{data.DescQ}";
        questType = $"{data.QType}";

        //jika tipe soal terdapat pada targetTipeSoal (jawaban benar) berbentuk URL akan di lakukan dekoding untuk mengambil nama file
        if (questType.Any(data => targetTipeSoal.Contains(questType)))
        {
            var decodedURLcorrectAnswer = DecodeURL($"{data.TrueAnswer}");
            correctAnswer = decodedURLcorrectAnswer;
        }
        else
        {
            correctAnswer = $"{data.TrueAnswer}";
        }

        if (questType == "Multi") //full text
        {
            QuestContainer[0].SetActive(true);
            QuestContainer[1].SetActive(false);
            AnswerContainer[0].SetActive(true);
            AnswerContainer[1].SetActive(false);

            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                var options = $"{data.Options[i]}";
                answers.Add(options); // tambahkan ke list answer
            }

            SattoloShuffle(answers); //randomifikasi menggunakan sattolo

            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                var answerMulti = btnMultiAnswers[i].transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
                answerMulti.text = answers[i];
                //listMultiAnswerText[i].text = answers[i]; //masukan & tampilkan hasil randomifikasi ke UI
            }
            answers.Clear(); //bersihkan data jawaban untuk soal selanjutnya.
        }
        else if (questType == "Multiimgd") // questions text+img + answer text
        {
            QuestContainer[0].SetActive(true);
            QuestContainer[1].SetActive(true);
            AnswerContainer[0].SetActive(true);
            AnswerContainer[1].SetActive(false);

            var soal_img = $"{data.DescImg}";

            string imgQPath = System.IO.Path.Combine(Application.persistentDataPath, "qdata", soal_img);

            if (System.IO.File.Exists(imgQPath))
            {
                // Baca byte dari file
                byte[] fileData = System.IO.File.ReadAllBytes(imgQPath);

                // Buat objek Texture2D dan muat data gambar
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);

                // Atur gambar pada RawImage
                QuestImg.texture = texture;
            }

            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                var options = $"{data.Options[i]}";
                answers.Add(options); // tambahkan ke list answer
            }

            SattoloShuffle(answers); //randomifikasi menggunakan sattolo

            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                var answerMulti = btnMultiAnswers[i].transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
                answerMulti.text = answers[i];
                //listMultiAnswerText[i].text = answers[i]; //masukan & tampilkan hasil randomifikasi ke UI
            }
            answers.Clear(); //bersihkan data jawaban untuk soal selanjutnya.

        }
        else if (questType == "Multidimg") // questions text + answer img
        {
            List<String> answG = new List<String>();
            Debug.Log("Questions text + IMG");

            QuestContainer[0].SetActive(true);
            QuestContainer[1].SetActive(false);
            AnswerContainer[0].SetActive(true);
            AnswerContainer[1].SetActive(false);

            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                var options = $"{data.Options[i]}";
                answG.Add(options); // tambahkan ke list answer
            }

            SattoloShuffle(answG); //randomifikasi menggunakan sattolo

            //download gambar secara langsung melalui url
            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                var answerMultiImg = btnMultiAnswers[i].transform.GetChild(1).gameObject.GetComponent<Image>();

                using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(answG[i]))
                {
                    // Mengirimkan permintaan dan menunggu tanggapan
                    yield return www.SendWebRequest();

                    // Menangani hasil setelah permintaan selesai
                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        // Mendapatkan tekstur dari permintaan
                        Texture2D texture = DownloadHandlerTexture.GetContent(www);

                        answerMultiImg.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                    }
                    else
                    {
                        // Menangani kesalahan jika ada
                        Debug.LogError("Error loading image: " + www.error);
                    }
                }

            }

            //melakukan decoding url untuk mengambil nama file untuk mempersingkat jawaban.
            foreach(var multiG in answG)
            {
                var decodedUrl = DecodeURL(multiG);
                answers.Add(decodedUrl);
            }

            //bersihkan answG
            answG.Clear();


            // set img dari file yang telah di download.
            //for (int i = 0; i < btnMultiAnswers.Length; i++)
            //{
            //    string imgAPath = System.IO.Path.Combine(Application.persistentDataPath, "qdata", answG[i]);

            //    if (System.IO.File.Exists(imgAPath))
            //    {
            //        // Baca byte dari file
            //        byte[] fileData = System.IO.File.ReadAllBytes(imgAPath);

            //        // Buat objek Texture2D dan muat data gambar
            //        Texture2D texture = new Texture2D(2, 2);
            //        texture.LoadImage(fileData);

            //        // Atur gambar pada RawImage
            //        var answerMultiImg = btnMultiAnswers[i].transform.GetChild(1).gameObject.GetComponent<RawImage>();
            //        answerMultiImg.texture = texture;
            //    }
            //}

            //perubahan value dari yang memiliki extensi menjadi non extensi (ex : i.img to i)
            //foreach (var multiAnswer in answG)
            //{
            //    int indextitikext = multiAnswer.LastIndexOf('.');
            //    if (indextitikext != -1)
            //    {
            //        string newName = multiAnswer.Substring(0, indextitikext);
            //        answers.Add(newName);
            //    }
            //}

            //answG.Clear();

            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                var answerMulti = btnMultiAnswers[i].transform.GetChild(0).gameObject.GetComponent<TMP_Text>(); //get component on child object index 0;
                answerMulti.text = answers[i];
                //listMultiAnswerText[i].text = answers[i]; //masukan & tampilkan hasil randomifikasi ke UI
            }

            answers.Clear(); //bersihkan data jawaban untuk soal selanjutnya.

        }
        else if (questType == "Multiimgf") // questions text+img + answer img
        {
            List<String> answG = new List<String>();
            Debug.Log("Questions IMG full");

            QuestContainer[0].SetActive(true);
            QuestContainer[1].SetActive(true);
            AnswerContainer[0].SetActive(true);
            AnswerContainer[1].SetActive(false);

            var soal_img = $"{data.DescImg}";

            string imgQPath = System.IO.Path.Combine(Application.persistentDataPath, "qdata", soal_img);

            if (System.IO.File.Exists(imgQPath))
            {
                // Baca byte dari file
                byte[] fileData = System.IO.File.ReadAllBytes(imgQPath);

                // Buat objek Texture2D dan muat data gambar
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);

                // Atur gambar pada RawImage
                QuestImg.texture = texture;
            }


            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                var options = $"{data.Options[i]}";
                answG.Add(options); // tambahkan ke list answer
            }

            SattoloShuffle(answG);

            //set string untuk gambar
            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                string imgAPath = System.IO.Path.Combine(Application.persistentDataPath, "qdata", answG[i]);

                if (System.IO.File.Exists(imgAPath))
                {
                    // Baca byte dari file
                    byte[] fileData = System.IO.File.ReadAllBytes(imgAPath);

                    // Buat objek Texture2D dan muat data gambar
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(fileData);

                    // Atur gambar pada RawImage
                    var answerMultiImg = btnMultiAnswers[i].transform.GetChild(1).gameObject.GetComponent<RawImage>();
                    answerMultiImg.texture = texture;
                    //listMultiAnswerImg[i].texture = texture;
                }
            }

            //pemrosesan string jawaban yang memiliki extensi menjadi non-extensi dan di simpan ke variabel global answers.
            foreach (var multiAnswer in answG)
            {
                int indextitikext = multiAnswer.LastIndexOf('.');
                if(indextitikext != -1)
                {
                    string newName = multiAnswer.Substring(0, indextitikext);
                    answers.Add(newName);
                }
            }

            //lakukan pembersihan variabel lokal
            answG.Clear();

            //atur text yang sudah berada di variabel answers ke ui text.
            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                var answerMulti = btnMultiAnswers[i].transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
                answerMulti.text = answers[i];
                //listMultiAnswerText[i].text = answers[i]; //masukan & tampilkan hasil randomifikasi ke UI
            }
            answers.Clear(); //bersihkan data jawaban untuk soal selanjutnya.
        }
        else if (questType == "Essay")
        {
            AnswerContainer[0].SetActive(false);
            AnswerContainer[1].SetActive(true);
        }
        else if (questType == "Essayimgd") //questions img + answer text
        {
            Debug.Log("Questions IMG + text");
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

    void DisplayCurPage()
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
        string fileName = $"{uID}_{quizkey}_D.txt";
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
            ResultQuiz();
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
public class JawabanSoal
{
    public string IdSoal { get; set; }
    public int SoalType { get; set; }
    public bool HasAnswer { get; set; }
    public string Jawaban { get; set; }
    public bool Status { get; set; }
    public double PointEssay { get; set; }
    public int timetake { get; set; }

}
public class DataSoal
{
    public string IdQ { get; set; }
    public string DescQ { get; set; }
    public string DescImg { get; set; }
    public string QType { get; set; }
    public string[] Options { get; set; }
    public string TrueAnswer { get; set; }
}

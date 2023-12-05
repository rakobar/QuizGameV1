using System;
using System.Threading;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Storage;
using TMPro;
//using Newtonsoft.Json;

public class QuizRDB : MonoBehaviour
{
    DatabaseReference dbref;
    Firebase.Storage.FirebaseStorage fs;

    //UI Text Component for Quiz Data.
    [Header("Ui Component Needed")]
    public TMP_Text QuestText;
    public TMP_Text[] listMultiAnswerText; //multiply answer
    public TMP_Text descAnswerText;

    [Header("Ui Component Needed (Actions)")]
    public GameObject[] QuestContainer; // 0 = text, 1 = img
    public GameObject[] AnswerContainer; // 0 = multi, 1 = essay



    //UI Component
    //public Button[] btnAnswers;

    //Deklarasi Firebase
    [Header("Firebase Require Variable")]
    public string fsUrl = "gs://theaswerqmaster.appspot.com";
    public string tableName ; //future setted to default fix table from firebase
    public string quizkey ; //future implemented to automatic get by input user
    private protected int questSize = 10; //max soal to load. in future this value fixed get from firebase.

    //String for store another Quiz Data
    public string questType;
    private protected string correctAnswer;
    private protected string pointQuest; //bobot nilai pertanyaan (pending....)

    int curpageIndex = 0; //initial page
    const int pageSize = 1; //1 quest per page. default is 1

    //untuk pendataan soal dengan jawaban benar, salah & tidak terisi.
    private protected int truePoint;
    private protected int falsePoint;
    private protected int notAnswered;

    //[Header("Dummy Data")]
    ////dummy data img
    //public string linkSample;
    //public Image imgSample;

    //list string untuk kelola data lokal.
    private List<string> ids = new List<string>(); //for store id soal
    private List<string> currentIds = new List<string>(); //for current page array
    private List<string> answers = new List<string>(); //for store answer
    List<JawabanSoal> jawabanSoalList = new List<JawabanSoal>(); //for store picked answer
    List<DataSoal> dataSoalList = new List<DataSoal>();
    DataSoal dataSoal = new DataSoal();
    JawabanSoal jawabanSoal = new JawabanSoal();

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
                StartCoroutine(getQuestData(tableName,quizkey));
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

    private void Update()
    {
        //CheckInternetAvailability();
        //GetRandQuestID();
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
            QType = child.Child("soal_type").Value.ToString(),
            Options = Enumerable.Range(1, 5) // Membuat urutan 1 sampai 5
            .Select(i => child.Child($"option_{i}").Value.ToString()) // Mengambil nilai dari option_1, option_2, ..., option_5
            .ToArray(),
            TrueAnswer = child.Child("true_answ").Value.ToString()
        }).ToList();


        SattoloShuffle(dataSoal); //randomisasi dengan algoritma Sattolo Shuffle.
        var dataSoalTaken = dataSoal.Take(questSize).ToList(); //ambil dataSoal yang sudah dirandomifikasi dengan jumlah yang sudah diatur pada questSize.
        var idList = dataSoalTaken.Select(entry => entry.IdQ).ToList(); // ambil id soal pada dataSoalTaken.
        ids.AddRange(idList); //Menyimpan idList(idsoal) ke variabel list global 'ids'
        dataSoalList.AddRange(dataSoalTaken); //Menyimpan dataSoal ke variabel list 'dataSoalList'

        //metode sementara untuk pengecekan soal yang memiliki data untuk di download.
        string[] targetTipeSoal = { "Multiimgd", "Multidimg", "Multiimgd", "Multiimgf", "Essayimgd" };
        if (dataSoalTaken.Any(data => targetTipeSoal.Contains(data.QType)))
        {

            var _img = dataSoalTaken
                .Where(data => targetTipeSoal.Contains(data.QType))
                .Select(data => data.IdQ).ToList();
            Debug.Log("Quest id yang memiliki data untuk di download :" + _img.Count);

            SearchData(_img); //cari data sekaligus download berdasarkan id soal.
        }
        else
        {
            //var _img = dataSoalTaken
            //    .Where(data => !targetTipeSoal.Contains(data.QType))
            //    .Select(data => data.IdQ).ToList();
            //Debug.Log("Quest id yang tidak memiliki data untuk di download :" + _img.Count);
            Debug.Log("Semua quest id tidak memiliki data untuk di download");
        }

        Debug.Log("Total Questions fetched : " + dataSoalTaken.Count); //mengecek apakah data soal sudah di dapat.
        //Debug.Log("Total ID fetched: " + idList.Count); //mengecek apakah idList(idsoal) sudah didapat.
        DisplayCurPage(); //memanggil fungsi DisplayCurPage untuk menampilkan data soal berdasarkan idsoal ke UI.
    }

    void SearchData<T>(List<T> prefixName) //mengambil list file berdasarkan prefixName
    {
        string[] extensionList = { ".jpg", ".png"};
        string[] variableName = { "_soalimg", "_option1img", "_option2img", "_option3img", "_option4img", "_option5img"};

        foreach (var prefix in prefixName)
        {
            foreach (var variable in variableName)
            {
                foreach (var ext in extensionList)
                {
                    var fileName = prefix + variable + ext; //example combination : Q1_soalimg.png
                    StorageReference fileRef = fs.GetReferenceFromUrl(fsUrl)
                        .Child("/" + tableName + "/" + quizkey + "/" + fileName); //data_quizsoal/(quizkey)/(datafiles)

                    fileRef.GetMetadataAsync().ContinueWithOnMainThread(task => {
                        if (task.IsCompleted && !task.IsFaulted && task.Result != null)
                        {
                            // File ada, lakukan sesuatu di sini
                            Debug.Log("File dengan nama " + fileName + " ada!");
                            DownloadFile(fileRef);
                        }
                        else
                        {
                            // File tidak ada, lakukan sesuatu di sini

                            Debug.Log("File dengan nama "+fileName+" tidak ada.");
                        }
                    });
                }
            }
        }
    }
    private void DownloadFile(StorageReference fileRef)
    {
        string localFilePath = Application.persistentDataPath + "/qData/" + fileRef.Name; //(local)/qData/

        fileRef.GetFileAsync(localFilePath).ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError($"Failed to download file {fileRef.Name}, file is missing / not found");
            }
            else
            {
                Debug.Log($"File {fileRef.Name} downloaded successfully to {localFilePath}");
            }
        });
    }

    //IEnumerator imgLoader() {
    //    using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(linkSample))
    //    {
    //        yield return req.SendWebRequest();

    //        if (req.isNetworkError || req.isHttpError)
    //        {
    //            Debug.LogError(req.error);
    //        }
    //        else
    //        {
    //            DownloadHandlerTexture handler = (DownloadHandlerTexture)req.downloadHandler;
    //            Texture2D sampleImg = handler.texture;

    //            Rect rect = new Rect(0, 0, sampleImg.width, sampleImg.height);
    //            Vector2 pivot = new Vector2(0.5f, 0.5f);

    //            Sprite newTexture = Sprite.Create(sampleImg, rect, pivot);

    //            imgSample.sprite = newTexture;
    //        }
    //    }

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
                QuestText.text = "Tidak ada koneksi internet.";
                //Debug.Log("Tidak ada koneksi internet.");
                break;

            case NetworkReachability.ReachableViaCarrierDataNetwork:
                //Debug.Log("Terhubung melalui jaringan data operator seluler.");
                QuestText.text = "Terhubung melalui jaringan data operator seluler.";
                break;

            case NetworkReachability.ReachableViaLocalAreaNetwork:
                QuestText.text = "Terhubung melalui jaringan lokal (Wi-Fi atau Ethernet).";
                Debug.Log("Terhubung melalui jaringan lokal (Wi-Fi atau Ethernet).");
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
                Debug.Log($"Data: {jawabanSoal.IdSoal}, {jawabanSoal.HasAnswer}, {jawabanSoal.Jawaban}, {jawabanSoal.Status}, {jawabanSoal.SoalType}, {jawabanSoal.PointEssay}");

                if (matchAnswer.SoalType == 2) //jika soal essay maka set jawaban soal yang sudah di pilih.
                {
                    descAnswerText.text = jawabanSoal.Jawaban;
                }

                if ((matchAnswer.HasAnswer == true) && (matchAnswer.Status == true))
                {
                    Debug.Log("Soal Telah Memiliki Jawaban dengan status benar");
                }
                else if ((matchAnswer.HasAnswer == true) && (matchAnswer.Status == false))
                {
                    Debug.Log("Soal Telah Memiliki Jawaban dengan status salah");
                }

            }
            else
            {
                Debug.Log("No entry found for ID :" + idQuest);
            }
        }
    }

    public void btnAnswer(int answerTyp)
    {
        bool isAnyAnswerUpdated = false; // Tandai apakah ada jawaban yang diupdate

        foreach (string currentIdSoal in currentIds)
        {
            jawabanSoal.IdSoal = currentIdSoal; // Set ID soal yang sedang aktif
        }

        //1 = multiply
        if (answerTyp == 1)
        {
            TMP_Text currentJwb = EventSystem.current.currentSelectedGameObject.transform.GetChild(0).GetComponent<TMP_Text>(); //ambil komponen teks dari btn jawaban.
            jawabanSoal.SoalType = answerTyp;
            jawabanSoal.Jawaban = currentJwb.text; // Set jawaban yang dipilih

            if (jawabanSoal.Jawaban == correctAnswer)
            {
                jawabanSoal.Status = true; // Set true ketika jawaban benar.
            }
            else
            {
                jawabanSoal.Status = false; // Set false ketika jawaban salah.
            }

            jawabanSoal.HasAnswer = true; // Set true ketika sudah dijawab.
            jawabanSoal.PointEssay = 0;
            isAnyAnswerUpdated = true; // Jawaban salah, tandai bahwa ada jawaban yang dapat diupdate.

        }
        //2 = essay //becareful this isn't recommended for long answer....
        else if (answerTyp == 2)
        {
            var currentJwb = descAnswerText.text;
            jawabanSoal.SoalType = answerTyp;
            jawabanSoal.HasAnswer = true;
            isAnyAnswerUpdated = true;
            jawabanSoal.Jawaban = currentJwb;

            //jaro-winkler performed on here...
            // lakukan pemrosesan teks pada kedua string yang akan di cocokan.
            string[] userInput = ProcessText(currentJwb);
            string[] keyAnswer = ProcessText(correctAnswer);

            double similarityScore = CalculateJaroWinklerScore(userInput, keyAnswer); //hitung kemiripan antara input pengguna & kunci jawaban.
            double threshold = 0.75; //batas poin untuk kemiripan string, atur sesuai kebutuhan

            if(similarityScore >= threshold) //jika diatas poin threshold maka status jawaban akan benar, jika di bawah maka status jawaban salah.
            {
                jawabanSoal.Status = true;
                jawabanSoal.PointEssay = similarityScore;
            }
            else
            {
                jawabanSoal.Status = false;
                jawabanSoal.PointEssay = similarityScore;
            }

        }
        else
        {
            Debug.LogError("AnswerTyp : Undefined type "+answerTyp+" isn't implemented!");
        }


        if (isAnyAnswerUpdated == true)
        {
            // Lakukan pembaruan hanya jika ada jawaban yang diupdate

            foreach (var idQuest in currentIds)
            {
                var matchAnswer = jawabanSoalList.Find(j => j.IdSoal == idQuest);

                if (matchAnswer != null)
                {
                    // Data sudah ada, lakukan update jika ada perubahan
                    if (matchAnswer.Jawaban != jawabanSoal.Jawaban || matchAnswer.Status != jawabanSoal.Status)
                    {
                        matchAnswer.SoalType = answerTyp;
                        matchAnswer.Jawaban = jawabanSoal.Jawaban;
                        matchAnswer.HasAnswer = jawabanSoal.HasAnswer;
                        matchAnswer.Status = jawabanSoal.Status;
                        matchAnswer.PointEssay = jawabanSoal.PointEssay;

                        //Debug.Log($"Data diperbarui untuk ID: {idQuest}");
                    }
                    else
                    {
                        //Debug.Log($"Data tidak berubah untuk ID: {idQuest}");
                    }
                }
                else
                {
                    //Debug.Log("Tidak ada entri ditemukan untuk ID :" + idQuest + ", Menambahkan Ke list Soal yang dijawab.");

                    // Buat objek baru dan tambahkan ke list
                    var newAnswer = new JawabanSoal();
                    newAnswer.IdSoal = idQuest;
                    newAnswer.Jawaban = jawabanSoal.Jawaban;
                    newAnswer.HasAnswer = jawabanSoal.HasAnswer;
                    newAnswer.Status = jawabanSoal.Status;

                    jawabanSoalList.Add(newAnswer);
                }
            }
        }
        else
        {
            //Debug.Log("Tidak ada jawaban yang diupdate.");
        }

        truePoint = jawabanSoalList.Count(j => j.Status); // ambil total jawaban benar dari jawabanSoalList
        falsePoint = jawabanSoalList.Count(j => !j.Status); // ambil total jawaban salah dari jawabanSoalList
        notAnswered = questSize - (truePoint + falsePoint); // jumlah soal - jawaban benar & salah
        NextPage();
        Debug.Log("Data jawaban soal saat ini :" + jawabanSoalList.Count);
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

    void GenerateQuestLocal(String qid)
    {
        var dataSoalToDisplay = dataSoalList.FirstOrDefault(dataSoal => dataSoal.IdQ == qid); //cocokan data id soal yang di panggil dengan id soal yang ada di data soal.
        GenerateQuestLocal(dataSoalToDisplay); // panggil data soal yang sesuai dengan id soal.
    }
    void GenerateQuestLocal(DataSoal data)
    {
        //var idQ = $"{data.IdQ}";
        QuestText.text = $"{data.DescQ}";
        questType = $"{data.QType}";
        correctAnswer = $"{data.TrueAnswer}";

        if (questType == "Multi") //full text
        {
            AnswerContainer[0].SetActive(true);
            AnswerContainer[1].SetActive(false);

            for (int i = 0; i < listMultiAnswerText.Length; i++)
            {
                var options = $"{data.Options[i]}";
                answers.Add(options); // tambahkan ke list answer
            }

            SattoloShuffle(answers); //randomifikasi menggunakan sattolo

            for (int i = 0; i < listMultiAnswerText.Length; i++)
            {
                listMultiAnswerText[i].text = answers[i]; //masukan & tampilkan hasil randomifikasi ke UI
            }
            answers.Clear(); //bersihkan data jawaban untuk soal selanjutnya.
        }
        else if (questType == "multiimgd") // questions text+img + answer text
        {
            Debug.Log("Questions IMG + text");
        }
        else if (questType == "multidimg") // questions text + answer img
        {
            Debug.Log("Questions text + IMG");
        }
        else if (questType == "multiimgf") // questions text+img + answer img
        {
            Debug.Log("Questions IMG full");
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
            AnswerContainer[0].SetActive(false);
            AnswerContainer[1].SetActive(false);
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

        //GenerateQuest untuk menampilkan data soal berdasarkan currentIds secara Lokal (ver. Offline)
        foreach (var ids in currentIds)
        {
            GenerateQuestLocal(ids);
            Debug.Log($"Halaman {curpageIndex + 1}: {ids}");
        }
        checkQuestionStatus();
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

    //Jaro Winkler
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

}
public class DataSoal //pending
{
    public string IdQ { get; set; }
    public string DescQ { get; set; }
    public string QType { get; set; }
    public string[] Options { get; set; }
    public string TrueAnswer { get; set; }
}

using Firebase;
using Firebase.Database;
using Firebase.Storage;
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
    //initial firebase component
    DatabaseReference databaseReference;
    //FirebaseStorage firebaseStorage;
    //StorageReference storageReference;
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

    private float animationTime = 0.30f;

    //String for store another Quiz Data
    private protected string questType;
    private protected List<string> correctAnswer = new List<string>();
    private protected List<string> keywords = new List<string>();
    private protected int pointQuest; //nilai point quest jika skill diaktifkan.
    [SerializeField] private protected double pointQuestTrue; //nilai point quest jika skill diaktifkan.
    [SerializeField] private protected double pointQuestFalse = 0; //nilai point quest jika skill diaktifkan.

    [SerializeField]int curpageIndex = 0; //initial page
    const int pageSize = 1; //1 quest per page. default is 1
    int countingdownload = 0;
    int faildownload = 0;

    private string[] fileName;

    //untuk pendataan soal dengan jawaban benar, salah & tidak terisi.
    [SerializeField]private int truePoint, falsePoint, hasAnswer, noAnswer;
    private double totalScore;
    private string pointAlphabet;
    private int consecutiveCorrectAnswers;

    private bool streakShield = false;
    //private bool indexChooser = false;
    //private int lastIndex = 0;

    //list string untuk kelola data lokal.
    private List<string> ids = new List<string>(); //for store id soal
    private List<string> currentIds = new List<string>(); //for current page selected
    private List<string> answers = new List<string>(); //for store answer
    List<AnswersData> jawabanSoalList = new List<AnswersData>();
    List<QuestionsData> dataSoalList = new List<QuestionsData>(); //for store questions data

    string[] targetTipeSoal = { "Multidimg", "Multiimgd", "Multiimgf", "Essayimgd" }; //for question has image
    string[] targetTipeSoalM = { "Multi", "Multidimg", "Multiimgd", "Multiimgf" }; //for multi questiom
    string[] targetTipeSoalE = { "Essay", "Essayimgd" }; //for essay question.
    string[] subTargetTipeSoal = { "Multidimg", "Multiimgf" }; //for question multi has image.
    string[] subTargetTipeSoalImage = { "Multiimgd", "Essayimgd" }; //for question (questin Image) has image.

    //AES Key & IV 16 byte
    private static readonly byte[] key = Encoding.UTF8.GetBytes("AzraRakobarReinz"); // Ganti dengan kunci rahasia Anda
    private static readonly byte[] iv = Encoding.UTF8.GetBytes("0721200007212024"); // Ganti dengan initial vector Anda
    private static readonly System.Random Rand = new System.Random();
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
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        //firebaseStorage = FirebaseStorage.DefaultInstance;

        btnNavigator[0].onClick.AddListener(NextPage);
        btnNavigator[1].onClick.AddListener(PreviousPage);
        //btnNavigator[2].gameObject.SetActive(false);

        btnStartText = btnStartQuiz.transform.GetComponentInChildren<TMP_Text>();
        descAnswerText = AnswerContainer[1].transform.GetComponentInChildren<TMP_InputField>();


        ScoreObj.transform.GetChild(1).transform.gameObject.SetActive(false);
        ScorePopupPos = ScoreObj.transform.GetChild(1).transform.localPosition;
        ScorePopupDefYPos = ScorePopupPos.y;
        ScorePopupPos.y = ScorePopupYPos;
        ScoreObj.transform.GetChild(1).transform.localPosition = ScorePopupPos;

    }

    public void getQuestData(string uid, string qkey, int questionSize, int questionPoint = 0, bool skillstat = false)
    {
        //reset Data.
        consecutiveCorrectAnswers = 0;
        curpageIndex = 0;
        totalScore = 0;
        hasAnswer = 0;
        noAnswer = 0;

        ids.Clear();
        currentIds.Clear();
        dataSoalList.Clear();
        jawabanSoalList.Clear();
        answers.Clear();
        correctAnswer.Clear();
        keywords.Clear();
        //end reset Data.

        this.skillstat = skillstat;

        questSize = questionSize;
        quizkey = qkey;
        uID = uid;
        pointQuest = questionPoint;
        pointQuestTrue = skillstat ? pointQuest : 0;
        pointQuestFalse = 0;

        btnNavigator[0].gameObject.SetActive(!skillstat);
        btnNavigator[1].gameObject.SetActive(!skillstat);
        ScoreObj.SetActive(skillstat);

        StartCoroutine(getQuestData(tableQuizName, qkey));
    }

    IEnumerator getQuestData(string tableReference, string quizKeyReference)
    {
        //dbref = FirebaseDatabase.DefaultInstance.RootReference;
        // Set query & Get data dari Firebase
        
        var matchedID = new List<string>(); //wadah untuk data soal dengan id yang sama persis.
        var matchedDifByID = new List<string>(); //wadah untuk data soal dengan id yang sama.
        var matchedData = new List<QuestionsData>(); //wadah untuk data soal yang sama persis.
        var matchedDifData = new List<QuestionsData>(); //wadah untuk data soal yang berbeda
        var rfDataQuestions = new List<QuestionsData>(); //wadah untuk data soal yang diperbaharui.
        var answerData = new List<AnswersData>(); //wadah untuk data jawaban soal (terdapat soal yang berbeda).
        var filePath = new List<string>(); //wadah untuk path file yang di Hashing

        var query = databaseReference.Child(tableReference).Child(quizKeyReference).OrderByKey();
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
                QuestionOptions = Enumerable.Range(1, btnMultiAnswers.Length + 1) // Membuat urutan 1 sampai (ukuran listMultiAnswerText.Length)
                .Select(i => child.Child($"option_{i}").Value.ToString()) // Mengambil nilai dari option_1, option_2, ..., option_5
                .ToArray(),
                //QuestionRightAnswer = child.Child("true_answ").Value.ToString()
                QuestionRightAnswer = child.Child("true_answ").HasChildren ? child.Child("true_answ").Children.Select(snapshot => snapshot.Value.ToString()).ToArray() : new string[] { child.Child("true_answ").Value.ToString() },
                QuestionKeyword = child.Child("keywords").Children != null ? child.Child("keywords").Children.Select(snapshot => snapshot.Value.ToString()).ToArray() : null
            }).ToList();

            SattoloShuffle(dataSoal); //randomisasi dengan algoritma Sattolo Shuffle.

            var dataSoalTaken = dataSoal.Take(questSize).ToList(); //ambil dataSoal yang sudah dirandomifikasi dengan jumlah yang sudah diatur pada questSize.

            //inisiasi penamaan dan alamat file untuk data soal, jawaban, dan index soal.
            
            fileName = new string[] { 
                $"{uID}_{quizkey}_{skillstat}_QD", 
                $"{uID}_{quizkey}_{skillstat}_AD", 
                $"{uID}_{quizkey}_{skillstat}_ID" , 
                $"{uID}_{quizkey}_{skillstat}_QTD", 
                $"{uID}_{quizkey}_{skillstat}_QSD",
                $"{uID}_{quizkey}_{skillstat}_SD0",
                $"{uID}_{quizkey}_{skillstat}_SD1",
                $"{uID}_{quizkey}_{skillstat}_SD2",
                $"{uID}_{quizkey}_{skillstat}_SD3",
                $"{uID}_{quizkey}_{skillstat}_QTD"}; //contoh : 10011232_QUIZ03_Q //contoh : 10011232_QUIZ03_Q

            
            foreach (var nameFile in fileName)
            {
                filePath.Add($"{Path.Combine(Application.persistentDataPath, EDProcessing.HashSHA384("sessionQuizData"), EDProcessing.HashSHA384(nameFile))}");
            }

            //cek apakah ada data soal pada penyimpanan internal
            if (File.Exists(filePath[0]))
            {
                //proses memasukan data pada soal dalam file ke dalam objek list localDataSoal
                int found = 0;
                var localQuestionsData = EDProcessing.Decrypt(File.ReadAllText(filePath[0]), key, iv).Split('\n');
                //var localDataSoal = new List<QuestionsData>();

                //pengecekan question data satu per satu.
                foreach (var localSoal in localQuestionsData)
                {
                    var localDataQuestion = JsonUtility.FromJson<QuestionsData>(localSoal);

                    //melakukan pencarian data yang sama.
                    var findMatchQuestion = dataSoal.Find(data => 
                    data.QuestionID == localDataQuestion.QuestionID &&
                    data.QuestionDescription == localDataQuestion.QuestionDescription &&
                    data.QuestionIMGURL == localDataQuestion.QuestionIMGURL &&
                    data.QuestionType == localDataQuestion.QuestionType &&
                    string.Join("|", data.QuestionOptions) == string.Join("|", localDataQuestion.QuestionOptions) &&
                    string.Join("|", data.QuestionRightAnswer) == string.Join("|", localDataQuestion.QuestionRightAnswer) &&
                    string.Join("|", data.QuestionKeyword) == string.Join("|", localDataQuestion.QuestionKeyword));

                    //melakukan pencarian data yang tidak sama berdasarkan ID.
                    var findMatchQuestionByID = dataSoal.Find(data =>
                    data.QuestionID == localDataQuestion.QuestionID && (
                    data.QuestionDescription != localDataQuestion.QuestionDescription ||
                    data.QuestionIMGURL != localDataQuestion.QuestionIMGURL ||
                    data.QuestionType != localDataQuestion.QuestionType ||
                    string.Join("|", data.QuestionOptions) != string.Join("|", localDataQuestion.QuestionOptions) ||
                    string.Join("|", data.QuestionRightAnswer) != string.Join("|", localDataQuestion.QuestionRightAnswer) ||
                    string.Join("|", data.QuestionKeyword) != string.Join("|", localDataQuestion.QuestionKeyword)));

                    //melakukan pencarian data yang tidak sama.
                    //var findNotMatchQuestion = dataSoal.Find(data =>
                    //data.QuestionID != localDataQuestion.QuestionID &&
                    //data.QuestionDescription != localDataQuestion.QuestionDescription &&
                    //data.QuestionIMGURL != localDataQuestion.QuestionIMGURL &&
                    //data.QuestionType != localDataQuestion.QuestionType &&
                    //string.Join("|", data.QuestionOptions) != string.Join("|", localDataQuestion.QuestionOptions) &&
                    //string.Join("|", data.QuestionRightAnswer) != string.Join("|", localDataQuestion.QuestionRightAnswer) &&
                    //string.Join("|", data.QuestionKeyword) != string.Join("|", localDataQuestion.QuestionKeyword));

                    //melakukan pencarian data dengan id yang tidak sama.
                    var findNotMatchQuestionByid = dataSoal.Find(data =>
                    data.QuestionID != localDataQuestion.QuestionID);

                    //memasukan data yang sama persis.
                    if (findMatchQuestion != null)
                    {
                        found++; //hitung data yang sama
                        matchedData.Add(findMatchQuestion);
                        matchedID.Add(findMatchQuestion.QuestionID);
                    }

                    //memasukan id question dengan data yang berbeda.
                    if (findMatchQuestionByID != null)
                    {
                        matchedDifByID.Add(findMatchQuestionByID.QuestionID);
                    }

                    //memasukan data yang tidak sama.
                    if (findNotMatchQuestionByid != null)
                    {
                        matchedDifData.Add(findNotMatchQuestionByid);
                    }
                }

                //jika jumlah data yang sama sesuai dengan pengaturan dari questSize, maka eksekusi data soal yang berada di penyimpanan internal.
                if (found == questSize)
                {
                    //ids.AddRange(matchedData.Select(entry => entry.QuestionID).ToList()); //menyimpan data id soal pada variabel global ids.
                    ids.AddRange(matchedID); //menyimpan data id soal pada variabel global ids.
                    dataSoalList.AddRange(matchedData); //simpan data soal yang sudah di load sebelumnya. 

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
                        //Debug.Log("Tidak Terdeteksi Adanya record file tipe AD, Tidak ada proses Load.");
                        NoAnswerAdd(ids, matchedData);
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

                    if (File.Exists(filePath[4])) //checking data if exist for consecutiveCorrectAnswers.
                    {
                        consecutiveCorrectAnswers = int.Parse(EDProcessing.Decrypt(File.ReadAllText(filePath[4]), key, iv));
                    }
                    else
                    {
                        consecutiveCorrectAnswers = 0;
                    }

                    PrepairingDownload(matchedData);
                }
                else
                {
                    //blok proses perubahan data soal.
                    var remainQDataNeeded = questSize - found;
                    int changedData = 0;
                    
                    rfDataQuestions.AddRange(matchedData); //data yang sama identik.

                    //pengecekan id yang sama dengan data yang berbeda.
                    if(matchedDifByID.Count != 0)
                    {
                        //melakukan loop untuk memasukan data id yang sama.
                        foreach (var QuestionID in matchedDifByID)
                        {
                            var findMatchDataByID = dataSoal.Find(data => data.QuestionID == QuestionID);

                            //jika findMatchDataByID tidak kosong
                            if (findMatchDataByID != null)
                            {
                                //tambahkan data soal ke rfDataQuestions
                                rfDataQuestions.Add(findMatchDataByID);
                                remainQDataNeeded--; //kurangi index data soal yang dibutuhkan
                                changedData++;
                            }
                        }
                        Debug.Log($"Data Soal Setelah ditambahkan berdasarkan ID yang sama : {rfDataQuestions.Count}");
                    }

                    //jika Question Data belum terpenuhi, lakukan penambahan data soal secara random.
                    if (remainQDataNeeded != 0)
                    {
                        if (matchedDifData != null)
                        {
                            SattoloShuffle(matchedDifData); //lakukan randomifikasi ulang.
                            rfDataQuestions.AddRange(matchedDifData.Take(remainQDataNeeded).ToList());
                            changedData += remainQDataNeeded;
                            remainQDataNeeded = 0;
                            Debug.Log($"Data Soal Setelah ditambahkan data id baru : {rfDataQuestions.Count}");
                        }
                        else
                        {
                            Debug.Log("tidak ada data soal yang tersedia");
                        }
                    }

                    //akhir blok.

                    //blok proses peresetan data jawaban 
                    if (File.Exists(filePath[1])) //pengecekan file jawaban.
                    {
                        
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
                        var findExceptIDQuestion = answerData.Except(findSameIDQuestion).ToList();
                        var findSameAnswerIDQuestion = findSameIDQuestion.Select(answer => answer.QuestionID).ToList();
                        var missingAnswer = answerData.Count - findSameIDQuestion.Count;
                        answerData.Clear();

                        //pengecekan Tipe Soal Untuk Jawaban yang telah di saring. jika berbeda terdapat perubahan.
                        foreach (var Answer in findSameIDQuestion)
                        {
                            var dataQuestions = rfDataQuestions.Find(question => question.QuestionID == Answer.QuestionID);
                            var questionType = targetTipeSoalE.Contains(dataQuestions.QuestionType) ? 2 : 1;

                            if (Answer.QuestionType != questionType)
                            {

                                Answer.QuestionType = questionType;
                                Answer.AnswerDescription = string.Empty;
                                Answer.AnswerStatus = false;
                                Answer.AnswerEssayPoint = 0;
                                Answer.AnswerScorePoint = 0;
                                Answer.QuestionHasAnswer = false; //set status jawaban ke false
                                Answer.QuestionTimeTake = 0;

                                answerData.Add(Answer);
                            }
                            else
                            {
                                if(Answer.QuestionType == 1)
                                {
                                    if (dataQuestions.QuestionOptions.Contains(Answer.AnswerDescription) || dataQuestions.QuestionRightAnswer.Contains(Answer.AnswerDescription))
                                    {
                                        answerData.Add(Answer);
                                    }
                                    else
                                    {
                                        Answer.QuestionType = questionType;
                                        Answer.AnswerDescription = string.Empty;
                                        Answer.AnswerStatus = false;
                                        Answer.AnswerEssayPoint = 0;
                                        Answer.AnswerScorePoint = 0;
                                        Answer.QuestionHasAnswer = false; //set status jawaban ke false
                                        Answer.QuestionTimeTake = 0;
                                        answerData.Add(Answer);
                                    }
                                }
                                else if(Answer.QuestionType == 2)
                                {
                                    var RegexAnswer = ProcessText(Answer.AnswerDescription);

                                    if(RegexAnswer.Length < 2)
                                    {
                                        if (dataQuestions.QuestionKeyword.Contains(RegexAnswer[0]) || dataQuestions.QuestionRightAnswer.Contains(RegexAnswer[0]))
                                        {
                                            answerData.Add(Answer);
                                        }
                                        else
                                        {
                                            Answer.QuestionType = questionType;
                                            Answer.AnswerDescription = string.Empty;
                                            Answer.AnswerStatus = false;
                                            Answer.AnswerEssayPoint = 0;
                                            Answer.AnswerScorePoint = 0;
                                            Answer.QuestionHasAnswer = false; //set status jawaban ke false
                                            Answer.QuestionTimeTake = 0;

                                            answerData.Add(Answer);
                                        }
                                    }
                                    else
                                    {
                                        foreach (var dqWord in dataQuestions.QuestionKeyword)
                                        {
                                            var keyword = ProcessText(dqWord);
                                            var foundKey = 0;

                                            foreach (var k in keyword)
                                            {
                                                if (RegexAnswer.Contains(k))
                                                {
                                                    foundKey++;
                                                    break;
                                                }
                                            }

                                            if(foundKey == 0)
                                            {
                                                Answer.QuestionType = questionType;
                                                Answer.AnswerDescription = string.Empty;
                                                Answer.AnswerStatus = false;
                                                Answer.AnswerEssayPoint = 0;
                                                Answer.AnswerScorePoint = 0;
                                                Answer.QuestionHasAnswer = false; //set status jawaban ke false
                                                Answer.QuestionTimeTake = 0;

                                                answerData.Add(Answer);
                                            }
                                            else
                                            {
                                                answerData.Add(Answer);
                                            }

                                        }
                                    }
                                }
                                else
                                {
                                    Debug.Log($"Unknown Answer Type :{Answer.QuestionType}");
                                }
                                
                            }
                        }

                        Debug.Log($"Data Answer yang telah di proses tahap 1 : {answerData.Count}");

                        if(findExceptIDQuestion.Count != 0)
                        {
                            foreach (var Answer in findExceptIDQuestion)
                            {
                                var dataQuestions = rfDataQuestions.Find(question => question.QuestionID == Answer.QuestionID);
                                var questionType = targetTipeSoalE.Contains(dataQuestions.QuestionType) ? 2 : 1;

                                if(dataQuestions != null)
                                {
                                    if (Answer.QuestionType != questionType)
                                    {
                                        Answer.QuestionType = questionType;
                                        Answer.AnswerDescription = string.Empty;
                                        Answer.AnswerStatus = false;
                                        Answer.AnswerEssayPoint = 0;
                                        Answer.AnswerScorePoint = 0;
                                        Answer.QuestionHasAnswer = false; //set status jawaban ke false
                                        Answer.QuestionTimeTake = 0;

                                        answerData.Add(Answer);
                                    }
                                    else
                                    {
                                        answerData.Add(Answer);
                                    }

                                    missingAnswer--;
                                }
                                else
                                {
                                    Debug.Log($"Data Soal Tidak Ada.");
                                }
                            }
                        }

                        Debug.Log($"Data Answer yang telah di proses tahap 2 : {answerData.Count}");

                        //jika data jawaban masih kurang dari data soal.
                        if (missingAnswer != 0)
                        {
                            var findQuestionNoAnswer = rfDataQuestions.Where(data => !findSameAnswerIDQuestion.Contains(data.QuestionID)).ToList();
                            var AnswerData = new AnswersData();

                            Debug.Log($"Jumlah Soal yang tidak memiliki wadah jawaban : {findQuestionNoAnswer.Count}");

                            foreach (var Question in findQuestionNoAnswer)
                            {
                                var questionType = targetTipeSoalE.Contains(Question.QuestionType) ? 2 : 1;

                                AnswerData.QuestionID = Question.QuestionID;
                                AnswerData.QuestionType = questionType;
                                AnswerData.AnswerDescription = string.Empty;
                                AnswerData.AnswerStatus = false;
                                AnswerData.AnswerEssayPoint = 0;
                                AnswerData.AnswerScorePoint = 0;
                                AnswerData.QuestionHasAnswer = false; //set status jawaban ke false
                                AnswerData.QuestionTimeTake = 0;

                                answerData.Add(AnswerData);

                                missingAnswer--;
                            }
                        }
                        else
                        {
                            Debug.Log($"Jawaban Sudah Sama dengan jumlah soal. Tidak dilakukan penambahan wadah jawaban.");
                        }

                        //mengurutkan data jawaban yang telah di jawab.
                        answerData.Sort((a, b) =>
                        {
                            if (a.QuestionHasAnswer && !b.QuestionHasAnswer)
                                return -1;
                            else if (!a.QuestionHasAnswer && b.QuestionHasAnswer)
                                return 1;
                            else
                                return 0;
                        });

                        //mengurutkan data soal berdasarkan data jawaban.
                        rfDataQuestions = rfDataQuestions.OrderBy(q => answerData.FindIndex(a => a.QuestionID == q.QuestionID)).ToList();

                        Debug.Log($"Data Soal :{rfDataQuestions.Count}");
                        Debug.Log($"Data Jawaban :{answerData.Count}");

                        //masukan data ke variabel global.
                        ids.AddRange(rfDataQuestions.Select(entry => entry.QuestionID).ToList());
                        dataSoalList.AddRange(rfDataQuestions);
                        jawabanSoalList.AddRange(answerData);

                    }
                    else
                    {
                        NoAnswerAdd(ids, rfDataQuestions);
                    }

                    if (File.Exists(filePath[2])) //pengecekan file untuk last index
                    {
                        string fileIndexData = File.ReadAllText(filePath[2]); //baca data index soal;

                        if(jawabanSoalList.Count(j => j.QuestionHasAnswer) != 0 && changedData != 0)
                        {
                            curpageIndex = jawabanSoalList.Count(j => j.QuestionHasAnswer);
                        }
                        else
                        {
                            curpageIndex = int.Parse(EDProcessing.Decrypt(fileIndexData, key, iv));
                        }
                    }
                    else
                    {
                        curpageIndex = 0;
                    }

                    if (File.Exists(filePath[4])) //checking data if exist for consecutiveCorrectAnswers.
                    {
                        consecutiveCorrectAnswers = int.Parse(EDProcessing.Decrypt(File.ReadAllText(filePath[4]), key, iv));
                    }
                    else
                    {
                        consecutiveCorrectAnswers = 0;
                    }

                    PrepairingDownload(rfDataQuestions);
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
        NoAnswerAdd(ids, dataQuests);
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
        
        if (dataQuests.Any(data => targetTipeSoal.Contains(data.QuestionType)))
        {

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
            Debug.Log("Semua quest id tidak memiliki data untuk di download");
        }

        //Debug.Log("Total Questions fetched : " + dataSoalTaken.Count); //mengecek apakah data soal sudah di dapat.
    }

    IEnumerator DownloadFile(string Url, int MaxUrlSelected)
    {
        if (!string.IsNullOrEmpty(Url))
        {
            string fileName = DecodeURL(Url);
            string folderPath = Path.Combine(Application.persistentDataPath, /*EDProcessing.HashSHA384(*/"QuestionsData"/*)*/, /*EDProcessing.HashSHA384(*/quizkey/*)*/);
            string localFilePath = Path.Combine(folderPath, fileName);
            string ErrorHolder = string.Empty;
            //var folderPath = Application.persistentDataPath + $"/{EDProcessing.HashSHA384("QuestionsData")}/{EDProcessing.HashSHA384(quizkey)}";
            //string firebaseUrlPath = $"gs://theaswerqmaster.appspot.com/{tableQuizName}/{quizkey}/{fileName}";
            //membuat folder jika tidak ada.

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            if (!File.Exists(localFilePath))
            {
                //storageReference = firebaseStorage.GetReferenceFromUrl(Url);

                //var downloadTask = storageReference.GetFileAsync(folderPath);
                //yield return new WaitUntil(() => downloadTask.IsCompleted);

                //if (downloadTask.IsCompleted)
                //{
                //    countingdownload++;
                //    Debug.Log($"Download : {countingdownload}");
                //}
                //else if (downloadTask.IsFaulted)
                //{
                //    Debug.LogError(downloadTask.Exception.InnerExceptions);
                //    faildownload++;
                //    Debug.Log($"Failed Download : {faildownload}");
                //}

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
                        //Debug.LogError("Download failed: " + www.error);
                        ErrorHolder = www.error;
                        faildownload++;
                    }
                }
            }
            else
            {
                /*
                storageReference = firebaseStorage.GetReferenceFromUrl(Url);
                var localFileMetaData = new FileInfo(localFilePath);
                var metaDataTask = storageReference.GetMetadataAsync();
                yield return new WaitUntil(() => metaDataTask.IsCompleted);

                if (metaDataTask.IsCompleted)
                {
                    StorageMetadata storageMetadata = metaDataTask.Result;
                    if (storageMetadata.SizeBytes == localFileMetaData.Length)
                    {
                        countingdownload++;
                        Debug.Log($"Download : {countingdownload}");
                    }
                    else
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
                                faildownload++;
                            }
                        }
                    }
                }
                else if (metaDataTask.IsFaulted)
                {
                    Debug.LogError(metaDataTask.Exception.InnerExceptions);
                }
                else
                {
                    Debug.Log(metaDataTask.Exception);
                }*/

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
                                //Debug.LogError("Download failed: " + www.error);
                                faildownload++;
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
                    //Debug.LogError("Gagal mendapatkan ukuran file di URL. Error: " + sizeRequest.error);
                    faildownload++;
                    ErrorHolder = sizeRequest.error;
                }
            }

            //Debug.Log("Downloaded :" + countingdownload);
            if (countingdownload == MaxUrlSelected)
            {
                //Debug.Log("Semua File Telah Di download");
                DisplayCurPage();
                countingdownload = 0;
                btnStartText.text = "Mulai";
                btnStartQuiz.interactable = true;
            }
            else if (faildownload != 0)
            {
                //Debug.Log("terdownload : " + countingdownload + ", tidak terdownload : " + faildownload + ", dikarenakan error");
                alertController.AlertSet($"{countingdownload} / {MaxUrlSelected} Data Soal Yang Diperlukan Tidak Terunduh\n Silahkan Ulangi Proses Memasukan Kode,\nJika Masih Sama Hubungi Pengelola Atau Guru Untuk Lebih Lanjut,\nError : {ErrorHolder}", false, TextAlignmentOptions.Center, false, cleanCounting);

                btnStartText.text = $"{countingdownload} / {MaxUrlSelected} (Error)";
                btnStartQuiz.interactable = false;
                DisplayCurPage();
            }
            else
            {
                btnStartQuiz.interactable = false;
                btnStartText.text = $"{countingdownload} / {MaxUrlSelected}";
                Debug.Log("Terdownload : " + countingdownload + " dari " + MaxUrlSelected);
            }
        }
        else
        {
            Debug.Log("Url Kosong");
        }
        
    }

    private void cleanCounting()
    {
        countingdownload = 0;
        faildownload = 0;
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
        double cResult = 0.0;
        if (skillstat)
        {
           
            cResult = totalScore;
            double maxScore = pointQuest * questSize;

            switch (cResult)
            {
                case var _ when cResult > maxScore:
                    pointAlphabet = "S+";
                    break;
                case var _ when cResult >= Math.Round(maxScore * 0.95) && cResult <= maxScore:
                    pointAlphabet = "S";
                    break;
                case var _ when cResult >= Math.Round(maxScore * 0.90) && cResult <= Math.Round(maxScore * 0.94):
                    pointAlphabet = "A+";
                    break;
                case var _ when cResult >= Math.Round(maxScore * 0.85) && cResult <= Math.Round(maxScore * 0.89):
                    pointAlphabet = "A";
                    break;
                case var _ when cResult >= Math.Round(maxScore * 0.80) && cResult <= Math.Round(maxScore * 0.84):
                    pointAlphabet = "A-";
                    break;
                case var _ when cResult >= Math.Round(maxScore * 0.75) && cResult <= Math.Round(maxScore * 0.79):
                    pointAlphabet = "B+";
                    break;
                case var _ when cResult >= Math.Round(maxScore * 0.70) && cResult <= Math.Round(maxScore * 0.74):
                    pointAlphabet = "B";
                    break;
                case var _ when cResult >= Math.Round(maxScore * 0.65) && cResult <= Math.Round(maxScore * 0.69):
                    pointAlphabet = "B-";
                    break;
                case var _ when cResult >= Math.Round(maxScore * 0.60) && cResult <= Math.Round(maxScore * 0.64):
                    pointAlphabet = "C+";
                    break;
                case var _ when cResult >= Math.Round(maxScore * 0.55) && cResult <= Math.Round(maxScore * 0.59):
                    pointAlphabet = "C";
                    break;
                case var _ when cResult >= Math.Round(maxScore * 0.50) && cResult <= Math.Round(maxScore * 0.54):
                    pointAlphabet = "C-";
                    break;
                case var _ when cResult >= Math.Round(maxScore * 0.40) && cResult <= Math.Round(maxScore * 0.49):
                    pointAlphabet = "D";
                    break;
                case var _ when cResult <= Math.Round(maxScore * 0.40):
                    pointAlphabet = "E";
                    break;

                default:
                    pointAlphabet = cResult.ToString();
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
                    pointAlphabet = cResult.ToString();
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
        StartCoroutine(sendDataResult(uID, quizkey, pointAlphabet, skillstat ? cResult : double.Parse(cResult.ToString("F2")), truePoint, falsePoint, noAnswer, $"{timeInMinutes:00} : {timeInSecs:00}"));

    }

    private IEnumerator sendDataForRank(string uid, string qKey,string charpoint, int points, int trueAnswer, int falseAnswer, int noAnswer)
    {
        ResultStore dataHasil = new(charpoint, points, trueAnswer, falseAnswer, noAnswer);

        string json = JsonUtility.ToJson(dataHasil);

        var query = databaseReference.Child(tableResultRank).Child(qKey).Child(uid).SetRawJsonValueAsync(json);//sending data to firebase
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
        string dateFormat = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss tt");

        var queryDate = databaseReference.Child(tableResultName).Child(qKey).Child(uid).GetValueAsync();
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
            dataHasil = new(qKey, uid, charpoint, points, trueAnswer, falseAnswer, noAnswer, timeLapse, checkdate.ToString(), dateFormat);
            json = JsonUtility.ToJson(dataHasil);
            query = databaseReference.Child(tableResultName).Child(qKey).Child(uid).SetRawJsonValueAsync(json);//sending data to firebase
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
            dataHasil = new(qKey,uid, charpoint, points, trueAnswer, falseAnswer, noAnswer, timeLapse, dateFormat, dateFormat);
            json = JsonUtility.ToJson(dataHasil);
            query = databaseReference.Child(tableResultName).Child(qKey).Child(uid).SetRawJsonValueAsync(json);//sending data to firebase
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
        //hasAnswer = 0;
        //this.noAnswer = 0;
        //truePoint = 0;
        //falsePoint = 0;

        DeleteData();
        //inisiasi penamaan dan alamat file untuk data soal, jawaban, dan index soal.
        //string[] fileName = { $"{uID}_{quizkey}_{skillstat}_QD", $"{uID}_{quizkey}_{skillstat}_AD", $"{uID}_{quizkey}_{skillstat}_ID", $"{uID}_{quizkey}_{skillstat}_QTD" }; //contoh : 10011232_QUIZ03_Q
        
    }

    public void DeleteData()
    {
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
        var DateRT = DateTime.Now.ToString("dd/MM/YYYY");
        var query = databaseReference.Child(tableQuestionsDataRecordName).Child(uID).Child(quizkey).Child(DateRT).SetRawJsonValueAsync(index.ToString());

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
                Debug.Log($"Data Jawaban : {matchAnswer.QuestionID}, {matchAnswer.QuestionHasAnswer}, {matchAnswer.AnswerDescription}, {matchAnswer.AnswerStatus}, {matchAnswer.QuestionType}, {matchAnswer.AnswerEssayPoint}");

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

                            //answerIMG.color = new Color32(255, 255, 255, 170);
                            answerMulti.color = new Color32(255, 255, 255, 255);

                            if (matchAnswer.QuestionHasAnswer == true)
                            {
                                if (answerMulti.text == matchAnswer.AnswerDescription && string.IsNullOrEmpty(matchAnswer.AnswerDescription))
                                {
                                    if (skillstat)
                                    {
                                        if(answerMulti.text == matchAnswer.AnswerDescription && matchAnswer.AnswerStatus)
                                        {
                                            answerIMG.color = new Color32(0, 255, 0, 170);
                                        }
                                        else
                                        {
                                            answerIMG.color = new Color32(255, 0, 0, 170);
                                        }
                                    }
                                    else
                                    {
                                        answerIMG.color = new Color32(255, 255, 0, 170);
                                    }
                                    
                                }
                                else
                                {
                                    answerIMG.color = new Color32(0, 0, 0, 170);
                                }
                            }
                            else
                            {
                                answerIMG.color = new Color32(0, 0, 0, 170);
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
                    var answerBtn = AnswerContainer[1].transform.GetChild(1).transform.GetComponent<Button>();
                    var btnImage = answerBtn.gameObject.transform.GetComponent<Image>();

                    if (matchAnswer.QuestionHasAnswer)
                    {
                        answerEssay.text = matchAnswer.AnswerDescription;

                        if (matchAnswer.AnswerStatus)
                        {
                            if (skillstat)
                            {
                                var ColorStatus = matchAnswer.AnswerStatus ? new Color32(0, 255, 0, 170) : new Color32(255, 0, 0, 170);
                                btnImage.color = ColorStatus;
                            }
                            else
                            {
                                btnImage.color = new Color32(255, 255, 0, 170);
                            }
                        }
                        else
                        {
                            btnImage.color = new Color32(0, 0, 0, 170);
                        }
                    }
                    else
                    {
                        btnImage.color = new Color32(0, 0, 0, 170);
                    }

                    if (!answerBtn.IsInteractable())
                    {
                        answerEssay.interactable = true;
                        answerBtn.interactable = true;
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

        if (skillstat)
        {
            var ScoreUI = ScoreObj.transform.GetChild(0).transform.GetComponent<TMP_Text>();
            ScoreUI.text = totalScore.ToString();
        }

        //Debug.Log("Data list jawaban saat ini : " + hasAnswer + " jawaban, dari " + questSize + " soal.");
    }

    public void btnAnswer(int answerTyp)
    {
        AudioController.Instance.PlayAudioSFX("ButtonClick");
        var TMPScorePopup = ScoreObj.transform.GetChild(1).gameObject.GetComponent<TMP_Text>();
        foreach (string currentIdSoal in currentIds)
        {
            var newAnswer = new AnswersData(); //buat objek baru untuk soal yang aktif
            newAnswer.QuestionID = currentIdSoal; // Set ID soal yang sedang aktif


            //1 = multiply
            if (answerTyp == 1)
            {
                var selectedObject = EventSystem.current.currentSelectedGameObject;
                var currentJwb = selectedObject.transform.GetChild(0).GetComponent<TMP_Text>(); //ambil komponen teks dari object yang terpilih.

                newAnswer.QuestionType = answerTyp;
                newAnswer.AnswerDescription = currentJwb.text; // Set jawaban yang dipilih
                newAnswer.QuestionHasAnswer = true; // Set true ketika sudah dijawab.
                newAnswer.AnswerEssayPoint = 0; //default data if question essay point essay.

                if (newAnswer.AnswerDescription == correctAnswer[0]) //jika jawaban sama dengan kunci jawaban.
                {
                    newAnswer.AnswerStatus = true; // Set true ketika jawaban benar.
                    newAnswer.AnswerScorePoint = skillstat ? pointQuestTrue : 0;
                    //consecutiveCorrectAnswers++;
                }
                else
                {
                    newAnswer.AnswerStatus = false; // Set false ketika jawaban salah.
                    newAnswer.AnswerScorePoint = skillstat ? pointQuestFalse : 0;
                    //consecutiveCorrectAnswers = 0;
                }

                if (skillstat)
                {
                    var colorStatus = newAnswer.AnswerStatus ? Color.green : Color.red;
                    selectedObject.transform.GetComponent<Image>().color = colorStatus;
                    AudioController.Instance.PlayAudioSFX(newAnswer.AnswerStatus ? "CorrectEffect" : "InCorrectEffect");
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
                    alertController.AlertSet("Soal Wajib di isi!", false, TextAlignmentOptions.Center, false);
                }
                else
                {
                    // lakukan pemrosesan teks pada kedua string yang akan di cocokan.
                    var userInput = ProcessText(currentJwb.TrimEnd());
                    var historySimilarity = new List<double>();
                    var threshold = 0.75; //batas poin untuk kemiripan string, atur sesuai kebutuhan.

                    foreach(var keyword in keywords)
                    {
                        var processedKeyword = ProcessText(keyword);

                        foreach(var word in processedKeyword)
                        {
                            if (userInput.Contains(word))
                            {
                                foreach (var keyAnswer in correctAnswer)
                                {
                                    if (!string.IsNullOrEmpty(keyAnswer))
                                    {
                                        var processedkeyAnswer = ProcessText(keyAnswer);
                                        var similarityScore = 0.0; //default point.

                                        if (userInput.Length == 1)
                                        {
                                            similarityScore = userInput[0].Contains(word) ? 1 : 0;
                                        }
                                        else
                                        {
                                            similarityScore = CalculateJaroWinklerScoreWithKeywordWeighting(userInput, processedkeyAnswer, processedKeyword);
                                        }

                                        historySimilarity.Add(similarityScore);
                                    }
                                }
                            }
                            else
                            {
                                historySimilarity.Add(0);
                            }
                        }
                    }

                    foreach (var sim in historySimilarity)
                    {
                        Debug.Log($"{newAnswer.QuestionID}, similarity :" + sim);
                    }

                    //mengambil nilai tertinggi dari historySimilarity dan jika diatas poin threshold maka status jawaban akan benar, jika di bawah maka status jawaban salah.
                    if (historySimilarity.Max() >= threshold)
                    {
                        newAnswer.AnswerStatus = true;
                        newAnswer.AnswerEssayPoint = historySimilarity.Max();
                        newAnswer.AnswerScorePoint = skillstat ? pointQuestTrue : 0;
                        //consecutiveCorrectAnswers++;
                    }
                    else
                    {
                        newAnswer.AnswerStatus = false;
                        newAnswer.AnswerEssayPoint = historySimilarity.Max();
                        newAnswer.AnswerScorePoint = skillstat ? pointQuestFalse : 0;
                        //consecutiveCorrectAnswers = 0;
                    }

                    if (skillstat)
                    {
                        var colorStatus = newAnswer.AnswerStatus ? Color.green : Color.red;
                        AnswerContainer[1].transform.GetChild(1).transform.GetComponent<Image>().color = colorStatus;
                        AudioController.Instance.PlayAudioSFX(newAnswer.AnswerStatus ? "CorrectEffect" : "InCorrectEffect");
                    }
                }
            }
            else
            {
                alertController.AlertSet("AnswerTyp : Undefined type " + answerTyp + " isn't implemented!", true,TextAlignmentOptions.Center);
            }

            if (skillstat)
            {
                //scorepoint
                //TMPScorePopup.text = $"{(point >= 0 ? "+" : "" )} {point}";

                if (newAnswer.AnswerScorePoint == 0)
                {
                    TMPScorePopup.color = Color.gray;
                }
                else if (newAnswer.AnswerScorePoint > pointQuest)
                {
                    TMPScorePopup.color = Color.yellow;
                }
                else if (newAnswer.AnswerScorePoint < 0)
                {
                    TMPScorePopup.color = Color.red;
                }
                else if (newAnswer.AnswerScorePoint == pointQuest)
                {
                    TMPScorePopup.color = Color.green;
                }
            
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

                        if (!streakShield)
                        {
                            if (!string.IsNullOrEmpty(matchAnswer.AnswerDescription))
                            {
                                consecutiveCorrectAnswers = newAnswer.AnswerStatus ? consecutiveCorrectAnswers + 1 : 0;
                            }
                            
                        }

                        if (skillstat & /*newAnswer.AnswerScorePoint != 0 &*/ newAnswer.AnswerDescription != string.Empty)
                        {
                            StartCoroutine(AnimatedScorePopupTime($"{(newAnswer.AnswerScorePoint >= 0 ? "+" : "")} {newAnswer.AnswerScorePoint}"));
                        }

                        //Debug.Log($"Data diperbarui untuk ID: {currentIdSoal}");
                    }
                    else
                    {
                        if (skillstat & /*matchAnswer.AnswerScorePoint != 0 &*/ newAnswer.AnswerDescription != string.Empty)
                        {
                            TMPScorePopup.color = new Color32(90, 195, 228, 255);
                            StartCoroutine(AnimatedScorePopupTime($"{matchAnswer.AnswerScorePoint} (Tidak Berubah)"));
                        }
                        //Debug.Log($"Data tidak berubah untuk ID: {currentIdSoal}");
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

            if (!string.IsNullOrEmpty(newAnswer.AnswerDescription))
            {
                foreach(var btn in btnMultiAnswers)
                {
                    btn.interactable = false;
                }

                if (streakShield)
                {
                    streakShield = false;
                }

                AnswerContainer[1].transform.GetChild(0).transform.GetComponent<TMP_InputField>().interactable = false;
                AnswerContainer[1].transform.GetChild(1).transform.GetComponent<Button>().interactable = false;

                NextPage();
                string streakFilePath = Path.Combine(Application.persistentDataPath, EDProcessing.HashSHA384("sessionQuizData"), EDProcessing.HashSHA384(fileName[4]));
                File.WriteAllText(streakFilePath, EDProcessing.Encrypt(consecutiveCorrectAnswers.ToString(),key, iv));
            }

        }



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
    
    private void NoAnswerAdd(List<string> idListData, List<QuestionsData> questionData) //inisialisasi jawaban kosong dari soal yang telah di list
    {
        foreach (string currentIdSoal in idListData)
        {
            var initialListAnswer = new AnswersData();
            var dataQuestions = questionData.Find(q => q.QuestionID == currentIdSoal);

            if(dataQuestions != null)
            {
                initialListAnswer.QuestionID = currentIdSoal; // Set ID soal yang sedang aktif
                initialListAnswer.QuestionType = targetTipeSoalE.Contains(dataQuestions.QuestionType) ? 2 : 1;
                initialListAnswer.AnswerDescription = string.Empty;
                initialListAnswer.AnswerStatus = false;
                initialListAnswer.AnswerEssayPoint = 0;
                initialListAnswer.AnswerScorePoint = 0;
                initialListAnswer.QuestionHasAnswer = false; //set status jawaban ke false
                initialListAnswer.QuestionTimeTake = 0;

                //Debug.Log($"No Answer Adding : {initialListAnswer.QuestionID}, {initialListAnswer.QuestionType}");

                jawabanSoalList.Add(initialListAnswer);
            }
            else
            {
                alertController.AlertSet("Error : Can't Read Question Data, Silahkan Hubungi Pengelola.");
                break;
            }
            
        }
    }

    void GenerateQuestLocalTemp(string qid)
    {
        var dataSoalToDisplay = dataSoalList.FirstOrDefault(dataSoal => dataSoal.QuestionID == qid); //cocokan data id soal yang di panggil dengan id soal yang ada di data soal.
        GenerateQuestLocalTemp(dataSoalToDisplay); // panggil data soal yang sesuai dengan id soal.
    }
    void GenerateQuestLocalTemp(QuestionsData data)
    {
        string imgPath = Path.Combine(Application.persistentDataPath, /*EDProcessing.HashSHA384(*/"QuestionsData"/*)*/, /*EDProcessing.HashSHA384(*/quizkey/*)*/);
        //string idQ = $"{data.IdQ}";
        var questText = QuestContainer[0].transform.GetChild(0).gameObject.GetComponent<TMP_Text>();
        questText.text = $"{data.QuestionDescription}";
        questType = $"{data.QuestionType}";
        //bersihkan penambung jawaban benar;
        correctAnswer.Clear();
        
        //jika tipe soal terdapat pada targetTipeSoal (jawaban benar) berbentuk URL akan di lakukan dekoding untuk mengambil nama file
        if (questType.Any(data => subTargetTipeSoal.Contains(questType)))
        {
            var decodedURLcorrectAnswer = DecodeURL($"{data.QuestionRightAnswer[0]}");
            correctAnswer.Add(decodedURLcorrectAnswer);
        }
        else
        {
            if (questType.Any(data => targetTipeSoalE.Contains(questType)))
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

        //jika data keyword tidak kosong, lakukan proses pembersihan dan penambahan keywords.
        if (data.QuestionKeyword != null)
        {
            keywords.Clear();

            foreach (var keyword in data.QuestionKeyword)
            {
                keywords.Add(keyword);
            }
        }

        if (!string.IsNullOrEmpty(data.QuestionIMGURL) && questType.Any(data => subTargetTipeSoalImage.Contains(questType)))
        {
            QuestContainer[0].SetActive(true);
            QuestContainer[1].SetActive(true);

            var soal_img_url = $"{data.QuestionIMGURL}";
            var soal_img_name = DecodeURL(soal_img_url);

            string imgQPath = Path.Combine(imgPath, soal_img_name);

            if (File.Exists(imgQPath))
            {
                var QuestImgContainer = QuestContainer[1].transform.GetChild(0);
                var QuestImg = QuestImgContainer.transform.GetChild(0).GetComponent<RawImage>();
                var QuestImgSubContainer = QuestImgContainer.GetComponent<Image>();
                var rectImgSubContainer = QuestImgSubContainer.GetComponent<RectTransform>();
                var rectQuestIMG = QuestImg.GetComponent<RectTransform>();
                // Baca byte dari file
                byte[] fileData = File.ReadAllBytes(imgQPath);

                // Buat objek Texture2D dan muat data gambar
                Texture2D texture = new Texture2D(2, 2);

                texture.LoadImage(fileData);

                int width = texture.width;
                int height = texture.height;

                // Atur gambar pada RawImage
                QuestImg.texture = texture;

                float aspectRatio = (float)QuestImg.texture.width / (float)QuestImg.texture.height;
                float newWidth = Mathf.Min(600f, QuestImg.texture.width);
                float newHeight = Mathf.Min(280f, QuestImg.texture.height);

                if (newWidth / newHeight > aspectRatio)
                {
                    newWidth = newHeight * aspectRatio;
                }
                else
                {
                    newHeight = newWidth / aspectRatio;
                }

                rectQuestIMG.sizeDelta = new Vector2(newWidth, newHeight);
                rectImgSubContainer.sizeDelta = new Vector2(newWidth+50, QuestImgSubContainer.sprite.texture.height);
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

            for (int i = 0; i < data.QuestionOptions.Length; i++)
            {
                var options = $"{data.QuestionOptions[i]}";
                if(options != data.QuestionRightAnswer[0])
                {
                    answers.Add(options); // tambahkan ke list answer
                }
            }

            int randIndex = UnityEngine.Random.Range(0, answers.Count);
            answers.RemoveAt(randIndex);
            answers.Add(data.QuestionRightAnswer[0]);

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

            for (int i = 0; i < data.QuestionOptions.Length; i++)
            {
                var options = $"{data.QuestionOptions[i]}";
                if(options != data.QuestionRightAnswer[0])
                {
                    answers.Add(options); // tambahkan ke list answer
                }
            }

            int randIndex = UnityEngine.Random.Range(0, answers.Count);
            answers.RemoveAt(randIndex);
            answers.Add(data.QuestionRightAnswer[0]);

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

            for (int i = 0; i < data.QuestionOptions.Length; i++)
            {
                var options = $"{data.QuestionOptions[i]}";
                var decodedUrl = DecodeURL(options);
                if (options != data.QuestionRightAnswer[0])
                {
                    answers.Add(decodedUrl); // tambahkan ke list answer
                }

            }

            int randIndex = UnityEngine.Random.Range(0, answers.Count);
            answers.RemoveAt(randIndex);
            answers.Add(DecodeURL($"{data.QuestionRightAnswer[0]}"));

            SattoloShuffle(answers); //randomifikasi menggunakan sattolo

            //set img dari file yang telah di download.
            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                string imgAPath = Path.Combine(imgPath, answers[i]);

                if (File.Exists(imgAPath))
                {
                    var answerMultiImg = btnMultiAnswers[i].transform.GetChild(1).transform.GetChild(0).GetComponent<RawImage>();
                    var rectAnswerImg = answerMultiImg.GetComponent<RectTransform>();
                    // Baca byte dari file
                    byte[] fileData = File.ReadAllBytes(imgAPath);

                    // Buat objek Texture2D dan muat data gambar
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(fileData);

                    int width = texture.width;
                    int height = texture.height;

                    // Atur gambar pada RawImage
                    answerMultiImg.texture = texture;

                    float aspectRatio = (float)answerMultiImg.texture.width / (float)answerMultiImg.texture.height;
                    float newWidth = Mathf.Min(300f, answerMultiImg.texture.width);
                    float newHeight = Mathf.Min(100f, answerMultiImg.texture.height);

                    if (newWidth / newHeight > aspectRatio)
                    {
                        newWidth = newHeight * aspectRatio;
                    }
                    else
                    {
                        newHeight = newWidth / aspectRatio;
                    }

                    rectAnswerImg.sizeDelta = new Vector2(newWidth, newHeight);
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

            for (int i = 0; i < data.QuestionOptions.Length; i++)
            {
                var options = $"{data.QuestionOptions[i]}";
                var decodedUrl = DecodeURL(options);
                if (options != data.QuestionRightAnswer[0])
                {
                    answers.Add(decodedUrl); // tambahkan ke list answer
                }
            }

            int randIndex = UnityEngine.Random.Range(0, answers.Count);
            answers.RemoveAt(randIndex);
            answers.Add(DecodeURL($"{data.QuestionRightAnswer[0]}"));

            SattoloShuffle(answers);

            for (int i = 0; i < btnMultiAnswers.Length; i++)
            {
                string imgAPath = Path.Combine(imgPath, answers[i]);

                if (File.Exists(imgAPath))
                {
                    var answerMultiImg = btnMultiAnswers[i].transform.GetChild(1).transform.GetChild(0).GetComponent<RawImage>();
                    var rectAnswerImg = answerMultiImg.GetComponent<RectTransform>();
                    // Baca byte dari file
                    byte[] fileData = File.ReadAllBytes(imgAPath);

                    // Buat objek Texture2D dan muat data gambar
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(fileData);

                    int width = texture.width;
                    int height = texture.height;

                    // Atur gambar pada RawImage
                    answerMultiImg.texture = texture;

                    float aspectRatio = (float)answerMultiImg.texture.width / (float)answerMultiImg.texture.height;
                    float newWidth = Mathf.Min(300f, answerMultiImg.texture.width);
                    float newHeight = Mathf.Min(100f, answerMultiImg.texture.height);

                    if (newWidth / newHeight > aspectRatio)
                    {
                        newWidth = newHeight * aspectRatio;
                    }
                    else
                    {
                        newHeight = newWidth / aspectRatio;
                    }

                    rectAnswerImg.sizeDelta = new Vector2(newWidth, newHeight);
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

        //Debug.Log($"{start},{end}");

        var currentPageIds = ids.GetRange(start, end - start); // indeks satu soal ke halaman
        currentIds = currentPageIds; //simpan currentPageIds ke variable list global 'currentIds'
        descAnswerText.text = null;
        //GenerateQuest(currentPageIds);// GenerateQuest untuk menampilkan data soal berdasarkan currentPageIds (ver. Online)

        //GenerateQuest untuk menampilkan data soal berdasarkan currentPageIds (ver. Offline)
        foreach (var ids in currentPageIds)
        {
            StartCoroutine(DelayGenerateQuestion(ids, skillstat));
        }

        if (skillstat)
        {
            var ScoreUI = ScoreObj.transform.GetChild(0).transform.GetComponent<TMP_Text>();
            ScoreUI.text = totalScore.ToString();
        }
        
        //simpan data indeks soal
        //string fileName = $"{uID}_{quizkey}_{skillstat}_ID";
        string filePath = Path.Combine(Application.persistentDataPath, EDProcessing.HashSHA384("sessionQuizData"), EDProcessing.HashSHA384(fileName[2]));

        File.WriteAllText(filePath, EDProcessing.Encrypt(curpageIndex.ToString(),key, iv));
    }

    private IEnumerator DelayGenerateQuestion(string ids, bool delayActive)
    {
        if (delayActive)
        {
            yield return new WaitForSeconds(animationTime);
        }

        checkQuestionStatus();
        GenerateQuestLocalTemp(ids);
        int index = curpageIndex + 1;

        btnIndexQuest.transform.GetChild(1).transform.GetComponent<TMP_Text>().text = index.ToString();

        //control button navigation
        if (index == 1)
        {
            btnNavigator[0].interactable = true;
            btnNavigator[1].interactable = false;
        }
        else if (index == questSize)
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

    public void NextPage() //function next page
    {
        
        if ((curpageIndex + 1) * pageSize < questSize) //example curpage is 0 + 1 = 1 and then 1 * page size have 1 and then < Size of Quest.
        {
            curpageIndex++;
            DisplayCurPage();
        }
        else
        {
            StartCoroutine(DelayedStatus(skillstat));

            
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
            StartCoroutine(DelayedStatus(skillstat));

        }
    }

    private IEnumerator DelayedStatus(bool status)
    {
        if (status)
        {
            yield return new WaitForSeconds(animationTime);
        }

        checkQuestionStatus();
    }

    //baris untuk ability bantuan....
    public void FalseRemover(int answertoRemove)
    {
        /**
         * Skill : False Remover (jumlah tombol dengan string salah yang akan di nonaktifkan), 
         * misalkan max pilihan ganda adalah 5 maka 4 tombol yang memiliki string yang salah dapat dimatikan.
         */
        if (skillstat)
        {
            foreach(var id in currentIds)
            {
                var findQuestionData = dataSoalList.Find(data => data.QuestionID == id);

                if (findQuestionData != null)
                {
                    if (!targetTipeSoalE.Contains(findQuestionData.QuestionType))
                    {
                        // Mengonfirmasi bahwa answertoRemove berada dalam rentang yang valid
                        if (answertoRemove >= 0 && answertoRemove <= btnMultiAnswers.Length - 1)
                        {
                            int countToDisable = 0;

                            List<int> btnHold = new List<int>();
                            for (int i = 0; i < btnMultiAnswers.Length; i++)
                            {
                                btnHold.Add(i);
                            }

                            SattoloShuffle(btnHold);

                            //loop melalui daftar index yang di randomisasi.
                            foreach (int indexBtn in btnHold)
                            {
                                string buttonText = btnMultiAnswers[indexBtn].transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text;
                                // Jika nilai teks tombol tidak sama dengan correctAnswer
                                if (buttonText != correctAnswer[0])
                                {
                                    // Nonaktifkan tombol dan hitung jumlah yang telah dinonaktifkan
                                    btnMultiAnswers[indexBtn].interactable = false;
                                    btnMultiAnswers[indexBtn].GetComponent<Image>().color = new Color32(90, 195, 228, 255);
                                    //btnMultiAnswers[indexBtn].transform.GetChild(0).gameObject.transform.GetComponent<TMP_Text>().color = Color.white;
                                    countToDisable++;

                                    // Keluar dari loop jika sudah menonaktifkan sejumlah yang diinginkan
                                    if (countToDisable == answertoRemove)
                                    {
                                        break;
                                    }
                                }
                            }
                            btnHold.Clear();
                        }
                        else
                        {
                            Debug.LogError("Invalid answertoRemove value. Ensure it is within the range of 0 to btnMultiAnswers.Length - 1.");
                        }
                    }
                    else
                    {
                        //implement Skill Hint in Future....
                    }
                }
            }
        }
    }

    public void StreakShield()
    {
        if (skillstat)
        {
            streakShield = true;
        }
    }

    public void QuestionsRewind()
    {
        if (skillstat)
        {
            //reset data jawaban.
            foreach(var answers in jawabanSoalList)
            {
                answers.AnswerDescription = string.Empty;
                answers.AnswerStatus = false;
                answers.AnswerEssayPoint = 0;
                answers.AnswerScorePoint = 0;
                answers.QuestionHasAnswer = false; //set status jawaban ke false
                answers.QuestionTimeTake = 0;
            }

            curpageIndex = 0;
            DisplayCurPage();
        }
    }

    //public void FalseAnswerTimeDecrease(float Time)
    //{
    //    foreach (var id in currentIds)
    //    {
    //        var matchAnswer = jawabanSoalList.Find(j => j.QuestionID == id);

    //        if (matchAnswer != null)
    //        {
    //            if (!matchAnswer.AnswerStatus)
    //            {
    //                timeController.TimeAdd(false, Time);
    //            }
    //        }
    //    }
    //}

    //public void pointTrueQuizMultiplier(bool active, bool isPercent = false, double addVal = 0)
    //{
    //    if (skillstat)
    //    {
    //        if (active)
    //        {
    //            if (isPercent)
    //            {
    //                var percentofPointQuest = pointQuest * addVal;
    //                pointQuestTrue += pointQuest + percentofPointQuest;
    //            }
    //            else
    //            {
    //                pointQuestTrue += pointQuest * addVal;
    //            }
    //        }
    //        else
    //        {
    //            pointQuestTrue = pointQuest;
    //        }
    //    }

    //}
    //public void PointFalseAddScore(bool active,bool isMinus = false, double percent = 0)
    //{
    //    if (skillstat)
    //    {
    //        if (active)
    //        {
    //            if (isMinus)
    //            {
    //                var minus = pointQuest * percent;
    //                pointQuestFalse += -minus;
    //            }
    //            else
    //            {
    //                pointQuestFalse += pointQuest * percent;
    //            }

    //        }
    //        else
    //        {
    //            pointQuestFalse = 0;
    //        }
    //    }

    //}

    public void AddPointQuest(bool forAnswerStatus, double point)
    {
        if (skillstat)
        {
            if (forAnswerStatus)
            {
                pointQuestTrue += point;
            }
            else
            {
                pointQuestFalse += point;
            }
        }
    }

    public void ResetScore(bool forAnswerStatus)
    {
        if (forAnswerStatus)
        {
            pointQuestTrue = skillstat ? pointQuest : 0;
        }
        else
        {
            pointQuestFalse = 0;
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
                /**jika data tidak kosong, akan dilakukan pengecekan data dengan membandingkan jawaban yang sudah ada dengan jawaban baru 
                 * kondisi perubahan akan terjadi jika jawaban lama != jawaban baru. **/

                if (matchAnswer != null)
                {
                    matchAnswer.AnswerDescription = "AnswerPass";
                    matchAnswer.AnswerStatus = true;
                    matchAnswer.QuestionHasAnswer = true;
                    matchAnswer.AnswerScorePoint = pointQuestTrue;

                    if (targetTipeSoalE.Contains(matchQuestions.QuestionType))
                    {
                        matchAnswer.QuestionType = 2;
                        matchAnswer.AnswerEssayPoint = 1;
                    }
                    else
                    {
                        matchAnswer.QuestionType = 0;
                        matchAnswer.AnswerEssayPoint = 1;
                    }

                    foreach(var AnswerBtn in btnMultiAnswers)
                    {
                        AnswerBtn.gameObject.transform.GetComponent<Image>().color = new Color32(0, 255, 0, 170);
                    }

                    //Debug.Log($"Jawaban diperbarui untuk ID: {idQuest}");
                }
                //else
                //{
                //    Debug.Log("Tidak Ada Jawaban");
                //}
            }

            NextPage();
        }
    }
    //sub blok handle animation
    private IEnumerator AnimatedScorePopupTime(string point)
    {
        var ScorePopup = ScoreObj.transform.GetChild(1).gameObject;
        var TMPScorePopup = ScoreObj.transform.GetChild(1).gameObject.GetComponent<TMP_Text>();
        TMPScorePopup.text = point;
        
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

    //Start of Return Value Used for another class.
    public int getMaxQuestionData()
    {
        return ids.Count;
    }

    public int TypeQuestTracker()
    {
        foreach (var id in currentIds)
        {
            var findQuestionData = dataSoalList.FirstOrDefault(data => data.QuestionID == id);

            if (findQuestionData != null && targetTipeSoalE.Contains(findQuestionData.QuestionType))
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }
        return 0;
    }

    public int AnswerTracking()
    {
        return consecutiveCorrectAnswers;
    }
    public int HasAnswerTracking()
    {
        return hasAnswer;
    }

    public string IdQuestTracking()
    {
        foreach(var id in currentIds)
        {
            return id;
        }
        return null;
    }
    public int getBasePoint()
    {
        if (skillstat)
        {
            return pointQuest;
        }
        return 0;
    }

    //End Of Return Value

    // Sattolo Shuffle
    void SattoloShuffle<T>(List<T> list)
    {
        //int i = list.Count; // Mengambil jumlah elemen dalam daftar

        for(var i = list.Count; i-- > 1;)
        {
            int j = Rand.Next(i);
            var tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }

        //while (i > 1) // Loop hingga hanya ada satu elemen yang tersisa
        //{
        //    i--; // Mengurangi nilai i setiap kali loop dieksekusi untuk menggerakkan pointer ke elemen sebelumnya
        //    int j = Rand.Next(0, i/* + 1*/); // Mengambil indeks acak dari 0 hingga i-1 (tidak termasuk i), membatasi rentang pengambilan indeks agar tidak mencakup elemen terakhir
        //    T tmp = list[j]; // Menyimpan nilai sementara dari elemen yang dipilih secara acak
        //    list[j] = list[i]; // Menukar nilai elemen yang dipilih dengan elemen ke-i (elemen yang dipilih secara acak tidak akan pernah menjadi elemen terakhir)
        //    list[i] = tmp; // Memindahkan nilai yang disimpan ke posisi yang dipilih secara acak, menyelesaikan pertukaran
        //}
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

    //jaro winkler with keyword weighting
    double CalculateJaroWinklerScoreWithKeywordWeighting(string[] words1, string[] words2, string[] keywords)
    {
        double jaroScore = CalculateJaroScore(words1, words2);
        double prefixLength = GetCommonPrefixLength(words1, words2);

        // Nilai kons tanpa pembobotan
        const double scalingFactor = 0.1;
        double prefixScaling = Math.Min(scalingFactor * prefixLength, 0.25);

        // Hitung pembobotan kata kunci
        double keywordWeighting = CalculateKeywordWeighting(words1, words2, keywords);

        // Hitung skor Jaro-Winkler dengan pembobotan kata kunci
        var calculate = (jaroScore + prefixScaling * (1 - jaroScore)) * keywordWeighting;
        calculate = Math.Max(0, Math.Min(1, calculate));
        return calculate;
    }

    double CalculateKeywordWeighting(string[] words1, string[] words2, string[] keywords)
    {
        int keywordMatches = 0;

        foreach (string keyword in keywords)
        {
            if (Array.Exists(words1, word => word.Equals(keyword, StringComparison.OrdinalIgnoreCase)) &&
                Array.Exists(words2, word => word.Equals(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                keywordMatches++;
            }
        }

        // Hitung pembobotan kata kunci
        double keywordWeighting = 1 + (double)keywordMatches / keywords.Length;

        // Batasi pembobotan maksimum menjadi 2
        return Math.Min(keywordWeighting, 2.0);
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
                if (!used2[j] && words1[i].Equals(words2[j], StringComparison.OrdinalIgnoreCase))
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
            if (index2 < words2.Length && words1[i].Equals(words2[index2], StringComparison.OrdinalIgnoreCase))
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

    //
    int GetCommonPrefixLength(string[] words1, string[] words2)
    {
        int minLength = Math.Min(words1.Length, words2.Length);
        int commonPrefix = 0;

        for (int i = 0; i < minLength; i++)
        {
            if (words1[i].Equals(words2[i], StringComparison.OrdinalIgnoreCase))
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

using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginRDB : MonoBehaviour
{
    DependencyStatus dependencyStatus;
    FirebaseAuth auth;
    FirebaseUser user;
    DatabaseReference dbRef;

    [Header("UI Component Login")]
    [Tooltip("0 = signIn, 1 = signOut")]
    public Button[] loginComponentBtn;
    [Tooltip("Input Email, Input password")]
    public TMP_InputField[] inputComponent;

    [Header("Gameobject Needed")]
    [SerializeField] AlertController AlertController;
    [Tooltip("Login Obj, Menu Panel Obj")]
    [SerializeField] GameObject[] ComponentUINeeded;

    private TMP_Text NamaUI, IDUI, KelasUI;

    //AES Key & IV 16 byte
    private static readonly byte[] key = Encoding.UTF8.GetBytes("AzraRakobarReinz"); // Ganti dengan kunci rahasia Anda
    private static readonly byte[] iv = Encoding.UTF8.GetBytes("0721200007212024"); // Ganti dengan initial vector Anda

    //private int MaxLoginAttemptFail = 5;
    //private int loginAttempt = 0;
    float time = 0.20f;
    //private string[] filePath = new string[2];
    // Start is called before the first frame update
    void Start()
    {
        ComponentUINeeded[1].transform.localScale = Vector3.zero;

        NamaUI = ComponentUINeeded[2].transform.GetChild(1).gameObject.GetComponent<TMP_Text>();
        IDUI = ComponentUINeeded[2].transform.GetChild(2).gameObject.GetComponent<TMP_Text>();
        KelasUI = ComponentUINeeded[2].transform.GetChild(3).gameObject.GetComponent<TMP_Text>();

        //filePath[0] = Path.Combine(Application.persistentDataPath, EDProcessing.HashSHA384("userData"), EDProcessing.HashSHA384("userLogData"));
        //filePath[1] = Path.Combine(Application.persistentDataPath, EDProcessing.HashSHA384("userData"), EDProcessing.HashSHA384("guestLogData"));

        //var folderPath = Path.Combine(Application.persistentDataPath, EDProcessing.HashSHA384("userData"));
        //if (!Directory.Exists(folderPath))
        //{
        //    Directory.CreateDirectory(folderPath);
        //}

        //initial start Addlistener Login Component

        loginComponentBtn[0].onClick.AddListener(login); //login
        loginComponentBtn[1].onClick.AddListener(logoutUI); //logout
        //loginComponentBtn[3].onClick.AddListener(guest); //Guest

        //FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        //{
        //    dependencyStatus = task.Result;

        //    if (dependencyStatus == DependencyStatus.Available)
        //    {
                
        //    }
        //    else
        //    {
        //        Debug.LogError("Could not resolve all firebase dependencies: " + dependencyStatus);
        //    }
        //});

        FirebaseApp app = FirebaseApp.DefaultInstance;
        FirebaseDatabase.DefaultInstance.PurgeOutstandingWrites();
        FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);
        auth = FirebaseAuth.DefaultInstance;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        CheckLoginStatus();
    }

    private void Update()
    {

        foreach (var uiComponent in ComponentUINeeded)
        {
            if (uiComponent.transform.localScale == Vector3.zero)
            {
                uiComponent.SetActive(false);
            }
            else
            {
                uiComponent.SetActive(true);
            }
        }

    }

    // Track state changes of the auth object.
    private void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;

            if (!signedIn && user != null)
            {
                //Debug.Log("Signed out " + user.UserId);
                AlertController.AlertSet("Berhasil Logout.");
            }

            user = auth.CurrentUser;

            if (signedIn)
            {
                //Debug.Log("Signed in " + user.UserId);
                //AlertController.AlertSet("Berhasil login.");
                StartCoroutine(getDataUser(user.UserId));
            }
        }
    }
    public void CheckLoginStatus()
    {
        //    if (File.Exists(filePath[0]))
        //    {
        //        ComponentUINeeded[0].transform.localScale = Vector3.zero;
        //        var readDataUser = File.ReadAllText(filePath[0]);
        //        var DecryptDataLog = EDProcessing.Decrypt(readDataUser, key, iv);
        //        var JsonDataLog = JsonUtility.FromJson<UserData>(DecryptDataLog);
        //        checkLogin(JsonDataLog.email, JsonDataLog.pass);
        //    }
        auth.StateChanged += AuthStateChanged;

        if(user != auth.CurrentUser && auth.CurrentUser != null)
        {
            ComponentUINeeded[0].transform.localScale = Vector3.zero;
        }
    }

    public void login()
    {
        var email = inputComponent[0].text;
        var pass = inputComponent[1].text;


        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(pass))
        {
            checkLogin(email, pass);
        }
        else
        {
            //implement ui alert..
            AlertInfo("insertnull", 1);
            cleartext();
        }
    }

    void checkLogin(string email, string pass)
    {
        //if (auth != null && user != null)
        //{
        //    Debug.Log(auth.CurrentUser.Email);
        //    AlertController.AlertSet($"{auth.CurrentUser.Email} Sedang Aktif.", true, TextAlignmentOptions.Center, false, backToLogin);

        //}
        //else
        //{
            StartCoroutine(login(email, pass));
        //}
    }

    //void backToLogin()
    //{
    //    StartCoroutine(LoginToMenu(false));
    //}

    //private void guest()
    //{
    //    string sId = inputComponent[2].text;
    //    string sClass = inputComponent[3].text;
    //    string sName = inputComponent[4].text;

    //    StartCoroutine(guestLogin(sId,sName,sClass));
    //}

    //IEnumerator guestLogin(string sid, string sname, string sclass)
    //{
    //    var query = dbRef.Child("data_murid").Child(sid).GetValueAsync();
    //    yield return new WaitUntil(() => query.IsCompleted);

    //    //if (query.Exception != null)
    //    //{
    //    //    // Handling error
    //    //    //Debug.LogError(query.Exception);
    //    //    cleartext();
    //    //    yield break;
    //    //}

    //    DataSnapshot snapshot = query.Result;

    //    if(snapshot.Child("murid_id").Value == null || snapshot.Exists == false)
    //    {
    //        NamaUI.text = sname;
    //        IDUI.text = sid;
    //        KelasUI.text = sclass;

    //        //masuk ke menu utama;
    //        StartCoroutine(LoginToMenu(true));
    //    }
    //    else
    //    {
    //        AlertInfo("login", 4);
    //        cleartext();
    //    }

    //}

    IEnumerator login(string email, string password)
    {
        //auth = FirebaseAuth.DefaultInstance;
        var loginData = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginData.IsCompleted);

        if (loginData.Exception != null)
        {
            //Debug.LogError("Login Failed : " + loginData.Exception.InnerException.Message);

            //FirebaseException firebaseException = loginData.Exception.GetBaseException() as FirebaseException;
            //AuthError authError = (AuthError)firebaseException.ErrorCode;

            //switch (authError)
            //{
            //    case AuthError.UserDisabled:
            //        AlertInfo("login", 3);
            //        break;
            //    case AuthError.WrongPassword:
            //        AlertInfo("login", 0);
            //        break;
            //        AlertInfo("login", 0);
            //        break;
            //    case AuthError.MissingEmail:
            //        failedMessage += "Email is missing";
            //        break;
            //    case AuthError.MissingPassword:
            //        failedMessage += "Password is missing";
            //        break;
            //    default:
            //        failedMessage = "Login Failed";
            //        break;

            //}

            if (loginData.Exception.InnerException.Message.Contains("The user account has been disabled by an administrator"))
            {
                AlertInfo("login", 3);
            }
            else if (loginData.Exception.InnerException.Message.Contains("An internal error has occurred."))
            {
                ComponentUINeeded[0].transform.LeanScale(Vector3.one, time);
            }
            else
            {
                AlertInfo("login", 0);
            }


            cleartext();
            //loginAttempt++;
            yield break;
        }

        AuthResult authResult = loginData.Result;
        user = authResult.User;
        StartCoroutine(getDataUser(authResult.User.UserId));
    }

    IEnumerator getDataUser(string uid)
    {
        var query = dbRef.Child("data_akun").Child(uid).GetValueAsync();
        yield return new WaitUntil(() => query.IsCompleted);

        if (query.Exception != null)
        {
            // Handling error
            //Debug.LogError(query.Exception);
            AlertInfo("login", 2);
            HandleLogout();
            cleartext();
            yield break;
        }

        DataSnapshot snapshot = query.Result;

        string id = snapshot.Child("user_id").Value.ToString();
        string email = snapshot.Child("user_email").Value.ToString();
        string role = snapshot.Child("user_gid").Value.ToString();
        string status = snapshot.Child("user_status").Value.ToString();

        if (role == "Murid")
        {
            if (status == "Aktif")
            {
                //mengambil data pada tabel murid dalam database yang sesuai dengan id yang di tuju.

                var dataStudent = dbRef.Child("data_murid").Child(id).GetValueAsync();
                yield return new WaitUntil(() => dataStudent.IsCompleted);

                if (dataStudent.Exception != null)
                {
                    // Handling error
                    //Debug.LogError(dataMurid.Exception);
                    AlertInfo("login", 1);
                    cleartext();
                    yield break;
                }

                DataSnapshot snapshotMurid = dataStudent.Result;

                if (snapshotMurid.Child("murid_name").Value == null && snapshotMurid.Child("murid_class").Value == null)
                {
                    AlertInfo("login", 2);
                    cleartext();
                    HandleLogout();
                    yield return 0;
                }
                else
                {

                    //set data dari firebase ke UI
                    NamaUI.text = snapshotMurid.Child("murid_name").Value.ToString();
                    IDUI.text = snapshotMurid.Child("murid_id").Value.ToString();
                    KelasUI.text = snapshotMurid.Child("murid_class").Value.ToString();

                    //masuk ke menu utama dan set semua string data user ke UI.
                    StartCoroutine(LoginToMenu(true));


                    //if (!File.Exists(filePath[0]))
                    //{
                    //    var pass = inputComponent[1].text;
                    //    UserData dataLog = new(email, pass);
                    //    var encryptedJsonDataLog = EDProcessing.Encrypt(JsonUtility.ToJson(dataLog), key, iv);
                    //    File.WriteAllText(filePath[0], encryptedJsonDataLog);
                    //}
                }

            }
            else
            {
                AlertInfo("login", 1);
                cleartext();
                HandleLogout();
            }

        }
        else
        {
            AlertInfo("login", 0);
            cleartext();
            HandleLogout();
        }
    }

    void logoutUI()
    {
        AlertController.AlertSet("Apakah Kamu Yakin ?", false, TextAlignmentOptions.Center, true, logout);
    }

    void logout()
    {
        HandleLogout();
        cleartext();
        //File.Delete(filePath[0]);

        NamaUI.text = string.Empty;
        IDUI.text = string.Empty;
        KelasUI.text = string.Empty;

        StartCoroutine(LoginToMenu(false));
    }

    private void HandleLogout()
    {
        if (auth != null && user != null)
        {
            try
            {
                auth.SignOut();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
            
        }
    }

    IEnumerator LoginToMenu(bool status)
    {
        
        if (!status) //false = menu to login
        {
            ComponentUINeeded[1].transform.LeanScale(Vector3.zero, time);
            yield return new WaitUntil(() => ComponentUINeeded[1].transform.localScale == Vector3.zero);
            if (ComponentUINeeded[1].transform.localScale == Vector3.zero)
            {
                ComponentUINeeded[0].transform.LeanScale(Vector3.one, time);
            }
        }
        else
        {
            // true = login to menu
            ComponentUINeeded[0].transform.LeanScale(Vector3.zero, time).setEaseInBack();
            yield return new WaitUntil(() => ComponentUINeeded[0].transform.localScale == Vector3.zero);
            if (ComponentUINeeded[0].transform.localScale == Vector3.zero)
            {
                ComponentUINeeded[1].transform.LeanScale(Vector3.one, time);
            }

        }

    }

    void cleartext()
    {
        for (int i = 0; i < inputComponent.Length; i++)
        {
            inputComponent[i].text = null;
        }
    }

    void AlertInfo(string alertType, int infoNum)
    {
        // Membuat dictionary untuk memetakan jenis alert dan nomor informasi ke pesan yang sesuai
        Dictionary<string, Dictionary<int, string>> alertMessages = new Dictionary<string, Dictionary<int, string>>
        {
            {
                "login", new Dictionary<int, string>
                {
                {0, "Email atau Password Salah."},
                {1, "Akun Belum Aktif, Silahkan Hubungi Guru atau Pengelola."},
                {2, "Data Diri Tidak Ditemukan Silahkan Hubungi Guru atau Pengelola."},
                {3, "Akun Dinonaktifkan guna Keamanan, silahkan kontak guru atau Pengelola untuk mengaktifkan."},
                {4, "Kamu Tidak Diizinkan Masuk Sebagai Tamu, Silahkan Hubungi Guru atau Pengelola."},
                {5, "Logout Berhasil."}
            }
        },
        {
            "insertnull", new Dictionary<int, string>
            {
                {0, "Data Tidak Boleh Kosong."},
                {1, "Email Atau Password Tidak Boleh Kosong."}
            }
        }
    };

        // Memeriksa apakah jenis alert ada dalam dictionary
        if (alertMessages.ContainsKey(alertType))
        {
            // Memeriksa apakah nomor informasi ada dalam dictionary yang sesuai
            if (alertMessages[alertType].ContainsKey(infoNum))
            {
                // Memunculkan pesan alert
                AlertController.AlertSet(alertMessages[alertType][infoNum], true, TextAlignmentOptions.Center);
            }
            else
            {
                // Menampilkan pesan alert jika nomor informasi tidak valid
                AlertController.AlertSet("Invalid AlertNum: Missing / not Implemented", true, TextAlignmentOptions.Center);
            }
        }
        else
        {
            // Menampilkan pesan alert jika jenis alert tidak valid
            AlertController.AlertSet("Invalid alertType: Missing / not Implemented", true, TextAlignmentOptions.Center);
        }
    }
}

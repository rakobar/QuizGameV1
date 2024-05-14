using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LoginRDB : MonoBehaviour
{
    //DependencyStatus dependencyStatus;
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
    [SerializeField] LoadingController loadingController;
    [Tooltip("Login Obj, Menu Panel Obj")]
    [SerializeField] GameObject[] ComponentUINeeded;
    [SerializeField] GameObject[] ComponentFormInput;

    private TMP_Text NamaUI, IDUI, KelasUI;
    bool loginForm;

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

        ComponentUINeeded[0].transform.GetChild(2).transform.gameObject.SetActive(true);
        ComponentUINeeded[0].transform.GetChild(3).transform.gameObject.SetActive(false);

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
        loginComponentBtn[2].onClick.AddListener(switchForm); //logout

        this.loginForm = PlayerPrefs.GetInt("login_LoginForm", this.loginForm ? 1 : 0) == 1;
        
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


        displayForm();
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
                //AlertController.AlertSet("Berhasil Logout.");
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
            loadingController.LoadingDisplay(true);
            ComponentUINeeded[0].transform.localScale = Vector3.zero;
        }
    }

    void switchForm()
    {
        this.loginForm = !this.loginForm;

        PlayerPrefs.SetInt("login_LoginForm", this.loginForm ? 1 : 0);

        displayForm();
    }

    void displayForm()
    {
        inputComponent[2].text = !this.loginForm ? PlayerPrefs.GetString("guestID") : null;
        inputComponent[3].text = !this.loginForm ? PlayerPrefs.GetString("guestName") : null;
        loginComponentBtn[0].onClick.RemoveAllListeners();
        loginComponentBtn[0].onClick.AddListener(this.loginForm ? login : guest);
        loginComponentBtn[2].transform.GetChild(0).transform.gameObject.GetComponent<TMP_Text>().text = this.loginForm ? "Pengguna" : "Tamu";
        ComponentUINeeded[0].transform.GetChild(2).transform.gameObject.SetActive(this.loginForm);
        ComponentUINeeded[0].transform.GetChild(3).transform.gameObject.SetActive(!this.loginForm);
    }
    public void login()
    {
        var email = inputComponent[0].text;
        var pass = inputComponent[1].text;

        AudioController.Instance.PlayAudioSFX("ButtonClick");
        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(pass))
        {
            loadingController.LoadingDisplay(true);
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

    private void guest()
    {
        IDUI.text = inputComponent[2].text;
        NamaUI.text = inputComponent[3].text;

        if(!string.IsNullOrWhiteSpace(IDUI.text) || !string.IsNullOrWhiteSpace(NamaUI.text))
        {
            AlertController.AlertSet("Pengingat, Pastikan Nama & IDmu Sudah Sesuai !", false, TextAlignmentOptions.Center, true, guestProcess);
            
        }
        else
        {
            AlertInfo("insertnull",0);
            cleartext();
        }
    }

    private void guestProcess()
    {
        loadingController.LoadingDisplay(true);
        StartCoroutine(guestLogin(IDUI.text, NamaUI.text, "Tidak Memiliki Kelas (Tamu)"));
    }

    IEnumerator guestLogin(string sid, string sname, string sclass)
    {
        var query = dbRef.Child("data_akun").GetValueAsync();
        //var saveVal = query.SetValueAsync();
        yield return new WaitUntil(() => query.IsCompleted);

        if (query.Exception != null)
        {
            // Handling error
            //Debug.LogError(query.Exception);
            AlertController.AlertSet($"Error : {query.Exception.InnerExceptions}");
            cleartext();
            yield break;
        }

        DataSnapshot snapshot = query.Result;
        var listUser = new List<string>();

        foreach(var snapData in snapshot.Children)
        {
            listUser.Add(snapData.Child("user_id").Value.ToString());
        }

        if (!listUser.Contains(sid))
        {
            NamaUI.text = sname;
            IDUI.text = sid;
            KelasUI.text = sclass;

            //send guest data to server

            var guestDataQuery = dbRef.Child("data_murid");
            guestDataQuery.Child(sid).Child("murid_name").SetValueAsync(sname);
            guestDataQuery.Child(sid).Child("murid_id").SetValueAsync(sid);
            guestDataQuery.Child(sid).Child("murid_class").SetValueAsync(sclass);

            //simpan data guest
            PlayerPrefs.SetString("guestID", sid);
            PlayerPrefs.SetString("guestName", sname);

            //masuk ke menu utama;
            StartCoroutine(LoginToMenu(true));
            loadingController.LoadingDisplay(false);
        }
        else
        {
            AlertInfo("login", 4);
            cleartext();
            PlayerPrefs.SetString("guestID", string.Empty);
            PlayerPrefs.SetString("guestName", string.Empty);
        }
    }

    IEnumerator login(string email, string password)
    {
        //auth = FirebaseAuth.DefaultInstance;
        var loginData = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginData.IsCompleted);

        if (loginData.Exception != null)
        {
            //Debug.LogError("Login Failed : " + loginData.Exception.InnerException.Message);

            FirebaseException firebaseException = loginData.Exception.GetBaseException() as FirebaseException;
            AuthError authError = (AuthError)firebaseException.ErrorCode;

            switch (authError)
            {
                case AuthError.UserDisabled:
                    AlertInfo("login", 3);
                    break;
                case AuthError.WrongPassword:
                    AlertInfo("login", 0);
                    break;
                case AuthError.NetworkRequestFailed:
                    AlertInfo("login", 0);
                    break;
                default:
                    AlertInfo("login", 0);
                    break;

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
            loadingController.LoadingDisplay(false);
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
                    loadingController.LoadingDisplay(false);
                    AlertInfo("login", 1);
                    cleartext();
                    yield break;
                }

                DataSnapshot snapshotMurid = dataStudent.Result;

                if (snapshotMurid.Child("murid_name").Value == null && snapshotMurid.Child("murid_class").Value == null)
                {
                    loadingController.LoadingDisplay(false);
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

                    //reset data guest User
                    PlayerPrefs.SetString ("guestID", string.Empty);
                    PlayerPrefs.SetString ("guestName", string.Empty);

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
                loadingController.LoadingDisplay(false);
                AlertInfo("login", 1);
                cleartext();
                HandleLogout();
            }

        }
        else
        {
            loadingController.LoadingDisplay(false);
            AlertInfo("login", 0);
            cleartext();
            HandleLogout();
        }
    }

    void logoutUI()
    {
        AudioController.Instance.PlayAudioSFX("ButtonClick");

        if(!KelasUI.text.Contains("Tamu"))
        {
            AlertController.AlertSet("Apakah Kamu Yakin ?", false, TextAlignmentOptions.Center, true, logout);
        }
        else
        {
            logout();
        }
    }

    void logout()
    {
        loadingController.LoadingDisplay(true);
        HandleLogout();
        cleartext();
        //File.Delete(filePath[0]);

        inputComponent[2].text = !this.loginForm ? PlayerPrefs.GetString("guestID") : null;
        inputComponent[3].text = !this.loginForm ? PlayerPrefs.GetString("guestName") : null;

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
            ComponentUINeeded[1].transform.LeanScale(Vector3.zero, time).setEaseInBack();
            yield return new WaitUntil(() => ComponentUINeeded[1].transform.localScale == Vector3.zero);
            ComponentUINeeded[0].transform.LeanScale(Vector3.one, time).setEaseOutBack();
            yield return new WaitUntil(() => ComponentUINeeded[0].transform.localScale == Vector3.one);
            loadingController.LoadingDisplay(false);
        }
        else
        {
            // true = login to menu
            ComponentUINeeded[0].transform.LeanScale(Vector3.zero, time).setEaseInBack();
            yield return new WaitUntil(() => ComponentUINeeded[0].transform.localScale == Vector3.zero);
            ComponentUINeeded[1].transform.LeanScale(Vector3.one, time).setEaseOutBack();
            yield return new WaitUntil(() => ComponentUINeeded[1].transform.localScale == Vector3.one);
            loadingController.LoadingDisplay(false);

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
                {4, "ID Telah Memiliki Akun, Tidak di izinkan masuk sebagai tamu."},
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
        loadingController.LoadingDisplay(false);
    }
}

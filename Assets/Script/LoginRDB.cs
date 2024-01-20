using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine.UI;
using TMPro;
using System;
using Firebase.Extensions;

public class LoginRDB : MonoBehaviour
{
    FirebaseAuth auth;
    DatabaseReference dbRef;

    [Tooltip("0 = login, 1 = Guest, 2 = guest login")]
    public Button[] loginComponentBtn;
    public TMP_InputField[] inputComponent;

    //auto login untuk guest user
    private protected string U = "user@guestuser.com";
    private protected string P = "!QMaster@GuestUser!C00M0N";

    // Start is called before the first frame update
    void Start()
    {
        //auth = FirebaseAuth.DefaultInstance;

        for (int i = 0; i < loginComponentBtn.Length; i++)
        {
            Button componentBtn = loginComponentBtn[i].GetComponent<Button>();
            
            if(i == 0)
            {
                //login btn
                componentBtn.onClick.AddListener(login);
            }
            else if(i == 1)
            {
                //guest btn
                componentBtn.onClick.AddListener(guestBtn);
            }
            else if (i == 2)
            {
                //guest login
                componentBtn.onClick.AddListener(guestBtn);
            }
            else
            {
                Debug.LogError(i + "not Implemented yet.");
            }
        }

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;
            dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        });

        //pengecekan login
        //if(auth.CurrentUser != null)
        //{
        //    Debug.Log("User Has Login!");
        //}
        //else
        //{

        //}
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void login()
    {
        var email = inputComponent[0].text;
        var pass = inputComponent[1].text;

        if(string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            Debug.Log("login data is null");
        }
        else
        {
            auth.SignInWithEmailAndPasswordAsync(email, pass).ContinueWith(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("Login Canceled!");
                    return;
                }

                if (task.IsFaulted)
                {
                    Debug.LogError("Login Failed : " + task.Exception);

                    foreach (Exception exception in task.Exception.InnerExceptions)
                    {
                        FirebaseException firebaseException = exception as FirebaseException;
                        if (firebaseException != null)
                        {
                            // Check specific Firebase error codes
                            if ((AuthError)firebaseException.ErrorCode == AuthError.WrongPassword)
                            {
                                Debug.LogError("Wrong password");
                            }
                            else if ((AuthError)firebaseException.ErrorCode == AuthError.InvalidEmail)
                            {
                                Debug.LogError("Invalid email");
                            }
                            else
                            {
                                // Handle other Firebase error codes as needed
                            }
                        }
                    }

                    return;

                }

                if (task.IsCompleted)
                {
                    AuthResult authResult = task.Result;
                    FirebaseUser user = authResult.User;

                    string uid = user.UserId;

                    StartCoroutine(getDataUser(uid));

                }
            });
        }

        
    }

    IEnumerator getDataUser(string uid)
    {
        var query = dbRef.Child("data_akun").Child(uid).GetValueAsync();
        yield return new WaitUntil(() => query.IsCompleted);

        if (query.Exception != null)
        {
            // Handling error
            Debug.LogError(query.Exception);
            yield break;
        }

        DataSnapshot snapshot = query.Result;

        string id = snapshot.Child("user_id").Value.ToString();
        string email = snapshot.Child("user_email").Value.ToString();
        string role = snapshot.Child("user_gid").Value.ToString();
        string status = snapshot.Child("user_status").Value.ToString();

        if(role == "Murid" && status == "Aktif")
        {
            //masuk ke menu utama dan set semua string data user ke UI.

            var dataMurid = dbRef.Child("data_murid").Child(id).GetValueAsync();
            yield return new WaitUntil(() => dataMurid.IsCompleted);

            if (dataMurid.Exception != null)
            {
                // Handling error
                Debug.LogError(dataMurid.Exception);
                yield break;
            }

            DataSnapshot snapshotMurid = dataMurid.Result;

            string nama = snapshotMurid.Child("murid_name").Value.ToString();
            string kelas = snapshotMurid.Child("murid_class").Value.ToString();

        }
        else
        {
            Debug.Log("Invalid Email");
        }


    }
    void cleartext()
    {
        for(int i = 0; i < inputComponent.Length; i++)
        {
            inputComponent[1].text = null;
        }
    }

    void guestBtn()
    {

    }

}

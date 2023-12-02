using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;

public class LoginRDB : MonoBehaviour
{
    FirebaseAuth auth;
    //auto login untuk guest user
    private protected string U = "user@guestuser.com";
    private protected string P = "!QMaster@GuestUser!C00M0N";

    // Start is called before the first frame update
    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        //pengecekan login
        if(auth.CurrentUser != null)
        {
            Debug.Log("User Has Login!");
        }
        else
        {
            AutoLogin();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private protected void AutoLogin()
    {
        auth.SignInWithEmailAndPasswordAsync(U, P).ContinueWith(task =>
         {
             if (task.IsCanceled)
             {
                 Debug.LogError("Login Canceled!");
                 return;
             }
             
             if(task.IsFaulted)
             {
                 Debug.LogError("Login Failed : " + task.Exception);
                 return;
             }

             if (task.IsCompleted)
             {
                 Debug.Log("Login Success! User: ");
             }

             //var user = task.Result;
             //Debug.Log("Login Success! User: " + user.DisplayName);
         });
    }
}

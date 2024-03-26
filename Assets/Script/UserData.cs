using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserData
{
    public string uid;
    public string suid;
    public string name;
    public string email;
    public string pass;
    public string level;
    public string status;
    public string kelas;

    public UserData(string email, string pass)
    {
        this.email = email;
        this.pass = pass;
    }
    public UserData(string uid, string suid, string name, string email, string pass, string level, string status, string kelas)
    {
        this.uid = uid;
        this.suid = suid;
        this.name = name;
        this.email = email;
        this.pass = pass;
        this.level = level;
        this.status = status;
        this.kelas = kelas;
    }
}

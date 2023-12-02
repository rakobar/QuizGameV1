using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ControlQuiz : MonoBehaviour
{
    [Serializable]
    public class Soal
    {

        [Serializable]
        public class KomponenSoal
        {
            public string soal;
            public string[] listJawaban;
            public int jawabanBenar;
        }
        public KomponenSoal komponenSoal;
    }

    public List<Soal> listSoal;

}

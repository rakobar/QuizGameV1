using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResultStore
{
    public int nilai;
    public int trueAnswer;
    public int falseAnswer;
    public int noAnswer;

    public ResultStore(int nilai, int trueAnswer, int falseAnswer, int noAnswer)
    {
        this.nilai = nilai;
        this.trueAnswer = trueAnswer;
        this.falseAnswer = falseAnswer;
        this.noAnswer = noAnswer;
    }
}

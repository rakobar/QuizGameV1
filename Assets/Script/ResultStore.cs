public class ResultStore
{
    public string quiz_quizkey;
    public double quiz_points;
    public int quiz_trueAnswer;
    public int quiz_falseAnswer;
    public int quiz_noAnswer;
    //public int quiz_timeClear;
    public string quiz_timeClear;
    public string quiz_dateadded;
    public string quiz_dateupdated;
    public string quiz_alphabetPoint;

    public ResultStore(string nilaiAlphabet, double nilai, int trueAnswer, int falseAnswer, int noAnswer)
    {
        this.quiz_points = nilai;
        this.quiz_alphabetPoint = nilaiAlphabet;
        this.quiz_trueAnswer = trueAnswer;
        this.quiz_falseAnswer = falseAnswer;
        this.quiz_noAnswer = noAnswer;
    }
    public ResultStore(string qkey, string nilaiAlphabet, double nilai, int trueAnswer, int falseAnswer, int noAnswer, string timeClear/*int timeClear*/, string dateadded, string dateupdated)
    {
        this.quiz_quizkey = qkey;
        this.quiz_points = nilai;
        this.quiz_alphabetPoint = nilaiAlphabet;
        this.quiz_trueAnswer = trueAnswer;
        this.quiz_falseAnswer = falseAnswer;
        this.quiz_noAnswer = noAnswer;

        this.quiz_timeClear = timeClear;
        this.quiz_dateadded = dateadded;
        this.quiz_dateupdated = dateupdated;
    }
}

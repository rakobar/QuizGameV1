public class ResultStore
{
    public int quiz_points;
    public int quiz_trueAnswer;
    public int quiz_falseAnswer;
    public int quiz_noAnswer;
    public string quiz_dateadded;
    public string quiz_dateupdated;

    public ResultStore(int nilai, int trueAnswer, int falseAnswer, int noAnswer)
    {
        this.quiz_points = nilai;
        this.quiz_trueAnswer = trueAnswer;
        this.quiz_falseAnswer = falseAnswer;
        this.quiz_noAnswer = noAnswer;
    }
    public ResultStore(int nilai, int trueAnswer, int falseAnswer, int noAnswer, string dateadded, string dateupdated)
    {
        this.quiz_points = nilai;
        this.quiz_trueAnswer = trueAnswer;
        this.quiz_falseAnswer = falseAnswer;
        this.quiz_noAnswer = noAnswer;
        this.quiz_dateadded = dateadded;
        this.quiz_dateupdated = dateupdated;
    }
}

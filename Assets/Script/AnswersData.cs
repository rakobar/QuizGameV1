public class AnswersData
{
    public string QuestionID;
    public int QuestionType;
    public bool QuestionHasAnswer;
    public string AnswerDescription;
    public bool AnswerStatus;
    public double AnswerEssayPoint;
    public int QuestionTimeTake;

    public AnswersData() { }

    public AnswersData(string QID, int QType, bool QHasAnswer, string QADescription, bool QAStatus, int QuestionTimeTake)
    {
        this.QuestionID = QID;
        this.QuestionType = QType;
        this.QuestionHasAnswer = QHasAnswer;
        this.AnswerDescription = QADescription;
        this.AnswerStatus = QAStatus;
        this.QuestionTimeTake = QuestionTimeTake;
    }
    public AnswersData(string QID, int QType, bool QHasAnswer, string QADescription, bool QAStatus, int QuestionTimeTake, double QAEPoint)
    {
        this.QuestionID = QID;
        this.QuestionType = QType;
        this.QuestionHasAnswer = QHasAnswer;
        this.AnswerDescription = QADescription;
        this.AnswerStatus = QAStatus;
        this.QuestionTimeTake = QuestionTimeTake;
        this.AnswerEssayPoint = QAEPoint;
    }
}

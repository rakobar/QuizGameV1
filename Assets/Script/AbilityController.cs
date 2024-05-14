using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AbilityController : MonoBehaviour
{
    [SerializeField] TimeController timeController;
    [SerializeField] QuizRDB quizController;
    [SerializeField] AlertController alertController;
    [Header("Ability Property Set")]
    [SerializeField] GameObject AbilityUsedPopup;
    [SerializeField] GameObject SkillBookUI;
    [SerializeField] GameObject AbilitySpawnPopup;
    [SerializeField] Transform[] AbilityContainer;
    [SerializeField] GameObject[] SkillDescObj;
    [Tooltip("Length Btn And Rate Must Same")]
    [SerializeField] SkillData[] skillData;
    [Tooltip("0: Passive Booster, 1: Passive Revive")]
    [SerializeField] GameObject[] PassiveAbility;

    [Header("Ability Property Option")]
    [SerializeField] float LastInterval = 0f;
    [SerializeField] float Interval = 0f;
    [SerializeField] float animatedTime = 0.3f;
    [SerializeField] int limitActivedBtn = 0;
    [SerializeField] int ActiveBtn = 0;
    [SerializeField] string CurrentIDQuest;
    bool AbilityAllowed = false;
    [SerializeField] int correctStreak = 0;
    [SerializeField] int correctStreakBackup = 0;
    [SerializeField] int correctTracking = 0;
    bool correctTrackingStatus = false;

    [SerializeField] bool triggerboostStreak = false;
    [SerializeField] double boostStreak = 0;
    [SerializeField] double initialBoostStreak = 0.01;
    [SerializeField] double limitBoostStreak = 0.05;
    [SerializeField] bool triggerboost = false;

    Vector3 localScaleSkillUsedPopup;
    Color colorSkillSpawnPopup;
    Vector3 imgSkillLocalPos;
    [SerializeField] List<int> indexSkillStored = new List<int>();

    private static readonly byte[] key = Encoding.UTF8.GetBytes("AzraRakobarReinz"); // Ganti dengan kunci rahasia Anda
    private static readonly byte[] iv = Encoding.UTF8.GetBytes("0721200007212024"); // Ganti dengan initial vector Anda

    //bool timeStopped = false;
    //float timerStop = 0;

    string[] filepath = new string[4];

    private void Start()
    {
        SkillBookUI.transform.localScale = Vector3.zero;
        if (skillData.Length == SkillDescObj.Length)
        {
            for (int i = 0; i < skillData.Length; i++)
            {
                SkillDescObj[i].transform.GetChild(0).GetComponent<Image>().sprite = skillData[i].SkillBtn.gameObject.transform.GetComponent<Image>().sprite;
                SkillDescObj[i].transform.GetChild(1).GetComponent<TMP_Text>().text = skillData[i].SkillDesc;
            }
        }
        else
        {
            alertController.AlertSet("Error Ability Loader : Skill Data Tidak Sesuai.");
        }

        //foreach (var data in skillData)
        //{
        //    data.SkillBtn.gameObject.SetActive(false);
        //    data.SkillBtn.transform.localScale = Vector3.zero;
        //    data.SkillBtn.interactable = false;
        //    data.firstSpawn = false;
            
        //    //btn.gameObject.SetActive(false);
        //    //btn.transform.localScale = Vector3.zero;
        //    //btn.interactable = false;
        //}

        for(int i = 0; i < skillData.Length; i++)
        {
            skillData[i].SkillBtn.gameObject.SetActive(false);
            skillData[i].SkillBtn.transform.localScale = Vector3.zero;
            skillData[i].SkillBtn.interactable = false;
            skillData[i].firstSpawn = false; //PlayerPrefs.GetInt($"skillQIndex{i}", skillData[i].firstSpawn ? 1 : 0) == 1;
        }

        foreach (var passive in PassiveAbility)
        {
            passive.SetActive(false);
        }
        AbilityUsedPopup.SetActive(false);

        localScaleSkillUsedPopup = AbilityUsedPopup.transform.localScale;
        localScaleSkillUsedPopup.y = 0;
        AbilityUsedPopup.transform.localScale = localScaleSkillUsedPopup;


        AbilitySpawnPopup.SetActive(false);
        AbilitySpawnPopup.transform.GetChild(1).gameObject.transform.GetChild(0).gameObject.transform.localScale = Vector3.zero;
        AbilitySpawnPopup.transform.GetChild(1).gameObject.transform.GetChild(1).gameObject.transform.localScale = Vector3.zero;

        imgSkillLocalPos = AbilitySpawnPopup.transform.GetChild(1).gameObject.transform.GetChild(0).transform.localPosition;

        colorSkillSpawnPopup = AbilitySpawnPopup.transform.GetComponent<RawImage>().color;
        colorSkillSpawnPopup.a = 0f;
        AbilitySpawnPopup.transform.GetComponent<RawImage>().color = colorSkillSpawnPopup;
    }

    private void Update()
    {
        if (AbilityAllowed)
        {
            //var timeElapse = timeController.getTimeElapse();
            correctTracking = quizController.AnswerTracking();
            if(timeController.getTime() <= 0)
            {
                foreach (var data in skillData)
                {
                    data.SkillBtn.gameObject.SetActive(false);
                    data.SkillBtn.transform.localScale = Vector3.zero;

                    //btn.gameObject.SetActive(false);
                    //btn.transform.localScale = Vector3.zero;
                    //btn.interactable = false;
                }

                Interval = 0;
                LastInterval = 0;
                ActiveBtn = 0;
                correctStreak = 0;
                correctTracking = 0;
                boostStreak = 0;
                correctTrackingStatus = false;
                triggerboostStreak = false;
                AbilityAllowed = false;
            }

            if(correctTracking == correctStreak - 1)
            {
                correctTrackingStatus = true;
                triggerboostStreak = true;
            }

            if (correctTracking == 0)
            {
                triggerboostStreak = false;

                if(boostStreak != 0)
                {
                    PassiveScoreAdd(true, quizController.getBasePoint() * -boostStreak);
                    boostStreak = 0;
                    StartCoroutine(AnimatedOut(PassiveAbility[0]));
                }
                correctStreak = correctStreakBackup;
            }

            if (correctTracking == correctStreak)
            {
                if (correctTrackingStatus)
                {
                    if(ActiveBtn < limitActivedBtn)
                    {
                        CallAbility();
                    }

                    correctStreak += correctStreakBackup;
                    correctTrackingStatus = false;
                }

                if (triggerboostStreak)
                {
                    boostStreak += initialBoostStreak;

                    if (boostStreak <= limitBoostStreak) //max 5% 
                    {
                        File.WriteAllText(filepath[1], EDProcessing.Encrypt(boostStreak.ToString(), key, iv));
                        File.WriteAllText(filepath[2], EDProcessing.Encrypt(correctStreak.ToString(), key, iv));
                        PassiveScoreAdd(true, quizController.getBasePoint() * boostStreak);
                        PassiveAbility[0].transform.GetChild(0).gameObject.transform.GetChild(0).GetComponent<TMP_Text>().text = $"{boostStreak * 100}%";
                        StartCoroutine(AnimatedIn(PassiveAbility[0]));

                    }
                    else
                    {
                        boostStreak = 0.05;
                    }
                    triggerboostStreak = false;
                }
            }

            if(ActiveBtn < limitActivedBtn)
            {
                LastInterval += Time.deltaTime;
                if (LastInterval >= Interval)
                {
                    CallAbility();
                    LastInterval = 0;
                }
            }

            if(CurrentIDQuest != quizController.IdQuestTracking())
            {
                if (triggerboost)
                {
                    quizController.ResetScore(true);
                    quizController.ResetScore(false);

                    if (boostStreak != 0)
                    {
                        PassiveScoreAdd(true, quizController.getBasePoint() * boostStreak);
                    }
                    triggerboost = false;
                }
            }

            foreach (var abilityData in skillData)
            {

                if (abilityData.SkillBtn.gameObject.activeSelf)
                {
                    abilityData.SkillBtn.transform.SetParent(AbilityContainer[0]);
                }
                else
                {
                    abilityData.SkillBtn.transform.SetParent(this.transform);
                }

                abilityData.SkillBtn.interactable = CurrentIDQuest == quizController.IdQuestTracking() ? false : true;
            }

            foreach(var Passive in PassiveAbility)
            {
                if (Passive.activeSelf)
                {
                    Passive.transform.SetParent(AbilityContainer[1]);
                }
                else
                {
                    Passive.transform.SetParent(this.transform);
                }
            }

            //Revive skill check
            if (skillData[0].SkillBtn.gameObject.activeSelf)
            {
                if(skillData[0].SkillBtn.transform.localScale == Vector3.one)
                {
                    StartCoroutine(AnimatedIn(PassiveAbility[1]));
                }
                
                if(timeController.getTime() <= 0.05)
                {
                    //min 90 seconds and max 5 minutes (300 seconds)
                    int RandVal = Random.Range(90, 300);
                    PassiveTimeExtend(RandVal);
                    StartCoroutine(AnimatedOut(skillData[0].SkillBtn));
                    ActiveBtn--;
                    if (skillData[0].SkillBtn.transform.localScale.x >= 0.5f)
                    {
                        StartCoroutine(AnimatedOut(PassiveAbility[1]));
                        StartCoroutine(SkillUsedPopup("Revive Diaktifkan!", new Color32(255, 215, 46, 180)));
                    }
                    
                }
            }
            else
            {
                if (skillData[0].SkillBtn.transform.localScale == Vector3.zero)
                {
                    StartCoroutine(AnimatedOut(PassiveAbility[1]));
                }
            }

            //if (timeStopped)
            //{
            //    timeController.TimeStop(timeStopped);
            //    timerStop -= Time.deltaTime;

            //    if(timerStop <= 1)
            //    {
            //        timeStopped = true;
            //        timeController.TimeStop(timeStopped);
            //        timerStop = 0;
            //    }
            //} 

        }
        else
        {
            Interval = 0;
            LastInterval = 0;
            ActiveBtn = 0;
            correctStreak = 0;
            correctTracking = 0;
            correctTrackingStatus = false;
            boostStreak = 0;
            triggerboostStreak = false;
            //AbilityAllowed = false;

            foreach (var ability in skillData)
            {
                if (ability.SkillBtn.transform.localScale != Vector3.zero || !ability.SkillBtn.transform.gameObject.activeSelf)
                {
                    ability.SkillBtn.transform.localScale = Vector3.zero;
                    ability.SkillBtn.transform.gameObject.SetActive(false);
                    ability.spawnCount = 0;
                    //ability.SkillBtn.interactable = false;
                }

            }
            //foreach (var ability in PassiveAbility)
            //{
            //    ability.transform.SetParent(this.transform);
            //}

            if (timeController.getTimeStoppedStatus())
            {
                timeController.ForceStopTime();
            }
        }
        
    }

    public void AbillitySet(bool Active = false, float intervalActiveOnSec = 0f, int correctStreak = 0, int limitActiveBtn = 0, string uid = null, string qkey = null)
    {
        //initial filePath
        for(int i = 0; i < filepath.Length; i++)
        {
            filepath[i] = Path.Combine(Application.persistentDataPath, EDProcessing.HashSHA384("sessionQuizData"), EDProcessing.HashSHA384($"{uid}_{qkey}_{Active}_SD{i}"));
        }

        foreach (var passive in PassiveAbility)
        {
            passive.SetActive(false);
        }

        AbilityContainer[1].gameObject.SetActive(Active);
        AbilityUsedPopup.SetActive(false);
        indexSkillStored.Clear();
        AbilityAllowed = Active;
        Interval = intervalActiveOnSec;
        this.correctStreak = correctStreak;
        correctStreakBackup = correctStreak;
        ActiveBtn = 0;
        CurrentIDQuest = string.Empty;
        //this.filepath = filepath;
        this.limitActivedBtn = limitActiveBtn;

        localScaleSkillUsedPopup = AbilityUsedPopup.transform.localScale;
        localScaleSkillUsedPopup.y = 0;
        AbilityUsedPopup.transform.localScale = localScaleSkillUsedPopup;

        if (Active)
        {

            foreach (var data in skillData)
            {
                data.SkillBtn.gameObject.SetActive(false);
                data.SkillBtn.transform.localScale = Vector3.zero;

            }

            //read data from file...
            //check skill active data...
            if (!string.IsNullOrEmpty(filepath[0]) && File.Exists(filepath[0]))
            {
                var readSkillData = File.ReadAllText(filepath[0]);
                var decryptData = EDProcessing.Decrypt(readSkillData, key, iv);

                if (!string.IsNullOrWhiteSpace(decryptData))
                {
                    var splitData = decryptData.Split("|");
                    foreach (var skillDataF in splitData)
                    {
                        indexSkillStored.Add(int.Parse(skillDataF));
                        StartCoroutine(AnimatedIn(skillData[int.Parse(skillDataF)].SkillBtn));
                        ActiveBtn++;
                    }
                }
            }
            //check boost streak data.
            if (!string.IsNullOrEmpty(filepath[1]) && File.Exists(filepath[1]))
            {
                var readSkillData = File.ReadAllText(filepath[1]);
                var decryptData = EDProcessing.Decrypt(readSkillData, key, iv);
                boostStreak = double.Parse(decryptData);
                PassiveScoreAdd(true, quizController.getBasePoint() * boostStreak);
                PassiveAbility[0].transform.GetChild(0).gameObject.transform.GetChild(0).GetComponent<TMP_Text>().text = $"{boostStreak * 100}%";
                StartCoroutine(AnimatedIn(PassiveAbility[0]));

            }
            //check correct streak data.
            if (!string.IsNullOrEmpty(filepath[2]) && File.Exists(filepath[2]))
            {
                var readSkillData = File.ReadAllText(filepath[2]);
                var decryptData = EDProcessing.Decrypt(readSkillData, key, iv);
                this.correctStreak = int.Parse(decryptData);
            }
            //check question id for active skill.
            if (!string.IsNullOrEmpty(filepath[3]) && File.Exists(filepath[3]))
            {
                var readSkillData = File.ReadAllText(filepath[3]);
                var decryptData = EDProcessing.Decrypt(readSkillData, key, iv);
                CurrentIDQuest = decryptData;
            }

        }
        else
        {

            //paksa mematikan skill yang sedang berjalan. kemungkinan akan bertambah skill yang harus di paksa berhenti
            foreach (var path in filepath)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            foreach (var ability in skillData)
            {
                ability.SkillBtn.transform.localScale = Vector3.zero;
                ability.SkillBtn.transform.gameObject.SetActive(false);
                ability.spawnCount = 0;
                //ability.SkillBtn.interactable = false;
            }
            foreach (var ability in PassiveAbility)
            {
                ability.transform.SetParent(this.transform);
            }
        }
    }

    private void CallAbility()
    {
        if(AbilityAllowed)
        {
            float totalRate = 0;
            if (ActiveBtn <= limitActivedBtn)
            {
                //menghitung total persentase kemunculan semua gambar.
                foreach (var rate in skillData)
                {
                    totalRate += rate.RateSkill;
                }

                //memilih skill yang akan ditampilkan berdasarkan persentase
                float randVal = Random.Range(0f, totalRate);
                float cumulativeRate = 0;

                
                for (int i = 0; i < skillData.Length; i++)
                {

                    //tambah nilai cumulativeRate dari rateShowBtn
                    cumulativeRate += skillData[i].RateSkill;

                    //randVal kurang dari atau sama dengan cumulativeRate;
                    if (randVal <= cumulativeRate)
                    {
                        if (!skillData[i].disabled)
                        {
                            if (!skillData[i].SkillBtn.gameObject.activeSelf)
                            {
                                if(skillData[i].maxSpawn > skillData[i].spawnCount || skillData[i].maxSpawn == 0)
                                {
                                    if (!skillData[i].firstSpawn)
                                    {
                                        StartCoroutine(MoveRotateOnce(skillData[i]));
                                        skillData[i].firstSpawn = true;
                                        PlayerPrefs.SetInt($"skillQIndex{i}", skillData[i].firstSpawn ? 1 : 0);
                                    }
                                    else
                                    {
                                        StartCoroutine(AnimatedIn(skillData[i].SkillBtn));
                                    }
                                    skillData[i].spawnCount++;
                                    
                                    indexSkillStored.Add(i);
                                    File.WriteAllText(filepath[0], EDProcessing.Encrypt(string.Join("|", indexSkillStored), key, iv));
                                    ActiveBtn++;
                                    break;
                                }
                                else
                                {
                                    Debug.Log($"Skill{i} : telah Mencapai batas untuk spawn.");
                                }
                            }
                            else
                            {
                                CallAbility();
                                break;
                            }
                        }
                        else
                        {
                            CallAbility();
                            break;
                        }
                        
                    }
                }
            }
            else
            {
                Debug.Log("skill max, cannot spawn again.");
            }
        }
        else
        {
            Debug.Log("Skill Disalowed to spawn.");
        }
    }

    //animation skill
    private IEnumerator SkillUsedPopup(string skillActivedText, Color32 color)
    {
        //set to half seconds to stay
        AudioController.Instance.PlayAudioSFX("Notification");
        AbilityUsedPopup.transform.GetComponentInChildren<TMP_Text>().text = skillActivedText;
        AbilityUsedPopup.transform.GetComponent<Image>().color = color;
        AbilityUsedPopup.SetActive(true);
        AbilityUsedPopup.transform.LeanScaleY(1f, 0.25f);
        yield return new WaitUntil(()=> Mathf.Approximately(AbilityUsedPopup.transform.localScale.y, 1f));
        yield return new WaitForSeconds(0.5f);
        AbilityUsedPopup.transform.LeanScaleY(0f, 0.25f);
        yield return new WaitUntil(() => Mathf.Approximately(AbilityUsedPopup.transform.localScale.y, 0f));
        AbilityUsedPopup.SetActive(false);
    }

    private IEnumerator AnimatedIn(GameObject Ability)
    {
        if (!Ability.activeSelf)
        {
            Ability.SetActive(true);
            Ability.transform.LeanScale(Vector3.one, animatedTime).setEaseOutBack();
            yield return new WaitUntil(() => Ability.transform.localScale == Vector3.one);
        }
        
    }

    private IEnumerator AnimatedIn(Button AbilityBtn)
    {
        AbilityBtn.gameObject.SetActive(true);
        AbilityBtn.transform.LeanScale(Vector3.one, animatedTime);
        yield return new WaitUntil(()=> AbilityBtn.transform.localScale == Vector3.one);
        //AbilityBtn.interactable = true;
    }

    private IEnumerator AnimatedOut(GameObject Ability)
    {
        if (Ability.activeSelf)
        {
            
            Ability.transform.LeanScale(Vector3.zero, animatedTime).setEaseInBack();
            yield return new WaitUntil(() => Ability.transform.localScale == Vector3.zero);
            Ability.SetActive(false);
        }

    }

    private IEnumerator AnimatedOut(Button abilityBtn)
    {
        //AbilityBtn.interactable = false;
        abilityBtn.transform.LeanScale(Vector3.zero, animatedTime).setEaseInBack();
        yield return new WaitUntil(() => abilityBtn.transform.localScale == Vector3.zero);
        abilityBtn.gameObject.SetActive(false);

        for(int i = 0; i < skillData.Length; i++)
        {
            if (indexSkillStored != null & indexSkillStored.Contains(i))
            {
                if (!skillData[i].SkillBtn.gameObject.activeSelf) //jika
                {
                    indexSkillStored.Remove(i);
                    //menyimpan data index skill terbaru.
                    File.WriteAllText(filepath[0], EDProcessing.Encrypt(string.Join("|", indexSkillStored), key, iv));
                }
            }
        }
    }

    private IEnumerator MoveRotateOnce(SkillData data)
    {
        var spawnSkillContainer = AbilitySpawnPopup.transform.GetChild(1);
        var skillImg = spawnSkillContainer.transform.GetChild(0).GetComponent<Image>();
        var skillDesc = spawnSkillContainer.transform.GetChild(1).GetComponent<TMP_Text>();

        skillDesc.text = data.SkillDesc;
        skillImg.sprite = data.SkillBtn.gameObject.transform.GetComponent<Image>().sprite;

        AbilitySpawnPopup.SetActive(true);
        LeanTween.value(AbilitySpawnPopup, ImgAlphaUpdate, 0f, 1f, 0.15f);
        yield return new WaitUntil(() => AbilitySpawnPopup.transform.GetComponent<RawImage>().color == new Color(colorSkillSpawnPopup.r, colorSkillSpawnPopup.g, colorSkillSpawnPopup.b, 1f));
        skillImg.transform.LeanScale(Vector3.one, animatedTime).setEaseOutBack();
        skillDesc.gameObject.transform.LeanScale(Vector3.one, animatedTime).setEaseOutBack();
        yield return new WaitForSeconds(3f);
        skillDesc.gameObject.transform.LeanScale(Vector3.zero, animatedTime).setEaseInBack();
        skillImg.gameObject.transform.LeanMoveLocal(new Vector3(500, 300, 0), animatedTime);
        //skillImg.transform.gameObject.transform.LeanRotateZ(90, animatedTime).setRotateZ();
        skillImg.gameObject.transform.LeanScale(Vector3.zero, animatedTime);
        yield return new WaitUntil(() => skillImg.gameObject.transform.localScale == Vector3.zero);
        LeanTween.value(AbilitySpawnPopup, ImgAlphaUpdate, 1f, 0f, 0.15f);
        yield return new WaitUntil(() => AbilitySpawnPopup.transform.GetComponent<RawImage>().color == new Color(colorSkillSpawnPopup.r, colorSkillSpawnPopup.g, colorSkillSpawnPopup.b, 0f));
        skillImg.gameObject.transform.localScale = Vector3.one;
        skillImg.gameObject.transform.localPosition = imgSkillLocalPos;
        AbilitySpawnPopup.SetActive(false);

        StartCoroutine(AnimatedIn(data.SkillBtn));
    }

    void ImgAlphaUpdate(float alpha)
    {
        var color = AbilitySpawnPopup.transform.GetComponent<RawImage>().color;
        color.a = alpha;
        AbilitySpawnPopup.transform.GetComponent<RawImage>().color = color;
    }

    //end animation

    //Skill Book 

    public void ShowSkillBook()
    {
        AudioController.Instance.PlayAudioSFX("ButtonClick");
        StartCoroutine(AnimatedIn(SkillBookUI));
    }
    public void CloseSkillBook()
    {
        AudioController.Instance.PlayAudioSFX("ButtonClick");
        StartCoroutine(AnimatedOut(SkillBookUI));
    }

    //End Skill Book

    public void DeactivedAbilityBtn()
    {
        var currentActiveBtn = EventSystem.current.currentSelectedGameObject.transform.GetComponent<Button>();
        ActiveBtn--;
        StartCoroutine(AnimatedOut(currentActiveBtn));

    }

    public void StreakShield()
    {
        BoostShield();
        StartCoroutine(SkillUsedPopup("Streak Shield Diaktifkan!", new Color32(140, 0, 212, 180)));
    }

    public void TimeExtendI()
    {
        TimeExtend(5);
        StartCoroutine(SkillUsedPopup("Extra 5 Detik Diaktifkan!", new Color32(0, 191, 212, 180)));
    }
    public void TimeExtendII()
    {
        TimeExtend(10);
        StartCoroutine(SkillUsedPopup("Extra 10 Detik Diaktifkan!", new Color32(140, 0, 212, 180)));
    }
    public void TimeExtendSpecial()
    {
        TimeExtend(12);
        StartCoroutine(SkillUsedPopup("Revive Diaktifkan!", new Color32(140, 0, 212, 180)));
    }

    public void TimeStopI()
    {
        AudioController.Instance.PlayAudioSFX("FreezeEffect");
        TimeStop(15);
        StartCoroutine(SkillUsedPopup("Freeze Diaktifkan!", new Color32(255, 215, 46, 180)));
    }

    public void AutoCorrect()
    {
        //Ace of The Quiz Skill Rotation, active per question
        AnswerAutoCorrect();

        StartCoroutine(SkillUsedPopup("ACE Diaktifkan!",new Color32(255,215,46,180)));
    }

    public void BoostScoreI()
    {
        //Boost 25% of Base Question Score, active per question
        ScoreAdd(true, quizController.getBasePoint() * 0.25); //basescore + 25%
        StartCoroutine(SkillUsedPopup("Boost Score 25% Diaktifkan!", new Color32(0, 191, 212, 180)));
    }
    public void BoostScoreII()
    {
        //Boost 75% of Base Question Score, active per question
        ScoreAdd(true, quizController.getBasePoint() * 0.75); //base score + 75%
        StartCoroutine(SkillUsedPopup("Boost Score 75% Diaktifkan!", new Color32(140, 0, 212, 180)));
    }

    public void Duality()
    {
        ScoreAdd(true, quizController.getBasePoint() * 3); // 4x = base score + (base score * 3);
        ScoreAdd(false, -(quizController.getBasePoint() * 4)); // -4x
        StartCoroutine(SkillUsedPopup("Duality Diaktifkan!", new Color32(255, 215, 46, 180)));
    }

    public void FalseRemoveIWExtendedTime()
    {
        //Active Per Question
        if (quizController.TypeQuestTracker() == 1) 
        {
            FalseRemove(1);
            StartCoroutine(SkillUsedPopup("Eraser Diaktifkan!", new Color32(140, 0, 212, 180)));
        }
        else if(quizController.TypeQuestTracker() == 2)
        {
            StartCoroutine(SkillUsedPopup("Extra 12 Detik Diaktifkan!", new Color32(140, 0, 212, 180)));
            TimeExtend(12);
        }
    }

    public void FalseRemoveIIWMultiplier()
    {
        //Active Per Question
        if (quizController.TypeQuestTracker() == 1)
        {
            FalseRemove(2);
            StartCoroutine(SkillUsedPopup("50/50 Diaktifkan!", new Color32(255, 215, 46, 180)));
        }
        else if (quizController.TypeQuestTracker() == 2)
        {
            ScoreAdd(true, quizController.getBasePoint() * 1.5);
            StartCoroutine(SkillUsedPopup("Boost Score 1.5x Diaktifkan!", new Color32(255, 215, 46, 180)));
        }
    }
    
    public void FalseRemoveIIIWTimeStop()
    {
        //Active Per Question
        if (quizController.TypeQuestTracker() == 1)
        {
            FalseRemove(3);
        }
        else if (quizController.TypeQuestTracker() == 2)
        {
            TimeStop(15);
        }
    }

    public void QuestionsRewind()
    {
        QuestionRewind();
        StartCoroutine(SkillUsedPopup("Rewind Diaktifkan!", new Color32(255, 215, 46, 180)));
    }

    //added Advance primary function

    //private void TimeDebuff(float time)
    //{
    //    if (AbilityAllowed)
    //    {
    //        quizController.FalseAnswerTimeDecrease(time);
    //        ActiveBtn--;
    //    }
    //    else
    //    {
    //        alertController.AlertSet("Skill Tidak Diizinkan.", true);
    //    }
    //    CurrentIDQuest = quizController.IdQuestTracking();
    //}

    private void BoostShield()
    {
        if (AbilityAllowed)
        {
            quizController.StreakShield();
        }
        else
        {
            alertController.AlertSet("Skill Tidak Diizinkan.", true);
        }
        setCurrentIDQuest();
    }

    private void AnswerAutoCorrect()
    {
        if (AbilityAllowed)
        {
            quizController.AutoCorrect();
            //ActiveBtn--;
        }
        else
        {
            alertController.AlertSet("Skill Tidak Diizinkan.", true);
        }
        setCurrentIDQuest();
    }

    private void TimeExtend(int TimeinSeconds)
    {
        if (AbilityAllowed)
        {
            timeController.TimeAdd(true, TimeinSeconds);
            //ActiveBtn--;
        }
        else
        {
            alertController.AlertSet("Skill Tidak Diizinkan.",true);
        }
        setCurrentIDQuest();
    }
    private void TimeDecrease(int TimeinSeconds)
    {
        if (AbilityAllowed)
        {
            timeController.TimeAdd(false, TimeinSeconds);
            //ActiveBtn--;
        }
        else
        {
            alertController.AlertSet("Skill Tidak Diizinkan.", true);
        }
        setCurrentIDQuest();
    }

    private void TimeStop(float timeSec) //byTime
    {
        if (AbilityAllowed)
        {
            timeController.TimeStop(timeSec);
            //ActiveBtn--;
        }
        else
        {
            alertController.AlertSet("Skill Tidak Diizinkan.", true);
        }
        setCurrentIDQuest();
    }

    private void QuestionRewind()
    {
        quizController.QuestionsRewind();
        //ActiveBtn--;
        setCurrentIDQuest();
    }

    private void FalseRemove(int objToRemove)
    {
        if (AbilityAllowed)
        {
            quizController.FalseRemover(objToRemove);
            //ActiveBtn--;
        }
        else
        {
            alertController.AlertSet("Skill Tidak Diizinkan.", true);
        }
        setCurrentIDQuest();

    }

    private void ScoreAdd(bool forStatusAnswer, double addVal)
    {
        if (AbilityAllowed)
        {
            quizController.AddPointQuest(forStatusAnswer, addVal);
            triggerboost = true;
            //ActiveBtn--;
        }
        else
        {
            alertController.AlertSet("Skill Tidak Diizinkan.", true);
        }
        setCurrentIDQuest();
    }

    private void PassiveScoreAdd(bool forStatusAnswer, double addVal)
    {
        if (AbilityAllowed)
        {
            quizController.AddPointQuest(forStatusAnswer, addVal);
            triggerboost = true;
        }
        else
        {
            alertController.AlertSet("Skill Tidak Diizinkan.", true);
        }
    }

    private void PassiveTimeExtend(int TimeinSeconds)
    {
        if (AbilityAllowed)
        {
            timeController.TimeAdd(true, TimeinSeconds);
            //ActiveBtn--;
        }
        else
        {
            alertController.AlertSet("Skill Tidak Diizinkan.", true);
        }
    }

    //end of primary function

    //start of utility

    private void setCurrentIDQuest()
    {
        CurrentIDQuest = quizController.IdQuestTracking();
        File.WriteAllText(filepath[3], EDProcessing.Encrypt(CurrentIDQuest, key, iv));
    }

    //end of utility
}

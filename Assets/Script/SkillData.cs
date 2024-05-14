using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SkillData
{
    public Button SkillBtn;
    public float RateSkill;
    public string SkillDesc;
    public bool firstSpawn;
    public bool disabled;
    public int maxSpawn;
    public int spawnCount;
}

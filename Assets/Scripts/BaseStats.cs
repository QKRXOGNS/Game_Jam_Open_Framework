using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System;
using System.Collections.Generic;

// JSON 파일에서 읽어올 스텟 베이스 정의
[System.Serializable]
public class StatDefinition
{
    public string name;
    public string description;
    public string icon;
}

[System.Serializable]
public class StatBaseEntry
{
    public string key; // STR, INT, CON, WIS
    public StatDefinition value;
}

[System.Serializable]
public class CharacterExample
{
    public string type;
    public string description;
    public BaseStatsData stats;
}

[System.Serializable]
public class StatsBaseData
{
    public StatBaseEntry[] statBase;
    public CharacterExample[] characterExamples;
    
    // Helper method to get stat definition by key
    public StatDefinition GetStatDefinition(string key)
    {
        foreach (var entry in statBase)
        {
            if (entry.key == key)
                return entry.value;
        }
        return null;
    }
}

[System.Serializable]
public class BaseStatsData
{
    public int STR; // 힘 (Strength)
    public int INT; // 지능 (Intelligence) 
    public int CON; // 체력 (Constitution)
    public int WIS; // 지혜 (Wisdom)
    
    public override string ToString()
    {
        return $"STR: {STR}, INT: {INT}, CON: {CON}, WIS: {WIS}";
    }
    
    // 스텟 값 검증 및 조정
    public void ValidateStats(int minValue, int maxValue)
    {
        STR = Mathf.Clamp(STR, minValue, maxValue);
        INT = Mathf.Clamp(INT, minValue, maxValue);
        CON = Mathf.Clamp(CON, minValue, maxValue);
        WIS = Mathf.Clamp(WIS, minValue, maxValue);
    }
    
    // 스텟 총합 계산
    public int GetTotalStats()
    {
        return STR + INT + CON + WIS;
    }
    
    // 평균 스텟 계산
    public float GetAverageStats()
    {
        return GetTotalStats() / 4f;
    }
    
    // JSON에서 스텟 설명과 함께 포맷된 문자열 반환
    public string ToDetailedString(StatsBaseData statsBase)
    {
        if (statsBase?.statBase == null) return ToString();
        
        string result = "";
        var strDef = statsBase.GetStatDefinition("STR");
        var intDef = statsBase.GetStatDefinition("INT");
        var conDef = statsBase.GetStatDefinition("CON");
        var wisDef = statsBase.GetStatDefinition("WIS");
        
        if (strDef != null) result += $"{strDef.icon} {strDef.name}: {STR}\n";
        if (intDef != null) result += $"{intDef.icon} {intDef.name}: {INT}\n";
        if (conDef != null) result += $"{conDef.icon} {conDef.name}: {CON}\n";
        if (wisDef != null) result += $"{wisDef.icon} {wisDef.name}: {WIS}";
            
        return result;
    }
}

[System.Serializable]
public class StatsResponse
{
    public string description; // AI가 생성한 설명
    public string jobClass;    // AI가 생성한 직업명
    public BaseStatsData stats;  // 기본 스텟 데이터
}

[System.Serializable]
public class StatsTextPart
{
    public string text;
}

// Gemini API 응답용 구조체들 (UnityAndGeminiV3.cs와 동일)
[System.Serializable]
public class StatsContent
{
    public string role;
    public StatsTextPart[] parts;
}

[System.Serializable]
public class StatsCandidate
{
    public StatsContent content;
}

[System.Serializable]
public class StatsApiResponse
{
    public StatsCandidate[] candidates;
}

// API 요청용 구조체들
[System.Serializable]
public class StatsRequestContent
{
    public StatsTextPart[] parts;
}

[System.Serializable] 
public class StatsRequest
{
    public StatsRequestContent[] contents;
}


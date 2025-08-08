using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System;
using UnityEngine.UI;
using System.IO;

public class StatsGenerator : MonoBehaviour
{
    [Header("Gemini API Configuration")]
    public TextAsset jsonApi; // Gemini API 키가 있는 JSON 파일
    
    [Header("Stats Base Configuration")]
    public TextAsset statsBaseJson; // 스텟 베이스와 캐릭터 예시가 있는 JSON 파일
    
    [Header("Image Generation Configuration")]
    public TextAsset imageGenerationJson; // 이미지 생성용 설정 JSON 파일
    public bool enableImageGeneration = true; // 이미지 생성 활성화
    
    [Header("Stats Generation Settings")]
    public TMP_InputField characterDescriptionInput; // 캐릭터 설명 입력창
    
    [Range(10, 5000)]
    public int maxStatValue = 5000; // 스텟 최대치
    [Range(1, 100)]  
    public int minStatValue = 1;  // 스텟 최소치
    
    [Header("UI References")]
    public TMP_Text statsDisplayText; // 스텟을 표시할 텍스트
    public TMP_Text descriptionText;  // AI 설명을 표시할 텍스트
    public TMP_Text jobClassText;     // 직업명을 표시할 텍스트
    public Button generateButton;     // 생성 버튼
    public UnityEngine.UI.Image characterImage; // 생성된 캐릭터 이미지 표시
    
    [Header("Generated Stats")]
    public BaseStatsData currentStats; // 현재 생성된 스텟
    public string currentJobClass;     // 현재 생성된 직업명
    public Texture2D currentCharacterImage; // 현재 생성된 캐릭터 이미지
    
    private string apiKey = "";
    private StatsBaseData statsBase;   // JSON에서 로드된 스텟 베이스 데이터
    private const string GEMINI_BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models/";
    private const string MODEL_NAME = "gemini-2.0-flash:generateContent";
    private const string IMAGE_MODEL_NAME = "gemini-2.0-flash-exp-image-generation:generateContent";
    
    void Start()
    {
        // API 키 로드
        if (jsonApi != null)
        {
            UnityAndGeminiKey jsonApiKey = JsonUtility.FromJson<UnityAndGeminiKey>(jsonApi.text);
            apiKey = jsonApiKey.key;
        }
        
        // 스텟 베이스 데이터 로드
        LoadStatsBase();
        
        // 버튼 이벤트 연결
        if (generateButton != null)
        {
            generateButton.onClick.AddListener(GenerateStats);
        }
        
        // 입력창 초기 설정
        SetupCharacterDescriptionInput();
        
        // 시작시 자동으로 스텟 생성 (테스트용)
        // StartCoroutine(RequestStatsFromAI());
    }
    
    private void LoadStatsBase()
    {
        if (statsBaseJson != null)
        {
            try
            {
                statsBase = JsonUtility.FromJson<StatsBaseData>(statsBaseJson.text);
                Debug.Log($"스텟 베이스 데이터 로드 완료 - 캐릭터 예시: {statsBase.characterExamples.Length}개");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"스텟 베이스 데이터 로드 실패: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("스텟 베이스 JSON 파일이 설정되지 않았습니다!");
        }
    }
    
    public void GenerateStats()
    {
        // 입력창 검증
        string description = GetCharacterDescription();
        if (string.IsNullOrEmpty(description.Trim()) || description == "판타지 RPG 캐릭터")
        {
            Debug.LogWarning("캐릭터 설명을 입력해주세요!");
            // 필요시 UI에 경고 메시지 표시 가능
        }
        
        StartCoroutine(GenerateStatsAndImage());
    }
    
    private IEnumerator GenerateStatsAndImage()
    {
        // 1. 먼저 스텟 생성
        yield return StartCoroutine(RequestStatsFromAI());
        
        // 2. 이미지 생성이 활성화되어 있다면 이미지 생성
        if (enableImageGeneration)
        {
            yield return StartCoroutine(RequestImageFromAI());
        }
    }
    
    private void SetupCharacterDescriptionInput()
    {
        if (characterDescriptionInput != null)
        {
            // placeholder 텍스트 설정
            if (characterDescriptionInput.placeholder != null)
            {
                var placeholderText = characterDescriptionInput.placeholder.GetComponent<TMP_Text>();
                if (placeholderText != null)
                {
                    placeholderText.text = "예: 불의 마법을 다루는 강력한 마법사";
                }
            }
            
            // 기본값이 비어있다면 예시 텍스트 설정
            if (string.IsNullOrEmpty(characterDescriptionInput.text))
            {
                characterDescriptionInput.text = "판타지 RPG 캐릭터";
            }
        }
    }
    
    private IEnumerator RequestStatsFromAI()
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API 키가 설정되지 않았습니다!");
            yield break;
        }
        
        // 프롬프트 구성
        string prompt = CreateStatsPrompt();
        
        string url = $"{GEMINI_BASE_URL}{MODEL_NAME}?key={apiKey}";
        
        // 구조체를 사용해서 안전한 JSON 생성
        StatsRequest request = new StatsRequest
        {
            contents = new StatsRequestContent[]
            {
                new StatsRequestContent
                {
                    parts = new StatsTextPart[]
                    {
                        new StatsTextPart { text = prompt }
                    }
                }
            }
        };

        string jsonData = JsonUtility.ToJson(request);
        Debug.Log($"요청 JSON: {jsonData}");
        Debug.Log($"프롬프트: {prompt}");
        
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
        
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            
            yield return www.SendWebRequest();
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"API 요청 실패: {www.error}");
                Debug.LogError($"응답 내용: {www.downloadHandler.text}");
            }
            else
            {
                Debug.Log("API 요청 성공!");
                ProcessStatsResponse(www.downloadHandler.text);
            }
        }
    }
    
    private string CreateStatsPrompt()
    {
        // 입력창에서 캐릭터 설명 가져오기
        string characterDescription = GetCharacterDescription();
        
        string basePrompt = $"캐릭터 설명: {characterDescription}\n\n이 캐릭터에 대한 기본 스텟과 판타지적이고 유니크한 직업명을 JSON 형식으로 생성해주세요.\n\n";
        
        // 스텟 정의 추가
        basePrompt += "=== 스텟 정의 ===\n";
        if (statsBase?.statBase != null)
        {
            foreach (var entry in statsBase.statBase)
            {
                basePrompt += $"{entry.value.icon} {entry.key} ({entry.value.name}): {entry.value.description}\n";
            }
        }
        else
        {
            basePrompt += "STR (힘): 물리적 힘과 근력\nINT (지능): 지능과 마법 능력\nCON (체력): 체력과 생명력\nWIS (지혜): 지혜와 정신력\n";
        }
        
        // 캐릭터 예시 추가
        basePrompt += "\n=== 캐릭터 예시 ===\n";
        if (statsBase?.characterExamples != null)
        {
            for (int i = 0; i < Mathf.Min(4, statsBase.characterExamples.Length); i++) // 최대 4개만 표시
            {
                var example = statsBase.characterExamples[i];
                basePrompt += $"{example.type}: {example.description} - STR:{example.stats.STR} INT:{example.stats.INT} CON:{example.stats.CON} WIS:{example.stats.WIS}\n";
            }
        }
        
        basePrompt += $"\n응답 형식: {{\"\"description\"\": \"\"캐릭터 상세 설명\"\", \"\"jobClass\"\": \"\"적합한 직업명\"\", \"\"stats\"\": {{\"\"STR\"\": 15, \"\"INT\"\": 12, \"\"CON\"\": 14, \"\"WIS\"\": 10}}}}";
        basePrompt += $"\n각 스텟은 {minStatValue}-{maxStatValue} 범위로 설정해주세요.";
        basePrompt += "\n\n=== 직업명 생성 가이드 ===";
        basePrompt += "\n직업명은 다음 조건을 만족하는 판타지적이고 유니크한 이름으로 생성해주세요:";
        basePrompt += "\n• 단순한 '전사', '마법사' 대신 창의적이고 독특한 이름 사용";
        basePrompt += "\n• 캐릭터의 특성과 능력을 반영하는 표현적인 직업명";
        basePrompt += "\n• 판타지 세계관에 어울리는 웅장하고 멋진 칭호";
        basePrompt += "\n• 예시: '화염술사' → '불꽃의 현자', '전사' → '강철의 수호자', '도적' → '그림자 무용가'";
        basePrompt += "\n• 직업명은 한국어로 작성하되, 2-6글자의 적절한 길이로 제한";
        basePrompt += "\n• 캐릭터 설명의 핵심 키워드를 직업명에 반영하여 창조해주세요.";
        
        return basePrompt;
    }
    
    private void ProcessStatsResponse(string responseText)
    {
        try
        {
            // Gemini API 응답 파싱 (UnityAndGeminiV3.cs와 동일한 구조 사용)
            StatsApiResponse apiResponse = JsonUtility.FromJson<StatsApiResponse>(responseText);
            
            if (apiResponse.candidates != null && apiResponse.candidates.Length > 0 &&
                apiResponse.candidates[0].content != null && 
                apiResponse.candidates[0].content.parts != null &&
                apiResponse.candidates[0].content.parts.Length > 0)
            {
                string aiResponseText = apiResponse.candidates[0].content.parts[0].text;
                Debug.Log($"AI 응답: {aiResponseText}");
                
                // JSON에서 중괄호 부분만 추출
                string jsonPart = ExtractJsonFromResponse(aiResponseText);
                
                if (!string.IsNullOrEmpty(jsonPart))
                {
                    // StatsResponse로 파싱
                    StatsResponse statsResponse = JsonUtility.FromJson<StatsResponse>(jsonPart);
                    
                    if (statsResponse != null)
                    {
                        currentStats = statsResponse.stats;
                        currentJobClass = statsResponse.jobClass;
                        
                        // 스텟 범위 검증 및 조정
                        currentStats.ValidateStats(minStatValue, maxStatValue);
                        
                        // 조정된 스텟으로 응답 업데이트
                        statsResponse.stats = currentStats;
                        
                        UpdateUI(statsResponse);
                        Debug.Log($"캐릭터 생성 완료 - 직업: {statsResponse.jobClass}, 스텟: {currentStats.ToString()} (총합: {currentStats.GetTotalStats()}, 평균: {currentStats.GetAverageStats():F1})");
                    }
                    else
                    {
                        Debug.LogError("스텟 응답 파싱 실패");
                    }
                }
                else
                {
                    Debug.LogError("JSON 추출 실패");
                }
            }
            else
            {
                Debug.LogError("API 응답에서 유효한 데이터를 찾을 수 없습니다.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"응답 처리 중 오류 발생: {e.Message}");
            Debug.LogError($"원본 응답: {responseText}");
        }
    }
    
    private string ExtractJsonFromResponse(string response)
    {
        // JSON 시작과 끝 찾기
        int startIndex = response.IndexOf('{');
        int lastBraceIndex = response.LastIndexOf('}');
        
        if (startIndex >= 0 && lastBraceIndex > startIndex)
        {
            return response.Substring(startIndex, lastBraceIndex - startIndex + 1);
        }
        
        return "";
    }
    
    private void UpdateUI(StatsResponse response)
    {
        // 직업명 표시
        if (jobClassText != null)
        {
            jobClassText.text = response.jobClass;
        }
        
        // 스텟 표시 (2개씩 가로 배치)
        if (statsDisplayText != null)
        {
            string statsText = "";
            
            // 2개씩 한 줄에 표시 (제목이나 추가 정보 없이)
            if (statsBase?.statBase != null)
            {
                var strDef = statsBase.GetStatDefinition("STR");
                var intDef = statsBase.GetStatDefinition("INT");
                var conDef = statsBase.GetStatDefinition("CON");
                var wisDef = statsBase.GetStatDefinition("WIS");
                
                string strName = strDef?.name ?? "힘";
                string intName = intDef?.name ?? "지능";
                string conName = conDef?.name ?? "체력";
                string wisName = wisDef?.name ?? "지혜";
                
                statsText += $"STR ({strName}): {response.stats.STR} INT ({intName}): {response.stats.INT}\n";
                statsText += $"CON ({conName}): {response.stats.CON} WIS ({wisName}): {response.stats.WIS}";
            }
            else
            {
                // 백업용 기본 표시
                statsText += $"STR (힘): {response.stats.STR} INT (지능): {response.stats.INT}\n";
                statsText += $"CON (체력): {response.stats.CON} WIS (지혜): {response.stats.WIS}";
            }
            
            statsDisplayText.text = statsText;
        }
        
        // 설명 표시
        if (descriptionText != null)
        {
            descriptionText.text = response.description;
        }
    }
    
    // 외부에서 스텟 접근용 메서드들
    public BaseStatsData GetCurrentStats()
    {
        return currentStats;
    }
    
    public string GetCurrentJobClass()
    {
        return currentJobClass;
    }
    
    // 입력창에서 캐릭터 설명 가져오기
    private string GetCharacterDescription()
    {
        if (characterDescriptionInput != null && !string.IsNullOrEmpty(characterDescriptionInput.text.Trim()))
        {
            return characterDescriptionInput.text.Trim();
        }
        return "판타지 RPG 캐릭터"; // 기본값
    }
    
    public void SetCharacterDescription(string description)
    {
        if (characterDescriptionInput != null)
        {
            characterDescriptionInput.text = description;
        }
    }
    
    // 현재 입력된 캐릭터 설명 조회
    public string GetCurrentCharacterDescription()
    {
        return GetCharacterDescription();
    }
    
    // 직업명만 설정
    public void SetJobClass(string jobClass)
    {
        currentJobClass = jobClass;
        
        // UI 업데이트 (기존 스텟이 있는 경우)
        if (currentStats != null)
        {
            StatsResponse response = new StatsResponse
            {
                description = "직업이 변경되었습니다.",
                jobClass = currentJobClass,
                stats = currentStats
            };
            UpdateUI(response);
        }
    }
    
    // 스텟과 직업 수동 설정 (범위 검증 포함)
    public void SetStats(int str, int intel, int con, int wis, string jobClass = "미정")
    {
        currentStats = new BaseStatsData
        {
            STR = str,
            INT = intel,
            CON = con,
            WIS = wis
        };
        
        currentJobClass = jobClass;
        currentStats.ValidateStats(minStatValue, maxStatValue);
        
        // UI 업데이트
        if (currentStats != null)
        {
            StatsResponse response = new StatsResponse
            {
                description = "수동으로 설정된 캐릭터",
                jobClass = currentJobClass,
                stats = currentStats
            };
            UpdateUI(response);
        }
    }
    
    // 스텟 범위 설정
    public void SetStatRange(int min, int max)
    {
        minStatValue = Mathf.Clamp(min, 1, 100);
        maxStatValue = Mathf.Clamp(max, 10, 5000);
        
        // 기존 스텟이 있다면 새 범위로 재검증
        if (currentStats != null)
        {
            currentStats.ValidateStats(minStatValue, maxStatValue);
            StatsResponse response = new StatsResponse
            {
                description = $"범위 조정됨 ({minStatValue}-{maxStatValue})",
                jobClass = currentJobClass ?? "미정",
                stats = currentStats
            };
            UpdateUI(response);
        }
    }
    
    // 스텟 베이스 데이터 접근 메서드들
    public StatsBaseData GetStatsBaseData()
    {
        return statsBase;
    }
    
    public CharacterExample[] GetCharacterExamples()
    {
        return statsBase?.characterExamples;
    }
    
    public CharacterExample GetRandomCharacterExample()
    {
        if (statsBase?.characterExamples != null && statsBase.characterExamples.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, statsBase.characterExamples.Length);
            return statsBase.characterExamples[randomIndex];
        }
        return null;
    }
    
    // 특정 캐릭터 예시로 스텟 설정
    public void SetStatsFromExample(string characterType)
    {
        if (statsBase?.characterExamples != null)
        {
            foreach (var example in statsBase.characterExamples)
            {
                if (example.type == characterType)
                {
                    SetStats(example.stats.STR, example.stats.INT, example.stats.CON, example.stats.WIS, example.type);
                    Debug.Log($"'{example.type}' 캐릭터 예시로 스텟 설정됨");
                    return;
                }
            }
        }
        Debug.LogWarning($"캐릭터 타입 '{characterType}'을 찾을 수 없습니다.");
    }
    
    // 스텟 베이스 다시 로드
    public void ReloadStatsBase()
    {
        LoadStatsBase();
    }
    
    // ========== 이미지 생성 메서드들 ==========
    
    private IEnumerator RequestImageFromAI()
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API 키가 설정되지 않았습니다!");
            yield break;
        }

        // 캐릭터 설명과 직업을 바탕으로 이미지 프롬프트 생성
        string imagePrompt = CreateImagePrompt();
        
        string url = $"{GEMINI_BASE_URL}{IMAGE_MODEL_NAME}?key={apiKey}";
        
        // 이미지 생성 요청 JSON 구성
        string jsonData = $@"{{
            ""contents"": [{{
                ""parts"": [{{
                    ""text"": ""{imagePrompt}""
                }}]
            }}],
            ""generationConfig"": {{
                ""responseModalities"": [""Text"", ""Image""]
            }}
        }}";

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) 
            {
                Debug.LogError($"이미지 생성 실패: {www.error}");
                Debug.LogError($"응답 내용: {www.downloadHandler.text}");
            } 
            else 
            {
                Debug.Log("이미지 생성 요청 성공!");
                ProcessImageResponse(www.downloadHandler.text);
            }
        }
    }
    
    private string CreateImagePrompt()
    {
        string description = GetCharacterDescription();
        string jobClass = currentJobClass ?? "판타지 캐릭터";
        
        // 한국어 직업명을 영어로 변환 (AI가 더 잘 이해할 수 있도록)
        string englishJobClass = TranslateJobClassToEnglish(jobClass);
        
        string prompt = $"Create a medieval fantasy character in pixel art style, full body sprite: {description}";
        prompt += $", Job class: {englishJobClass}";
        prompt += ". Pixel art style, 16-bit retro gaming style, pixelated character sprite, full body standing pose, medieval fantasy setting, RPG character sprite, detailed pixel work, retro game art.";
        
        return prompt;
    }
    
    private string TranslateJobClassToEnglish(string koreanJobClass)
    {
        // 일반적인 직업명 변환
        switch (koreanJobClass)
        {
            case "전사": return "Warrior";
            case "마법사": return "Mage";
            case "도적": return "Rogue";
            case "궁수": return "Archer";
            case "팔라딘": return "Paladin";
            case "드루이드": return "Druid";
            case "바드": return "Bard";
            case "야만용사": return "Barbarian";
            case "무극의 궤": return "Ultimate Weapon Master";
            case "화염의 현자": return "Flame Sage";
            case "강철의 수호자": return "Steel Guardian";
            case "그림자 무용가": return "Shadow Dancer";
            case "얼음의 여제": return "Ice Empress";
            case "대지의 파수꾼": return "Earth Warden";
            case "번개의 기사": return "Lightning Knight";
            case "바람의 유랑자": return "Wind Wanderer";
            default:
                // 한국어가 포함된 경우 영어로 설명 추가
                if (koreanJobClass.Contains("마법") || koreanJobClass.Contains("술사"))
                    return $"{koreanJobClass} (Fantasy Mage)";
                else if (koreanJobClass.Contains("전사") || koreanJobClass.Contains("기사"))
                    return $"{koreanJobClass} (Fantasy Warrior)";
                else if (koreanJobClass.Contains("도적") || koreanJobClass.Contains("암살"))
                    return $"{koreanJobClass} (Fantasy Rogue)";
                else
                    return $"{koreanJobClass} (Fantasy Character)";
        }
    }
    
    private void ProcessImageResponse(string responseText)
    {
        try
        {
            // Gemini API 이미지 응답 파싱 (UnityAndGeminiV3.cs와 유사)
            ImageResponse response = JsonUtility.FromJson<ImageResponse>(responseText);
            
            if (response.candidates != null && response.candidates.Length > 0 && 
                response.candidates[0].content != null && 
                response.candidates[0].content.parts != null)
            {
                foreach (var part in response.candidates[0].content.parts)
                {
                    if (!string.IsNullOrEmpty(part.text))
                    {
                        Debug.Log($"이미지 생성 텍스트 응답: {part.text}");
                    }
                    else if (part.inlineData != null && !string.IsNullOrEmpty(part.inlineData.data))
                    {
                        // Base64 이미지 데이터를 Texture2D로 변환
                        byte[] imageBytes = System.Convert.FromBase64String(part.inlineData.data);
                        
                        Texture2D tex = new Texture2D(2, 2);
                        tex.LoadImage(imageBytes);
                        
                        // 생성된 이미지 저장 (선택사항)
                        SaveGeneratedImage(tex);
                        
                        // 현재 캐릭터 이미지 업데이트
                        currentCharacterImage = tex;
                        
                        // UI에 이미지 표시
                        DisplayCharacterImage(tex);
                        
                        Debug.Log("캐릭터 이미지 생성 완료!");
                        break;
                    }
                }
            }
            else
            {
                Debug.LogWarning("이미지 응답에서 유효한 데이터를 찾을 수 없습니다.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"이미지 응답 처리 중 오류 발생: {e.Message}");
            Debug.LogError($"원본 응답: {responseText}");
        }
    }
    
    private void SaveGeneratedImage(Texture2D texture)
    {
        try
        {
            byte[] pngBytes = texture.EncodeToPNG();
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"character_{currentJobClass}_{timestamp}.png";
            string path = Path.Combine(Application.persistentDataPath, filename);
            File.WriteAllBytes(path, pngBytes);
            Debug.Log($"캐릭터 이미지 저장됨: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"이미지 저장 실패: {e.Message}");
        }
    }
    
    private void DisplayCharacterImage(Texture2D texture)
    {
        if (characterImage != null)
        {
            // Texture2D를 Sprite로 변환해서 UI Image에 표시
            Sprite imageSprite = Sprite.Create(texture, 
                new Rect(0.0f, 0.0f, texture.width, texture.height), 
                new Vector2(0.5f, 0.5f));
            
            characterImage.sprite = imageSprite;
            Debug.Log("캐릭터 이미지가 UI에 표시되었습니다!");
        }
        else
        {
            Debug.LogWarning("Character Image UI 컴포넌트가 설정되지 않았습니다!");
        }
    }
    
    // ========== 외부 이미지 제어 메서드들 ==========
    
    /// <summary>
    /// 현재 캐릭터 이미지 가져오기
    /// </summary>
    public Texture2D GetCurrentCharacterImage()
    {
        return currentCharacterImage;
    }
    
    /// <summary>
    /// 이미지 생성 기능 활성화/비활성화
    /// </summary>
    public void SetImageGenerationEnabled(bool enabled)
    {
        enableImageGeneration = enabled;
    }
    
    /// <summary>
    /// 수동으로 이미지 생성 (스텟과 별도로)
    /// </summary>
    public void GenerateImageOnly()
    {
        if (enableImageGeneration)
        {
            StartCoroutine(RequestImageFromAI());
        }
        else
        {
            Debug.LogWarning("이미지 생성 기능이 비활성화되어 있습니다!");
        }
    }
}
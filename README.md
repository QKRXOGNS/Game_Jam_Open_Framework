# 🎮 Game Jam Open Framework

Unity 기반 AI 캐릭터 생성 프레임워크입니다. Gemini API를 활용하여 판타지 RPG 캐릭터의 스텟과 이미지를 자동 생성할 수 있습니다.

## 🚀 빠른 시작

### 1. Gemini API 키 설정
1. [Google AI Studio](https://aistudio.google.com/prompts/new_chat)에서 Gemini API 키를 발급받으세요
2. `Assets/GeminiManager/JSON_KEY_TEMPLATE.json` 파일을 열어서 발급받은 API 키를 입력하세요:
```json
{
  "key": "여기에_발급받은_API_키를_붙여넣어주세요"
}
```

### 2. 프로젝트 실행
1. Unity에서 프로젝트를 열어주세요
2. `Assets/4_scene/intro.unity` 씬을 실행하면 바로 사용 가능합니다!

## 📋 프레임워크 구성

### 🎯 주요 기능
1. **Gemini API 스크립트** - AI 기반 캐릭터 생성
2. **OpenAI API 스크립트** (주석처리) - 추후 확장 가능
3. **게임 씬 구성**: 
   - `intro.unity` - 인트로 씬
   - `main.unity` - 메인 게임 씬
4. **JSON 설정 파일** - API 키 및 캐릭터 데이터 관리
5. **즉시 사용 가능** - 별도 설정 없이 바로 실행

## 🛠️ 프로젝트 구조 분석

### 📁 주요 디렉토리

```
Assets/
├── 🔧 Scripts/                    # 핵심 스크립트
│   ├── StatsGenerator.cs          # AI 캐릭터 스텟 생성기
│   ├── BaseStats.cs              # 스텟 데이터 구조체
│   └── intro/
│       └── IntroManager.cs       # 인트로 씬 관리
├── 🤖 GeminiManager/              # Gemini API 관리
│   ├── Scripts/
│   │   └── UnityAndGeminiV3.cs   # Gemini API 통신
│   ├── JSON_KEY_TEMPLATE.json    # API 키 설정 파일
│   └── Example/
│       └── Chatbot 1.unity       # 챗봇 예제 씬
├── 📊 2_data/                     # 게임 데이터
│   ├── character_stats_base.json # 캐릭터 스텟 기본 설정
│   └── image_generation_config.json # 이미지 생성 설정
├── 🎮 4_scene/                    # 게임 씬
│   ├── intro.unity               # 인트로 씬
│   └── main.unity                # 메인 게임 씬
└── 🎨 1_resources/                # 게임 리소스 (이미지, 사운드 등)
```

### 🧩 핵심 스크립트 분석

#### 1. **StatsGenerator.cs** - AI 캐릭터 생성 엔진
- **기능**: Gemini API를 통한 캐릭터 스텟 및 이미지 자동 생성
- **주요 특징**:
  - 사용자 입력 기반 캐릭터 설명 → AI 스텟 생성
  - 판타지 직업명 자동 생성 (예: "화염의 현자", "강철의 수호자")
  - 픽셀 아트 스타일 캐릭터 이미지 생성
  - 스텟 범위 검증 및 조정 (1-5000)
  - 실시간 UI 업데이트

```csharp
// 주요 메서드
public void GenerateStats()           // 스텟 생성 시작
public void SetCharacterDescription() // 캐릭터 설명 설정
public BaseStatsData GetCurrentStats() // 현재 스텟 조회
```

#### 2. **BaseStats.cs** - 데이터 구조 정의
- **기능**: 캐릭터 스텟 데이터 구조체 및 JSON 파싱
- **스텟 시스템**:
  - `STR` (💪 힘): 물리 공격력
  - `INT` (🧠 지능): 마법 공격력, 마나
  - `CON` (❤️ 체력): HP, 방어력  
  - `WIS` (✨ 지혜): 마법 방어력, 스킬 포인트

#### 3. **IntroManager.cs** - 인트로 씬 관리
- **기능**: 인트로 화면 애니메이션 및 씬 전환
- **특징**:
  - 부드러운 이미지 위아래 움직임 (Sin 함수 기반)
  - 터치/클릭 입력 감지 (모바일/PC 호환)
  - 자동 씬 전환 (`intro` → `main`)

#### 4. **UnityAndGeminiV3.cs** - Gemini API 통신
- **기능**: Google Gemini API와의 HTTP 통신 처리
- **지원 모델**:
  - Gemini 2.0 Flash (텍스트/이미지 생성)
  - Gemini 1.5 Pro/Flash
  - 이미지 생성 전용 모델

### 📊 데이터 파일 분석

#### `character_stats_base.json` - 캐릭터 기본 데이터
```json
{
  "statBase": [
    {"key": "STR", "value": {"name": "힘", "icon": "💪"}},
    {"key": "INT", "value": {"name": "지능", "icon": "🧠"}},
    // ...
  ],
  "characterExamples": [
    {"type": "전사", "stats": {"STR": 18, "INT": 8, "CON": 16, "WIS": 10}},
    {"type": "마법사", "stats": {"STR": 6, "INT": 20, "CON": 8, "WIS": 18}},
    // 총 8개 직업 예시 포함
  ]
}
```

#### `image_generation_config.json` - 이미지 생성 설정
- 픽셀 아트 스타일 설정
- 16비트 레트로 게임 스타일
- 512x512 해상도
- 중세 판타지 테마

## 🎯 사용법

### 기본 사용법
1. **인트로 씬**: 화면을 터치/클릭하여 메인 씬으로 이동
2. **메인 씬**: 
   - 캐릭터 설명 입력 (예: "불의 마법을 다루는 강력한 마법사")
   - "생성" 버튼 클릭
   - AI가 자동으로 스텟과 직업명, 이미지 생성

### 고급 설정
```csharp
// 스텟 범위 조정
statsGenerator.SetStatRange(1, 100);

// 수동 스텟 설정
statsGenerator.SetStats(15, 12, 14, 10, "커스텀 직업");

// 이미지 생성 활성화/비활성화
statsGenerator.SetImageGenerationEnabled(true);
```

## 🔧 기술 스택

- **Unity 2022.3+** - 게임 엔진
- **C#** - 프로그래밍 언어
- **Google Gemini API** - AI 텍스트/이미지 생성
- **TextMeshPro** - UI 텍스트 렌더링
- **Unity Input System** - 입력 처리
- **JSON** - 데이터 직렬화

## 🚀 확장 가능성

### 현재 구현된 기능
- ✅ AI 기반 캐릭터 스텟 생성
- ✅ 픽셀 아트 캐릭터 이미지 생성
- ✅ 판타지 직업명 자동 생성
- ✅ 실시간 UI 업데이트
- ✅ 모바일/PC 입력 지원

### 확장 가능한 기능
- 🔄 OpenAI API 통합 (현재 주석처리)
- 🎨 다양한 아트 스타일 지원
- 💾 캐릭터 데이터 저장/로드
- 🎮 RPG 게임 로직 추가
- 🌐 멀티플레이어 지원

## 📝 라이선스

이 프로젝트는 게임 잼 및 학습 목적으로 제작된 오픈 소스 프레임워크입니다.

## 🤝 기여하기

1. 이 저장소를 Fork 하세요
2. 새로운 기능 브랜치를 생성하세요 (`git checkout -b feature/새기능`)
3. 변경사항을 커밋하세요 (`git commit -am '새 기능 추가'`)
4. 브랜치에 Push 하세요 (`git push origin feature/새기능`)
5. Pull Request를 생성하세요

## 📞 문의

프로젝트에 대한 질문이나 제안사항이 있으시면 Issue를 생성해 주세요.

---

**🎮 Happy Game Development! 🎮**

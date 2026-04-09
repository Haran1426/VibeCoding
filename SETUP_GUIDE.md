# Color Pop Puzzle — Unity 씬 설정 가이드

## 1. 레벨 데이터 자동 생성
Unity 에디터 상단 메뉴에서:
```
Tools > Color Pop > Generate 20 Levels
```
→ `Assets/ScriptableObjects/Levels/` 에 Level_01 ~ Level_20 생성됨

---

## 2. Block 프리팹 생성

1. **Hierarchy** → Create → UI → Image
2. 이름: `Block`
3. **Image** 컴포넌트: Source Image = 흰색 스프라이트 (Rounded Square 권장)
4. **Block.cs** 컴포넌트 추가
   - `Bg Image` → 자신의 Image 컴포넌트
   - `Glow Image` → 자식 Image 오브젝트 하나 추가 (흰색, alpha 0)
5. **Prefabs/** 폴더로 드래그해서 프리팹 저장

---

## 3. Game 씬 구조

```
Canvas (Screen Space - Overlay, 1080×1920 권장)
├── Background          (Image, 전체 화면 배경색)
│
├── HUD                 (최상단 영역)
│   ├── LevelTitle      (TextMeshPro) ← UIManager.levelTitleText
│   ├── ScoreText       (TextMeshPro) ← UIManager.scoreText
│   ├── BestScoreText   (TextMeshPro) ← UIManager.bestScoreText
│   └── Stars           (3개의 Image)← UIManager.starImages[0~2]
│
├── BoardArea           (중앙 정사각형 영역)
│   └── BoardContainer  (RectTransform) ← BoardManager.boardContainer
│       └── (Block들이 여기에 생성됨)
│
├── PopScoreText        (TextMeshPro, 중앙 위) ← UIManager.popScoreText
│
└── ResultPanel         (Panel)         ← UIManager.resultPanel
    ├── ResultTitle     (TextMeshPro)   ← UIManager.resultTitleText
    ├── ResultScore     (TextMeshPro)   ← UIManager.resultScoreText
    ├── ResultStars     (3개 Image)     ← UIManager.resultStarImages
    ├── NextButton      (Button)        ← UIManager.nextButton
    ├── RetryButton     (Button)        → GameManager.OnRetryClicked()
    └── MenuButton      (Button)        → GameManager.OnMenuClicked()

GameManagers (빈 오브젝트)
├── GameManager.cs
│   ├── Board Manager → BoardManager 오브젝트
│   ├── UI Manager    → UIManager 오브젝트 (또는 Canvas)
│   ├── Audio Manager → AudioManager 오브젝트
│   └── Levels        → Level_01 ~ Level_20 드래그
├── BoardManager.cs
│   ├── Board Container → BoardArea/BoardContainer
│   └── Block Prefab    → Prefabs/Block
├── UIManager.cs
└── AudioManager.cs
```

---

## 4. 점수 공식
```
획득 점수 = 그룹 크기² × 10
전체 클리어 보너스 = 1000점
```

## 5. 빌드 설정 (스토브 인디)
- **File > Build Settings**
  - Platform: PC, Mac & Linux Standalone
  - Target Platform: Windows
  - Architecture: x86_64
  - Resolution: 1280×720 이상 권장

## 6. 스토브 인디 등록 필수 자료
| 항목 | 규격 |
|------|------|
| 대표 이미지 | 460×215 px |
| 캡처 이미지 | 1280×720 이상, 최소 3장 |
| 게임 제목 | Color Pop Puzzle |
| 장르 | 캐주얼/퍼즐 |
| 연령 등급 | 전체 이용가 |

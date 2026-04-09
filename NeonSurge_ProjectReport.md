# NeonSurge — 프로젝트 전체 현황 보고서
> Unity 6 (6000.0.60f1) · C# · 작성일: 2026-04-09  
> 목적: 현재 구현 상태 전체 정리 (GPT 컨펌용)

---

## 1. 게임 개요

| 항목 | 내용 |
|---|---|
| 장르 | Roguelite Top-down Survivor Shooter |
| 레퍼런스 | Vampire Survivors, 20 Minutes Till Dawn |
| 플랫폼 목표 | PC (Steam, Stove Store), Android (Google Play) |
| 세션 길이 | 15~30분 |
| 아트 | 외부 에셋 없음 — Unity Primitive + Neon Material로 구현 |

### 핵심 루프
```
이동 & 자동 발사 → 적 처치 → EXP 오브 드롭 → 수집 → 레벨업
→ 3개 업그레이드 중 1 선택 → 더 강해진 상태로 반복
```

---

## 2. 기술 스택

| 항목 | 내용 |
|---|---|
| 엔진 | Unity 6 (6000.0.60f1) |
| 언어 | C# |
| 아키텍처 원칙 | SOLID (SRP·OCP·DIP 중심) |
| 이벤트 시스템 | 정적 EventBus (컴포넌트 간 직접 참조 제거) |
| 오브젝트 풀 | 제네릭 ObjectPool<T> |
| 입력 추상화 | IInputProvider (PC/모바일 분리) |
| UI | TextMeshPro (Paperlogy 폰트, 한글+영문) |
| 오디오 | 외부 파일 없음 — 절차적 사인파 합성 |
| 저장 | PlayerPrefs (최고점수, 볼륨 설정) |

---

## 3. 씬 구성

### 3-1. MenuScene (`Assets/Scenes/MenuScene/MenuScene.unity`)

**기능**
- 타이틀 표시, 게임 시작, 설정, 종료
- 설정: 마스터 볼륨 슬라이더, SFX 볼륨 슬라이더 (PlayerPrefs 저장)
- 최고 점수 표시 (PlayerPrefs `BestScore`)

**하이어라키**
```
MenuScene
├── EventSystem
├── MenuCanvas
│   ├── MainPanel
│   │   ├── TitleText
│   │   ├── BestScoreText
│   │   ├── PlayButton
│   │   ├── SettingsButton
│   │   └── QuitButton
│   └── SettingsPanel
│       ├── MasterVolumeSlider + Label
│       └── SFXVolumeSlider + Label
└── MenuManager (MonoBehaviour)
```

**스크립트**: `MenuManager.cs`
- `ShowMain()` / `ShowSettings()` 패널 전환
- `OnPlayClicked()` → `SceneManager.LoadScene("GameScene")`
- `OnMasterVolumeChanged(float)` / `OnSFXVolumeChanged(float)` → PlayerPrefs 저장 + AudioManager.RefreshVolume()

---

### 3-2. GameScene (`Assets/Scenes/GameScene/GameScene.unity`)

**하이어라키**
```
GameScene
├── _Systems                  ← Singleton 매니저 (게임플레이 전체 관할)
│   ├── GameStateManager
│   ├── ScoreSystem
│   ├── ExpSystem
│   └── AudioManager
├── _Managers                 ← 씬 전용 매니저
│   ├── WaveSpawner
│   ├── AbilityManager
│   └── PoolManager
├── World
│   └── Ground
├── Main Camera               (CameraFollow 부착)
├── Directional Light
├── Player                    (PlayerStats, PlayerController, PlayerShooter, FirePoint)
├── SceneBootstrapper         (씬 시작 시 레퍼런스 검증)
├── HUD Canvas                (HUDManager)
│   ├── HealthBar (Slider)
│   ├── ExpBar (Slider)
│   ├── TimerText
│   ├── ScoreText
│   ├── LevelText
│   └── KillText
├── LevelUpPanel              (UIManager)
│   └── 능력 버튼 3개 (동적 생성)
├── GameOverPanel             (UIManager)
│   ├── 생존시간 / 점수 / 처치수 텍스트
│   ├── RestartButton
│   └── MenuButton
└── MobileControlsCanvas      (MobileControlsCanvas.cs, 모바일만 활성화)
    ├── LeftJoystick  (VirtualJoystick, JoystickSide.Left)
    └── RightJoystick (VirtualJoystick, JoystickSide.Right)
```

---

## 4. 스크립트 전체 목록

### 4-1. Interfaces

| 파일 | 역할 |
|---|---|
| `IDamageable.cs` | `TakeDamage(float)`, `CurrentHealth`, `MaxHealth`, `IsAlive` |
| `IAbility.cs` | `Id`, `DisplayName`, `Description`, `MaxLevel`, `CurrentLevel`, `CanOffer`, `Apply(PlayerStats)` |
| `IInputProvider.cs` | `GetMoveInput()`, `GetAimInput(Vector3, Camera)` |

---

### 4-2. Core

| 파일 | 역할 | 비고 |
|---|---|---|
| `EventBus.cs` | 정적 이벤트 채널 | 씬 전환 시 `Clear()` 호출 |
| `ObjectPool.cs` | 제네릭 오브젝트 풀 | `Get(pos, rot)` / `Return(obj)` |
| `PoolManager.cs` | Bullet / ExpOrb / EnemyBullet 풀 | Inspector에서 프리팹 연결 |
| `VFXManager.cs` | 파티클 이펙트 재생 | 외부 에셋 없이 절차적 생성 |
| `AudioManager.cs` | BGM + SFX 재생 | 절차적 사인파 합성 |

#### EventBus 이벤트 목록
```
OnStateChanged(GameState)       — 게임 상태 변경
OnGameStarted()                 — 게임 시작
OnGameOver()                    — 게임 오버
OnScoreChanged(int)             — 점수 변경
OnExpChanged(int, int)          — 경험치 변경 (현재, 최대)
OnPlayerLevelUp(int)            — 레벨업 (새 레벨)
OnPlayerHealthChanged(float, float) — 체력 변경 (현재, 최대)
OnAbilityChoiceReady(List<IAbility>) — 능력 선택 패널 열기
```

#### AudioManager SFX 메서드
```
PlayShoot()       — 발사 (2200→800Hz 스윕, 0.07s)
PlayBulletHit()   — 총알 충돌 (노이즈 버스트, 0.05s)
PlayEnemyDeath()  — 적 사망 (피치다운 + 노이즈, 0.18s)
PlayLevelUp()     — 레벨업 (Do-Mi-Sol-Do' 아르페지오, 0.35s)
PlayGameOver()    — 게임오버 (하강 4음, 0.55s)
PlayExpPickup()   — EXP 흡수 (2000Hz 핑, 0.06s)
PlayPlayerHurt()  — 피격 (저음 쿵 + 노이즈, 0.1s)
```

BGM: 55/110/165/220/440Hz 드론 레이어 + 0.4Hz LFO + 글리치 게이트 (4초 루프)

---

### 4-3. System

| 파일 | 역할 | 패턴 |
|---|---|---|
| `GameState.cs` | `enum GameState { Playing, LevelUp, Paused, GameOver }` | — |
| `GameStateManager.cs` | 상태 전환 + 씬 이동 + 최고점수 저장 | Singleton, SRP |
| `ScoreSystem.cs` | 점수 / 처치 수 집계 | Singleton, SRP |
| `ExpSystem.cs` | 경험치 / 레벨업 처리 | Singleton, SRP |
| `WaveSpawner.cs` | 가중치 기반 적 스폰 | OCP (ScriptableObject 배열) |
| `CameraFollow.cs` | 플레이어 추적 카메라 | — |
| `SceneBootstrapper.cs` | 씬 시작 시 레퍼런스 검증 | — |
| `ExpOrb.cs` | EXP 오브 동작 (자석 흡수, 픽업) | — |
| `NeonMaterialHelper.cs` | 네온 머티리얼 절차적 생성 | — |

#### GameStateManager 주요 메서드
```
SetState(GameState)       — 상태 전환 + Time.timeScale 제어
TriggerGameOver()         — 최고점수 저장 후 GameOver 상태
PauseForLevelUp()         — LevelUp 상태 (Time.timeScale = 0)
ResumeFromLevelUp()       — Playing 상태 복귀
RestartGame()             — EventBus.Clear() 후 씬 재로드
GoToMainMenu()            — MenuScene 로드
GetFormattedTime()        — "MM:SS" 형식 반환
```

---

### 4-4. Player

| 파일 | 역할 |
|---|---|
| `PlayerStats.cs` | 스탯 데이터 + 체력 관리 (Singleton) |
| `PlayerController.cs` | 이동 + 조준 + IDamageable 구현 |
| `PlayerShooter.cs` | 발사 로직 (일반/트리플/십자) |
| `KeyboardMouseInput.cs` | PC 입력 (WASD + 마우스 레이캐스트) |
| `MobileInput.cs` | 모바일 입력 (VirtualJoystick에서 주입) |
| `VirtualJoystick.cs` | 터치 조이스틱 UI |

#### PlayerStats 기본값
```
maxHealth = 100     moveSpeed = 8       bulletDamage = 25
fireRate = 0.2s     bulletSpeed = 20    bulletRange = 15
pierceCount = 0     magnetRadius = 3    damageMultiplier = 1
speedMultiplier = 1 fireRateMultiplier = 1
hasTripleShot = false  hasSplitShot = false
```

#### 입력 분기 (PlayerController.Awake)
```csharp
#if UNITY_ANDROID || UNITY_IOS
    input = new MobileInput();
#else
    input = new KeyboardMouseInput();
#endif
```

---

### 4-5. Enemy

| 파일 | 역할 | 패턴 |
|---|---|---|
| `EnemyHealth.cs` | IDamageable 구현, 사망 처리 | SRP |
| `EnemyMover.cs` | 플레이어 방향 이동 | SRP |
| `EnemyShooterBehavior.cs` | 원거리 발사 (Shooter 전용) | SRP |
| `EnemyContactDamage.cs` | 접촉 데미지 (OnCollisionStay) | SRP |
| `EnemyBullet.cs` | 적 총알 (IDamageable로 플레이어 피해) | — |
| `EnemySpawnConfig.cs` | ScriptableObject 스폰 설정 | OCP |

#### EnemySpawnConfig 필드
```
prefab          (GameObject)
baseHealth      float
baseSpeed       float
expReward       int
scoreReward     int
minSpawnTime    float   — 이 시간 이후부터 등장
spawnWeight     float   — 가중치 랜덤
```

#### 적 프리팹 & ScriptableObject
```
Assets/Prefabs/Enemies/
  Chaser.prefab      (빨간, baseHP=30, speed=4, minTime=0,  weight=3)
  Shooter.prefab     (주황, baseHP=40, speed=2.5, minTime=30, weight=1.5)
  BigChaser.prefab   (보라, baseHP=120, speed=2, minTime=60, weight=1, scale=1.8)

Assets/ScriptableObjects/EnemyConfigs/
  ChaserConfig.asset
  ShooterConfig.asset
  BigChaserConfig.asset
```

---

### 4-6. Abilities

**추상 기반**: `AbilityBase.cs` (IAbility 구현)  
- `Apply(PlayerStats)` → `OnApply()` 호출 후 `CurrentLevel++`  
- `CanOffer` → `CurrentLevel < MaxLevel`

**구현체 11종** (`Assets/Scripts/Abilities/Impl/`)

| 클래스 | 효과 | MaxLv |
|---|---|---|
| `DamageUpAbility` | 데미지 +30% (damageMultiplier) | 5 |
| `FireRateUpAbility` | 발사속도 +25% (fireRateMultiplier) | 5 |
| `SpeedUpAbility` | 이동속도 +20% (speedMultiplier) | 4 |
| `HealthUpAbility` | 최대체력 +30 (즉시 회복 포함) | 4 |
| `HealAbility` | 즉시 체력 +20 | 3 |
| `TripleShotAbility` | 3방향 발사 활성화 (hasTripleShot) | 1 |
| `SplitShotAbility` | 좌우 추가 발사 (hasSplitShot) | 1 |
| `PierceAbility` | 관통 +1 (pierceCount) | 3 |
| `MagnetAbility` | 자석반경 +50% (magnetRadius) | 3 |
| `BulletSpeedAbility` | 탄속 +30% (bulletSpeed) | 3 |
| `BulletRangeAbility` | 사거리 +40% (bulletRange) | 3 |

**AbilityManager**: EventBus.OnPlayerLevelUp 구독 → 랜덤 3개 추출 → AbilityChoiceReady 발행

---

### 4-7. UI

| 파일 | 역할 |
|---|---|
| `HUDManager.cs` | 체력바/EXP바/타이머/점수/레벨/처치수 갱신 (EventBus 구독) |
| `UIManager.cs` | LevelUp / GameOver 패널 관리 |
| `MenuManager.cs` | 메인메뉴 패널 전환 + 볼륨 설정 |
| `MobileControlsCanvas.cs` | 모바일 빌드에서만 활성화 |

HUDManager는 EventBus 이벤트만 구독 — 싱글턴 직접 참조 없음 (DIP 준수)

---

## 5. 에셋 구조

### 머티리얼
```
Assets/Materials/
  PlayerMat.mat       (네온 블루 #00BFFF + Emission)
  GroundMat.mat       (다크 #1A1A2E)

Assets/Prefabs/Enemies/
  ChaserMat.mat       (네온 레드 #FF2244)
  ShooterMat.mat      (네온 오렌지 #FF6600)
  BigChaserMat.mat    (네온 퍼플 #9933FF)

Assets/Prefabs/Projectiles/
  BulletMat.mat       (네온 시안 #00CCFF)
  EnemyBulletMat.mat  (네온 오렌지레드 #FF4400)
```

### 폰트
```
Assets/Fonts/
  Paperlogy-1Thin ~ 9Black.ttf   (원본 TTF 9종)

Assets/Fonts/TMP/
  Paperlogy-Regular SDF.asset    (4096×4096, SDFAA)
  Paperlogy-Bold SDF.asset       (4096×4096, SDFAA)
  Paperlogy-ExtraBold SDF.asset  (4096×4096, SDFAA)
```

**포함 문자**: 한글 음절 전체(가~힣 11,172자) + 한글 자모 + 영문 A-Za-z + 숫자 + 특수문자(!@#$%^&*...)

**적용 규칙**
- 타이틀/헤더류 → ExtraBold
- 점수/레벨/버튼 → Bold
- 그 외 일반 텍스트 → Regular
- TMP Settings 기본 폰트 → Regular

### ScriptableObjects
```
Assets/ScriptableObjects/EnemyConfigs/
  ChaserConfig.asset
  ShooterConfig.asset
  BigChaserConfig.asset
```

---

## 6. Build Settings

```
[0] Assets/Scenes/MenuScene/MenuScene.unity
[1] Assets/Scenes/GameScene/GameScene.unity
```

---

## 7. 현재 구현 완료 항목

| 카테고리 | 항목 | 상태 |
|---|---|---|
| 게임플레이 | 플레이어 이동 & 자동 발사 | ✅ |
| 게임플레이 | 트리플샷 / 십자샷 / 관통 | ✅ |
| 게임플레이 | EXP 오브 자동 흡수 (자석) | ✅ |
| 적 AI | Chaser / Shooter / BigChaser | ✅ |
| 적 AI | 웨이브 스포너 (난이도 스케일링) | ✅ |
| 시스템 | EXP / 레벨업 / 업그레이드 선택 | ✅ |
| 시스템 | 점수 / 처치 수 | ✅ |
| 시스템 | 게임 상태 FSM (Playing/LevelUp/Paused/GameOver) | ✅ |
| 시스템 | 최고 점수 저장 (PlayerPrefs) | ✅ |
| UI | HUD (체력/EXP/타이머/점수/레벨/처치) | ✅ |
| UI | 레벨업 패널 (3개 선택지) | ✅ |
| UI | 게임오버 패널 (결과 + 재시작/메뉴) | ✅ |
| UI | 메인 메뉴 (시작/설정/종료) | ✅ |
| UI | 볼륨 설정 (마스터/SFX 슬라이더) | ✅ |
| VFX | 총알 충돌 파티클 | ✅ |
| VFX | 적 사망 파티클 | ✅ |
| VFX | EXP 흡수 파티클 | ✅ |
| VFX | 레벨업 파티클 | ✅ |
| 오디오 | BGM (절차적 사이버펑크 앰비언트) | ✅ |
| 오디오 | SFX 7종 (발사/충돌/사망/레벨업/게임오버/EXP/피격) | ✅ |
| 입력 | PC (WASD + 마우스 조준) | ✅ |
| 입력 | 모바일 터치 (듀얼 가상 조이스틱) | ✅ |
| 폰트 | Paperlogy 한글+영문 TMP 폰트 에셋 | ✅ |
| 아키텍처 | SOLID + EventBus + ObjectPool | ✅ |

---

## 8. 미구현 / 다음 할 일

| 우선순위 | 항목 | 비고 |
|---|---|---|
| 높음 | 실제 비주얼 에셋 교체 | 현재 Primitive 큐브/구체 — 출시 필수 |
| 높음 | 보스 적 추가 | 2~3분마다 등장하는 강적 |
| 중간 | 추가 적 타입 (Splitter, Bomber) | EnemySpawnConfig 추가만 하면 됨 |
| 중간 | 챌린지 모드 / 데일리 챌린지 | 시드 기반 RNG |
| 중간 | 컨트롤러(게임패드) 지원 | IInputProvider 추가 구현체로 확장 가능 |
| 낮음 | Stove SDK 연동 | 앱 등록 + AppKey 발급 필요 |
| 낮음 | Steam SDK (Steamworks.NET) 연동 | 도전과제/리더보드 |
| 낮음 | Windows / Android 최종 빌드 패키징 | |

---

## 9. 아키텍처 설계 원칙 요약

### SRP (단일 책임)
- `GameManager` 1개 → `GameStateManager` + `ScoreSystem` + `ExpSystem` 3개로 분리
- `EnemyAI` 1개 → `EnemyMover` + `EnemyShooterBehavior` + `EnemyContactDamage` 3개로 분리

### OCP (개방/폐쇄)
- 새 능력 추가 = `AbilityBase` 상속 클래스 1개만 추가
- 새 적 추가 = `EnemySpawnConfig` ScriptableObject 에셋 1개만 추가 (코드 수정 없음)

### DIP (의존성 역전)
- `IDamageable` — 적 총알이 구체 타입 대신 인터페이스로 피해 처리
- `IInputProvider` — 플레이어가 PC/모바일 입력 구현체에 직접 의존하지 않음
- `EventBus` — UI/시스템 간 직접 참조 없이 이벤트로 통신

---

## 10. 주요 데이터 흐름

```
[PlayerShooter]
  Shoot() → AudioManager.PlayShoot()
          → Bullet.Init() → OnTriggerEnter(Enemy)
              → EnemyHealth.TakeDamage()
              → VFXManager.PlayBulletHit()
              → AudioManager.PlayBulletHit()

[EnemyHealth.Die()]
  → ScoreSystem.AddScore() / RegisterKill()
  → ExpSystem.AddExp()
  → VFXManager.PlayEnemyDeath()
  → AudioManager.PlayEnemyDeath()
  → ExpOrb.SpawnAt()

[ExpOrb.OnTriggerEnter(Player)]
  → VFXManager.PlayExpOrbAbsorb()
  → AudioManager.PlayExpPickup()
  → ExpSystem.AddExp()

[ExpSystem.AddExp()]  — 레벨업 시
  → GameStateManager.PauseForLevelUp()
  → EventBus.RaisePlayerLevelUp(level)

[EventBus.OnPlayerLevelUp]
  → AbilityManager.OnLevelUp()
  → EventBus.RaiseAbilityChoiceReady(choices)

[EventBus.OnAbilityChoiceReady]
  → UIManager: LevelUp 패널 표시

[AbilityManager.ApplyAbility()]
  → ability.Apply(PlayerStats)
  → VFXManager.PlayLevelUp()
  → AudioManager.PlayLevelUp()
  → GameStateManager.ResumeFromLevelUp()

[PlayerStats.TakeDamage()]
  → EventBus.RaisePlayerHealthChanged()
  → AudioManager.PlayPlayerHurt()
  → OnDeath() → GameStateManager.TriggerGameOver()

[GameStateManager.TriggerGameOver()]
  → PlayerPrefs 최고점수 저장
  → SetState(GameOver) → EventBus.RaiseGameOver()
  → AudioManager: BGM 정지 + PlayGameOver()
```

---

## 11. 파일 구조 전체

```
Assets/
├── Fonts/
│   ├── Paperlogy-1Thin.ttf ~ Paperlogy-9Black.ttf
│   └── TMP/
│       ├── Paperlogy-Regular SDF.asset
│       ├── Paperlogy-Bold SDF.asset
│       └── Paperlogy-ExtraBold SDF.asset
├── GDD.md
├── Materials/
│   ├── PlayerMat.mat
│   └── GroundMat.mat
├── Prefabs/
│   ├── Enemies/
│   │   ├── Chaser.prefab + ChaserMat.mat
│   │   ├── Shooter.prefab + ShooterMat.mat
│   │   └── BigChaser.prefab + BigChaserMat.mat
│   └── Projectiles/
│       ├── Bullet.prefab + BulletMat.mat
│       └── EnemyBullet.prefab + EnemyBulletMat.mat
├── Scenes/
│   ├── MenuScene/MenuScene.unity
│   └── GameScene/GameScene.unity
├── ScriptableObjects/
│   └── EnemyConfigs/
│       ├── ChaserConfig.asset
│       ├── ShooterConfig.asset
│       └── BigChaserConfig.asset
└── Scripts/
    ├── Abilities/
    │   ├── AbilityBase.cs
    │   └── Impl/
    │       ├── DamageUpAbility.cs
    │       ├── FireRateUpAbility.cs
    │       ├── SpeedUpAbility.cs
    │       ├── HealthUpAbility.cs
    │       ├── HealAbility.cs
    │       ├── TripleShotAbility.cs
    │       ├── SplitShotAbility.cs
    │       ├── PierceAbility.cs
    │       ├── MagnetAbility.cs
    │       ├── BulletSpeedAbility.cs
    │       └── BulletRangeAbility.cs
    ├── Core/
    │   ├── AudioManager.cs
    │   ├── EventBus.cs
    │   ├── ObjectPool.cs
    │   ├── PoolManager.cs
    │   └── VFXManager.cs
    ├── Enemy/
    │   ├── EnemyBullet.cs
    │   ├── EnemyContactDamage.cs
    │   ├── EnemyHealth.cs
    │   ├── EnemyMover.cs
    │   ├── EnemyShooterBehavior.cs
    │   └── EnemySpawnConfig.cs
    ├── Interfaces/
    │   ├── IAbility.cs
    │   ├── IDamageable.cs
    │   └── IInputProvider.cs
    ├── Player/
    │   ├── Bullet.cs
    │   ├── KeyboardMouseInput.cs
    │   ├── MobileInput.cs
    │   ├── PlayerController.cs
    │   ├── PlayerShooter.cs
    │   ├── PlayerStats.cs
    │   └── VirtualJoystick.cs
    ├── System/
    │   ├── CameraFollow.cs
    │   ├── ExpOrb.cs
    │   ├── GameState.cs
    │   ├── GameStateManager.cs
    │   ├── NeonMaterialHelper.cs
    │   ├── SceneBootstrapper.cs
    │   ├── ScoreSystem.cs
    │   ├── ExpSystem.cs
    │   └── WaveSpawner.cs
    └── UI/
        ├── HUDManager.cs
        ├── MenuManager.cs
        ├── MobileControlsCanvas.cs
        └── UIManager.cs
```

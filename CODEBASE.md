# Neon Rewind Arena — 코드베이스 문서

> 작성일: 2026-04-27  
> Unity 버전: URP / Unity 6  
> 네트워크: Unity Netcode for GameObjects  
> 입력: Unity Input System  
> UI: TextMeshPro  

---

## 목차

1. [프로젝트 개요](#1-프로젝트-개요)
2. [씬 구조](#2-씬-구조)
3. [아키텍처 원칙](#3-아키텍처-원칙)
4. [스크립트 전체 목록](#4-스크립트-전체-목록)
5. [Core 시스템](#5-core-시스템)
6. [Player 시스템](#6-player-시스템)
7. [Input 시스템](#7-input-시스템)
8. [Clone 시스템](#8-clone-시스템)
9. [Match 시스템](#9-match-시스템)
10. [Network 시스템](#10-network-시스템)
11. [UI 시스템](#11-ui-시스템)
12. [로컬 멀티플레이어](#12-로컬-멀티플레이어)
13. [씬별 Inspector 연결 정보](#13-씬별-inspector-연결-정보)
14. [주요 버그 수정 기록](#14-주요-버그-수정-기록)

---

## 1. 프로젝트 개요

**Neon Rewind Arena**는 Smash Bros 방식 넉백+낙사 시스템에 **분신(Clone) 리플레이** 메카닉을 더한 아레나 격투 게임입니다.

- 플레이어가 낙사하면 → 그 생 동안의 입력이 **분신**으로 재생
- 분신은 시간이 지날수록 빨라짐 (GDD 설계)
- 로컬 멀티(2~10명, 게임패드) + 네트워크 멀티(최대 4인) 동시 지원

---

## 2. 씬 구조

| 씬 | 역할 |
|---|---|
| `MenuScene` | 타이틀 화면 (로컬/네트워크 멀티 선택, 설정) |
| `ArenaScene` | 게임 플레이 (싱글 / 로컬 멀티 / 네트워크 멀티 공용) |

### ArenaScene 주요 GameObject 계층

```
ArenaScene
├── ArenaCamera          ArenaCamera(singleton)
├── MatchManager         MatchManager, ScoreSystem
├── CloneManager         CloneManager
├── RespawnManager       RespawnManager
├── AudioManager         AudioManager(DontDestroyOnLoad)
├── VFXManager           VFXManager
├── SceneBootstrapper    SceneBootstrapper
├── LocalArenaBootstrapper  LocalArenaBootstrapper  ← 로컬 멀티 전용
├── Platform             BoxCollider(tag: Ground)
├── SpawnPoints
│   ├── Spawn0 ~ Spawn3
├── DeathZone            BoxCollider(tag: DeathZone, isTrigger)
├── Player               PlayerStats, PlayerController, PlayerInput,
│                        GamepadInput, InputRecorder, KnockbackReceiver,
│                        DeathDetector, PlayerVisuals, Rigidbody, CapsuleCollider
└── Arena Canvas (UI)
    ├── HUDPanel         HUDManager
    ├── ScoreboardPanel  ScoreboardUI
    ├── KillFeedPanel    KillFeedUI
    ├── PausePanel       PauseManager
    └── ResultsPanel     ResultsPanel
```

### MenuScene 주요 GameObject 계층

```
MenuScene
├── Menu Canvas          MenuManager, NetworkLobbyUI
│   ├── Background
│   ├── MainPanel        (버튼: 로컬플레이, 멀티, 설정, 종료)
│   ├── SettingsPanel    (볼륨 슬라이더)
│   ├── LobbyPanel       NetworkLobbyUI (네트워크 멀티)
│   │   ├── ConnectPanel
│   │   └── WaitingPanel
│   └── LocalLobbyPanel  LocalLobbyUI (로컬 멀티)
│       └── Box          (인원 선택 ±, 장치 정보, 시작/뒤로)
└── NetworkManager       NeonNetworkManager, UnityTransport
```

---

## 3. 아키텍처 원칙

### SRP (단일 책임)
각 스크립트는 하나의 역할만 담당합니다.

| 스크립트 | 담당 |
|---|---|
| `PlayerStats` | 스탯·넉백 수치 |
| `PlayerController` | 이동·점프·대시·공격 실행 |
| `KnockbackReceiver` | 넉백 물리 처리 |
| `DeathDetector` | 낙사 감지 |
| `PlayerVisuals` | 색상·플래시·깜빡이기 |
| `InputRecorder` | 입력 프레임 기록 |

### DIP (의존성 역전)
`PlayerController`는 `IInputProvider` 인터페이스에만 의존합니다.  
런타임에 `PlayerInput` → `GamepadInput` → `CloneInput`으로 교체 가능합니다.

### EventBus (이벤트 채널)
싱글턴 직접 참조 대신 `EventBus` 정적 클래스를 통해 이벤트를 발행/구독합니다.  
씬 전환 시 `EventBus.Clear()`를 호출해 메모리 누수를 방지합니다.

---

## 4. 스크립트 전체 목록

```
Assets/Scripts/
├── Core/
│   ├── AudioManager.cs
│   ├── EventBus.cs
│   ├── ObjectPool.cs
│   └── VFXManager.cs
├── Input/
│   ├── CloneInput.cs
│   ├── GamepadInput.cs
│   ├── IInputProvider.cs
│   ├── ISnapshotCapture.cs
│   └── PlayerInput.cs
├── Player/
│   ├── DeathDetector.cs
│   ├── InputFrame.cs
│   ├── InputRecorder.cs
│   ├── KnockbackReceiver.cs
│   ├── PlayerController.cs
│   ├── PlayerStats.cs
│   └── PlayerVisuals.cs
├── Clone/
│   ├── CloneController.cs
│   └── CloneManager.cs
├── Match/
│   ├── LocalArenaBootstrapper.cs
│   ├── MatchManager.cs
│   ├── MatchState.cs
│   ├── RespawnManager.cs
│   └── ScoreSystem.cs
├── Network/
│   ├── MatchNetworkManager.cs
│   ├── NeonNetworkManager.cs
│   └── PlayerNetworkSync.cs
├── System/
│   ├── LocalMultiplayerConfig.cs
│   └── SceneBootstrapper.cs
└── UI/
    ├── HUDManager.cs
    ├── KillFeedUI.cs
    ├── LocalLobbyUI.cs
    ├── MenuManager.cs
    ├── NetworkLobbyUI.cs
    ├── PauseManager.cs
    ├── ResultsPanel.cs
    └── ScoreboardUI.cs
```

---

## 5. Core 시스템

### EventBus.cs
```csharp
public static class EventBus
{
    public static event Action<MatchState>          OnMatchStateChanged;
    public static event Action                      OnMatchStarted;
    public static event Action<Dictionary<int,int>> OnMatchEnded;
    public static event Action<int, Vector3, int>   OnEntityDied;    // (entityId, pos, hitBy)
    public static event Action<int, int>            OnScoreChanged;  // (playerId, score)
    public static event Action<int, float>          OnKnockbackChanged; // (entityId, %)
    public static event Action<int>                 OnCloneSpawned;  // cloneCount

    // Raise helpers: RaiseMatchStateChanged, RaiseEntityDied, RaiseScoreChanged, ...
    public static void Clear(); // 씬 전환 시 반드시 호출
}
```

### AudioManager.cs
- `DontDestroyOnLoad` 싱글턴
- 절차적 BGM 생성 (사이버펑크 드론 + 글리치 게이트)
- SFX: 발사·충돌·사망·레벨업·게임오버·EXP·피격·대시·분신생성·리스폰
- `PlayerPrefs`의 `MasterVolume` / `SFXVolume` 반영
- `RefreshVolume()` — MenuManager 설정 슬라이더 연동

### VFXManager.cs
- 싱글턴. 프리팹이 없으면 코드로 파티클 즉석 생성
- `PlayAttack(pos)` — 공격 이펙트
- `PlayEnemyDeath(pos, color)` — 사망 이펙트
- `PlayLevelUp(pos)` — 리스폰/레벨업 링 이펙트

---

## 6. Player 시스템

### PlayerStats.cs
```
playerId     int     — 플레이어 식별자 (분신은 100+)
isClone      bool    — 분신 여부
moveSpeed    float   — 8f
jumpForce    float   — 10f
dashSpeed    float   — 20f
dashDuration float   — 0.14s
dashCooldown float   — 0.9s
attackPower  float   — 10f
attackDamage float   — 12f
attackRadius float   — 1.6f
attackRange  float   — 1.4f
attackCooldown float — 0.35s

knockbackPercent float  — 누적 넉백 % (Smash Bros 방식)
lastHitBy        int    — 마지막 공격자 ID
```

**핵심 메서드:**
- `AddKnockback(damage, attackerId)` — 넉백 누적 + EventBus 발행
- `GetKnockbackForce(basePower)` → `basePower × (1 + % / 60)`
- `StartInvincibility(duration)` — 무적 + 깜빡이기 시작
- `ResetKnockback()` — 리스폰 시 초기화

### PlayerController.cs
`IInputProvider` 기반 이동·점프·대시·공격. `Rigidbody` 직접 조작.

**[버그1 픽스]** 넉백 면역 타이머 (`_knockbackTimer = 0.4s`):  
`KnockbackReceiver.ApplyKnockback()` → `NotifyKnockedBack()` 호출 →  
`FixedUpdate`에서 이 시간 동안 velocity 덮어쓰기 차단.

**네트워크:** `IsOwner && IsSpawned`일 때 `AttackServerRpc` 전송, 싱글이면 로컬 처리.

### KnockbackReceiver.cs
- `ApplyKnockback(direction, basePower, attackerId)`
  1. `PlayerStats.IsInvincible`이면 완전 무시
  2. `AddKnockback` → `GetKnockbackForce` → `AddForce(Impulse)`
  3. `PlayerController.NotifyKnockedBack()` 호출
  4. `PlayerVisuals.PlayHitFlash()`
  5. force ≥ `shakeThreshold` → `ArenaCamera.Shake()`
  6. `AudioManager.PlayPlayerHurt()`

### DeathDetector.cs
- `deathY` 아래로 떨어지거나 `DeathZone` 트리거 진입 시 사망 처리
- **싱글/분신:** `EventBus.RaiseEntityDied`
- **네트워크(Owner):** 입력 기록(최대 2000프레임)과 함께 `DiedServerRpc` 전송

### PlayerVisuals.cs
플레이어 ID 기반 색상 팔레트:

| ID | 색상 |
|---|---|
| 0 | 네온 블루 `#00BFFF` |
| 1 | 네온 핑크 `#FF2D95` |
| 2 | 네온 퍼플 `#9933FF` |
| 3 | 네온 옐로우 `#FFD400` |

- `ApplyPlayerColor(playerId)` — 모든 Renderer의 Material에 색 적용
- `PlayHitFlash()` — 0.08초 흰색 번쩍
- `StartInvincibilityBlink()` / `StopInvincibilityBlink()` — 0.1초 간격 깜빡이기
- 분신(`CloneController` 보유)은 자동 스킵

### InputRecorder.cs
- `ISnapshotCapture.GetSnapshot()` — `FixedUpdate`마다 `InputFrame` 기록
- `GetRecording()` — 현재까지 기록된 프레임 목록 반환 (복사본)
- `ClearRecording()` — 리스폰 시 초기화
- **[버그2 픽스]** `PlayerController`가 입력을 소비하기 **전에** 스냅샷 저장

---

## 7. Input 시스템

### IInputProvider 인터페이스
```csharp
public interface IInputProvider
{
    Vector2 GetMoveInput();
    Vector2 GetAimInput();
    bool    GetJumpDown();
    bool    GetDashDown();
    bool    GetAttackDown();
}
```

### ISnapshotCapture 인터페이스
```csharp
public interface ISnapshotCapture
{
    InputFrame GetSnapshot();
}
```

### InputFrame 구조체
```csharp
[System.Serializable]
public struct InputFrame : INetworkSerializable
{
    public float moveX, moveY;
    public float aimX,  aimZ;
    public bool  jumpDown, dashDown, attackDown;
}
```

### PlayerInput.cs (키보드+마우스)
- WASD 이동, 마우스 방향 조준, Space 점프, Shift 대시, LMB 공격
- `IInputProvider` + `ISnapshotCapture` 구현

### GamepadInput.cs (게임패드)
```csharp
public int gamepadIndex = 0; // 로컬 멀티: P2=0, P3=1, ...

// 버튼 매핑:
// Left Stick          → 이동
// Right Stick         → 조준
// South (A / Cross)   → 점프
// West  (X / Square)  → 대시
// Right Trigger       → 공격 (없으면 Right Shoulder)
```
`GetPad()` — `Gamepad.all[gamepadIndex]` 반환 (없으면 null)

### CloneInput.cs
기록된 `InputFrame` 목록을 순서대로 재생. `IsFinished`가 true가 되면 `CloneManager`가 풀로 반환.

---

## 8. Clone 시스템

### CloneManager.cs
- 싱글턴. 분신 오브젝트 풀 관리
- `SpawnClone(List<InputFrame> frames)` — `RespawnManager`에서 호출
  - 최대 `maxClones=8` 초과 시 가장 오래된 분신 제거
  - 분신 ID: 100번부터 시작
  - 색상: 네온 블루·핑크·퍼플·시안 순환
- `Update()` — 리플레이 완료된 분신 자동 회수 (**[버그6 픽스]**)

### CloneController.cs
- `Init(frames, spawnPos, cloneId, color)` — 분신 활성화
- **속도 가속** (GDD):

| 경과시간 | 배율 |
|---|---|
| 0초 | 1.0× |
| 60초 | 1.5× |
| 120초 | 2.2× |

- **[버그4 픽스]** Material을 `Init` 당 1회 생성 후 캐싱 — 풀 재사용 시 VRAM 누수 방지
- URP / Built-in RP 자동 감지 투명 Material 생성

---

## 9. Match 시스템

### MatchState.cs (enum)
```csharp
public enum MatchState { WaitingToStart, Countdown, Playing, Ended }
```

### MatchManager.cs
- 싱글턴. 타이머와 상태 전환만 담당
- `matchDuration=120s`, `countdownSeconds=3s`
- `RunMatch()` 코루틴: `Countdown` → `Playing` → `Ended`
- `RestartMatch()` — `EventBus.Clear()` + 씬 재로드
- `GoToMainMenu()` — MenuScene 로드

### ScoreSystem.cs
- 싱글플레이 전용 점수 집계
- 멀티플레이 중(`NetworkManager.IsListening`)이면 아무것도 하지 않음
- `RegisterHit(attackerId)` — +1점
- `OnEntityDied` — 처치자 +5점
- `BestScore` PlayerPrefs 저장

### RespawnManager.cs
- 싱글턴. 싱글/로컬 멀티 리스폰만 담당
- 멀티플레이 중이면 완전 비활성
- `RegisterPlayer(go)` — `LocalArenaBootstrapper`에서 등록
- `ClearPlayers()` — 재등록 전 초기화
- `OnEntityDied` → `InputRecorder.GetRecording()` → `CloneManager.SpawnClone()` → 코루틴으로 리스폰
- 리스폰 후: 위치 복원 + `ResetKnockback` + `ResetDead` + 1.5초 무적 + VFX/SFX

---

## 10. Network 시스템

### PlayerNetworkSync.cs
**NetworkVariable:**
- `NetKnockback` (float, Server write) — 넉백 % 동기화
- `NetScore` (int, Server write) — 점수 동기화
- `NetPlayerId` (int, Server write) — playerId 동기화

**ServerRpc:**
- `AttackServerRpc(aimDir)` — 서버 히트 판정 → `AttackVFXClientRpc`
- `DiedServerRpc(frames[], killerId)` — 분신 스폰 + 리스폰 처리
- `CloneKilledServerRpc()` — 분신 처치 점수 (+1)

**ClientRpc:**
- `SpawnCloneClientRpc(frames[], deathPos, cloneId)` — 모든 클라이언트 분신 생성
- `NotifyDiedClientRpc(victimId, pos, killerId)` — 사망 이벤트 브로드캐스트
- `AttackVFXClientRpc(center)` — VFX + 히트음
- `RespawnClientRpc(pos)` — 리스폰 처리

### NeonNetworkManager.cs
- `GetNextSpawnPoint()` — 라운드로빈 스폰 포인트 제공

### NetworkLobbyUI.cs
- Host 버튼 → `NetworkManager.StartHost()`
- IP 입력 + Join 버튼 → `NetworkManager.StartClient()`
- 최대 4인 (`ConnectionApprovalCallback`)
- 타임아웃: 10초
- `ShowLobby(onBack)` / `HideLobby()` — `MenuManager`에서 호출

---

## 11. UI 시스템

### HUDManager.cs
- 타이머, 넉백 % 바, 분신 수, 카운트다운, 리스폰 대기 표시
- 넉백 색상 단계: 흰색(0%) → 노랑(50%) → 주황(100%) → 빨강(150%)
- 멀티에서 `PlayerNetworkSync.OwnerClientId`로 로컬 플레이어 ID 지연 해석 (0.5초 간격, 10초 타임아웃)

### ScoreboardUI.cs
- 싱글: 행 1개 고정
- 멀티: 접속 플레이어 수만큼 동적 표시 (최대 4)
- 행 구성: `[색상 닷] [이름(YOU / P2 / ...)] [점수]`
- `EventBus.OnScoreChanged` 구독

### KillFeedUI.cs
- 처치/낙사 메시지를 라운드로빈 슬롯에 표시
- 메시지 형식:
  - 처치: `<color=#FF6060>Killer</color>  ▶  Victim`
  - 낙사: `Victim  낙사`
- 3초 표시 후 0.5초 페이드 아웃

### PauseManager.cs
- ESC 키 → 일시정지 토글
- 싱글플레이 전용 (`NetworkManager.IsListening`이면 비활성)
- 매치 종료 시 자동 unpause

### ResultsPanel.cs
- `EventBus.OnMatchEnded` → 패널 표시
- 점수 내림차순 정렬 → 순위 표시 (1st/2nd/3rd/4th + 메달)
- 내 항목 강조 (청록색 + `◀` 마커)
- BestScore PlayerPrefs 갱신
- 재시작 / 메뉴 버튼

### MenuManager.cs
흐름:
```
MainPanel
  ├── "로컬 플레이" → LocalLobbyPanel (localLobbyUI.ShowPanel)
  ├── "멀티플레이"  → LobbyPanel (lobbyUI.ShowLobby)
  ├── "설정"       → SettingsPanel
  └── "종료"       → Application.Quit
```
- 볼륨 슬라이더 → `PlayerPrefs` 저장 + `AudioManager.RefreshVolume()`
- BestScore 표시

---

## 12. 로컬 멀티플레이어

### 전체 흐름

```
MenuScene
  → "로컬 플레이" 버튼 클릭
  → LocalLobbyPanel 표시
  → 인원 선택 (2~10명, ±버튼)
  → 장치 정보 표시
     P1: 키보드+마우스
     P2+: 게임패드 0번, 1번, ...
  → "게임 시작" 클릭
  → LocalMultiplayerConfig.PlayerCount = N
  → LocalMultiplayerConfig.IsLocalMode = true
  → ArenaScene 로드

ArenaScene
  → LocalArenaBootstrapper.Start()
  → IsLocalMode가 true인 경우에만 실행
  → 기존 싱글 Player 오브젝트 제거
  → N명 스폰 (LocalPlayer 프리팹)
  → 입력 할당:
     playerIndex == 0 → PlayerInput 활성 (키보드+마우스)
     playerIndex >= 1 → GamepadInput 활성, gamepadIndex = playerIndex - 1
  → ArenaCamera.SetTargets() — 전원 중심점 추적
  → RespawnManager 등록
```

### LocalMultiplayerConfig.cs
```csharp
public static class LocalMultiplayerConfig
{
    public static int  PlayerCount { get; set; } = 2;
    public static bool IsLocalMode { get; set; } = false;
    public static void Reset() { PlayerCount = 2; IsLocalMode = false; }
}
```
씬 간 static으로 유지. `DontDestroyOnLoad` 불필요.

### LocalLobbyUI.cs
```
[Header] panel          — LocalLobbyPanel 루트 GameObject
[Header] minusButton    — 인원 감소 버튼
[Header] plusButton     — 인원 증가 버튼
[Header] countText      — 현재 인원 표시
[Header] deviceInfoText — 장치 정보 (P1: 키보드, P2+: 게임패드명 or "연결 안 됨 ⚠")
[Header] startButton    — 게임 시작
[Header] backButton     — 뒤로가기
```
인원 범위: Min=2, Max=10

### LocalArenaBootstrapper.cs
```
[Header] localPlayerPrefab — Assets/Prefabs/LocalPlayer.prefab
[Header] spawnPoints       — SpawnPoints 자식들 (Inspector 연결)
[Header] spawnY            — 스폰 Y좌표 (기본 2.5)
```
스폰 포인트가 부족하면 원형 자동 배치.

### ArenaCamera 다중 타겟
```csharp
public void SetTargets(Transform[] targets) // 로컬 멀티: 전원 중심 추적
public void SetTarget(Transform t)          // 싱글/네트워크: 단일 타겟
```
비활성 플레이어는 중심 계산에서 제외.

---

## 13. 씬별 Inspector 연결 정보

### ArenaScene

| GameObject | 컴포넌트 | 연결 필드 |
|---|---|---|
| ArenaCamera | ArenaCamera | target → Player |
| RespawnManager | RespawnManager | playerObject → Player, spawnPoints → Spawn0~3 |
| CloneManager | CloneManager | clonePrefab → Clone.prefab, spawnPoints → Spawn0~3 |
| SceneBootstrapper | SceneBootstrapper | spawnPoints → Spawn0~3 |
| LocalArenaBootstrapper | LocalArenaBootstrapper | localPlayerPrefab → LocalPlayer.prefab, spawnPoints → Spawn0~3 |
| HUDPanel | HUDManager | timerText, knockbackText, knockbackFill, cloneCountText, countdownText, respawnText |
| ScoreboardPanel | ScoreboardUI | rows[0~3] (각 ScoreRow: root, colorDot, nameText, scoreText) |
| KillFeedPanel | KillFeedUI | feedSlots[0~3] |
| PausePanel | PauseManager | pausePanel, resumeButton, menuButton |
| ResultsPanel | ResultsPanel | panel, titleText, rankTexts[0~3], bestScoreText, restartButton, menuButton |

**Rigidbody (Player):**
```
linearDamping: 4
interpolation: Interpolate
collisionDetectionMode: ContinuousDynamic
constraints: FreezeRotX + FreezeRotZ
```

**CapsuleCollider (Player):** height=2, radius=0.5, center=(0,0,0)

**BoxCollider (Platform):** size=(22,1,22), isTrigger=false  
**BoxCollider (DeathZone):** size=(200,10,200), isTrigger=true, tag=DeathZone

### MenuScene

| GameObject | 컴포넌트 | 연결 필드 |
|---|---|---|
| Menu Canvas | MenuManager | mainPanel, settingsPanel, lobbyUI, localLobbyUI, playSingleButton, playMultiButton, settingsButton, quitButton, bestScoreText, versionText, masterVolumeSlider, sfxVolumeSlider, settingsBackButton |
| Menu Canvas | NetworkLobbyUI | lobbyRoot, connectPanel, hostButton, ipInputField, joinButton, connectBackButton, connectStatusText, waitingPanel, playerCountText, waitingStatusText, startButton, waitingCancelButton |
| LocalLobbyPanel | LocalLobbyUI | panel, minusButton, plusButton, countText, deviceInfoText, startButton, backButton |

---

## 14. 주요 버그 수정 기록

| 번호 | 증상 | 원인 | 해결 |
|---|---|---|---|
| 버그1 | 넉백 직후 이동으로 바로 상쇄 | `FixedUpdate`가 velocity를 즉시 덮어씀 | `KnockbackImmuneDuration=0.4s` 타이머 추가 |
| 버그2 | 분신 점프/대시 누락 | 스냅샷이 입력 소비 후 저장 | `ISnapshotCapture.GetSnapshot()`을 소비 전에 저장 |
| 버그3 | 멀티에서 공격 판정 불일치 | Owner 로컬 판정 → 다른 클라이언트와 불일치 | `AttackServerRpc`로 서버 위임 |
| 버그4 | 분신 색상 VRAM 누수 | 풀 재사용마다 Material 새로 생성 | `Init` 당 1회 생성 후 캐싱 |
| 버그5 | 분신 입력 프레임 오프셋 | `Update` 기준 기록, `FixedUpdate` 기준 재생 | `CloneInput.Advance()`를 `FixedUpdate`에서 호출 |
| 버그6 | 대시/분신 SFX NullRef | `AudioClip` 미리 베이킹 안 함 | `_clipDash`, `_clipCloneSpawn` BakeAllSFX에 추가 |
| 버그7 | 플레이어 공중 부양 | `CapsuleCollider` 스케일(22,0.5,22) → 돔 형태 | Platform을 `BoxCollider`로 교체 |
| 버그8 | 지면 체크 실패 | `groundCheckDist=0.2`가 캡슐 하단에 못 미침 | `groundCheckDist=1.1`로 수정 |
| 버그9 | 플레이어 미끄러짐 | `linearDamping=0` | `linearDamping=4`로 수정 |
| 버그10 | PlayerSpawner 플레이모드 오류 | `EditorSceneManager.MarkSceneDirty` 플레이 중 호출 | `Application.isPlaying` 가드 추가 |

---

## 프리팹 목록

| 경로 | 설명 |
|---|---|
| `Assets/Prefabs/LocalPlayer.prefab` | 로컬 멀티 플레이어 프리팹 (ArenaScene Player에서 저장) |
| `Assets/Prefabs/NetworkPlayer.prefab` | 네트워크 플레이어 프리팹 |
| `Assets/Prefabs/Arena/Clone.prefab` | 분신 프리팹 |

---

*이 문서는 현재 구현 상태를 기준으로 작성되었습니다.*

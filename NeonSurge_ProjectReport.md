# Neon Rewind Arena — 프로젝트 전체 현황 보고서
> Unity 6 (6000.0.60f1) · C# · Unity Netcode for GameObjects  
> 작성일: 2026-04-10

---

## 1. 게임 개요

| 항목 | 내용 |
|---|---|
| 장르 | 3D 탑뷰 아레나 배틀 (로컬/온라인 멀티) |
| 레퍼런스 | Super Smash Bros. (넉백 %) + Stick Fight: The Game |
| 플랫폼 목표 | PC (Steam, Stove Store) |
| 세션 길이 | 2~5분 (매치 120초) |
| 아트 | 외부 에셋 없음 — Unity Primitive + Neon Material로 구현 |

### 핵심 루프
```
이동/점프/대시/공격 → 상대 넉백 % 축적 → 아레나 밖으로 날려 처치
  → 사망 시 입력 기록이 '분신'으로 소환 → 리스폰 후 반복
  → 120초 후 최다 처치자 승리
```

---

## 2. 기술 스택

| 항목 | 내용 |
|---|---|
| 엔진 | Unity 6 (6000.0.60f1) |
| 언어 | C# |
| 네트워크 | Unity Netcode for GameObjects |
| 아키텍처 원칙 | SOLID (SRP·OCP·DIP 중심) |
| 이벤트 시스템 | 정적 EventBus (컴포넌트 간 직접 참조 제거) |
| 오브젝트 풀 | ObjectPool (CloneController 재사용) |
| 입력 추상화 | IInputProvider (PlayerInput / CloneInput 분리) |
| UI | TextMeshPro (Paperlogy 폰트, 한글+영문) |
| 오디오 | 절차적 사인파 합성 (외부 파일 없음) |
| 저장 | PlayerPrefs (최고점수, 볼륨 설정) |

---

## 3. 씬 구성

### 3-1. MenuScene

**기능**
- 타이틀 표시, Host/Join 네트워크 로비
- 설정: 마스터 볼륨 슬라이더, SFX 볼륨 슬라이더

**스크립트**: `MenuManager.cs`, `NetworkLobbyUI.cs`

---

### 3-2. ArenaScene

**하이어라키**
```
ArenaScene
├── _Systems
│   ├── MatchManager        ← 타이머 & 상태 FSM
│   ├── RespawnManager      ← 사망/리스폰 처리
│   ├── CloneManager        ← 분신 생성 & 풀링
│   ├── ScoreSystem         ← 처치 수 집계
│   └── AudioManager        ← BGM + SFX
├── _Core
│   ├── NeonNetworkManager  ← NGO NetworkManager
│   ├── VFXManager
│   ├── ObjectPool
│   └── SceneBootstrapper
├── Arena
│   ├── Platform
│   └── SpawnPoints[]       (4개)
├── Player
│   ├── PlayerController
│   ├── PlayerStats
│   ├── PlayerInput
│   ├── InputRecorder
│   ├── DeathDetector
│   ├── KnockbackReceiver
│   └── PlayerNetworkSync
├── Main Camera             (ArenaCamera)
└── HUD Canvas              (HUDManager)
    ├── TimerText
    ├── ScoreText
    ├── KnockbackText + KnockbackFill
    ├── CloneCountText
    └── CountdownText
```

---

## 4. 스크립트 전체 목록

### 4-1. Interfaces

| 파일 | 역할 |
|---|---|
| `IInputProvider.cs` | `GetMoveInput()`, `GetJump()`, `GetDash()`, `GetAttack()` |

---

### 4-2. Core

| 파일 | 역할 | 비고 |
|---|---|---|
| `EventBus.cs` | 정적 이벤트 채널 | 씬 전환 시 `Clear()` 호출 |
| `ObjectPool.cs` | 제네릭 오브젝트 풀 | CloneController 재사용 |
| `VFXManager.cs` | 파티클 이펙트 재생 | 절차적 생성 |
| `AudioManager.cs` | BGM + SFX 재생 | 절차적 사인파 합성 |

#### EventBus 이벤트 목록
```
OnMatchStateChanged(MatchState)          — 매치 상태 변경
OnMatchStarted()                         — 매치 시작
OnMatchEnded(Dictionary<int,int>)        — 매치 종료 (playerId → score)
OnEntityDied(int, Vector3, int)          — 엔티티 사망 (id, pos, killerId)
OnScoreChanged(int, int)                 — 점수 변경 (playerId, score)
OnKnockbackChanged(int, float)           — 넉백 % 변경 (entityId, percent)
OnCloneSpawned(int)                      — 분신 소환 (현재 분신 수)
```

#### AudioManager SFX 메서드
```
PlayCloneSpawn()   — 분신 소환
PlayRespawn()      — 리스폰
PlayGameOver()     — 매치 종료
```

---

### 4-3. Match

| 파일 | 역할 | 패턴 |
|---|---|---|
| `MatchState.cs` | `enum MatchState { WaitingToStart, Countdown, Playing, Ended }` | — |
| `MatchManager.cs` | 카운트다운 + 타이머 + 상태 전환 + 씬 이동 | Singleton, SRP |
| `RespawnManager.cs` | 플레이어 사망 감지 → 분신 소환 → 리스폰 | Singleton, SRP |
| `ScoreSystem.cs` | 처치 수 집계 (적중 +1, 처치 +5) + 최고점수 저장 | Singleton, SRP |

#### MatchManager 주요 메서드
```
RunMatch()           — 코루틴: 카운트다운 → Playing 진입
EndMatch()           — 시간 종료 → Ended 상태 + MatchEnded 발행
RestartMatch()       — EventBus.Clear() 후 씬 재로드
GoToMainMenu()       — MenuScene 로드
GetFormattedTime()   — "MM:SS" 형식 반환
```

---

### 4-4. Player

| 파일 | 역할 |
|---|---|
| `PlayerController.cs` | 이동 / 점프 / 대시 / 공격 / 넉백 면역 처리 |
| `PlayerStats.cs` | 스탯 데이터 + 넉백 % 누적 관리 |
| `PlayerInput.cs` | PC 키보드/마우스 입력 (IInputProvider 구현) |
| `InputRecorder.cs` | FixedUpdate 마다 InputFrame 기록 |
| `InputFrame.cs` | 단일 프레임 입력 스냅샷 (이동/점프/대시/공격) |
| `DeathDetector.cs` | Y축 낙사 감지 → EntityDied 발행 |
| `KnockbackReceiver.cs` | 넉백 물리 힘 적용 |

#### PlayerStats 기본값
```
moveSpeed = 8        jumpForce = 10       dashSpeed = 20
dashDuration = 0.14s dashCooldown = 0.9s
attackPower = 10     attackDamage = 12    attackRadius = 1.6
attackRange = 1.4    attackCooldown = 0.35s
knockbackPercent = 0 (런타임, 넉백 피격 시 누적)
```

#### 넉백 힘 계산
```
knockbackForce = basePower × (1 + knockbackPercent / 60)
```

---

### 4-5. Clone

| 파일 | 역할 | 패턴 |
|---|---|---|
| `CloneManager.cs` | 분신 생성 & 풀링, 최대 8개 제한 | Singleton, SRP |
| `CloneController.cs` | 분신 초기화 (프레임 재생, 색상, ID 설정) | SRP |

#### CloneManager 분신 색상 (순환)
```
네온 블루   rgba(0.2, 0.2, 0.9, 0.5)
네온 핑크   rgba(0.9, 0.1, 0.5, 0.5)
네온 퍼플   rgba(0.6, 0.1, 0.9, 0.5)
네온 시안   rgba(0.1, 0.8, 0.9, 0.5)
```

#### 분신 흐름
```
플레이어 사망
  → RespawnManager → InputRecorder.GetRecording()
  → CloneManager.SpawnClone(frames)
  → CloneController.Init(frames, pos, id, color)
  → CloneInput(frames) → PlayerController 에 주입
  → 기록된 입력 재생 (FixedUpdate 단위)
```

---

### 4-6. Input

| 파일 | 역할 |
|---|---|
| `IInputProvider.cs` | 입력 추상 인터페이스 |
| `PlayerInput.cs` | 실제 키보드/마우스 입력 + `GetSnapshot()` |
| `CloneInput.cs` | 기록된 `InputFrame` 목록을 순서대로 재생 |

---

### 4-7. Network

| 파일 | 역할 |
|---|---|
| `NeonNetworkManager.cs` | NetworkManager 상속, 스폰 포인트 관리 |
| `MatchNetworkManager.cs` | 매치 시작/종료 네트워크 동기화 |
| `PlayerNetworkSync.cs` | 위치/회전 보간 동기화 + `DiedServerRpc` |

#### PlayerNetworkSync 핵심
- 낙사 감지 시 `DiedServerRpc(InputFrame[])` 호출
- 서버에서 `CloneManager.SpawnClone()` 및 리스폰 처리
- 최대 2,000 프레임으로 페이로드 제한

---

### 4-8. Arena

| 파일 | 역할 |
|---|---|
| `ArenaCamera.cs` | 고정 탑뷰 카메라 |

---

### 4-9. UI

| 파일 | 역할 |
|---|---|
| `HUDManager.cs` | 타이머 / 점수 / 넉백 % / 분신 수 갱신 (EventBus 구독) |
| `MenuManager.cs` | 메인메뉴 패널 전환 + 볼륨 설정 |
| `NetworkLobbyUI.cs` | Host / Join 버튼 + IP 입력 |
| `ResultsPanel.cs` | 매치 종료 시 점수 랭킹 표시 |

---

## 5. 에셋 구조

### 폰트
```
Assets/Fonts/
  Paperlogy-1Thin ~ 9Black.ttf   (원본 TTF 9종)

Assets/Fonts/TMP/
  Paperlogy-Regular SDF.asset    (4096×4096, SDFAA)
  Paperlogy-Bold SDF.asset       (4096×4096, SDFAA)
  Paperlogy-ExtraBold SDF.asset  (4096×4096, SDFAA)
```

**포함 문자**: 한글 음절 전체(가~힣) + 한글 자모 + 영문 A-Za-z + 숫자 + 특수문자

**적용 규칙**
- 타이틀/헤더 → ExtraBold
- 점수/레벨/버튼 → Bold
- 일반 텍스트 → Regular

---

## 6. Build Settings

```
[0] Assets/Scenes/MenuScene/MenuScene.unity
[1] Assets/Scenes/ArenaScene/ArenaScene.unity
```

---

## 7. 현재 구현 완료 항목

| 카테고리 | 항목 | 상태 |
|---|---|---|
| 게임플레이 | 플레이어 이동 / 점프 / 대시 | ✅ |
| 게임플레이 | 근접 공격 + 넉백 % 시스템 | ✅ |
| 게임플레이 | 낙사 감지 (DeathDetector) | ✅ |
| 게임플레이 | 넉백 물리 처리 (KnockbackReceiver) | ✅ |
| 게임플레이 | 넉백 면역 타이머 (ApplyKnockback 후 0.4초) | ✅ |
| 분신 | 입력 기록 (InputRecorder) | ✅ |
| 분신 | 분신 소환 & 재생 (CloneManager + CloneController) | ✅ |
| 분신 | 분신 색상 / ID 관리 (최대 8개, 순환 색상) | ✅ |
| 분신 | Material 캐싱 (풀 재사용 시 VRAM 누수 방지) | ✅ |
| 매치 | 카운트다운 → Playing → Ended FSM | ✅ |
| 매치 | 120초 타이머 | ✅ |
| 매치 | 리스폰 (2초 후 부활, 넉백 % 초기화) | ✅ |
| 시스템 | 점수 집계 (적중 +1, 처치 +5) | ✅ |
| 시스템 | 최고 점수 저장 (PlayerPrefs) | ✅ |
| 네트워크 | NGO 기반 Host-Client 구조 | ✅ |
| 네트워크 | 위치/회전 동기화 (PlayerNetworkSync) | ✅ |
| 네트워크 | 사망 시 입력 기록 ServerRpc 전송 | ✅ |
| UI | HUD (타이머 / 점수 / 넉백 % / 분신 수) | ✅ |
| UI | 카운트다운 텍스트 (READY → FIGHT) | ✅ |
| UI | 메인 메뉴 + 네트워크 로비 | ✅ |
| 오디오 | 절차적 오디오 (AudioManager) | ✅ |
| 폰트 | Paperlogy 한글+영문 TMP 폰트 에셋 | ✅ |
| 아키텍처 | SOLID + EventBus + ObjectPool | ✅ |

---

## 8. 미구현 / 다음 할 일

### 2단계 — 게임 완성
| 항목 | 비고 |
|---|---|
| 결과 패널 완성 | ResultsPanel — 순위 / 처치수 표시 |
| 아레나 낙사존 완성 | DeathZone 태그 콜라이더 (현재 Y축 수치만으로 감지) |
| 맵 1개 완성 | 원형 아레나 + 낙사존 |
| SFX 완성 | 공격 히트 / 분신 소환(왜곡음) / 리스폰 / 매치 종료 |

### 3단계 — 폴리시
| 항목 | 비고 |
|---|---|
| VFX — 공격 히트 / 분신 소환 / 낙사 폭발 | VFXManager 확장 |
| 카메라 쉐이크 | 강한 넉백 시, ArenaCamera 확장 |
| 분신 잔상 이펙트 | Trail Renderer 활용 |
| 시간 지날수록 분신 속도 증가 | CloneController 확장 |

### 4단계 — 멀티 & 출시
| 항목 | 비고 |
|---|---|
| 온라인 매치메이킹 | Unity Relay 서비스 연동 |
| 추가 맵 2종 | 점프 패드 맵, 회전 장애물 맵 |
| 서든데스 타이브레이커 | 동점 처리 |
| Steam / Stove SDK 연동 | 리더보드, 도전과제 |

---

## 9. 아키텍처 설계 원칙 요약

### SRP (단일 책임)
- `MatchManager` — 타이머 & 상태 전환만
- `RespawnManager` — 리스폰만
- `CloneManager` — 분신 생성 & 풀링만
- `ScoreSystem` — 점수 집계만
- `DeathDetector` — 낙사 감지만
- `KnockbackReceiver` — 물리 힘 적용만
- `InputRecorder` — 프레임 기록만

### DIP (의존성 역전)
- `IInputProvider` — PlayerController가 PlayerInput/CloneInput에 직접 의존하지 않음
- `EventBus` — 매니저/UI 간 직접 참조 없이 이벤트로 통신

### OCP (개방/폐쇄)
- 새 입력 타입 추가 = `IInputProvider` 구현체 1개만 추가
- 새 이벤트 채널 추가 = EventBus에 static event 1개 추가

---

## 10. 주요 데이터 흐름

```
[플레이어 공격]
  PlayerController.Attack()
    → OverlapSphere로 적중 대상 탐색
    → KnockbackReceiver.ApplyKnockback()
    → PlayerStats.AddKnockback(damage, attackerId)
    → EventBus.RaiseKnockbackChanged()
    → ScoreSystem.RegisterHit(attackerId)

[플레이어 낙사]
  DeathDetector.TriggerDeath()
    → gameObject.SetActive(false)
    → [싱글] EventBus.RaiseEntityDied(id, pos, hitBy)
    → [멀티] PlayerNetworkSync.DiedServerRpc(frames)

[EventBus.OnEntityDied — 플레이어(id=0)]
  → ScoreSystem: killerId에 +5점
  → RespawnManager.OnEntityDied()
      → InputRecorder.GetRecording() → CloneManager.SpawnClone(frames)
      → InputRecorder.ClearRecording()
      → StartCoroutine(DoRespawn) — 2초 후 부활

[CloneManager.SpawnClone(frames)]
  → CloneController.Init(frames, pos, id, color)
  → CloneInput(frames) → PlayerController.SetInputProvider()
  → EventBus.RaiseCloneSpawned(count)

[MatchManager 시간 종료]
  → SetState(Ended)
  → ScoreSystem.GetAllScores() → EventBus.RaiseMatchEnded(scores)
  → AudioManager.PlayGameOver()
  → ResultsPanel 표시
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
│   └── (네온 Primitive 머티리얼)
├── Prefabs/
│   ├── Player.prefab
│   └── Clone.prefab
├── Scenes/
│   ├── MenuScene/MenuScene.unity
│   └── ArenaScene/ArenaScene.unity
└── Scripts/
    ├── Arena/
    │   └── ArenaCamera.cs
    ├── Clone/
    │   ├── CloneController.cs
    │   └── CloneManager.cs
    ├── Core/
    │   ├── AudioManager.cs
    │   ├── EventBus.cs
    │   ├── ObjectPool.cs
    │   └── VFXManager.cs
    ├── Input/
    │   ├── CloneInput.cs
    │   ├── IInputProvider.cs
    │   └── PlayerInput.cs
    ├── Match/
    │   ├── MatchManager.cs
    │   ├── MatchState.cs
    │   ├── RespawnManager.cs
    │   └── ScoreSystem.cs
    ├── Network/
    │   ├── MatchNetworkManager.cs
    │   ├── NeonNetworkManager.cs
    │   └── PlayerNetworkSync.cs
    ├── Player/
    │   ├── DeathDetector.cs
    │   ├── InputFrame.cs
    │   ├── InputRecorder.cs
    │   ├── KnockbackReceiver.cs
    │   ├── PlayerController.cs
    │   └── PlayerStats.cs
    ├── System/
    │   └── SceneBootstrapper.cs
    └── UI/
        ├── HUDManager.cs
        ├── MenuManager.cs
        ├── NetworkLobbyUI.cs
        └── ResultsPanel.cs
```

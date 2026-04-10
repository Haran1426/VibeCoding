# Neon Rewind Arena — Unity 씬 설정 가이드

---

## 1. 씬 목록 (Build Settings)

```
[0] Assets/Scenes/MenuScene/MenuScene.unity
[1] Assets/Scenes/ArenaScene/ArenaScene.unity
```

---

## 2. ArenaScene 오브젝트 연결

### _Systems 빈 오브젝트 구성

| 컴포넌트 | 연결할 필드 |
|---|---|
| `MatchManager` | — |
| `RespawnManager` | Respawn Delay=2, Player Object→Player, Spawn Points[]→4개 |
| `CloneManager` | Clone Prefab→Clone.prefab, Max Clones=8, Spawn Points[]→4개 |
| `ScoreSystem` | — |
| `AudioManager` | — |

### _Core 빈 오브젝트 구성

| 컴포넌트 | 연결할 필드 |
|---|---|
| `NeonNetworkManager` | Player Prefab→Player.prefab, Spawn Points[]→4개 |
| `VFXManager` | — |
| `ObjectPool` | — |
| `SceneBootstrapper` | — |

---

## 3. Player 프리팹 컴포넌트

```
Player (GameObject)
├── Rigidbody           (Use Gravity=true, Freeze Rotation XZ)
├── CapsuleCollider     (높이 2, 반지름 0.5)
├── PlayerController    (Attack Target Mask=Player+Clone Layer)
├── PlayerStats         (playerId=0, isClone=false)
├── PlayerInput
├── InputRecorder
├── DeathDetector       (Death Y=-8, Death Zone Tag="DeathZone")
├── KnockbackReceiver
└── PlayerNetworkSync   (NGO NetworkObject 필요)
```

---

## 4. Clone 프리팹 컴포넌트

```
Clone (GameObject)
├── Rigidbody           (Use Gravity=true)
├── CapsuleCollider
├── PlayerController
├── PlayerStats         (isClone=true 고정)
├── CloneController     (Init에서 자동 설정)
├── DeathDetector
└── KnockbackReceiver
```

> `CloneInput`은 `CloneController.Init()` 호출 시 코드에서 생성 & 주입됩니다. Inspector 연결 불필요.

---

## 5. HUD Canvas 구조

```
HUD Canvas (Screen Space - Overlay)
├── TimerText       (TextMeshProUGUI) ← HUDManager.timerText
├── ScoreText       (TextMeshProUGUI) ← HUDManager.scoreText
├── KnockbackText   (TextMeshProUGUI) ← HUDManager.knockbackText
├── KnockbackFill   (Image, Fill)     ← HUDManager.knockbackFill
├── CloneCountText  (TextMeshProUGUI) ← HUDManager.cloneCountText
└── CountdownText   (TextMeshProUGUI) ← HUDManager.countdownText
```

---

## 6. 레이어 & 태그 설정

| 항목 | 값 |
|---|---|
| Player 레이어 | `Player` |
| Clone 레이어 | `Clone` |
| Arena 낙사존 태그 | `DeathZone` |
| Attack Target Mask | `Player` + `Clone` 레이어 포함 |

---

## 7. 네트워크 테스트 (로컬)

1. File > Build Settings → Build (Windows)
2. 빌드 실행 → **Host** 버튼 클릭
3. Unity Editor에서 **Play** → **Join** 버튼 클릭 + IP `127.0.0.1` 입력
4. 두 클라이언트 모두 ArenaScene 진입 확인

---

## 8. 빌드 설정 (Steam / Stove)

- **Platform**: PC, Mac & Linux Standalone
- **Target**: Windows x86_64
- **Company Name**: (팀명)
- **Product Name**: Neon Rewind Arena
- **Version**: 0.1.0

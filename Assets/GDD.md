# Neon Rewind Arena — Game Design Document
> Version 0.3 | Last Updated: 2026-05-15

---

## 1. 컨셉

**죽을수록 과거의 내가 적으로 남는 멀티 PvP 난투 게임**

> "이거 내가 한 짓인데 왜 이렇게 어려워짐?"

- 플레이어는 계속 리스폰됨
- 죽을 때마다 자신의 행동이 **"분신"** 으로 아레나에 남음
- 시간이 지날수록 전장이 점점 혼란스러워짐

### 핵심 훅 (USP)

| 포인트 | 설명 |
|---|---|
| PvP + 시간 누적 구조 | 한 판 안에서 난이도가 계속 상승 |
| 자작 패턴과의 싸움 | 내가 만든 행동 기록이 나를 공격함 |
| 웃기는 혼돈 | 잘할수록 오히려 불리해지는 역설 |

**한 줄**: 플레이할수록 적이 늘어나는 PvP

---

## 2. 게임 구조

### 한 판 흐름

```
매치 시작 (카운트다운 3초)
    ↓
플레이어 전투
    ↓
사망
    ↓
분신 생성 (직전 라이프 행동 기록 → 그대로 재생)
    ↓
리스폰 (2초 후, 넉백 % 초기화)
    ↓
반복 → 시간 종료 (120초)
    ↓
점수 정산
```

### 승리 조건

- 제한 시간 종료 시 **점수 1등**

### 점수 획득

| 조건 | 점수 |
|---|---|
| 플레이어 처치 | +5 |
| 분신 처치 | +1 |
| 낙사 유도 | 처치로 인정 |

---

## 3. 핵심 시스템

### 3-1. 입력 기록 시스템 (`InputRecorder`)

플레이어의 모든 행동을 `FixedUpdate` 기준으로 프레임마다 저장

```csharp
struct InputFrame
{
    Vector3 move;
    bool    attack;
    bool    dash;
    bool    jump;
    Vector3 aim;
}
```

- 라이프 시작 시 초기화, 라이프 동안 계속 누적
- 사망 시 기록된 프레임 목록을 `CloneManager`에 전달

---

### 3-2. 분신(Clone) 시스템 (`CloneManager`)

**생성 조건**: 플레이어 사망 시

**특징**
- 기록된 입력 그대로 재생 (AI 없음 — 순수 리플레이)
- 플레이어가 공격 가능 (점수 획득 대상)
- 반투명 + 어두운 네온 색으로 플레이어와 구분

**제한**

| 항목 | 값 |
|---|---|
| 최대 분신 수 | 8 |
| 초과 시 처리 | 가장 오래된 분신 자동 제거 |
| 분신 ID | 100번부터 부여 (플레이어: 0~99) |

---

### 3-3. 분신 재생 (`CloneController`)

```csharp
// Update 마다 기록된 프레임 순서대로 입력 주입
ApplyInput(recordedFrames[currentFrame]);
```

- `PlayerController`를 그대로 재사용
- `IInputProvider`만 `CloneInput`으로 교체 (DIP)
- 기존 이동 / 공격 / 대시 / 점프 로직 동일하게 작동

---

## 4. 전투 시스템

### 기본 액션

| 액션 | 설명 |
|---|---|
| 이동 | WASD |
| 점프 | Space |
| 대시 | Shift (이동 방향, 쿨다운 0.9초) |
| 공격 | LMB / F (근접, 쿨다운 0.35초) |

### 설계 방향

- ❌ 체력 깎는 전투 X
- ✅ **넉백 / 위치 싸움 중심** (Smash Bros 방식)

### 넉백 시스템

- 피격 시 `knockbackPercent` 누적 (초기값 0)
- 실제 날아가는 힘 = `basePower × (1 + knockbackPercent / 60)`
- % 가 높을수록 작은 공격에도 낙사
- 리스폰 시 0으로 초기화

---

## 5. 맵 디자인

### 원칙

- 단순하게
- **낙사 존재** (핵심 메커니즘)
- 기믹 1~2개만

### 맵 예시

| 맵 | 구조 | 기믹 |
|---|---|---|
| 원형 아레나 | 원형 플랫폼, 바깥 낙사존 | 일부 바닥 붕괴 |
| 점프 패드 맵 | 중소형 플랫폼 다수 | 점프 패드로 공중 이동 |
| 회전 장애물 맵 | 사각 아레나 | 회전 장애물이 플레이어 밀어냄 |

---

## 6. 아트 스타일

### 방향

- 저폴리 / Primitive 유지 (외부 에셋 없음)
- **네온 + 다크 배경**

### 색감

| 오브젝트 | 색상 |
|---|---|
| 배경 / 바닥 | 거의 검정 `#0A0A0A` |
| 플레이어 1 | 네온 블루 `#00BFFF` |
| 플레이어 2 | 네온 핑크 `#FF2D95` |
| 플레이어 3 | 네온 퍼플 `#9933FF` |
| 플레이어 4 | 네온 옐로우 `#FFD400` |
| 분신 (공통) | 해당 플레이어 색 — 반투명 + 글로우 약하게 |
| UI 라인 | 네온 화이트 `#E0E0FF` |

> 한눈에 플레이어 / 분신 구분 가능해야 함

---

## 7. 사운드 방향

| 상황 | 사운드 |
|---|---|
| 공격 히트 | 짧고 강한 클릭/충격음 |
| 사망 | 낮은 톤, 낙하음 |
| 분신 생성 | 왜곡된 리버브 사운드 (시간 왜곡 느낌) |
| 리스폰 | 밝은 아르페지오 |
| 매치 종료 | 하강 4음 |
| BGM | 다크 사이버펑크 앰비언트 |

---

## 8. UI

### 필수 요소

| 요소 | 위치 |
|---|---|
| 남은 시간 | 상단 중앙 |
| 내 점수 | 상단 우측 |
| 넉백 % 게이지 | 하단 좌측 |
| 현재 분신 수 | 하단 좌측 |
| 카운트다운 텍스트 | 화면 중앙 |

### 스타일

- 미니멀
- 네온 라인 UI
- 텍스트: Paperlogy Bold / ExtraBold

---

## 9. 기술 구조

### 기존 시스템 활용

| 시스템 | 용도 |
|---|---|
| `PlayerController` | 플레이어 / 분신 공통 사용 |
| `EventBus` | 상태 관리 (사망/점수/분신) |
| `ObjectPool` | 분신 풀링 |

### 추가 구조

```
Player
  └── InputRecorder    ← 매 프레임 InputFrame 저장
        ↓ 사망 시
  CloneManager         ← 분신 생성 / 최대 수 관리
        ↓
  CloneController      ← PlayerController에 CloneInput 주입
        ↓
  재생 (기록된 프레임 순서대로)
```

### 멀티 구조 방향

- Host-Client (Unity Netcode for GameObjects)
- **입력만 동기화** — 분신은 로컬에서 재생, 최소 데이터만 공유
- `DiedServerRpc(InputFrame[])` 로 기록 전송 후 서버에서 분신 스폰

---

## 10. 개발 우선순위

### 1단계 — 핵심 (지금)
- [x] 이동 / 점프 / 대시 / 공격
- [x] 죽음 + 리스폰
- [x] 입력 기록 (`InputRecorder`)
- [x] 분신 생성 + 재생 (`CloneManager`, `CloneController`)
- [x] 넉백 시스템 (Smash Bros 방식)
- [x] 매치 타이머 & FSM

### 2단계 — 게임 완성
- [x] 점수 시스템
- [x] HUD (타이머 / 점수 / 넉백 % / 분신 수)
- [ ] 결과 패널 (점수 랭킹)
- [ ] 맵 1개 완성 (낙사존 포함)
- [ ] 오디오 완성

### 3단계 — 폴리시
- [ ] VFX (공격 히트 / 분신 소환 / 낙사)
- [ ] 카메라 쉐이크
- [ ] 분신 잔상 이펙트
- [ ] 시간 지날수록 분신 속도 증가

### 4단계 — 멀티 & 출시
- [ ] 온라인 매치메이킹
- [ ] 추가 맵 2종
- [ ] 밸런싱
- [ ] Steam / Stove 출시

---

## 11. 확장 요소 (추후)

| 아이디어 | 설명 |
|---|---|
| 분신 속도 증가 | 시간 지날수록 분신이 빨라짐 |
| 분신 강화 | 누적될수록 공격력 증가 |
| 랜덤 룰 변경 | 라운드마다 새 규칙 등장 |
| 추가 모드 | 라운드제, 팀전, 서든데스 |

---

## 최종 한 줄

> **"내 플레이가 적이 되는 게임"**
## UI / Visual Direction Update - 2026-05-12

### Reference Direction

- Overall UI should use a playful, readable, rounded party-brawler style inspired by Fall Guys.
- This is a reference for design language only, not a direct copy.
- Avoid flat rectangular debug-looking panels. Use chunky rounded panels, thick bright outlines, soft shadows, high contrast, and candy-like color blocking.
- The game should feel approachable and toy-like even while supporting online competitive brawler rules.

### Arena HUD Requirements

- Timer: large rounded capsule at top center, readable at a glance.
- Player danger/knockback: bottom-left rounded card with a clear `DANGER` label, large percent text, and a fill bar.
- Scoreboard: top-right rounded card with clear `SCORE` header, player colors, and enough spacing to scan during combat.
- Pause/results screens: large centered rounded modal panels, bright color identity, no bare black rectangles.
- Buttons: rounded capsule buttons with accent stripe, hover/pressed colors, white text, and readable shadow.

### Daily Maintenance Note

- Whenever UI, gameplay, scene layout, lobby flow, online behavior, or release-readiness changes are made, summarize the meaningful changes in this document once per day.
- Keep updates short and concrete: what changed, why it matters, and any remaining blocker.

---

## Daily Notes

### 2026-05-15

- **Menu readability fix**: Menu flow now rebuilds the title/lobby UI at runtime and uses the working title `REWIND RUMBLE`. Critical labels/buttons get a font-independent `PixelTextGraphic` fallback, so Host/Join/Settings/Quit remain readable even if TMP font rendering breaks.
- **Bug fix / movement tuning**: Fixed rounded UI panels missing `CanvasRenderer` by requiring/adding it for `RoundedRectGraphic`, disabling panel raycast targets except real buttons, and repairing authored/runtime rounded panel creation. Player movement defaults were raised to `moveSpeed=10.5`, `jumpForce=11`, `dashSpeed=26` across scene/player prefabs/clones for snappier brawler control.
- **Spawn/drop safety**: Network respawn fallback now also uses `Y=8`, and `DeathDetector` ignores death during countdown plus a short spawn grace window so the entry drop does not immediately count as a fall death before `FIGHT!`. If a player slips below the death plane before death is active, they are lifted back to the drop height instead of being killed on match start.
- **Entry drop tuning**: Arena player spawn points now start at `Y=8` so players fall into the arena during the `3-2-1` countdown before controls unlock on `FIGHT!`. Online, runtime-repaired, local, and fallback spawn paths use the same height; scene spawn objects use the registered `SpawnPoint` tag.
- **UI spacing / overlap pass**: Runtime title, lobby, and arena HUD layouts were re-spaced so cards, labels, buttons, score rows, and kill feed messages keep readable margins instead of stacking on top of each other at 16:9. The title/lobby right-side placeholder bars were replaced with rounded arena showcase cards that communicate drop-in starts, ringouts, replay clones, and 4-player online play.
- **Online end feedback cleanup**: `MatchNetworkManager` no longer plays an additional game-over clip after broadcasting match results. Online match end audio now routes through `GameFeelDirector` only, preventing stacked end stingers while keeping local/offline `MatchManager` game-over fallback intact.
- **Validation (debug build)**: `dotnet build VibeCoding.sln --no-restore` passes with the existing Unity/MCP `System.Net.Http` and `System.IO.Compression` warnings.
- **Change status (since 2026-05-14 run)**: No new commits landed; the game-feel + HUD/killfeed/results polish is still in the working tree, so it’s not yet protected by version control or visible to collaborators.
- **Validation (re-run)**: `dotnet build VibeCoding.sln -c Release --no-restore` still fails with `MSB4184` due to access denied on `C:\\Users\\sdh24\\AppData\\Local\\Microsoft SDKs` (environment/permission issue). This blocks CI-style sanity checks from the CLI and needs resolving before release hardening.
- **Remaining blockers / next smoke tests**: remove any tracked `.dotnet-home` telemetry files from git (keep `.dotnet-home/` ignored), run `Tools/Neon Rewind/Validate Release Scenes`, then do a quick host+client smoke test focusing on KO feedback (slow-mo rules + clone filtering), results reveal timing during slow motion, and match-end flow.

### 2026-05-14

- **UI style consistency (runtime + authored)**: `RuntimeUIFactory` now defaults to rounded panels and capsule-style buttons via `RoundedRectGraphic` (shadows, outlines, accent strip, brighter palette), and `UIScenePolisher` updates the menu/lobby preview cards + sliders + input styling to match. This reduces “flat debug UI” regressions when UI is rebuilt at runtime or regenerated in scenes.
- **Runtime fallback bug fix**: `MenuManager` slider handles now target `Graphic` instead of assuming `Image`, which keeps settings sliders functional after rounded runtime panels replaced square images.
- **Arena HUD fallback**: `HUDManager` now creates a rounded timer capsule, rounded player status card, `DANGER` label, and filled knockback bar only when authored HUD references are missing. This improves fallback readability without duplicating UI in authored scenes.
- **Game-feel pass**: Added `GameFeelDirector` to coordinate kill and match-end feedback. Credited kills now trigger stronger camera shake, finisher SFX, colored burst/ring/confetti VFX, and offline-only micro slow motion; match end triggers safe post-game slow motion, camera shake, celebration VFX, and a stinger.
- **Game-feel tuning**: Clone deaths are now filtered out of full finisher treatment in `GameFeelDirector`; clones keep the lightweight death burst/hit feedback while real player KOs get the large camera/VFX/audio payoff. This avoids visual noise when multiple replay clones are cleared quickly.
- **Audio/BGM pass**: `AudioManager` now has finisher, ring-out, and match-end stinger procedural SFX plus a new 8-second arcade brawler BGM loop with kick, clap, bass, hats, and a gated lead line.
- **UI feedback pass**: `KillFeedUI` now pops KO messages using unscaled time, and `ResultsPanel` pops the result modal and reveals ranked rows sequentially so the end screen feels like an actual match payoff during slow motion.
- **Release readiness / repo hygiene**: `.dotnet-home` telemetry artifacts are still being generated locally; ensure the folder stays ignored so it doesn’t re-enter source control.
- **Validation**: `dotnet build VibeCoding.sln --no-restore` passes. Remaining warnings are the existing Unity/MCP `System.Net.Http` and `System.IO.Compression` assembly version conflicts.

### 2026-05-13

- **UI visual polish pass**: `UIScenePolisher` now uses `RoundedRectGraphic` for more menu/lobby surfaces: arena preview card, rules strip, connect/waiting lobby cards, side preview, sliders, input fields, and mini-map preview shapes. This continues the rounded party-brawler direction and removes more flat debug-style rectangles from authored UI generation.
- **Online lobby flow / UI**: `NetworkLobbyUI` gained a clearer host/join-by-IP flow (timeout, cancel/back, connection approval, min players gate) and can rebuild lobby UI at runtime via `RuntimeUIFactory` + `RoundedRectGraphic` (less dependence on fragile authored canvases).
- **Online match behavior**: `MatchNetworkManager` now syncs match state + remaining time via `NetworkVariable`, and broadcasts final scores at match end; `MatchManager` disables itself when a network match is running (prevents double-timers).
- **Arena / scene stability**: `ArenaMapRuntimeBuilder` can build/repair the baseline arena at runtime (platforms, rails, jump pads, impulse gates, spinner hazard, death zone, spawn points) to reduce scene-wiring breakage during online tests.
- **Release readiness**: new editor-side `ReleaseSceneValidator` + scene polishers; Build Settings appear tightened to `MenuScene` + `ArenaScene`; `SceneRedirector` helps recover from landing in the wrong scene.
- **Remaining blockers**: `.dotnet-home` telemetry/sentinel files were committed (should be ignored/removed); run `Tools/Neon Rewind/Validate Release Scenes` and do a quick host+client smoke test to confirm scene-load + end-match UX.

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SRP: 매 FixedUpdate 마다 InputFrame 을 기록합니다.
///
/// [버그2 픽스]
/// IInputProvider 대신 PlayerInput.GetSnapshot() 을 사용합니다.
/// PlayerController 가 Update 에서 버튼 입력을 소비하기 전에
/// PlayerInput 이 스냅샷을 저장하므로 점프/대시/공격이 정확히 기록됩니다.
/// </summary>
public class InputRecorder : MonoBehaviour
{
    private PlayerInput              _playerInput;
    private readonly List<InputFrame> _frames = new List<InputFrame>(4096);
    private bool _recording;

    void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
    }

    void OnEnable()  => EventBus.OnMatchStateChanged += OnMatchState;
    void OnDisable() => EventBus.OnMatchStateChanged -= OnMatchState;

    private void OnMatchState(MatchState state)
    {
        _recording = (state == MatchState.Playing);
        if (state == MatchState.Playing) _frames.Clear(); // 새 라이프 시작 시 초기화
    }

    void FixedUpdate()
    {
        if (!_recording || _playerInput == null) return;
        _frames.Add(_playerInput.GetSnapshot()); // 소비 전 스냅샷 사용
    }

    /// <summary>현재까지 기록된 프레임 목록 반환 (복사본)</summary>
    public List<InputFrame> GetRecording() => new List<InputFrame>(_frames);

    /// <summary>리스폰 시 초기화</summary>
    public void ClearRecording() => _frames.Clear();
}

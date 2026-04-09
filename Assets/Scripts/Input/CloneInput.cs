using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기록된 InputFrame 목록을 순서대로 재생하는 입력 공급자.
/// 마지막 프레임 이후에는 입력 없음(정지) 상태를 반환합니다.
/// </summary>
public class CloneInput : IInputProvider
{
    private readonly List<InputFrame> _frames;
    private int _index;

    public bool IsFinished => _index >= _frames.Count;

    public CloneInput(List<InputFrame> frames)
    {
        _frames = frames ?? new List<InputFrame>();
        _index  = 0;
    }

    // 매 FixedUpdate 마다 한 프레임씩 전진
    public void Advance() => _index++;

    // ── IInputProvider ───────────────────────────────────────
    public Vector2 GetMoveInput()
    {
        if (IsFinished) return Vector2.zero;
        var f = _frames[_index];
        return new Vector2(f.moveX, f.moveY);
    }

    public Vector2 GetAimInput()
    {
        if (IsFinished) return Vector2.zero;
        var f = _frames[_index];
        return new Vector2(f.aimX, f.aimZ);
    }

    public bool GetJumpDown()   => !IsFinished && _frames[_index].jumpDown;
    public bool GetDashDown()   => !IsFinished && _frames[_index].dashDown;
    public bool GetAttackDown() => !IsFinished && _frames[_index].attackDown;
}

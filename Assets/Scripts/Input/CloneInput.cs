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
    private int _jumpConsumedIndex = -1;
    private int _dashConsumedIndex = -1;
    private int _attackConsumedIndex = -1;

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
        Vector2 move = new Vector2(f.moveX, f.moveY);
        return move.sqrMagnitude > 1f ? move.normalized : move;
    }

    public Vector2 GetAimInput()
    {
        if (IsFinished) return Vector2.zero;
        var f = _frames[_index];
        return new Vector2(f.aimX, f.aimZ);
    }

    public bool GetJumpDown()
    {
        if (IsFinished) return false;
        return ConsumeButton(_frames[_index].jumpDown, ref _jumpConsumedIndex);
    }

    public bool GetDashDown()
    {
        if (IsFinished) return false;
        return ConsumeButton(_frames[_index].dashDown, ref _dashConsumedIndex);
    }

    public bool GetAttackDown()
    {
        if (IsFinished) return false;
        return ConsumeButton(_frames[_index].attackDown, ref _attackConsumedIndex);
    }

    private bool ConsumeButton(bool pressed, ref int consumedIndex)
    {
        if (IsFinished || !pressed || consumedIndex == _index) return false;
        consumedIndex = _index;
        return true;
    }
}

/// <summary>
/// InputRecorder가 분신 기록용 스냅샷을 가져올 수 있는 입력 소스 인터페이스.
/// PlayerInput / GamepadInput 양쪽이 구현합니다.
/// </summary>
public interface ISnapshotCapture
{
    InputFrame GetSnapshot();
}

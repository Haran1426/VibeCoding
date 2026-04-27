/// <summary>
/// 씬 전환 사이에 로컬 멀티플레이어 설정을 유지하는 정적 클래스.
/// </summary>
public static class LocalMultiplayerConfig
{
    public static int  PlayerCount { get; set; } = 2;
    public static bool IsLocalMode { get; set; } = false;

    public static void Reset()
    {
        PlayerCount = 2;
        IsLocalMode = false;
    }
}

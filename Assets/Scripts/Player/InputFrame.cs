using Unity.Netcode;

/// <summary>
/// 한 FixedUpdate 의 입력 스냅샷.
/// INetworkSerializable 구현으로 NGO RPC 에서 직렬화됩니다.
/// </summary>
[System.Serializable]
public struct InputFrame : INetworkSerializable
{
    public float moveX;
    public float moveY;
    public float aimX;
    public float aimZ;
    public bool  jumpDown;
    public bool  dashDown;
    public bool  attackDown;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref moveX);
        serializer.SerializeValue(ref moveY);
        serializer.SerializeValue(ref aimX);
        serializer.SerializeValue(ref aimZ);
        serializer.SerializeValue(ref jumpDown);
        serializer.SerializeValue(ref dashDown);
        serializer.SerializeValue(ref attackDown);
    }
}

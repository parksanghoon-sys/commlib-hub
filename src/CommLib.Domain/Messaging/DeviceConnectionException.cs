namespace CommLib.Domain.Messaging;

/// <summary>
/// 디바이스 연결 수명주기 작업이 실패했을 때 작업 단계와 디바이스 식별자를 함께 전달합니다.
/// </summary>
public sealed class DeviceConnectionException : InvalidOperationException
{
    /// <summary>
    /// <see cref="DeviceConnectionException"/> 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="deviceId">실패한 디바이스 식별자입니다.</param>
    /// <param name="operation">실패한 연결 작업 이름입니다.</param>
    /// <param name="innerException">원본 예외입니다.</param>
    public DeviceConnectionException(string deviceId, string operation, Exception innerException)
        : base($"Device '{deviceId}' failed during {operation}. See inner exception for details.", innerException)
    {
        DeviceId = deviceId;
        Operation = operation;
    }

    /// <summary>
    /// 실패한 디바이스 식별자입니다.
    /// </summary>
    public string DeviceId { get; }

    /// <summary>
    /// 실패한 연결 작업 이름입니다.
    /// </summary>
    public string Operation { get; }
}

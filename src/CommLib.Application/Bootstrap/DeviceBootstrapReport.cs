namespace CommLib.Application.Bootstrap;

/// <summary>
/// 디바이스 부트스트랩의 성공/실패 결과를 함께 담습니다.
/// </summary>
public sealed class DeviceBootstrapReport
{
    /// <summary>
    /// <see cref="DeviceBootstrapReport"/> 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="connectedDeviceIds">성공적으로 연결된 디바이스 ID 목록입니다.</param>
    /// <param name="failures">실패한 디바이스 목록입니다.</param>
    public DeviceBootstrapReport(
        IReadOnlyList<string> connectedDeviceIds,
        IReadOnlyList<DeviceBootstrapFailure> failures)
    {
        ConnectedDeviceIds = connectedDeviceIds;
        Failures = failures;
    }

    /// <summary>
    /// 성공적으로 연결된 디바이스 ID 목록입니다.
    /// </summary>
    public IReadOnlyList<string> ConnectedDeviceIds { get; }

    /// <summary>
    /// 실패한 디바이스와 원인 예외 목록입니다.
    /// </summary>
    public IReadOnlyList<DeviceBootstrapFailure> Failures { get; }

    /// <summary>
    /// 실패 항목이 하나라도 있으면 <see langword="true"/>를 반환합니다.
    /// </summary>
    public bool HasFailures => Failures.Count > 0;
}

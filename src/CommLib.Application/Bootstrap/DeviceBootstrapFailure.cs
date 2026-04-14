namespace CommLib.Application.Bootstrap;

/// <summary>
/// 부트스트랩 중 실패한 디바이스와 그 원인 예외를 나타냅니다.
/// </summary>
/// <param name="DeviceId">실패한 디바이스 ID입니다.</param>
/// <param name="Exception">실패 원인 예외입니다.</param>
public sealed record DeviceBootstrapFailure(string DeviceId, Exception Exception);

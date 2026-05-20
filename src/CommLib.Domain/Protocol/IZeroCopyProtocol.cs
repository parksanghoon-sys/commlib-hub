namespace CommLib.Domain.Protocol;

/// <summary>
/// payload를 새 배열로 복사하지 않고, 호출자가 제공한 frame 메모리의 slice로 노출할 수 있는 선택적 프로토콜 계약입니다.
/// </summary>
public interface IZeroCopyProtocol : IProtocol
{
    /// <summary>
    /// 입력 buffer에서 완성된 frame 하나를 찾고, payload를 원본 memory slice로 반환합니다.
    /// </summary>
    /// <param name="buffer">frame decode를 시도할 caller-owned memory입니다.</param>
    /// <param name="result">성공 시 payload slice와 소비한 frame 길이를 담습니다.</param>
    /// <returns>완성된 frame이 있으면 <see langword="true"/>, 아직 부족하면 <see langword="false"/>입니다.</returns>
    bool TryDecode(ReadOnlyMemory<byte> buffer, out ProtocolDecodeResult result);
}

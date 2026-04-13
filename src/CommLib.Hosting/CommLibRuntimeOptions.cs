namespace CommLib.Hosting;

/// <summary>
/// CommLib 호스팅 레이어에서 조정할 수 있는 런타임 옵션입니다.
/// </summary>
public sealed class CommLibRuntimeOptions
{
    /// <summary>
    /// 장치별 비요청 inbound queue capacity를 지정합니다.
    /// </summary>
    public int InboundQueueCapacity { get; set; } = 256;
}

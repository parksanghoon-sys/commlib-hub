namespace CommLib.Domain.Configuration;

/// <summary>
/// 모든 전송 형식이 공통으로 따르는 설정 계약을 나타냅니다.
/// </summary>
public abstract record TransportOptions
{
    /// <summary>
    /// 전송 구현을 구분하는 식별 값을 가져옵니다.
    /// </summary>
    public required string Type { get; init; }
}

/// <summary>
/// 장치 연결에 사용하는 TCP 클라이언트 전송 설정을 나타냅니다.
/// </summary>
public sealed record TcpClientTransportOptions : TransportOptions
{
    /// <summary>
    /// 연결할 원격 호스트 이름 또는 IP 주소를 가져옵니다.
    /// </summary>
    public string Host { get; init; } = string.Empty;
    /// <summary>
    /// 원격 TCP 포트를 가져옵니다.
    /// </summary>
    public int Port { get; init; }
    /// <summary>
    /// 연결 제한 시간(밀리초)을 가져옵니다.
    /// </summary>
    public int ConnectTimeoutMs { get; init; } = 3000;
    /// <summary>
    /// 소켓 버퍼 크기(바이트)를 가져옵니다.
    /// </summary>
    public int BufferSize { get; init; } = 8192;
    /// <summary>
    /// Nagle 알고리즘 비활성화 여부를 가져옵니다.
    /// </summary>
    public bool NoDelay { get; init; } = true;
}

/// <summary>
/// 데이터그램 기반 통신에 사용하는 UDP 전송 설정을 나타냅니다.
/// </summary>
public sealed record UdpTransportOptions : TransportOptions
{
    /// <summary>
    /// 바인딩할 로컬 UDP 포트를 가져옵니다.
    /// </summary>
    public int LocalPort { get; init; }
    /// <summary>
    /// 지정 송신에 사용할 선택적 원격 호스트 이름 또는 IP 주소를 가져옵니다.
    /// </summary>
    public string? RemoteHost { get; init; }
    /// <summary>
    /// 지정 송신에 사용할 선택적 원격 UDP 포트를 가져옵니다.
    /// </summary>
    public int? RemotePort { get; init; }
}

/// <summary>
/// 시리얼 포트 전송 설정을 나타냅니다.
/// </summary>
public sealed record SerialTransportOptions : TransportOptions
{
    /// <summary>
    /// 운영체제 시리얼 포트 이름을 가져옵니다.
    /// </summary>
    public string PortName { get; init; } = string.Empty;
    /// <summary>
    /// 시리얼 통신에 사용할 보드레이트를 가져옵니다.
    /// </summary>
    public int BaudRate { get; init; } = 9600;
    /// <summary>
    /// 각 프레임의 데이터 비트 수를 가져옵니다.
    /// </summary>
    public int DataBits { get; init; } = 8;
    /// <summary>
    /// 패리티 설정 이름을 가져옵니다.
    /// </summary>
    public string Parity { get; init; } = "None";
    /// <summary>
    /// 정지 비트 설정 이름을 가져옵니다.
    /// </summary>
    public string StopBits { get; init; } = "One";
    /// <summary>
    /// 시리얼 회선을 반이중 모드로 사용하는지 여부를 가져옵니다.
    /// </summary>
    public bool HalfDuplex { get; init; }
    /// <summary>
    /// 반이중 송수신 전환 사이의 간격(밀리초)을 가져옵니다.
    /// </summary>
    public int TurnGapMs { get; init; } = 50;
    /// <summary>
    /// 시리얼 읽기 버퍼 크기(바이트)를 가져옵니다.
    /// </summary>
    public int ReadBufferSize { get; init; } = 4096;
    /// <summary>
    /// 시리얼 쓰기 버퍼 크기(바이트)를 가져옵니다.
    /// </summary>
    public int WriteBufferSize { get; init; } = 4096;
}

/// <summary>
/// 그룹 통신에 사용하는 멀티캐스트 전송 설정을 나타냅니다.
/// </summary>
public sealed record MulticastTransportOptions : TransportOptions
{
    /// <summary>
    /// 멀티캐스트 그룹 IP 주소를 가져옵니다.
    /// </summary>
    public string GroupAddress { get; init; } = string.Empty;
    /// <summary>
    /// 멀티캐스트 포트를 가져옵니다.
    /// </summary>
    public int Port { get; init; }
    /// <summary>
    /// 멀티캐스트 그룹 참여에 사용할 선택적 로컬 인터페이스를 가져옵니다.
    /// </summary>
    public string? LocalInterface { get; init; }
    /// <summary>
    /// 송신 멀티캐스트 패킷의 TTL 값을 가져옵니다.
    /// </summary>
    public int Ttl { get; init; } = 1;
    /// <summary>
    /// 멀티캐스트 루프백 사용 여부를 가져옵니다.
    /// </summary>
    public bool Loopback { get; init; }
}

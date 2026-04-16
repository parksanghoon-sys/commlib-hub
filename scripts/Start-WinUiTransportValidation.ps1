[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("TcpEcho", "UdpEcho", "MulticastReceive", "MulticastSend")]
    [string]$Mode,

    [int]$Port,

    [string]$Group = "239.0.0.241",

    [string]$Message = "hello from WinUI validation helper",

    [int]$TimeoutMs,

    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$consoleProject = Join-Path $repoRoot "examples\CommLib.Examples.Console\CommLib.Examples.Console.csproj"

if (-not (Test-Path $consoleProject)) {
    throw "Console project not found: $consoleProject"
}

$arguments = @("run", "--project", $consoleProject)
if ($NoBuild) {
    $arguments += "--no-build"
}

$arguments += "--"

switch ($Mode) {
    "TcpEcho" {
        $resolvedPort = if ($PSBoundParameters.ContainsKey("Port")) { $Port } else { 7001 }
        $arguments += @("tcp-echo-server", "--port", $resolvedPort)
        if ($PSBoundParameters.ContainsKey("TimeoutMs")) {
            $arguments += @("--timeout-ms", $TimeoutMs)
        }
    }
    "UdpEcho" {
        $resolvedPort = if ($PSBoundParameters.ContainsKey("Port")) { $Port } else { 7002 }
        $arguments += @("udp-echo-server", "--port", $resolvedPort)
        if ($PSBoundParameters.ContainsKey("TimeoutMs")) {
            $arguments += @("--timeout-ms", $TimeoutMs)
        }
    }
    "MulticastReceive" {
        $resolvedPort = if ($PSBoundParameters.ContainsKey("Port")) { $Port } else { 7004 }
        $arguments += @("multicast-receive", "--group", $Group, "--port", $resolvedPort)
        if ($PSBoundParameters.ContainsKey("TimeoutMs")) {
            $arguments += @("--timeout-ms", $TimeoutMs)
        }
    }
    "MulticastSend" {
        $resolvedPort = if ($PSBoundParameters.ContainsKey("Port")) { $Port } else { 7004 }
        $arguments += @("multicast-send", "--group", $Group, "--port", $resolvedPort, "--message", $Message)
    }
}

Write-Host "[helper] Running: dotnet $($arguments -join ' ')" -ForegroundColor Cyan
& dotnet @arguments
$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    exit $exitCode
}

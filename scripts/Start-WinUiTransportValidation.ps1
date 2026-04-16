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
$consoleProject = Join-Path $repoRoot "examples\\CommLib.Examples.Console\\CommLib.Examples.Console.csproj"
$consoleProjectDir = Split-Path -Parent $consoleProject

if (-not (Test-Path $consoleProject)) {
    throw "Console project not found: $consoleProject"
}

if ($NoBuild) {
    $outputDll = Get-ChildItem -Path (Join-Path $consoleProjectDir "bin") -Filter "CommLib.Examples.Console.dll" -Recurse |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $outputDll) {
        throw "No built console output found under '$consoleProjectDir\\bin'. Run the helper once without -NoBuild or build the console project first."
    }

    $arguments = @($outputDll.FullName)
}
else {
    $arguments = @("run", "--project", $consoleProject)
    $arguments += "--"
}

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

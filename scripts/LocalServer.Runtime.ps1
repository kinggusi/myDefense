# Side-effect-free helpers. Dot-sourcing never starts a process or opens a database.
function Assert-LocalServerSnapshotHash {
    param([string]$SourceHash, [string]$SnapshotHash)
    if ($SourceHash -cnotmatch '^[0-9a-f]{64}$' -or $SourceHash -cne $SnapshotHash) {
        throw 'Runtime JAR integrity check failed; no server was started.'
    }
}

function New-LocalServerRuntimeSnapshot {
    param([Parameter(Mandatory)][string]$SourceJar, [Parameter(Mandatory)][string]$RuntimeRoot)
    $source = [IO.File]::Open($SourceJar, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $hash = [BitConverter]::ToString($sha.ComputeHash($source)).Replace('-', '').ToLowerInvariant()
        $source.Position = 0
        $directory = Join-Path $RuntimeRoot ((Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + $hash.Substring(0, 12) + '-' + [Guid]::NewGuid().ToString('N'))
        [void][IO.Directory]::CreateDirectory($directory)
        $targetPath = Join-Path $directory 'server.jar'
        $target = [IO.File]::Open($targetPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        try {
            $source.CopyTo($target)
            $target.Flush($true)
            $target.Position = 0
            $targetHash = [BitConverter]::ToString($sha.ComputeHash($target)).Replace('-', '').ToLowerInvariant()
            Assert-LocalServerSnapshotHash $hash $targetHash
        } finally { $target.Dispose() }
        # Rebuilds only touch build/libs. Each launch gets a unique, never-overwritten runtime path.
        [pscustomobject]@{ Path = $targetPath; Sha256 = $hash; Source = [IO.Path]::GetFullPath($SourceJar) }
    } finally { $sha.Dispose(); $source.Dispose() }
}

function Test-LocalServerChildIdentity {
    param($Process, [datetime]$StartedAt, [string]$RuntimeJar)
    $Process.Refresh()
    if ($Process.HasExited -or $Process.StartTime.ToUniversalTime().Ticks -ne $StartedAt.ToUniversalTime().Ticks) { return $false }
    $details = Get-CimInstance Win32_Process -Filter "ProcessId = $($Process.Id)" -ErrorAction Stop
    if (!$details -or !$details.CommandLine) { return $false }
    # The quoted absolute snapshot path is exactly the argument supplied by this launcher.
    return $details.CommandLine.Contains(('"' + $RuntimeJar + '"'))
}

function Stop-LocalServerOwnChild {
    param($Process, [datetime]$StartedAt, [string]$RuntimeJar)
    if (Test-LocalServerChildIdentity $Process $StartedAt $RuntimeJar) {
        # Kill via the original Process handle, not a new PID lookup which could target a reused PID.
        $Process.Kill()
        [void]$Process.WaitForExit(5000)
    }
}

function Wait-LocalServerReady {
    param($Process, [datetime]$StartedAt, [string]$RuntimeJar, [int]$Port,
        [string]$Stdout, [string]$Stderr, [string]$Marker, [int]$TimeoutSeconds = 45)
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $Process.Refresh()
        if ($Process.HasExited) { throw "Local server exited before ready. See $Stdout and $Stderr" }
        $listeners = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
        $ownsPort = @($listeners | Where-Object OwningProcess -EQ $Process.Id).Count -gt 0
        $ready = $false
        if (Test-Path -LiteralPath $Stdout) {
            $ready = [bool](Select-String -LiteralPath $Stdout -SimpleMatch "LOCAL_SERVER_APPLICATION_READY marker=$Marker" -Quiet)
        }
        if ($ownsPort -and $ready -and (Test-LocalServerChildIdentity $Process $StartedAt $RuntimeJar)) { return }
        Start-Sleep -Milliseconds 250
    }
    throw "Local server was not ready within $TimeoutSeconds seconds. See $Stdout and $Stderr"
}

# No Java, actual server, DB, DPAPI key or credentials are used by these tests.
$ErrorActionPreference = 'Stop'
$helper = Join-Path $PSScriptRoot '../LocalServer.Runtime.ps1'
$launcher = Join-Path $PSScriptRoot '../Start-LocalServer.ps1'
foreach ($file in @($helper, $launcher)) {
    $tokens = $null; $errors = $null
    [Management.Automation.Language.Parser]::ParseFile($file, [ref]$tokens, [ref]$errors) | Out-Null
    if ($errors.Count) { throw ($errors | Out-String) }
}
. $helper
$script:passed = 0
$temp = Join-Path ([IO.Path]::GetTempPath()) ('shop-launcher-test-' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($temp)
function Assert-True([bool]$Condition, [string]$Message) { if (!$Condition) { throw $Message } }
function Assert-Throws([scriptblock]$Action, [string]$Message) {
    $thrown = $false
    try { & $Action | Out-Null } catch { $thrown = $true }
    Assert-True $thrown $Message
}
function Test-Case([string]$Name, [scriptblock]$Action) {
    & $Action
    $script:passed++
    Write-Host "PASS $Name"
}
try {
    $source = Join-Path $temp 'build.jar'
    [IO.File]::WriteAllBytes($source, [byte[]](0..255))
    $runtime = Join-Path $temp 'runtime'
    $first = New-LocalServerRuntimeSnapshot $source $runtime
    Test-Case 'Snapshot SHA256 matches source and unique path is outside build path' {
        Assert-True ($first.Sha256 -ceq (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()) 'Wrong source hash.'
        Assert-True ($first.Path -ne $source -and $first.Path.StartsWith($runtime)) 'Unsafe snapshot path.'
        Assert-True ((Get-FileHash $first.Path).Hash.ToLowerInvariant() -ceq $first.Sha256) 'Wrong snapshot hash.'
    }
    Test-Case 'Rebuild replaces source bytes without changing existing runtime snapshot' {
        [IO.File]::WriteAllBytes($source, [byte[]](255..0))
        Assert-True ((Get-FileHash $first.Path).Hash.ToLowerInvariant() -ceq $first.Sha256) 'Running snapshot changed.'
        $second = New-LocalServerRuntimeSnapshot $source $runtime
        Assert-True ($second.Path -ne $first.Path -and $second.Sha256 -ne $first.Sha256) 'Rebuild reused previous runtime.'
        $third = New-LocalServerRuntimeSnapshot $source $runtime
        Assert-True ($third.Path -ne $second.Path -and $third.Sha256 -ceq $second.Sha256) 'Same build launch was not unique.'
    }
    Test-Case 'Concurrent writer prevents snapshot instead of copying inconsistent JAR' {
        $writer = [IO.File]::Open($source, [IO.FileMode]::Open, [IO.FileAccess]::Write, [IO.FileShare]::ReadWrite)
        try { Assert-Throws { New-LocalServerRuntimeSnapshot $source $runtime } 'Concurrent source write was allowed.' }
        finally { $writer.Dispose() }
    }
    Test-Case 'Missing source fails without launch' {
        Assert-Throws { New-LocalServerRuntimeSnapshot (Join-Path $temp 'missing.jar') $runtime } 'Missing source was accepted.'
    }
    Test-Case 'Hash mismatch and empty digest fail closed' {
        Assert-Throws { Assert-LocalServerSnapshotHash ('a' * 64) ('b' * 64) } 'Corrupt copy was accepted.'
        Assert-Throws { Assert-LocalServerSnapshotHash '' '' } 'Empty digest was accepted.'
    }

    $script:fakePid = 12345
    $script:fake = [pscustomobject]@{ Id = $script:fakePid; HasExited = $false; StartTime = [datetime]'2026-09-19T00:00:00Z'; Killed = $false }
    $script:fake | Add-Member ScriptMethod Refresh { }
    $script:fake | Add-Member ScriptMethod Kill { $this.Killed = $true }
    $script:fake | Add-Member ScriptMethod WaitForExit { param($Timeout) return $true }
    $script:commandLine = 'java -jar "' + $first.Path + '"'
    $script:listenerPid = $script:fakePid
    function Get-CimInstance { param($ClassName, $Filter, $ErrorAction) return [pscustomobject]@{ CommandLine = $script:commandLine } }
    function Get-NetTCPConnection { param($LocalPort, $State, $ErrorAction) return [pscustomobject]@{ OwningProcess = $script:listenerPid } }
    $out = Join-Path $temp 'stdout.log'; $err = Join-Path $temp 'stderr.log'
    $marker = [Guid]::NewGuid().ToString()
    [IO.File]::WriteAllText($out, "LOCAL_SERVER_APPLICATION_READY marker=$marker")
    Test-Case 'Ready requires owned listen port and own ready marker and process identity' {
        Wait-LocalServerReady $fake $fake.StartTime $first.Path 18080 $out $err $marker 1
    }
    Test-Case 'Port without ApplicationReady marker is not ready' {
        [IO.File]::WriteAllText($out, 'Started DefenseServerApplication; listener exists but runners are incomplete')
        Assert-Throws { Wait-LocalServerReady $fake $fake.StartTime $first.Path 18080 $out $err $marker 1 } 'Early listening was accepted.'
    }
    Test-Case 'Ready marker from another launch is rejected' {
        [IO.File]::WriteAllText($out, 'LOCAL_SERVER_APPLICATION_READY marker=other')
        Assert-Throws { Wait-LocalServerReady $fake $fake.StartTime $first.Path 18080 $out $err $marker 1 } 'Wrong marker was accepted.'
    }
    Test-Case 'Other process owning port is never readiness' {
        [IO.File]::WriteAllText($out, "LOCAL_SERVER_APPLICATION_READY marker=$marker")
        $script:listenerPid = 99999
        Assert-Throws { Wait-LocalServerReady $fake $fake.StartTime $first.Path 18080 $out $err $marker 1 } 'Other listener was accepted.'
        $script:listenerPid = $script:fakePid
    }
    Test-Case 'Early child exit fails immediately without dumping log contents' {
        $fake.HasExited = $true
        try {
            $message = ''
            try { Wait-LocalServerReady $fake $fake.StartTime $first.Path 18080 $out $err $marker 1 } catch { $message = $_.Exception.Message }
            Assert-True ($message.Contains('exited before ready') -and !$message.Contains($marker)) 'Wrong/sensitive failure output.'
        } finally { $fake.HasExited = $false }
    }
    Test-Case 'Cleanup kills only matching PID/start time/snapshot via original handle' {
        Stop-LocalServerOwnChild $fake $fake.StartTime $first.Path
        Assert-True $fake.Killed 'Matching own child was not stopped.'
        $fake.Killed = $false
        Stop-LocalServerOwnChild $fake ($fake.StartTime.AddSeconds(-1)) $first.Path
        Assert-True (!$fake.Killed) 'Start time mismatch was killed.'
        $script:commandLine = 'java -jar "C:\other\server.jar"'
        Stop-LocalServerOwnChild $fake $fake.StartTime $first.Path
        Assert-True (!$fake.Killed) 'Other snapshot was killed.'
    }
    Test-Case 'Launcher records no secrets and uses only runtime snapshot with fixed safety settings' {
        $code = Get-Content -LiteralPath $launcher -Raw
        Assert-True ($code.Contains("('`"' + `$snapshot.Path + '`"')") -and !$code.Contains("('`"' + `$jar.FullName + '`"')")) 'Launcher still executes mutable build JAR.'
        $record = $code.Substring($code.IndexOf('$record = @{'), $code.IndexOf('$record | ConvertTo-Json') - $code.IndexOf('$record = @{'))
        Assert-True ($record -notmatch '(?i)password|signingkey|token') 'Secret field in record.'
        Assert-True ($code.Contains("--spring.jpa.hibernate.ddl-auto=update") -and $code.Contains("--spring.sql.init.mode=never")) 'DB persistence protections changed.'
        Assert-True ($code.IndexOf('Get-NetTCPConnection') -lt $code.IndexOf('New-LocalServerRuntimeSnapshot')) 'Port check happens after side effects.'
    }
    Write-Host "Local launcher tests: $script:passed/12 PASS (temporary files and mocked processes only)."
} finally {
    $resolved = [IO.Path]::GetFullPath($temp)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (!$resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -or !(Split-Path $resolved -Leaf).StartsWith('shop-launcher-test-')) { throw 'Unsafe test cleanup target.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}

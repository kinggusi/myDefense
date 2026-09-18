# Launches one interactive Development Client after a read-only local API preflight.
[CmdletBinding()]
param(
    [string]$ExePath = 'C:\myDefense\_localbuild\P2Validation\P2DailyMutationLabR3\Client.exe',
    [ValidateSet('CULT', 'MUT')][string]$Content = 'CULT',
    [ValidateRange(1, 5)][int]$Stage = 1,
    [string]$ApiBaseUrl = 'http://127.0.0.1:8082/api',
    [string]$LogDir = 'C:\myDefense\_localbuild\P2Validation\DailyManual'
)

function Get-P22LocalApiUri {
    param([string]$ApiBaseUrl)
    $parsed = $null
    if (-not [Uri]::TryCreate($ApiBaseUrl, [UriKind]::Absolute, [ref]$parsed) -or
        $parsed.Scheme -cne 'http' -or -not $parsed.IsLoopback -or
        $parsed.Host -notin @('127.0.0.1', 'localhost', '[::1]', '::1') -or
        $parsed.AbsolutePath.TrimEnd('/') -cne '/api' -or
        $parsed.UserInfo -or $parsed.Query -or $parsed.Fragment) {
        throw 'ApiBaseUrl must be a loopback HTTP URL ending in /api, with no credentials, query or fragment.'
    }
    return $parsed.AbsoluteUri.TrimEnd('/')
}

function Test-P22DailyPreflight {
    param([string]$ExePath, [string]$ApiBaseUrl)
    $api = Get-P22LocalApiUri $ApiBaseUrl
    if (-not (Test-Path -LiteralPath $ExePath -PathType Leaf) -or
        [IO.Path]::GetExtension($ExePath) -ine '.exe') {
        throw "Development Client executable not found: $ExePath. Supply -ExePath for the desired build."
    }
    $exe = (Resolve-Path -LiteralPath $ExePath -ErrorAction Stop).ProviderPath
    $dataDir = Join-Path (Split-Path -Parent $exe) ([IO.Path]::GetFileNameWithoutExtension($exe) + '_Data')
    $manifestPath = Join-Path $dataDir 'StreamingAssets\Balance\generated\balance-manifest.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Build manifest not found: $manifestPath"
    }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
    if ([string]::IsNullOrWhiteSpace($manifest.balanceVersion) -or
        [string]$manifest.contentHash -cnotmatch '^[0-9a-f]{64}$') {
        throw 'Build manifest is missing a canonical balanceVersion/contentHash.'
    }
    try {
        $catalog = Invoke-RestMethod -Uri ($api + '/battle/entry/attack-snapshots?playerId=dev-daily') `
            -Method Get -TimeoutSec 10 -MaximumRedirection 0 -ErrorAction Stop
    }
    catch {
        throw "Attack Snapshot GET failed. Start the matching Spring local profile first, then retry with a new Client. $($_.Exception.Message)"
    }
    if ($catalog.playerId -cne 'dev-daily' -or
        $catalog.balanceVersion -cne $manifest.balanceVersion -or
        $catalog.contentHash -ine $manifest.contentHash) {
        throw 'Server Attack Snapshot playerId/version/hash differs from this build. Use the same canonical data before launching.'
    }
    $aliens = @($catalog.aliens)
    if ($aliens.Count -eq 0) { throw 'Server Attack Snapshot catalog is empty.' }
    $seen = [Collections.Generic.HashSet[long]]::new()
    foreach ($alien in $aliens) {
        if ($null -eq $alien -or [long]$alien.alienId -le 0 -or -not $seen.Add([long]$alien.alienId)) {
            throw 'Server Attack Snapshot contains missing or duplicate Alien IDs.'
        }
        foreach ($field in @('damage', 'attackRate', 'range')) {
            $value = 0.0
            if (-not [double]::TryParse([string]$alien.$field,
                    [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$value) -or
                [double]::IsNaN($value) -or [double]::IsInfinity($value) -or $value -le 0) {
                throw "Server Attack Snapshot has an invalid $field value."
            }
        }
    }
    return [pscustomobject]@{ ExePath = $exe; ApiBaseUrl = $api; BalanceVersion = $manifest.balanceVersion; ContentHash = $manifest.contentHash; AlienCount = $aliens.Count }
}

function Start-P22Daily {
    [CmdletBinding()]
    param(
        [string]$ExePath = 'C:\myDefense\_localbuild\P2Validation\P2DailyMutationLabR3\Client.exe',
        [ValidateSet('CULT', 'MUT')][string]$Content = 'CULT',
        [ValidateRange(1, 5)][int]$Stage = 1,
        [string]$ApiBaseUrl = 'http://127.0.0.1:8082/api',
        [string]$LogDir = 'C:\myDefense\_localbuild\P2Validation\DailyManual'
    )
    $preflight = Test-P22DailyPreflight -ExePath $ExePath -ApiBaseUrl $ApiBaseUrl
    $Content = $Content.ToUpperInvariant()
    $session = 'P22-{0}-S{1}-{2}-{3}' -f $Content, $Stage, (Get-Date -Format 'yyyyMMddHHmmss'), [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $resolvedLogDir = [IO.Path]::GetFullPath($LogDir)
    New-Item -ItemType Directory -Path $resolvedLogDir -Force -ErrorAction Stop | Out-Null
    $logPath = Join-Path $resolvedLogDir ($session + '.log')
    # Start-Process joins ArgumentList strings; preserve spaces in the log path explicitly.
    $arguments = @(
        "-fusionSession=$session", '-fusionUserId=dev-daily', '-fusionRole=host', '-env=local',
        "-apiBaseUrl=$($preflight.ApiBaseUrl)", '-screen-fullscreen=0', '-screen-width=1280', '-screen-height=720',
        '-logFile', ('"' + $logPath + '"')
    )
    $client = Start-Process -FilePath $preflight.ExePath -ArgumentList $arguments -WindowStyle Normal -PassThru -ErrorAction Stop
    Write-Host "Daily solo Host started. Session: $session"
    Write-Host "Snapshot preflight: $($preflight.AlienCount) Aliens, $($preflight.BalanceVersion). Log: $logPath"
    Write-Host 'Check the Client log for Applied ... server-calculated attack snapshots before Kidnap. Close this Client before starting the next Stage.'
    return [pscustomobject]@{ Session = $session; ProcessId = $client.Id; LogPath = $logPath; ExePath = $preflight.ExePath; Content = $Content; Stage = $Stage }
}

# Dot-sourcing exposes functions without launching a process (also used by the mock tests).
if ($MyInvocation.InvocationName -ne '.') {
    Start-P22Daily -ExePath $ExePath -Content $Content -Stage $Stage -ApiBaseUrl $ApiBaseUrl -LogDir $LogDir
}

# Dependency-free regression tests; all filesystem, HTTP and process effects are mocked.
$ErrorActionPreference = 'Stop'
$launcher = Join-Path $PSScriptRoot '..\Start-P22Daily.ps1'
$tokens = $null
$parseErrors = $null
[Management.Automation.Language.Parser]::ParseFile($launcher, [ref]$tokens, [ref]$parseErrors) | Out-Null
if ($parseErrors.Count) { throw ($parseErrors | Out-String) }
. $launcher

$script:passed = 0
$script:getCount = 0
$script:launches = @()
$script:missingPath = $false
$script:badManifest = $false
$script:httpFailure = $false
$script:fixtureHash = 'a' * 64
$script:catalog = $null
function Reset-Fixture {
    $script:getCount = 0
    $script:launches = @()
    $script:missingPath = $false
    $script:badManifest = $false
    $script:httpFailure = $false
    $script:catalog = [pscustomobject]@{
        playerId = 'dev-daily'; balanceVersion = 'test-version'; contentHash = $script:fixtureHash
        aliens = @([pscustomobject]@{ alienId = 29; damage = 120; attackRate = 1.2; range = 8 })
    }
}
function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
function Assert-Throws([scriptblock]$Action, [string]$Message) {
    $didThrow = $false
    try { & $Action | Out-Null } catch { $didThrow = $true }
    Assert-True $didThrow $Message
}
function Test-Case([string]$Name, [scriptblock]$Action) {
    Reset-Fixture
    & $Action
    $script:passed++
    Write-Host "PASS $Name"
}
function Test-Path { param($LiteralPath, $PathType) return -not $script:missingPath }
function Resolve-Path { param($LiteralPath, $ErrorAction) return [pscustomobject]@{ ProviderPath = $LiteralPath } }
function Get-Content {
    param($LiteralPath, [switch]$Raw, $ErrorAction)
    if ($script:badManifest) { return '{"balanceVersion":"","contentHash":"invalid"}' }
    return ([pscustomobject]@{ balanceVersion = 'test-version'; contentHash = $script:fixtureHash } | ConvertTo-Json)
}
function Invoke-RestMethod {
    param($Uri, $Method, $TimeoutSec, $MaximumRedirection, $ErrorAction)
    $script:getCount++
    Assert-True ($Uri -eq 'http://127.0.0.1:8082/api/battle/entry/attack-snapshots?playerId=dev-daily') 'Unexpected HTTP destination.'
    Assert-True ($Method -eq 'Get' -and $MaximumRedirection -eq 0) 'Only a nonredirecting GET is allowed.'
    if ($script:httpFailure) { throw 'Mock HTTP unavailable.' }
    return $script:catalog
}
function New-Item { param($ItemType, $Path, [switch]$Force, $ErrorAction) }
function Start-Process {
    param($FilePath, $ArgumentList, $WindowStyle, [switch]$PassThru, $ErrorAction)
    $script:launches += [pscustomobject]@{ FilePath = $FilePath; ArgumentList = $ArgumentList }
    return [pscustomobject]@{ Id = 1234 }
}

Test-Case 'PowerShell parser accepts launcher and dot-source does not launch' {
    Assert-True ($script:launches.Count -eq 0) 'Unexpected launch while dot-sourcing.'
}
Test-Case 'Loopback URL allowlist and /api normalization' {
    foreach ($url in @('http://127.0.0.1:8082/api', 'http://localhost:8082/api/', 'http://[::1]:8082/api')) {
        Assert-True (-not [string]::IsNullOrEmpty((Get-P22LocalApiUri $url))) "Rejected $url"
    }
}
Test-Case 'Remote/credential/query/fragment/redirect style URLs are rejected before HTTP' {
    foreach ($url in @('https://127.0.0.1/api', 'http://example.com/api', 'http://127.0.0.1.example.com/api',
            'http://user:pw@localhost/api', 'http://localhost/api?q=x', 'http://localhost/api#x',
            'http://localhost/not-api', 'file:///C:/api')) {
        Assert-Throws { Test-P22DailyPreflight 'C:\Build\Client.exe' $url } "Accepted $url"
    }
    Assert-True ($script:getCount -eq 0) 'Invalid URL reached HTTP.'
}
Test-Case 'Missing executable and malformed manifest prevent HTTP and process launch' {
    $script:missingPath = $true
    Assert-Throws { Start-P22Daily -ExePath 'C:\Build\Client.exe' } 'Missing executable accepted.'
    $script:missingPath = $false
    $script:badManifest = $true
    Assert-Throws { Start-P22Daily -ExePath 'C:\Build\Client.exe' } 'Invalid manifest accepted.'
    Assert-True ($script:getCount -eq 0 -and $script:launches.Count -eq 0) 'Invalid build had side effects.'
}
Test-Case 'Unavailable API never starts Client' {
    $script:httpFailure = $true
    Assert-Throws { Start-P22Daily -ExePath 'C:\Build\Client.exe' } 'Unavailable API accepted.'
    Assert-True ($script:launches.Count -eq 0) 'Client started without Snapshot.'
}
Test-Case 'Catalog identity, version and hash mismatch never starts Client' {
    foreach ($field in @('playerId', 'balanceVersion', 'contentHash')) {
        Reset-Fixture
        $script:catalog.$field = 'mismatch'
        Assert-Throws { Start-P22Daily -ExePath 'C:\Build\Client.exe' } "Accepted mismatch: $field"
        Assert-True ($script:launches.Count -eq 0) 'Mismatched catalog launched Client.'
    }
}
Test-Case 'Empty, duplicate and nonpositive damage catalogs are rejected' {
    $script:catalog.aliens = @()
    Assert-Throws { Start-P22Daily -ExePath 'C:\Build\Client.exe' } 'Empty catalog accepted.'
    Reset-Fixture
    $script:catalog.aliens += $script:catalog.aliens[0]
    Assert-Throws { Start-P22Daily -ExePath 'C:\Build\Client.exe' } 'Duplicate Alien accepted.'
    Reset-Fixture
    $script:catalog.aliens[0].damage = 0
    Assert-Throws { Start-P22Daily -ExePath 'C:\Build\Client.exe' } 'Zero damage accepted.'
    Assert-True ($script:launches.Count -eq 0) 'Invalid catalog launched Client.'
}
Test-Case 'Content and Stage parameters reject unsupported input' {
    Assert-Throws { Start-P22Daily -Content UNKNOWN } 'Invalid content accepted.'
    Assert-Throws { Start-P22Daily -Stage 0 } 'Stage zero accepted.'
    Assert-Throws { Start-P22Daily -Stage 6 } 'Stage six accepted.'
    Assert-True ($script:getCount -eq 0 -and $script:launches.Count -eq 0) 'Bad parameters had side effects.'
}
Test-Case 'Valid CULT/MUT launch exactly one local solo Host and unique Session' {
    $sessions = @()
    foreach ($content in @('CULT', 'MUT', 'CULT')) {
        Reset-Fixture
        $result = Start-P22Daily -ExePath 'C:\Build With Space\Client.exe' -Content $content -Stage 5 -LogDir 'C:\Logs With Space'
        Assert-True ($script:getCount -eq 1 -and $script:launches.Count -eq 1) 'Expected one GET and one Client.'
        Assert-True ($result.Session -cmatch ('^P22-' + $content + '-S5-[0-9]{14}-[0-9a-f]{8}$')) 'Invalid Session grammar.'
        $capturedArgs = $script:launches[0].ArgumentList
        Assert-True ($capturedArgs -contains '-fusionRole=host' -and $capturedArgs -contains '-fusionUserId=dev-daily' -and $capturedArgs -contains '-env=local') 'Invalid solo identity.'
        Assert-True ($capturedArgs[-1] -eq ('"' + $result.LogPath + '"')) 'Log path spaces were not quoted.'
        $sessions += $result.Session
    }
    Assert-True (@($sessions | Select-Object -Unique).Count -eq $sessions.Count) 'Repeated Session name.'
}
Write-Host "Launcher tests: $script:passed/9 PASS (mocked HTTP/process, no Client launched)."

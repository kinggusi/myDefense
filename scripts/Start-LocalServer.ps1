[CmdletBinding()]
param(
    [ValidateRange(1024, 65535)][int]$Port = 8080,
    [switch]$BootstrapAccounts,
    [string[]]$Usernames = @('jjangash', 'kingusi'),
    [Security.SecureString]$BootstrapPassword,
    [string]$JavaPath = 'java'
)

# Local Windows development only. Secrets and the H2 database stay outside Git.
$ErrorActionPreference = 'Stop'
# A lower-level JPA setting can override ddl-auto (for example
# SPRING_JPA_PROPERTIES_HIBERNATE_HBM2DDL_AUTO=create-drop). Reject inherited
# Spring configuration wholesale instead of trying to enumerate destructive aliases.
foreach ($entry in Get-ChildItem Env: | Where-Object { $_.Name -match '^spring([._]|$)' }) {
    if (![string]::IsNullOrWhiteSpace($entry.Value)) {
        throw "Inherited $($entry.Name) is not allowed by the local persistence launcher. Clear it explicitly before starting."
    }
}
foreach ($override in @('SPRING_APPLICATION_JSON', 'JAVA_TOOL_OPTIONS', 'JDK_JAVA_OPTIONS', '_JAVA_OPTIONS')) {
    if (![string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($override, 'Process'))) {
        throw "Inherited $override may override local database/security settings. Clear it explicitly before starting."
    }
}
$root = Split-Path -Parent $PSScriptRoot
$server = Join-Path $root 'server'
$local = Join-Path $root '.local'
$data = Join-Path $local 'data'
$secrets = Join-Path $local 'secrets'
$logs = Join-Path $local 'logs'
if (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue) {
    throw "Port $Port already has a listener. No existing process was stopped."
}
$jar = Get-ChildItem -LiteralPath (Join-Path $server 'build/libs') -Filter '*.jar' |
    Where-Object { $_.Name -notlike '*-plain.jar' } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (!$jar) { throw 'Build the server first: server/gradlew.bat bootJar' }
$java = (Get-Command $JavaPath -ErrorAction Stop).Source
foreach ($folder in @($data, $secrets, $logs)) { [void](New-Item -ItemType Directory -Path $folder -Force) }
Add-Type -AssemblyName System.Security
$keyFile = Join-Path $secrets 'local-jwt.dpapi'
if (Test-Path -LiteralPath $keyFile) {
    # A damaged/unreadable key must fail, never silently replace a persisted key.
    $key = [Security.Cryptography.ProtectedData]::Unprotect(
        [IO.File]::ReadAllBytes($keyFile), $null, [Security.Cryptography.DataProtectionScope]::CurrentUser)
} else {
    $key = New-Object byte[] 32
    $random = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $random.GetBytes($key) } finally { $random.Dispose() }
    $protected = [Security.Cryptography.ProtectedData]::Protect(
        $key, $null, [Security.Cryptography.DataProtectionScope]::CurrentUser)
    $stream = [IO.File]::Open($keyFile, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $stream.Write($protected, 0, $protected.Length) } finally { $stream.Dispose() }
}
if ($BootstrapAccounts -and !$BootstrapPassword) {
    $BootstrapPassword = Read-Host 'Development account password (not saved)' -AsSecureString
}
$values = @{
    SPRING_PROFILES_ACTIVE = 'local'
    SERVER_ADDRESS = '127.0.0.1'
    SERVER_PORT = [string]$Port
    MYDEFENSE_LOCAL_DB_PATH = (Join-Path $data 'mydefense').Replace('\', '/')
    SPRING_DATASOURCE_URL = ('jdbc:h2:file:' + (Join-Path $data 'mydefense').Replace('\', '/') + ';MODE=MySQL')
    SPRING_JPA_HIBERNATE_DDLAUTO = 'update'
    SPRING_SQL_INIT_MODE = 'never'
    MYDEFENSE_AUTH_SIGNINGKEYBASE64 = [Convert]::ToBase64String($key)
    MYDEFENSE_AUTH_LOCALACCOUNTS_ENABLED = 'true'
    MYDEFENSE_AUTH_LOCALACCOUNTS_BOOTSTRAPENABLED = ([string][bool]$BootstrapAccounts).ToLowerInvariant()
    MYDEFENSE_AUTH_LOCALACCOUNTS_USERNAMES = ($Usernames -join ',')
    MYDEFENSE_AUTH_LOCALACCOUNTS_PASSWORD = ''
}
$previous = @{}
$pointer = [IntPtr]::Zero
try {
    if ($BootstrapAccounts) {
        $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($BootstrapPassword)
        $values.MYDEFENSE_AUTH_LOCALACCOUNTS_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    foreach ($name in $values.Keys) {
        $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [Environment]::SetEnvironmentVariable($name, $values[$name], 'Process')
    }
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
    $stdout = Join-Path $logs "server-$stamp.out.log"
    $stderr = Join-Path $logs "server-$stamp.err.log"
    # Pin non-secret safety settings at CLI precedence; credentials stay out of arguments.
    $arguments = @('-jar', ('"' + $jar.FullName + '"'), '--spring.profiles.active=local',
        '--server.address=127.0.0.1', "--server.port=$Port",
        ('"--spring.datasource.url=' + $values.SPRING_DATASOURCE_URL + '"'),
        '--spring.jpa.hibernate.ddl-auto=update', '--spring.sql.init.mode=never',
        '--mydefense.auth.local-accounts.enabled=true',
        ('--mydefense.auth.local-accounts.bootstrap-enabled=' + $values.MYDEFENSE_AUTH_LOCALACCOUNTS_BOOTSTRAPENABLED))
    $process = Start-Process -FilePath $java -ArgumentList $arguments `
        -WorkingDirectory $server -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    $record = @{ processId = $process.Id; port = $Port; database = $values.MYDEFENSE_LOCAL_DB_PATH; stdout = $stdout; stderr = $stderr }
    $record | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $local "server-$Port.json") -Encoding UTF8
    [pscustomobject]$record
} finally {
    foreach ($name in $previous.Keys) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
    if ($pointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
    if ($key) { [Array]::Clear($key, 0, $key.Length) }
    $values.MYDEFENSE_AUTH_LOCALACCOUNTS_PASSWORD = $null
    $values.MYDEFENSE_AUTH_SIGNINGKEYBASE64 = $null
}

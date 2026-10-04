param([string]$Instance = 'mssqllocaldb')
$ErrorActionPreference = 'Stop'
$taskApiDirectory = Join-Path $PSScriptRoot '../src/BlogManagement.Api'
$taskSettings = Get-Content (Join-Path $taskApiDirectory 'appsettings.json') -Raw | ConvertFrom-Json
$taskConnection = $taskSettings.ConnectionStrings.DefaultConnection
if ($taskConnection -notmatch '(?i)(?:Server|Data Source)\s*=\s*\(localdb\)\\([^;]+)') {
    Write-Output 'Skipping LocalDB preparation: appsettings.json uses another SQL Server connection.'
    return
}
if (!$PSBoundParameters.ContainsKey('Instance')) { $Instance = $Matches[1].Trim() }
$taskLocalDb = Get-Command SqlLocalDB.exe -ErrorAction SilentlyContinue
if ($taskLocalDb) { $taskExecutable = $taskLocalDb.Source }
else {
    $taskExecutable = Get-ChildItem (Join-Path $env:ProgramFiles 'Microsoft SQL Server') -Directory |
        ForEach-Object { Join-Path $_.FullName 'Tools/Binn/SqlLocalDB.exe' } |
        Where-Object { Test-Path -LiteralPath $_ } | Sort-Object -Descending | Select-Object -First 1
}
if (!$taskExecutable) { throw 'SQL Server LocalDB is not installed.' }
& $taskExecutable start $Instance
if ($LASTEXITCODE -ne 0) { throw 'LocalDB failed to start.' }
$taskInfo = (& $taskExecutable info $Instance) -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Could not read LocalDB instance information.' }
$taskInfo = $taskInfo.Replace([string][char]0, '')
$taskPipeOffset = $taskInfo.IndexOf('\\.\pipe\', [StringComparison]::OrdinalIgnoreCase)
if ($taskPipeOffset -lt 0) { throw "No running named pipe reported: $taskInfo" }
$taskPipe = 'np:' + ($taskInfo.Substring($taskPipeOffset) -split '[\r\n]')[0].Trim()
$taskConnection = [regex]::Replace($taskConnection, '(?i)(?:Server|Data Source)\s*=\s*[^;]+', "Server=$taskPipe")
$taskLocalPath = Join-Path $taskApiDirectory 'appsettings.Local.json'
$taskLocalSettings = if (Test-Path -LiteralPath $taskLocalPath) {
    Get-Content -LiteralPath $taskLocalPath -Raw | ConvertFrom-Json
} else { [pscustomobject]@{} }
if (!$taskLocalSettings.ConnectionStrings) {
    $taskLocalSettings | Add-Member -NotePropertyName ConnectionStrings -NotePropertyValue ([pscustomobject]@{}) -Force
}
$taskLocalSettings.ConnectionStrings | Add-Member -NotePropertyName DefaultConnection -NotePropertyValue $taskConnection -Force
$taskLocalSettings | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $taskLocalPath -Encoding UTF8
Write-Output 'Prepared appsettings.Local.json. Restart the API in Visual Studio. Run this script again after restarting LocalDB.'

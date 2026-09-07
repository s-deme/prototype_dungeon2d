param(
    [string]$UnityEditor = 'E:\game_projects\editor\6000.3.22f1\Editor\Unity.exe',
    [switch]$Unity,
    [switch]$Build
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    if (($Unity -or $Build) -and !(Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw "Unity Editor not found: $UnityEditor" }
    dotnet test Tools/Domain.Tests.csproj --logger 'trx;LogFileName=domain.trx' --results-directory TestResults
    if ($LASTEXITCODE -ne 0) { throw 'Domain tests failed.' }
    if (Test-Path -LiteralPath $UnityEditor) {
        $editorRoot = Split-Path $UnityEditor -Parent
        dotnet build Tools/Unity.Compile.csproj "-p:UnityEditorRoot=$editorRoot" --nologo
        if ($LASTEXITCODE -ne 0) { throw 'Unity API compile check failed.' }
    }
    if ($Unity) {
        foreach ($mode in @('EditMode', 'PlayMode')) {
            $resultsPath = Join-Path $projectRoot "TestResults/$mode.xml"
            if (Test-Path -LiteralPath $resultsPath) { Remove-Item -LiteralPath $resultsPath }
            $arguments = "-batchmode -projectPath `"$projectRoot`" -runTests -testPlatform $mode -testResults `"$projectRoot\TestResults\$mode.xml`" -logFile `"$projectRoot\TestResults\$mode.log`""
            $editorProcess = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
            $editorProcess.WaitForExit()
            if ($editorProcess.ExitCode -ne 0) { throw "Unity $mode failed; read TestResults/$mode.log." }
            if (!(Test-Path -LiteralPath $resultsPath)) { throw "Unity $mode produced no test results." }
            $testRun = ([xml](Get-Content -LiteralPath $resultsPath -Raw)).'test-run'
            if ([int]$testRun.failed -ne 0 -or [int]$testRun.passed -lt 1) { throw "Unity $mode did not pass any tests or reported failures." }
        }
    }
    if ($Build) {
        $arguments = "-batchmode -quit -projectPath `"$projectRoot`" -executeMethod LanternDepths.Editor.BuildGame.Windows -logFile `"$projectRoot\TestResults\build.log`""
        $editorProcess = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
        $editorProcess.WaitForExit()
        if ($editorProcess.ExitCode -ne 0) { throw 'Unity Windows build failed.' }
    }
}
finally { Pop-Location }

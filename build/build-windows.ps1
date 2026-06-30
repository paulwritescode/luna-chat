# Build a self-contained Windows binary for luna-chat (x64).
# Run from the repo root:  ./build/build-windows.ps1

$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")

$project = "luna-chat.csproj"
$rid = "win-x64"
$out = "./publish/$rid"

Write-Host "==> Publishing $rid" -ForegroundColor Green
dotnet publish $project `
  -c Release `
  -r $rid `
  --self-contained true `
  -p:UseAppHost=true `
  -o $out

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed." -ForegroundColor Red
    exit 1
}

Write-Host "==> Done. Output at $out" -ForegroundColor Green
Write-Host ""
Write-Host "Optional: package an installer with Velopack (vpk) once installed:"
Write-Host "  dotnet tool install -g vpk"
Write-Host "  vpk pack --packId dev.paul.lunachat --packVersion 1.0.0 ``"
Write-Host "    --packDir $out --mainExe LunaChat.exe --outputDir ./dist/win"

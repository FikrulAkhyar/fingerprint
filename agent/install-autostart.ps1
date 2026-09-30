# Jalankan sekali (klik kanan file ini > "Run with PowerShell") supaya
# Fingerprint Agent otomatis nyala tiap kali Windows login — user tidak
# perlu menjalankannya manual setiap hari.
#
# Build dulu project-nya (dotnet build -c Release) sebelum menjalankan script ini.

$exePath = Join-Path $PSScriptRoot "FingerprintAgent\bin\Release\net48\FingerprintAgent.exe"

if (-not (Test-Path $exePath)) {
    Write-Host "FingerprintAgent.exe belum ditemukan di:"
    Write-Host "  $exePath"
    Write-Host "Build dulu project-nya: cd FingerprintAgent, lalu 'dotnet build -c Release'"
    exit 1
}

$startupFolder = [Environment]::GetFolderPath("Startup")
$shortcutPath = Join-Path $startupFolder "FingerprintAgent.lnk"

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = Split-Path $exePath
$shortcut.Description = "Fingerprint Agent - ZKTeco Live20R"
$shortcut.Save()

Write-Host "Selesai. Fingerprint Agent akan otomatis jalan setiap kali Windows login."
Write-Host "Shortcut dibuat di: $shortcutPath"

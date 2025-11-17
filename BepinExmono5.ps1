cd UniverseLib
& ".\build.ps1" # 建議使用 & 調用運算符，並將腳本名稱加引號，以避免 "command not recognized" 的錯誤。如果 UniverseLib 的構建是必需的，請保留此行。
cd ..

# ----------- BepInEx 5 Mono -----------
dotnet build src/CinematicUnityExplorer.sln -c Release_BIE5_Mono

$Path = "Release/CinematicUnityExplorer.BepInEx5.Mono"

# ===================================================================================
# 新增步驟: 複製 0Harmony.dll 到 ILRepack 的搜尋路徑
# ===================================================================================
# 將 $harmonySourcePath 改為相對路徑！
$harmonySourcePath = "UnityEditorPackage\Runtime\0Harmony.dll"
$harmonyTargetPath = Join-Path $Path "0Harmony.dll" # 目標是放置在 ILRepack 的搜尋路徑中

Write-Host "INFO: Checking for 0Harmony.dll at '$harmonySourcePath'"
if (Test-Path -Path $harmonySourcePath) {
    # 確保目標目錄存在
    if (-not (Test-Path -Path $Path -PathType Container)) {
        New-Item -ItemType Directory -Force -Path $Path
    }
    Copy-Item -Path $harmonySourcePath -Destination $harmonyTargetPath -Force
    Write-Host "INFO: Successfully copied 0Harmony.dll to '$harmonyTargetPath'"
} else {
    Write-Error "ERROR: 0Harmony.dll not found at '$harmonySourcePath'. Please verify the relative path from the script's execution directory."
    exit 1 # 終止腳本，因為這是個致命錯誤
}
# ===================================================================================

# ILRepack
lib/ILRepack.exe /target:library /lib:lib/net35 /lib:lib/net35/BepInEx /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.BIE5.Mono.dll $Path/CinematicUnityExplorer.BIE5.Mono.dll $Path/mcs.dll $Path/Tomlet.dll

# (cleanup and move files)
Remove-Item $Path/Tomlet.dll -ErrorAction SilentlyContinue
Remove-Item $Path/mcs.dll -ErrorAction SilentlyContinue
Remove-Item $Path/0Harmony.dll -ErrorAction SilentlyContinue # 移除作為參考的 0Harmony.dll

New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
New-Item -Path "$Path" -Name "plugins/CinematicUnityExplorer" -ItemType "directory" -Force
Move-Item -Path $Path/CinematicUnityExplorer.BIE5.Mono.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
Move-Item -Path $Path/UniverseLib.Mono.dll -Destination $Path/plugins/CinematicUnityExplorer -Force

# (create zip archive)
Remove-Item $Path/../CinematicUnityExplorer.BepInEx5.Mono.zip -ErrorAction SilentlyContinue
# 這裡使用 -Path 參數明確指定要壓縮的內容，而不是 .\$Path\*，以提高兼容性
compress-archive -Path "$Path\*" -DestinationPath "$Path/../CinematicUnityExplorer.BepInEx5.Mono.zip" -Force
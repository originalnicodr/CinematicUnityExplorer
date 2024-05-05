cd UniverseLib
.\build.ps1
cd ..

# ----------- MelonLoader IL2CPP (net6) -----------
dotnet build src/CinematicUnityExplorer.sln -c Release_ML_Cpp_net6preview
$Path = "Release\CinematicUnityExplorer.MelonLoader.IL2CPP.net6preview"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib/net6 /lib:lib/unhollowed /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.ML.IL2CPP.net6preview.dll $Path/CinematicUnityExplorer.ML.IL2CPP.net6preview.dll $Path/mcs.dll
# (cleanup and move files)
Remove-Item $Path/CinematicUnityExplorer.ML.IL2CPP.net6preview.deps.json
Remove-Item $Path/Tomlet.dll
Remove-Item $Path/mcs.dll
Remove-Item $Path/Iced.dll
Remove-Item $Path/UnhollowerBaseLib.dll
New-Item -Path "$Path" -Name "Mods" -ItemType "directory" -Force
Move-Item -Path $Path/CinematicUnityExplorer.ML.IL2CPP.net6preview.dll -Destination $Path/Mods -Force
New-Item -Path "$Path" -Name "UserLibs" -ItemType "directory" -Force
Move-Item -Path $Path/UniverseLib.IL2CPP.Unhollower.dll -Destination $Path/UserLibs -Force
# (create zip archive)
Remove-Item $Path/../CinematicUnityExplorer.MelonLoader.IL2CPP.net6preview.zip -ErrorAction SilentlyContinue
compress-archive .\$Path\* $Path/../CinematicUnityExplorer.MelonLoader.IL2CPP.net6preview.zip

# ----------- MelonLoader IL2CPP CoreCLR (net6) -----------
dotnet build src/CinematicUnityExplorer.sln -c Release_ML_Cpp_CoreCLR
$Path = "Release\CinematicUnityExplorer.MelonLoader.IL2CPP.CoreCLR"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib/net6 /lib:lib/interop /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.ML.IL2CPP.CoreCLR.dll $Path/CinematicUnityExplorer.ML.IL2CPP.CoreCLR.dll $Path/mcs.dll
# (cleanup and move files)
Remove-Item $Path/CinematicUnityExplorer.ML.IL2CPP.CoreCLR.deps.json
Remove-Item $Path/Tomlet.dll
Remove-Item $Path/mcs.dll
Remove-Item $Path/Iced.dll
Remove-Item $Path/Il2CppInterop.Common.dll
Remove-Item $Path/Il2CppInterop.Runtime.dll
Remove-Item $Path/Microsoft.Extensions.Logging.Abstractions.dll
New-Item -Path "$Path" -Name "Mods" -ItemType "directory" -Force
Move-Item -Path $Path/CinematicUnityExplorer.ML.IL2CPP.CoreCLR.dll -Destination $Path/Mods -Force
New-Item -Path "$Path" -Name "UserLibs" -ItemType "directory" -Force
Move-Item -Path $Path/UniverseLib.ML.IL2CPP.Interop.dll -Destination $Path/UserLibs -Force
# (create zip archive)
Remove-Item $Path/../CinematicUnityExplorer.MelonLoader.IL2CPP.CoreCLR.zip -ErrorAction SilentlyContinue
compress-archive .\$Path\* $Path/../CinematicUnityExplorer.MelonLoader.IL2CPP.CoreCLR.zip

# ----------- MelonLoader IL2CPP (net472) -----------
dotnet build src/CinematicUnityExplorer.sln -c Release_ML_Cpp_net472
$Path = "Release/CinematicUnityExplorer.MelonLoader.IL2CPP"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib/net472 /lib:lib/net35 /lib:lib/unhollowed /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.ML.IL2CPP.dll $Path/CinematicUnityExplorer.ML.IL2CPP.dll $Path/mcs.dll
# (cleanup and move files)
Remove-Item $Path/Tomlet.dll
Remove-Item $Path/mcs.dll
Remove-Item $Path/Iced.dll
Remove-Item $Path/UnhollowerBaseLib.dll
New-Item -Path "$Path" -Name "Mods" -ItemType "directory" -Force
Move-Item -Path $Path/CinematicUnityExplorer.ML.IL2CPP.dll -Destination $Path/Mods -Force
New-Item -Path "$Path" -Name "UserLibs" -ItemType "directory" -Force
Move-Item -Path $Path/UniverseLib.IL2CPP.Unhollower.dll -Destination $Path/UserLibs -Force
# (create zip archive)
Remove-Item $Path/../CinematicUnityExplorer.MelonLoader.IL2CPP.zip -ErrorAction SilentlyContinue
compress-archive .\$Path\* $Path/../CinematicUnityExplorer.MelonLoader.IL2CPP.zip

# ----------- MelonLoader Mono -----------
dotnet build src/CinematicUnityExplorer.sln -c Release_ML_Mono
$Path = "Release/CinematicUnityExplorer.MelonLoader.Mono"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib/net35 /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.ML.Mono.dll $Path/CinematicUnityExplorer.ML.Mono.dll $Path/mcs.dll
# (cleanup and move files)
Remove-Item $Path/Tomlet.dll
Remove-Item $Path/mcs.dll
New-Item -Path "$Path" -Name "Mods" -ItemType "directory" -Force
Move-Item -Path $Path/CinematicUnityExplorer.ML.Mono.dll -Destination $Path/Mods -Force
New-Item -Path "$Path" -Name "UserLibs" -ItemType "directory" -Force
Move-Item -Path $Path/UniverseLib.Mono.dll -Destination $Path/UserLibs -Force
# (create zip archive)
Remove-Item $Path/../CinematicUnityExplorer.MelonLoader.Mono.zip -ErrorAction SilentlyContinue
compress-archive .\$Path\* $Path/../CinematicUnityExplorer.MelonLoader.Mono.zip

# ----------- BepInEx IL2CPP -----------
dotnet build src/CinematicUnityExplorer.sln -c Release_BIE_Cpp
$Path = "Release/CinematicUnityExplorer.BepInEx.IL2CPP"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib/net472/BepInEx/build423~577 /lib:lib/unhollowed /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.BIE.IL2CPP.dll $Path/CinematicUnityExplorer.BIE.IL2CPP.dll $Path/mcs.dll $Path/Tomlet.dll
# (cleanup and move files)
Remove-Item $Path/Tomlet.dll
Remove-Item $Path/mcs.dll
Remove-Item $Path/Iced.dll
Remove-Item $Path/UnhollowerBaseLib.dll
New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
New-Item -Path "$Path" -Name "plugins/CinematicUnityExplorer" -ItemType "directory" -Force
Move-Item -Path $Path/CinematicUnityExplorer.BIE.IL2CPP.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
Move-Item -Path $Path/UniverseLib.IL2CPP.Unhollower.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
# (create zip archive)
Remove-Item $Path/../CinematicUnityExplorer.BepInEx.IL2CPP.zip -ErrorAction SilentlyContinue
compress-archive .\$Path\* $Path/../CinematicUnityExplorer.BepInEx.IL2CPP.zip

# ----------- BepInEx IL2CPP CoreCLR -----------
dotnet build src/CinematicUnityExplorer.sln -c Release_BIE_CoreCLR
$Path = "Release/CinematicUnityExplorer.BepInEx.IL2CPP.CoreCLR"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib/net472/BepInEx/build423~577 /lib:lib/net6/ /lib:lib/interop/ /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.BIE.IL2CPP.CoreCLR.dll $Path/CinematicUnityExplorer.BIE.IL2CPP.CoreCLR.dll $Path/mcs.dll $Path/Tomlet.dll
# (cleanup and move files)
Remove-Item $Path/Tomlet.dll
Remove-Item $Path/mcs.dll
Remove-Item $Path/Iced.dll
Remove-Item $Path/Il2CppInterop.Common.dll
Remove-Item $Path/Il2CppInterop.Runtime.dll
Remove-Item $Path/Microsoft.Extensions.Logging.Abstractions.dll
Remove-Item $Path/CinematicUnityExplorer.BIE.IL2CPP.CoreCLR.deps.json
New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
New-Item -Path "$Path" -Name "plugins/CinematicUnityExplorer" -ItemType "directory" -Force
Move-Item -Path $Path/CinematicUnityExplorer.BIE.IL2CPP.CoreCLR.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
Move-Item -Path $Path/UniverseLib.BIE.IL2CPP.Interop.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
# (create zip archive)
Remove-Item $Path/../CinematicUnityExplorer.BepInEx.IL2CPP.CoreCLR.zip -ErrorAction SilentlyContinue
compress-archive .\$Path\* $Path/../CinematicUnityExplorer.BepInEx.IL2CPP.CoreCLR.zip

# ----------- BepInEx Unity IL2CPP CoreCLR -----------
dotnet build src/CinematicUnityExplorer.sln -c Release_BIE_Unity_Cpp
$Path = "Release/CinematicUnityExplorer.BepInEx.Unity.IL2CPP.CoreCLR"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib/net472/BepInEx/build647+ /lib:lib/net6/ /lib:lib/interop/ /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.BIE.Unity.IL2CPP.CoreCLR.dll $Path/CinematicUnityExplorer.BIE.Unity.IL2CPP.CoreCLR.dll $Path/mcs.dll $Path/Tomlet.dll
# (cleanup and move files)
Remove-Item $Path/Tomlet.dll
Remove-Item $Path/mcs.dll
Remove-Item $Path/Iced.dll
Remove-Item $Path/Il2CppInterop.Common.dll
Remove-Item $Path/Il2CppInterop.Runtime.dll
Remove-Item $Path/Microsoft.Extensions.Logging.Abstractions.dll
Remove-Item $Path/CinematicUnityExplorer.BIE.Unity.IL2CPP.CoreCLR.deps.json
New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
New-Item -Path "$Path" -Name "plugins/CinematicUnityExplorer" -ItemType "directory" -Force
Move-Item -Path $Path/CinematicUnityExplorer.BIE.Unity.IL2CPP.CoreCLR.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
Move-Item -Path $Path/UniverseLib.BIE.IL2CPP.Interop.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
# (create zip archive)
Remove-Item $Path/../CinematicUnityExplorer.BepInEx.Unity.IL2CPP.CoreCLR.zip -ErrorAction SilentlyContinue
compress-archive .\$Path\* $Path/../CinematicUnityExplorer.BepInEx.Unity.IL2CPP.CoreCLR.zip

# ----------- BepInEx 5 Mono -----------
dotnet build src/CinematicUnityExplorer.sln -c Release_BIE5_Mono
$Path = "Release/CinematicUnityExplorer.BepInEx5.Mono"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib/net35 /lib:lib/net35/BepInEx /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.BIE5.Mono.dll $Path/CinematicUnityExplorer.BIE5.Mono.dll $Path/mcs.dll $Path/Tomlet.dll
# (cleanup and move files)
Remove-Item $Path/Tomlet.dll
Remove-Item $Path/mcs.dll
New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
New-Item -Path "$Path" -Name "plugins/CinematicUnityExplorer" -ItemType "directory" -Force
Move-Item -Path $Path/CinematicUnityExplorer.BIE5.Mono.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
Move-Item -Path $Path/UniverseLib.Mono.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
# (create zip archive)
Remove-Item $Path/../CinematicUnityExplorer.BepInEx5.Mono.zip -ErrorAction SilentlyContinue
compress-archive .\$Path\* $Path/../CinematicUnityExplorer.BepInEx5.Mono.zip

# ----------- BepInEx 6 Mono -----------
dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Mono
$Path = "Release/CinematicUnityExplorer.BepInEx6.Mono"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib/net35 /lib:lib/net35/BepInEx/build423~577 /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.BIE6.Mono.dll $Path/CinematicUnityExplorer.BIE6.Mono.dll $Path/mcs.dll $Path/Tomlet.dll
# (cleanup and move files)
Remove-Item $Path/Tomlet.dll
Remove-Item $Path/mcs.dll
New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
New-Item -Path "$Path" -Name "plugins/CinematicUnityExplorer" -ItemType "directory" -Force
Move-Item -Path $Path/CinematicUnityExplorer.BIE6.Mono.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
Move-Item -Path $Path/UniverseLib.Mono.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
# (create zip archive)
Remove-Item $Path/../CinematicUnityExplorer.BepInEx6.Mono.zip -ErrorAction SilentlyContinue
compress-archive .\$Path\* $Path/../CinematicUnityExplorer.BepInEx6.Mono.zip

# ----------- BepInEx 6 Unity Mono -----------
dotnet build src/CinematicUnityExplorer.sln -c Release_BIE6_Unity_Mono
$Path = "Release/CinematicUnityExplorer.BepInEx6.Unity.Mono"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib/net35 /lib:lib/net35/BepInEx/build647+ /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.BIE6.Unity.Mono.dll $Path/CinematicUnityExplorer.BIE6.Unity.Mono.dll $Path/mcs.dll $Path/Tomlet.dll
# (cleanup and move files)
Remove-Item $Path/Tomlet.dll
Remove-Item $Path/mcs.dll
New-Item -Path "$Path" -Name "plugins" -ItemType "directory" -Force
New-Item -Path "$Path" -Name "plugins/CinematicUnityExplorer" -ItemType "directory" -Force
Move-Item -Path $Path/CinematicUnityExplorer.BIE6.Unity.Mono.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
Move-Item -Path $Path/UniverseLib.Mono.dll -Destination $Path/plugins/CinematicUnityExplorer -Force
# (create zip archive)
Remove-Item $Path/../CinematicUnityExplorer.BepInEx6.Unity.Mono.zip -ErrorAction SilentlyContinue
compress-archive .\$Path\* $Path/../CinematicUnityExplorer.BepInEx6.Unity.Mono.zip

# ----------- Standalone Mono -----------
dotnet build src/CinematicUnityExplorer.sln -c Release_STANDALONE_Mono
$Path = "Release/CinematicUnityExplorer.Standalone.Mono"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib/net35 /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.Standalone.Mono.dll $Path/CinematicUnityExplorer.Standalone.Mono.dll $Path/mcs.dll $Path/Tomlet.dll
# (cleanup and move files)
Remove-Item $Path/Tomlet.dll
Remove-Item $Path/mcs.dll
Remove-Item $Path/../CinematicUnityExplorer.Standalone.Mono.zip -ErrorAction SilentlyContinue
compress-archive .\$Path\* $Path/../CinematicUnityExplorer.Standalone.Mono.zip

# ----------- Standalone IL2CPP -----------
dotnet build src/CinematicUnityExplorer.sln -c Release_STANDALONE_Cpp
$Path = "Release/CinematicUnityExplorer.Standalone.IL2CPP"
# ILRepack
lib/ILRepack.exe /target:library /lib:lib/net472 /lib:lib/unhollowed /lib:$Path /internalize /out:$Path/CinematicUnityExplorer.Standalone.IL2CPP.dll $Path/CinematicUnityExplorer.Standalone.IL2CPP.dll $Path/mcs.dll $Path/Tomlet.dll
# (cleanup and move files)
Remove-Item $Path/Tomlet.dll
Remove-Item $Path/mcs.dll
Remove-Item $Path/Iced.dll
Remove-Item $Path/UnhollowerBaseLib.dll
Remove-Item $Path/../CinematicUnityExplorer.Standalone.IL2CPP.zip -ErrorAction SilentlyContinue
compress-archive .\$Path\* $Path/../CinematicUnityExplorer.Standalone.IL2CPP.zip

# ----------- Editor (mono) -----------
$Path1 = "Release/CinematicUnityExplorer.Standalone.Mono"
$Path2 = "UnityEditorPackage/Runtime"
Copy-Item $Path1/CinematicUnityExplorer.STANDALONE.Mono.dll -Destination $Path2
Copy-Item $Path1/UniverseLib.Mono.dll -Destination $Path2
Remove-Item Release/CinematicUnityExplorer.Editor.zip -ErrorAction SilentlyContinue
compress-archive .\UnityEditorPackage\*  Release/CinematicUnityExplorer.Editor.zip

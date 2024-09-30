# ----------- Build UnityIGCSConnector ------------
msbuild.exe UnityIGCSConnector/UnityIGCSConnector.sln -p:Configuration=Release -p:Platform=x64

msbuild.exe UnityIGCSConnector/UnityIGCSConnector.sln -p:Configuration=Release -p:Platform=x86



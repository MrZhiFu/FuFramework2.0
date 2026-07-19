cd /d ../Tools/ProtoExport\bin\Debug\net8.0
call dotnet ProtoExport.dll --mode unity --inputPath ./../../../../../Protobuf/Proto --outputPath ./../../../../../Unity/Assets/Scripts/Hotfix/Game/AutoGen/Proto --namespaceName Hotfix.Game.Proto --isGenerateErrorCode true

pause
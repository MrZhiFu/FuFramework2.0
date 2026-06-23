cd /d ../Tools/ProtoExport\bin\Debug\net8.0
call dotnet ProtoExport.dll --mode unity --inputPath ./../../../../../Protobuf --outputPath ./../../../../../Unity/Assets/Scripts/Hotfix/Proto --namespaceName Hotfix.Proto --isGenerateErrorCode true

pause
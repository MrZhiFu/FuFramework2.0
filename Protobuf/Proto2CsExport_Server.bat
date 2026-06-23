cd /d ../Tools/ProtoExport\bin\Debug\net8.0
call dotnet ProtoExport.dll --mode server --inputPath ./../../../../../Protobuf --outputPath ./../../../../../Server/FuFramework.Proto/Proto --namespaceName FuFramework.Proto.Proto --isGenerateErrorCode true

pause
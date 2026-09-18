@echo off

rem ============================================================
rem  服务端配置表生成脚本（bin 二进制变体：数据 .bytes，cs-dotnet-bin 目标）
rem
rem  功能：
rem    1. 检测 Luban.dll 缺失时自动先构建（换机 / 新 clone 后无需手动构建）
rem    2. server 段：全部业务表与本地化表（服务端视角）
rem       → 数据产物 Server/FuFramework.Config/Json/
rem       → 代码产物 Server/FuFramework.Config/Config/（表与 bean，cs-dotnet-* 目标）
rem  用法：在 Config/ 目录下运行；末尾 pause 等待按键确认
rem  注意：改动 Luban 源码后需先手动跑 Tools/Luban/build-luban.bat 重建
rem ============================================================

rem Luban.dll 缺失时先自动构建（换机 / 新 clone 后无需手动构建）。
if not exist "..\Tools\Luban\bin\Luban.dll" (
    chcp 65001 >nul
    echo [Luban] bin/Luban.dll 未找到，先自动构建 ...
    call "..\Tools\Luban\build-luban.bat" ci
)

dotnet ../Tools/Luban/bin/Luban.dll ^
    -t server ^
    -d bin ^
    -c cs-dotnet-bin ^
    -x outputDataDir=../Server/FuFramework.Config/Json ^
    -x outputCodeDir=../Server/FuFramework.Config/Config ^
    -x tableImporter.name=fuframework ^
    --conf ./Luban.conf
pause
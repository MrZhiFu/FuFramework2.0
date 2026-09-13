#!/bin/bash
# 从源码构建 Luban 工具（Tools/Luban/source/src）→ Tools/Luban/bin
cd -P "$(dirname "$0")" || exit 1
[ -d bin ] && rm -rf bin
dotnet build source/src/Luban/Luban.csproj -c Release -o bin

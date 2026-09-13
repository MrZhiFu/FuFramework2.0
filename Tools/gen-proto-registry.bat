@echo off
rem 生成协议消息注册表（Framework/Network/Generated/ProtoMessageRegistry.g.cs）
rem proto 变更或新增 [MessageHandler] 方法后必须重新运行本脚本。
cd /d "%~dp0"
python gen-proto-registry.py
pause

#!/bin/bash
# 生成协议消息注册表（Framework/Network/Generated/ProtoMessageRegistry.g.cs）
# proto 变更或新增 [MessageHandler] 方法后必须重新运行本脚本。

# 切换目录，-P 选项是用来处理符号链接的
cd -P "$(dirname "$0")" || exit 1

# 启动生成脚本
python3 gen-proto-registry.py

# 暂停，等待用户按任意键继续，由于 shell 环境没有直接的 pause 命令，使用 read 模拟
read -p "Press any key to continue . . . " -n1 -s

echo ""  # 输出一个新行

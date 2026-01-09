#!/bin/bash

echo "=================================="
echo "  EonVientiane 服务端启动脚本"
echo "=================================="
echo ""

# 设置默认端口
PORT=7777

# 检查是否有参数
if [ $# -gt 0 ]; then
    PORT=$1
fi

echo "正在启动服务端，端口: $PORT"
echo ""

# 启动服务器
cd "$(dirname "$0")/EonVientianeServer"
dotnet run $PORT

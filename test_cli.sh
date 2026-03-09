#!/bin/bash

# 游戏CLI测试脚本

cd /home/qazokmwsxijn/Documents/EonVientiane/EonVientiane

echo "================================"
echo "启动 Eon Vientiane 游戏 CLI 版本"
echo "================================"
echo ""

# 构建项目
echo "编译项目..."
dotnet build -c Debug > /dev/null 2>&1

# 运行游戏
echo "启动游戏..."
echo ""

# 使用管道发送命令给游戏
{
    sleep 1
    echo "help"
    sleep 1
    echo "status"
    sleep 1
    echo "levels"
    sleep 1
    echo "loadlevel test"
    sleep 1
    echo "inv"
    sleep 1
    echo "equip 铁剑"
    sleep 1
    echo "inv"
    sleep 1
    echo "unequip 铁剑"
    sleep 1
    echo "exit"
} | dotnet run --project EonVientiane.CLI/EonVientiane.CLI.csproj -c Debug

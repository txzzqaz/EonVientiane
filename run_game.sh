#!/usr/bin/env bash

set -euo pipefail

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_URL="${EV_SERVER_URL:-http://127.0.0.1:5000}"
SERVER_LOG="${PROJECT_DIR}/.run_game_server.log"

cleanup() {
    if [[ -n "${SERVER_PID:-}" ]] && kill -0 "${SERVER_PID}" 2>/dev/null; then
        kill "${SERVER_PID}" 2>/dev/null || true
        wait "${SERVER_PID}" 2>/dev/null || true
    fi
}

trap cleanup EXIT INT TERM

cd "${PROJECT_DIR}"

echo "========================================"
echo "Eon Vientiane 一键运行脚本"
echo "========================================"
echo "项目目录: ${PROJECT_DIR}"
echo "服务地址: ${SERVER_URL}"
echo

echo "[1/4] 编译项目..."
dotnet build -c Debug

echo
echo "[2/4] 启动服务端..."
ASPNETCORE_URLS="${SERVER_URL}" dotnet run --project EonVientiane.Server/EonVientiane.Server.csproj -c Debug >"${SERVER_LOG}" 2>&1 &
SERVER_PID=$!
echo "服务进程 PID: ${SERVER_PID}"
echo "服务日志: ${SERVER_LOG}"

echo
echo "[3/4] 等待服务就绪..."
HEALTH_URL="${SERVER_URL}/health"
for i in {1..30}; do
    if curl -fsS "${HEALTH_URL}" >/dev/null 2>&1; then
        echo "服务已就绪: ${HEALTH_URL}"
        break
    fi

    if ! kill -0 "${SERVER_PID}" 2>/dev/null; then
        echo "❌ 服务端提前退出，启动失败。"
        echo "请查看日志: ${SERVER_LOG}"
        exit 1
    fi

    sleep 1

    if [[ "${i}" -eq 30 ]]; then
        echo "❌ 服务端在 30 秒内未就绪。"
        echo "请查看日志: ${SERVER_LOG}"
        exit 1
    fi
done

echo
echo "[4/4] 启动 CLI 游戏（交互模式）..."
echo "提示: 首次进入请先创建账户或登录。"
echo

EV_SERVER_URL="${SERVER_URL}" dotnet run --project EonVientiane.CLI/EonVientiane.CLI.csproj -c Debug

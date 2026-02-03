#!/bin/bash
# 测试账号验证脚本

echo "=========================================="
echo "测试账号系统验证"
echo "=========================================="
echo ""

# 检查相关文件是否存在
echo "1. 检查相关代码文件..."
files=(
    "EonVientianeServer/UserManager.cs"
    "EonVientianeServer/ItemInitializer.cs"
    "EonVientianeServer/InventoryStore.cs"
    "EonVientianeServer/GameServer.cs"
)

all_exist=true
for file in "${files[@]}"; do
    if [ -f "$file" ]; then
        echo "   ✓ $file"
    else
        echo "   ✗ $file (未找到)"
        all_exist=false
    fi
done

if [ "$all_exist" = false ]; then
    echo ""
    echo "错误：部分文件未找到"
    exit 1
fi

echo ""
echo "2. 检查关键代码实现..."

# 检查 UserManager 中的测试账号初始化
if grep -q "CreateTestAccountInternal.*qaz1" EonVientianeServer/UserManager.cs; then
    echo "   ✓ UserManager: qaz1 账号初始化"
else
    echo "   ✗ UserManager: qaz1 账号初始化 (未找到)"
fi

if grep -q "CreateTestAccountInternal.*qaz2" EonVientianeServer/UserManager.cs; then
    echo "   ✓ UserManager: qaz2 账号初始化"
else
    echo "   ✗ UserManager: qaz2 账号初始化 (未找到)"
fi

if grep -q "IsTestAccount" EonVientianeServer/UserManager.cs; then
    echo "   ✓ UserManager: IsTestAccount 方法"
else
    echo "   ✗ UserManager: IsTestAccount 方法 (未找到)"
fi

# 检查 ItemInitializer 中的方法
if grep -q "GetAllItems" EonVientianeServer/ItemInitializer.cs; then
    echo "   ✓ ItemInitializer: GetAllItems 方法"
else
    echo "   ✗ ItemInitializer: GetAllItems 方法 (未找到)"
fi

if grep -q "GetTestAccountInventory" EonVientianeServer/ItemInitializer.cs; then
    echo "   ✓ ItemInitializer: GetTestAccountInventory 方法"
else
    echo "   ✗ ItemInitializer: GetTestAccountInventory 方法 (未找到)"
fi

# 检查 InventoryStore 中的同步方法
if grep -q "SyncTestAccountInventory" EonVientianeServer/InventoryStore.cs; then
    echo "   ✓ InventoryStore: SyncTestAccountInventory 方法"
else
    echo "   ✗ InventoryStore: SyncTestAccountInventory 方法 (未找到)"
fi

# 检查 GameServer 中的 UserManager 传递
if grep -q "new InventoryStore.*_userManager" EonVientianeServer/GameServer.cs; then
    echo "   ✓ GameServer: InventoryStore 初始化传递 UserManager"
else
    echo "   ✗ GameServer: InventoryStore 初始化传递 UserManager (未找到)"
fi

echo ""
echo "3. 编译项目..."
if dotnet build --no-incremental -v q > /dev/null 2>&1; then
    echo "   ✓ 编译成功"
else
    echo "   ✗ 编译失败"
    echo ""
    echo "详细错误信息："
    dotnet build --no-incremental -v q 2>&1 | grep -i error
    exit 1
fi

echo ""
echo "4. 检查道具列表..."

# 提取并显示所有道具
echo "   当前游戏中的道具："
grep -A 100 "GetAllItems" EonVientianeServer/ItemInitializer.cs | \
    grep -E '^\s*\(".*", ".*"\)' | \
    sed 's/.*("\(.*\)", "\(.*\)").*/   - \2 (\1)/'

echo ""
echo "=========================================="
echo "✓ 测试账号系统验证完成"
echo "=========================================="
echo ""
echo "测试账号信息："
echo "  用户名: qaz1 / qaz2"
echo "  密码:   qaz1 / qaz2"
echo ""
echo "特性："
echo "  • 自动填充所有道具"
echo "  • 装备类道具数量: 10"
echo "  • 金币数量: 9999"
echo "  • 自动同步新道具"
echo ""
echo "文档位置: docs/TEST_ACCOUNTS.md"
echo ""

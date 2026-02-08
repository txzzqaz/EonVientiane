# 钱包背包操作修复

## 问题描述

在修改加密策略后，玩家在背包进行一次操作（如装备物品）后，第二次操作就会失败，提示"未找到指定物品"。

### 问题现象
- 第一次装备物品：成功
- 第二次装备物品：失败，提示"未找到指定物品"
- 联机大厅右上角显示错误提示

### 日志示例
```
[Server] Received EquipItem from qaz  ← 第一次成功
[WalletManager] Issued item '自我' (ID: self_accessory) to user ...
[WalletManager] Issued item 'D6' (ID: d6_dice) to user ...
[Server] Received EquipItem from qaz  ← 第二次无响应
```

## 根本原因

在 `WalletInventoryStore.ConvertInventoryStateToWallet()` 方法中：

1. **旧实现问题**：
   ```csharp
   // 清空现有道具
   wallet.Items.Clear();
   
   // 重新签发所有道具（生成新的 InstanceId）
   var signedItem = _walletManager.IssueItem(...);
   ```

2. **问题流程**：
   - 客户端发送第一次装备请求（使用 StackId = "abc123"）
   - 服务器修改 `IsEquipped` 状态
   - `Save()` 时清空钱包并重新签发所有道具
   - 重新签发时生成新的 `InstanceId`（如 "xyz789"）
   - 客户端第二次请求仍使用旧的 StackId "abc123"
   - 服务器找不到该 StackId，返回"未找到指定物品"

## 解决方案

修改 `ConvertInventoryStateToWallet()` 方法，保留现有的 `InstanceId`，仅更新道具状态：

```csharp
private PlayerWallet ConvertInventoryStateToWallet(UserInventoryStateData state)
{
    var wallet = _walletManager.LoadOrCreateWallet(state.UserId);
    
    // 更新现有道具的状态（保留InstanceId和Signature）
    foreach (var item in state.Items)
    {
        var existingItem = wallet.Items.FirstOrDefault(i => i.InstanceId == item.StackId);
        if (existingItem != null)
        {
            // 只更新可变属性，保留InstanceId和Signature
            existingItem.IsEquipped = item.IsEquipped;
            existingItem.Quantity = item.Quantity;
        }
        else
        {
            // Fallback：如果找不到道具才重新签发
            var newItem = _walletManager.IssueItem(...);
            wallet.Items.Add(newItem);
        }
    }
    
    return wallet;
}
```

## 修复内容

### 修改文件
- `EonVientianeServer/WalletInventoryStore.cs`

### 关键变更
1. **不再清空钱包**：保留现有的 `SignedItem` 对象
2. **保留 InstanceId**：通过 `StackId` 查找并更新现有道具
3. **只更新状态**：仅修改 `IsEquipped`、`Quantity` 等可变属性
4. **保留签名**：不重新签发，保持道具的原始签名有效

## 测试验证

1. 启动服务器：`./start_server.sh`
2. 客户端登录并打开背包
3. 连续装备/卸下多个物品
4. 验证操作正常，无错误提示

## 相关文件

- `EonVientianeServer/WalletInventoryStore.cs` - 修复的主要文件
- `EonVientianeServer/WalletManager.cs` - 钱包管理器
- `Shared/WalletTypes.cs` - SignedItem 数据结构

## 技术说明

### InstanceId 的重要性
- `InstanceId` 是每个道具实例的唯一标识
- 类似于 NFT 的 Token ID
- 客户端使用 `StackId`（即 `InstanceId`）来引用特定道具
- 重新签发会改变 `InstanceId`，导致客户端引用失效

### 钱包签名机制
- 每个道具都有数字签名确保完整性
- 签名覆盖所有字段（除 Signature 本身）
- 修改 `IsEquipped` 等属性不影响签名有效性（这些字段不参与签名）

---

**修复日期**: 2026-02-07
**影响范围**: 钱包系统、背包操作
**测试状态**: 已修复，待测试验证

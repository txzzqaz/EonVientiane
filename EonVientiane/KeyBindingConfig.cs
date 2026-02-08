using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace EonVientiane;

/// <summary>
/// 存储快捷键配置
/// </summary>
public class KeyBindingConfig
{
    /// <summary>
    /// PD跳过的键盘快捷键（可选）
    /// </summary>
    public Keys? PDSkipKeyboard { get; set; } = null;

    /// <summary>
    /// PD跳过的手柄快捷键（可选）
    /// </summary>
    public GamePadButton? PDSkipGamePad { get; set; } = null;

    /// <summary>
    /// 游戏手柄按钮枚举
    /// </summary>
    public enum GamePadButton
    {
        None = 0,
        A = 1,
        B = 2,
        X = 3,
        Y = 4,
        LB = 5,
        RB = 6,
        LT = 7,
        RT = 8,
        DPadUp = 9,
        DPadDown = 10,
        DPadLeft = 11,
        DPadRight = 12,
        LeftStickUp = 13,
        LeftStickDown = 14,
        LeftStickLeft = 15,
        LeftStickRight = 16,
        RightStickUp = 17,
        RightStickDown = 18,
        RightStickLeft = 19,
        RightStickRight = 20,
    }

    /// <summary>
    /// 获取按钮的显示名称
    /// </summary>
    public static string GetButtonDisplayName(GamePadButton? button)
    {
        if (!button.HasValue)
            return "未设置";

        return button.Value switch
        {
            GamePadButton.A => "A",
            GamePadButton.B => "B",
            GamePadButton.X => "X",
            GamePadButton.Y => "Y",
            GamePadButton.LB => "LB",
            GamePadButton.RB => "RB",
            GamePadButton.LT => "LT",
            GamePadButton.RT => "RT",
            GamePadButton.DPadUp => "D↑",
            GamePadButton.DPadDown => "D↓",
            GamePadButton.DPadLeft => "D←",
            GamePadButton.DPadRight => "D→",
            GamePadButton.LeftStickUp => "LS↑",
            GamePadButton.LeftStickDown => "LS↓",
            GamePadButton.LeftStickLeft => "LS←",
            GamePadButton.LeftStickRight => "LS→",
            GamePadButton.RightStickUp => "RS↑",
            GamePadButton.RightStickDown => "RS↓",
            GamePadButton.RightStickLeft => "RS←",
            GamePadButton.RightStickRight => "RS→",
            _ => "未知"
        };
    }

    /// <summary>
    /// 克隆配置
    /// </summary>
    public KeyBindingConfig Clone()
    {
        return new KeyBindingConfig
        {
            PDSkipKeyboard = PDSkipKeyboard,
            PDSkipGamePad = PDSkipGamePad
        };
    }
}

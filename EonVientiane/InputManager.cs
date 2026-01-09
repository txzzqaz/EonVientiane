using Microsoft.Xna.Framework.Input;
using System;

namespace EonVientiane;

/// <summary>
/// 输入管理器 - 处理键盘和鼠标输入
/// </summary>
public class InputManager
{
    public KeyboardState PreviousKeyboardState { get; private set; }
    
    public void Update()
    {
        PreviousKeyboardState = Keyboard.GetState();
    }
    
    /// <summary>
    /// 处理键盘输入
    /// </summary>
    public void ProcessKeyboardInput(KeyboardState keyboardState, LoginManager loginManager, InputField activeInputField)
    {
        if (activeInputField == InputField.None)
            return;
        
        Keys[] pressedKeys = keyboardState.GetPressedKeys();
        
        foreach (Keys key in pressedKeys)
        {
            // 只处理新按下的键
            if (PreviousKeyboardState.IsKeyUp(key))
            {
                string targetText = activeInputField == InputField.Username 
                    ? loginManager.Username 
                    : loginManager.Password;
                
                // 处理退格键
                if (key == Keys.Back)
                {
                    if (targetText.Length > 0)
                    {
                        targetText = targetText.Substring(0, targetText.Length - 1);
                    }
                }
                // 处理Tab键切换输入框
                else if (key == Keys.Tab)
                {
                    // 在调用处理 - 此处仅标记
                    continue;
                }
                // 处理Enter键登录
                else if (key == Keys.Enter)
                {
                    // 在调用处理 - 此处仅标记
                    continue;
                }
                // 处理字符输入
                else
                {
                    char? character = GetCharFromKey(key, keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift));
                    if (character.HasValue && targetText.Length < 20)
                    {
                        targetText += character.Value;
                    }
                }
                
                // 更新文本
                if (activeInputField == InputField.Username)
                    loginManager.Username = targetText;
                else
                    loginManager.Password = targetText;
            }
        }
    }
    
    /// <summary>
    /// 从键盘按键获取字符
    /// </summary>
    public static char? GetCharFromKey(Keys key, bool shift)
    {
        // 字母键
        if (key >= Keys.A && key <= Keys.Z)
        {
            char baseChar = (char)('a' + (key - Keys.A));
            return shift ? char.ToUpper(baseChar) : baseChar;
        }
        
        // 数字键
        if (key >= Keys.D0 && key <= Keys.D9)
        {
            if (shift)
            {
                return ")!@#$%^&*("[key - Keys.D0];
            }
            return (char)('0' + (key - Keys.D0));
        }
        
        // 小键盘数字
        if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
        {
            return (char)('0' + (key - Keys.NumPad0));
        }
        
        // 特殊字符
        switch (key)
        {
            case Keys.Space: return ' ';
            case Keys.OemPeriod: return shift ? '>' : '.';
            case Keys.OemComma: return shift ? '<' : ',';
            case Keys.OemMinus: return shift ? '_' : '-';
            case Keys.OemPlus: return shift ? '+' : '=';
            case Keys.OemQuestion: return shift ? '?' : '/';
            case Keys.OemSemicolon: return shift ? ':' : ';';
            case Keys.OemQuotes: return shift ? '"' : '\'';
            case Keys.OemOpenBrackets: return shift ? '{' : '[';
            case Keys.OemCloseBrackets: return shift ? '}' : ']';
            case Keys.OemPipe: return shift ? '|' : '\\';
            case Keys.OemTilde: return shift ? '~' : '`';
            default: return null;
        }
    }
}

using Microsoft.Xna.Framework.Input;
using System;

namespace EonVientiane;

/// <summary>
/// 登录界面输入处理器
/// </summary>
public class LoginInputHandler
{
    private int _menuWidth;
    private InputManager _inputManager;

    public LoginInputHandler(int menuWidth, InputManager inputManager)
    {
        _menuWidth = menuWidth;
        _inputManager = inputManager;
    }

    /// <summary>
    /// 处理注册窗口的输入
    /// </summary>
    public LoginInputResult HandleRegistrationInput(MouseState mouseState, MouseState previousMouseState,
        LoginManager loginManager, ref InputField activeInputField, int screenWidth, int screenHeight)
    {
        var result = new LoginInputResult();
        KeyboardState keyboardState = Keyboard.GetState();

        if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
        {
            int panelX = _menuWidth;
            int panelWidth = screenWidth - _menuWidth;
            int panelHeight = screenHeight;
            int formWidth = Math.Min(500, panelWidth - 100);
            int formHeight = 460;
            int windowX = panelX + (panelWidth - formWidth) / 2;
            int windowY = (panelHeight - formHeight) / 2;

            // 输入框区域
            var usernameInputRect = new Microsoft.Xna.Framework.Rectangle(windowX + 40, windowY + 150, formWidth - 80, 45);
            var passwordInputRect = new Microsoft.Xna.Framework.Rectangle(windowX + 40, windowY + 240, formWidth - 80, 45);
            var emailInputRect = new Microsoft.Xna.Framework.Rectangle(windowX + 40, windowY + 330, formWidth - 80, 45);

            // 两个按钮：提交、返回
            int buttonWidth = 140;
            int buttonHeight = 45;
            int buttonY = windowY + formHeight - 80;
            int spacing = 20;
            int totalWidth = buttonWidth * 2 + spacing;
            int startX = windowX + (formWidth - totalWidth) / 2;

            var submitButtonRect = new Microsoft.Xna.Framework.Rectangle(startX, buttonY, buttonWidth, buttonHeight);
            var backButtonRect = new Microsoft.Xna.Framework.Rectangle(startX + buttonWidth + spacing, buttonY, buttonWidth, buttonHeight);

            var mousePoint = new Microsoft.Xna.Framework.Point(mouseState.X, mouseState.Y);

            if (usernameInputRect.Contains(mousePoint))
            {
                activeInputField = InputField.Username;
            }
            else if (passwordInputRect.Contains(mousePoint))
            {
                activeInputField = InputField.Password;
            }
            else if (emailInputRect.Contains(mousePoint))
            {
                activeInputField = InputField.Email;
            }
            else if (submitButtonRect.Contains(mousePoint))
            {
                if (loginManager.Register(loginManager.Username, loginManager.Password, loginManager.Email))
                {
                    result.RegistrationRequested = true;
                    activeInputField = InputField.None;
                }
            }
            else if (backButtonRect.Contains(mousePoint))
            {
                result.BackToLoginClicked = true;
                activeInputField = InputField.None;
            }
            else
            {
                activeInputField = InputField.None;
            }
        }

        // 处理键盘输入（注册界面支持三种输入框）
        ProcessRegistrationKeyboardInput(Keyboard.GetState(), loginManager, activeInputField, ref result);

        return result;
    }

    private void ProcessRegistrationKeyboardInput(KeyboardState keyboardState, LoginManager loginManager, InputField activeInputField, ref LoginInputResult result)
    {
        if (activeInputField == InputField.None)
            return;

        Keys[] pressedKeys = keyboardState.GetPressedKeys();
        foreach (var key in pressedKeys)
        {
            if (_inputManager.PreviousKeyboardState.IsKeyUp(key))
            {
                string targetText = activeInputField == InputField.Username
                    ? loginManager.Username
                    : activeInputField == InputField.Password
                        ? loginManager.Password
                        : loginManager.Email;

                if (key == Keys.Back)
                {
                    if (targetText.Length > 0)
                        targetText = targetText.Substring(0, targetText.Length - 1);
                }
                else if (key == Keys.Tab)
                {
                    // 在注册表单中循环切换 Username -> Password -> Email -> Username
                    if (activeInputField == InputField.Username)
                        activeInputField = InputField.Password;
                    else if (activeInputField == InputField.Password)
                        activeInputField = InputField.Email;
                    else
                        activeInputField = InputField.Username;
                    return;
                }
                else if (key == Keys.Enter)
                {
                    if (loginManager.Register(loginManager.Username, loginManager.Password, loginManager.Email))
                    {
                        result.RegistrationRequested = true;
                    }
                    return;
                }
                else
                {
                    char? character = InputManager.GetCharFromKey(key, keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift));
                    if (character.HasValue && targetText.Length < 50)
                    {
                        targetText += character.Value;
                    }
                }

                if (activeInputField == InputField.Username)
                    loginManager.Username = targetText;
                else if (activeInputField == InputField.Password)
                    loginManager.Password = targetText;
                else
                    loginManager.Email = targetText;
            }
        }
    }
    /// <summary>
    /// 处理登录窗口的输入
    /// </summary>
    public LoginInputResult HandleInput(MouseState mouseState, MouseState previousMouseState, 
        LoginManager loginManager, ref InputField activeInputField, int screenWidth, int screenHeight)
    {
        var result = new LoginInputResult();
        KeyboardState keyboardState = Keyboard.GetState();

        if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
        {
            int panelX = _menuWidth;
            int panelWidth = screenWidth - _menuWidth;
            int panelHeight = screenHeight;
            int formWidth = Math.Min(500, panelWidth - 100);
            int formHeight = 400;
            int windowX = panelX + (panelWidth - formWidth) / 2;
            int windowY = (panelHeight - formHeight) / 2;

            // 检查用户名输入框
            Microsoft.Xna.Framework.Rectangle usernameInputRect = new Microsoft.Xna.Framework.Rectangle(windowX + 40, windowY + 150, formWidth - 80, 45);
            // 检查密码输入框
            Microsoft.Xna.Framework.Rectangle passwordInputRect = new Microsoft.Xna.Framework.Rectangle(windowX + 40, windowY + 240, formWidth - 80, 45);

            // 三个按钮：注册、登录、取消
            int buttonWidth = 120;
            int buttonHeight = 45;
            int buttonY = windowY + formHeight - 80;
            int spacing = 15;
            int totalWidth = buttonWidth * 3 + spacing * 2;
            int startX = windowX + (formWidth - totalWidth) / 2;

            // 注册按钮（最左）
            Microsoft.Xna.Framework.Rectangle registerButtonRect = new Microsoft.Xna.Framework.Rectangle(startX, buttonY, buttonWidth, buttonHeight);
            // 登录按钮（中间）
            Microsoft.Xna.Framework.Rectangle loginButtonRect = new Microsoft.Xna.Framework.Rectangle(startX + buttonWidth + spacing, buttonY, buttonWidth, buttonHeight);
            // 取消按钮（最右）
            Microsoft.Xna.Framework.Rectangle cancelButtonRect = new Microsoft.Xna.Framework.Rectangle(startX + (buttonWidth + spacing) * 2, buttonY, buttonWidth, buttonHeight);

            Microsoft.Xna.Framework.Point mousePoint = new Microsoft.Xna.Framework.Point(mouseState.X, mouseState.Y);

            // 检查点击输入框
            if (usernameInputRect.Contains(mousePoint))
            {
                activeInputField = InputField.Username;
            }
            else if (passwordInputRect.Contains(mousePoint))
            {
                activeInputField = InputField.Password;
            }
            else if (loginButtonRect.Contains(mousePoint))
            {
                // 执行登录逻辑
                if (loginManager.Login(loginManager.Username, loginManager.Password))
                {
                    // 本地校验通过，向服务器发起登录请求（由上层处理）
                    result.LoginRequested = true;
                    activeInputField = InputField.None;
                }
                else
                {
                    // 本地输入无效
                    System.Diagnostics.Debug.WriteLine("Login failed: invalid input");
                }
            }
            else if (cancelButtonRect.Contains(mousePoint))
            {
                // 关闭登录窗口
                result.CancelClicked = true;
                loginManager.ClearInput();
                activeInputField = InputField.None;
            }
            else if (registerButtonRect.Contains(mousePoint))
            {
                // 点击注册按钮
                result.RegisterClicked = true;
                System.Diagnostics.Debug.WriteLine("Register button clicked");
            }
            else
            {
                activeInputField = InputField.None;
            }
        }

        // 处理键盘输入
        ProcessKeyboardInput(keyboardState, loginManager, activeInputField, ref result);

        return result;
    }

    private void ProcessKeyboardInput(KeyboardState keyboardState, LoginManager loginManager, InputField activeInputField, ref LoginInputResult result)
    {
        if (activeInputField == InputField.None)
            return;

        Keys[] pressedKeys = keyboardState.GetPressedKeys();

        foreach (Keys key in pressedKeys)
        {
            // 只处理新按下的键
            if (_inputManager.PreviousKeyboardState.IsKeyUp(key))
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
                    // Tab处理留给Game1
                    return;
                }
                // 处理Enter键登录
                else if (key == Keys.Enter)
                {
                    if (loginManager.Login(loginManager.Username, loginManager.Password))
                    {
                        // 本地校验通过，触发登录请求
                        result.LoginRequested = true;
                    }
                    return;
                }
                // 处理字符输入
                else
                {
                    char? character = InputManager.GetCharFromKey(key, keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift));
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
}

/// <summary>
/// 登录输入结果
/// </summary>
public class LoginInputResult
{
    public bool LoginRequested { get; set; }
    public bool LoginSucceeded { get; set; }
    public bool CancelClicked { get; set; }
    public bool RegisterClicked { get; set; }
    // 注册界面使用
    public bool RegistrationRequested { get; set; }
    public bool BackToLoginClicked { get; set; }
    public UserProfile CurrentUser { get; set; }
}

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using EonVientiane.GUI.GuiMenus;
using EonVientiane.GUI.Services;
using System.Text;

namespace EonVientiane.GUI;

public partial class MainWindow : Window
{
    private readonly StringBuilder outputBuffer = new();
    private readonly CliProcessBridge cliBridge;
    private readonly string workspaceRoot;

    private IReadOnlyDictionary<string, IGuiContentModule> contentProviders =
        new Dictionary<string, IGuiContentModule>();

    public MainWindow()
    {
        InitializeComponent();

        workspaceRoot = ResolveWorkspaceRoot();
        cliBridge = new CliProcessBridge(workspaceRoot);
        cliBridge.OutputReceived += HandleCliOutput;

        ConfigureAuthActions();
        ConfigureFixedButtons();
        UpdateAuthUiState();

        Closing += OnWindowClosing;
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        await cliBridge.DisposeAsync();
    }

    // 切换到日志视图
    private void ShowLogView()
    {
        ModuleContentPanel.IsVisible = false;
        ModuleContentPanel.Content = null;
        OutputTextBox.IsVisible = true;
    }

    // 切换到模块自定义内容面板
    private void ShowModuleContent(string moduleId)
    {
        if (!contentProviders.TryGetValue(moduleId, out var provider))
        {
            ShowLogView();
            return;
        }

        try
        {
            var control = provider.CreateContentPanel();
            ModuleContentPanel.Content = control;
            OutputTextBox.IsVisible = false;
            ModuleContentPanel.IsVisible = true;
        }
        catch (Exception ex)
        {
            AppendOutput($"❌ 模块内容面板加载失败: {ex.Message}");
            ShowLogView();
        }
    }

    private void ConfigureAuthActions()
    {
        LoginButton.Click += async (_, _) => await HandleLoginAsync();
        RegisterButton.Click += (_, _) => HandleRegister();
    }

    private void ConfigureFixedButtons()
    {
        HelpButton.Click   += (_, _) => { ShowLogView(); _ = SendCommandAsync("help"); };
        AccountButton.Click += (_, _) => { ShowLogView(); _ = SendCommandAsync("account"); };
        UsersButton.Click   += (_, _) => { ShowLogView(); _ = SendCommandAsync("users"); };
        SyncButton.Click    += (_, _) => { ShowLogView(); _ = SendCommandAsync("sync"); };
        QuitButton.Click    += async (_, _) =>
        {
            ShowLogView();
            await cliBridge.LogoutAsync();
            AppendOutput("✓ 已退出当前账号");
            UpdateAuthUiState();
        };
        ClearButton.Click += (_, _) =>
        {
            ShowLogView();
            outputBuffer.Clear();
            OutputTextBox.Text = string.Empty;
            _ = SendCommandAsync("clear");
        };
    }

    private void BuildModuleMenus()
    {
        var bundle = GuiMenuLoader.LoadModules(workspaceRoot);
        contentProviders = bundle.ContentProviders;
        ModuleMenuContainer.Children.Clear();

        if (bundle.Menus.Count == 0)
        {
            ModuleMenuContainer.Children.Add(new TextBlock
            {
                Text = "当前没有可用模块菜单。请由模块提供 GUI 菜单 DLL 并放入 gui-modules/ 目录。",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });
            return;
        }

        foreach (var menu in bundle.Menus)
        {
            ModuleMenuContainer.Children.Add(CreateMenuCard(menu));
        }
    }

    private Control CreateMenuCard(GuiMenuDefinition menu)
    {
        var card = new Border
        {
            BorderThickness = new Avalonia.Thickness(1),
            BorderBrush = Avalonia.Media.Brushes.Gray,
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(8)
        };

        var rootPanel = new StackPanel { Spacing = 6 };
        rootPanel.Children.Add(new TextBlock
        {
            Text = menu.Title,
            FontWeight = Avalonia.Media.FontWeight.SemiBold
        });

        Panel buttonsPanel;
        if (menu.Layout == GuiMenuLayout.TwoColumns)
        {
            buttonsPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                ItemWidth = 150,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
        }
        else
        {
            buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 6
            };
        }

        foreach (var buttonDef in menu.Buttons)
        {
            var button = new Button
            {
                Content = buttonDef.Text,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Avalonia.Thickness(2),
                Tag = (menu.ModuleId, buttonDef.Command, buttonDef.ActivatesContent)
            };
            button.Click += OnMenuButtonClick;
            buttonsPanel.Children.Add(button);
        }

        rootPanel.Children.Add(buttonsPanel);
        card.Child = rootPanel;
        return card;
    }

    private async void OnMenuButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.Tag is not (string moduleId, string command, bool activatesContent)) return;

        if (activatesContent)
            ShowModuleContent(moduleId);
        else
            ShowLogView();

        await SendCommandAsync(command);
    }

    private async Task HandleLoginAsync()
    {
        var username = LoginUsernameTextBox.Text?.Trim() ?? string.Empty;
        var password = LoginPasswordTextBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            AppendOutput("❌ 登录失败：请输入用户名与密码");
            return;
        }

        var loginResult = await cliBridge.LoginAsync(username, password);
        AppendOutput(loginResult.Message);
        if (loginResult.Success)
        {
            UpdateAuthUiState();
            BuildModuleMenus();
            await SendCommandAsync("help");
        }
    }

    private void HandleRegister()
    {
        var username = RegisterUsernameTextBox.Text?.Trim() ?? string.Empty;
        var email    = RegisterEmailTextBox.Text?.Trim() ?? string.Empty;
        var password = RegisterPasswordTextBox.Text ?? string.Empty;
        var confirm  = RegisterConfirmPasswordTextBox.Text ?? string.Empty;

        if (password != confirm) { AppendOutput("❌ 创建账户失败：两次密码不一致"); return; }

        AppendOutput(cliBridge.CreateAccount(username, email, password));
    }

    private async Task SendCommandAsync(string command)
    {
        if (!cliBridge.IsLoggedIn) { AppendOutput("❌ 请先登录"); return; }
        AppendOutput($"> {command}");
        await cliBridge.SendInputAsync(command);
    }

    private void HandleCliOutput(string line) =>
        Dispatcher.UIThread.Post(() => AppendOutput(line));

    private void AppendOutput(string line)
    {
        outputBuffer.AppendLine(line);
        OutputTextBox.Text = outputBuffer.ToString();
        OutputTextBox.CaretIndex = OutputTextBox.Text?.Length ?? 0;
    }

    private void UpdateAuthUiState()
    {
        var loggedIn = cliBridge.IsLoggedIn;
        AuthPanel.IsVisible  = !loggedIn;
        GamePanel.IsVisible  = loggedIn;

        HelpButton.IsEnabled    = loggedIn;
        AccountButton.IsEnabled = loggedIn;
        UsersButton.IsEnabled   = loggedIn;
        SyncButton.IsEnabled    = loggedIn;
        ClearButton.IsEnabled   = true;
        QuitButton.IsEnabled    = loggedIn;

        var name = cliBridge.CurrentUsername;
        UserStatusText.Text = loggedIn && !string.IsNullOrWhiteSpace(name)
            ? $"当前用户: {name}" : "未登录";

        if (!loggedIn) ShowLogView();
    }

    private static string ResolveWorkspaceRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "EonVientiane.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return Directory.GetCurrentDirectory();
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using EonVientiane.GUI.GuiMenus;
using EonVientiane.GUI.Services;
using System.Text;

namespace EonVientiane.GUI;

public partial class MainWindow : Window
{
    private readonly StringBuilder outputBuffer = new();
    private readonly CliProcessBridge cliBridge;
    private string? activeContentModuleId;

    private IReadOnlyDictionary<string, GuiContentProviderDefinition> contentProviders =
        new Dictionary<string, GuiContentProviderDefinition>();

    public MainWindow()
    {
        InitializeComponent();

        cliBridge = new CliProcessBridge(ResolveWorkspaceRoot());
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
        activeContentModuleId = null;
        ModuleContentPanel.IsVisible = false;
        ModuleContentPanel.Content = null;
        OutputTextBox.IsVisible = true;
    }

    // 切换到模块结构化内容面板
    private void ShowModuleContent(string moduleId)
    {
        if (!contentProviders.TryGetValue(moduleId, out var provider))
        {
            ShowLogView();
            return;
        }

        if (!cliBridge.TryGetStructuredContent(provider, out var content, out var errorMessage))
        {
            AppendOutput(errorMessage);
            ShowLogView();
            return;
        }

        activeContentModuleId = moduleId;
        ModuleContentPanel.Content = CreateStructuredContentControl(content);
        OutputTextBox.IsVisible = false;
        ModuleContentPanel.IsVisible = true;
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
        var bundle = GuiMenuLoader.LoadModules();
        contentProviders = bundle.ContentProviders;
        ModuleMenuContainer.Children.Clear();

        if (bundle.Menus.Count == 0)
        {
            ModuleMenuContainer.Children.Add(new TextBlock
            {
                Text = "当前没有可用模块菜单。请由已加载业务模块提供 GetGuiMenuDefinition()。",
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

        if (!string.IsNullOrWhiteSpace(command))
        {
            await SendCommandAsync(command);
        }
    }

    private Control CreateStructuredContentControl(GuiStructuredContentDefinition content)
    {
        if (string.Equals(content.ModuleId, "equipment", StringComparison.OrdinalIgnoreCase))
        {
            return CreateEquipmentContentControl(content);
        }

        var root = new StackPanel
        {
            Spacing = 10,
            Margin = new Avalonia.Thickness(4)
        };

        root.Children.Add(new TextBlock
        {
            Text = content.Title,
            FontSize = 22,
            FontWeight = FontWeight.Bold
        });

        foreach (var section in content.Sections)
        {
            root.Children.Add(CreateStructuredContentSection(section));
        }

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = root
        };
    }

    private Control CreateEquipmentContentControl(GuiStructuredContentDefinition content)
    {
        var availableDice = content.Sections.FirstOrDefault(x => x.Title.StartsWith("未装备骰子", StringComparison.OrdinalIgnoreCase));
        var availableAccessories = content.Sections.FirstOrDefault(x => x.Title.StartsWith("未装备饰品", StringComparison.OrdinalIgnoreCase));
        var diceSlots = content.Sections.FirstOrDefault(x => x.Title.StartsWith("骰子位", StringComparison.OrdinalIgnoreCase));
        var accessorySlots = content.Sections.FirstOrDefault(x => x.Title.StartsWith("饰品位", StringComparison.OrdinalIgnoreCase));
        var equippedAccessories = content.Sections.FirstOrDefault(x => x.Title.StartsWith("已装备饰品", StringComparison.OrdinalIgnoreCase));

        var root = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("430,*"),
            Margin = new Avalonia.Thickness(4)
        };

        var leftPanel = new StackPanel { Spacing = 10 };
        leftPanel.Children.Add(new TextBlock
        {
            Text = "未装备物品",
            FontSize = 20,
            FontWeight = FontWeight.Bold
        });

        var availableGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*")
        };

        var diceColumn = CreateEquipmentColumn(availableDice ?? new GuiStructuredContentSection("未装备骰子 (0)", Array.Empty<GuiStructuredContentItem>()));
        diceColumn.Margin = new Avalonia.Thickness(0, 0, 5, 0);
        availableGrid.Children.Add(diceColumn);
        var accessoryColumn = CreateEquipmentColumn(availableAccessories ?? new GuiStructuredContentSection("未装备饰品 (0)", Array.Empty<GuiStructuredContentItem>()));
        accessoryColumn.Margin = new Avalonia.Thickness(5, 0, 0, 0);
        Grid.SetColumn(accessoryColumn, 1);
        availableGrid.Children.Add(accessoryColumn);
        leftPanel.Children.Add(availableGrid);

        var rightPanel = new StackPanel { Spacing = 12 };
        rightPanel.Children.Add(new TextBlock
        {
            Text = content.Title,
            FontSize = 20,
            FontWeight = FontWeight.Bold
        });

        rightPanel.Children.Add(CreateEquipmentDiceSlots(diceSlots ?? new GuiStructuredContentSection("骰子位 0/8", Array.Empty<GuiStructuredContentItem>())));
        rightPanel.Children.Add(CreateEquipmentAccessorySlots(accessorySlots ?? new GuiStructuredContentSection("饰品位 0/12", Array.Empty<GuiStructuredContentItem>())));
        rightPanel.Children.Add(CreateEquipmentColumn(equippedAccessories ?? new GuiStructuredContentSection("已装备饰品 (0)", Array.Empty<GuiStructuredContentItem>())));

        root.Children.Add(new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Margin = new Avalonia.Thickness(0, 0, 6, 0),
            Content = leftPanel
        });

        var rightScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Margin = new Avalonia.Thickness(6, 0, 0, 0),
            Content = rightPanel
        };
        Grid.SetColumn(rightScroll, 1);
        root.Children.Add(rightScroll);

        return root;
    }

    private Control CreateEquipmentColumn(GuiStructuredContentSection section)
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = section.Title,
            FontSize = 16,
            FontWeight = FontWeight.SemiBold
        });

        if (section.Items.Count == 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "暂无内容",
                Foreground = Brushes.Gray
            });
        }
        else
        {
            foreach (var item in section.Items)
            {
                panel.Children.Add(CreateStructuredContentItem(item));
            }
        }

        return new Border
        {
            BorderThickness = new Avalonia.Thickness(1),
            BorderBrush = Brushes.Gray,
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(10),
            Child = panel
        };
    }

    private Control CreateEquipmentDiceSlots(GuiStructuredContentSection section)
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = section.Title,
            FontSize = 16,
            FontWeight = FontWeight.SemiBold
        });

        var wrap = new WrapPanel
        {
            ItemWidth = 180,
            Orientation = Orientation.Horizontal
        };

        foreach (var item in section.Items)
        {
            wrap.Children.Add(CreateStructuredContentItem(item));
        }

        panel.Children.Add(wrap);

        return new Border
        {
            BorderThickness = new Avalonia.Thickness(1),
            BorderBrush = Brushes.Gray,
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(10),
            Child = panel
        };
    }

    private Control CreateEquipmentAccessorySlots(GuiStructuredContentSection section)
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = section.Title,
            FontSize = 16,
            FontWeight = FontWeight.SemiBold
        });

        var wrap = new WrapPanel
        {
            ItemWidth = 64,
            Orientation = Orientation.Horizontal
        };

        foreach (var item in section.Items)
        {
            wrap.Children.Add(new Border
            {
                BorderThickness = new Avalonia.Thickness(1),
                BorderBrush = Brushes.DarkSlateGray,
                Background = string.Equals(item.Badge, "占用", StringComparison.OrdinalIgnoreCase)
                    ? Brushes.SteelBlue
                    : Brushes.Transparent,
                CornerRadius = new Avalonia.CornerRadius(4),
                Margin = new Avalonia.Thickness(2),
                Padding = new Avalonia.Thickness(8, 10),
                Child = new TextBlock
                {
                    Text = item.PrimaryText,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            });
        }

        panel.Children.Add(wrap);
        return new Border
        {
            BorderThickness = new Avalonia.Thickness(1),
            BorderBrush = Brushes.Gray,
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(10),
            Child = panel
        };
    }

    private Control CreateStructuredContentSection(GuiStructuredContentSection section)
    {
        var sectionPanel = new StackPanel { Spacing = 8 };
        sectionPanel.Children.Add(new TextBlock
        {
            Text = section.Title,
            FontSize = 16,
            FontWeight = FontWeight.SemiBold
        });

        if (section.Items.Count == 0)
        {
            sectionPanel.Children.Add(new TextBlock
            {
                Text = "暂无内容",
                Foreground = Brushes.Gray
            });
        }
        else
        {
            foreach (var item in section.Items)
            {
                sectionPanel.Children.Add(CreateStructuredContentItem(item));
            }
        }

        return new Border
        {
            BorderThickness = new Avalonia.Thickness(1),
            BorderBrush = Brushes.Gray,
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(10),
            Child = sectionPanel
        };
    }

    private Control CreateStructuredContentItem(GuiStructuredContentItem item)
    {
        var topRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };

        topRow.Children.Add(new TextBlock
        {
            Text = item.PrimaryText,
            FontWeight = FontWeight.Medium,
            TextWrapping = TextWrapping.Wrap
        });

        if (!string.IsNullOrWhiteSpace(item.Badge))
        {
            var badge = new Border
            {
                Background = Brushes.DimGray,
                CornerRadius = new Avalonia.CornerRadius(10),
                Padding = new Avalonia.Thickness(8, 2),
                HorizontalAlignment = HorizontalAlignment.Right,
                Child = new TextBlock
                {
                    Text = item.Badge,
                    Foreground = Brushes.White,
                    FontSize = 12
                }
            };
            Grid.SetColumn(badge, 1);
            topRow.Children.Add(badge);
        }

        var itemPanel = new StackPanel { Spacing = 4 };
        itemPanel.Children.Add(topRow);

        if (!string.IsNullOrWhiteSpace(item.SecondaryText))
        {
            itemPanel.Children.Add(new TextBlock
            {
                Text = item.SecondaryText,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.LightGray
            });
        }

        if (!string.IsNullOrWhiteSpace(item.ActionCommand))
        {
            var actionButton = new Button
            {
                Content = string.IsNullOrWhiteSpace(item.ActionText) ? "执行" : item.ActionText,
                HorizontalAlignment = HorizontalAlignment.Right,
                Tag = item.ActionCommand,
                Margin = new Avalonia.Thickness(0, 4, 0, 0)
            };
            actionButton.Click += OnContentActionButtonClick;
            itemPanel.Children.Add(actionButton);
        }

        return new Border
        {
            BorderThickness = new Avalonia.Thickness(1),
            BorderBrush = Brushes.DarkSlateGray,
            CornerRadius = new Avalonia.CornerRadius(4),
            Padding = new Avalonia.Thickness(8),
            Child = itemPanel
        };
    }

    private async void OnContentActionButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string command || string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        await SendCommandAsync(command);

        if (!string.IsNullOrWhiteSpace(activeContentModuleId))
        {
            ShowModuleContent(activeContentModuleId);
        }
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

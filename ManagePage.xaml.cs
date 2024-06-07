using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace MinecraftServerLauncher
{
    public sealed partial class ManagePage : Page
    {
        public ObservableCollection<MinecraftServer> Servers { get; set; }
        private bool _isDialogOpen = false; // 用于跟踪是否有对话框打开

        public ManagePage()
        {
            this.InitializeComponent();
            Servers = new ObservableCollection<MinecraftServer>();
            ServerList.ItemsSource = Servers;
            UpdateEmptyMessageVisibility();
        }

        private async void CreateServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDialogOpen) return; // 如果有对话框打开，则返回
            _isDialogOpen = true;

            var dialogContent = new StackPanel
            {
                Children =
                {
                    CreateTextBox("服务器名称", "输入服务器名称", "例子: 我的服务器"),
                    CreateTextBoxWithFilePicker("服务端核心位置", "选择服务端核心位置", true, "例子: C:\\Minecraft\\server.jar"),
                    CreateTextBox("启动参数", "输入启动参数", "例子: -Xmx1024M -Xms1024M -jar minecraft_server.jar nogui"),
                    CreateTextBoxWithFilePicker("Java地址", "选择Java地址", true, "例子: C:\\Program Files\\Java\\jdk-16\\bin\\java.exe"),
                    CreateTextBoxWithFilePicker("服务器文件地址", "选择服务器文件地址", true, "例子: C:\\Minecraft\\server", isFolderPicker: true)
                }
            };

            var dialog = new ContentDialog
            {
                Title = "创建新服务器",
                PrimaryButtonText = "创建",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
                Width = 1000, // 设置对话框宽度
                Content = dialogContent
            };

            var result = await dialog.ShowAsync();
            _isDialogOpen = false; // 对话框关闭后重置标志位

            if (result == ContentDialogResult.Primary)
            {
                var serverNameBox = (TextBox)((Grid)((StackPanel)dialogContent.Children[0]).Children[0]).Children[0];
                var serverCorePathBox = (TextBox)((Grid)((StackPanel)dialogContent.Children[1]).Children[0]).Children[0];
                var startArgumentsBox = (TextBox)((Grid)((StackPanel)dialogContent.Children[2]).Children[0]).Children[0];
                var javaPathBox = (TextBox)((Grid)((StackPanel)dialogContent.Children[3]).Children[0]).Children[0];
                var serverFilePathBox = (TextBox)((Grid)((StackPanel)dialogContent.Children[4]).Children[0]).Children[0];

                var server = new MinecraftServer
                {
                    Name = serverNameBox.Text,
                    Info = $"{serverCorePathBox.Text}",
                    Address = $"{serverFilePathBox.Text}",
                    Port = "25565"
                };

                Servers.Add(server);
                UpdateEmptyMessageVisibility();

                // 创建服务器文件夹并复制服务端核心
                await CreateServerFolderAndCopyCore(server.Name, serverCorePathBox.Text, serverFilePathBox.Text);

                // 创建 start.bat 文件
                await CreateStartBatFile(serverFilePathBox.Text, javaPathBox.Text, startArgumentsBox.Text);
            }
        }

        private async Task CreateServerFolderAndCopyCore(string serverName, string serverCorePath, string serverFilePath)
        {
            try
            {
                // 创建服务器文件夹
                var serverFolder = Path.Combine(serverFilePath, serverName);
                if (!Directory.Exists(serverFolder))
                {
                    Directory.CreateDirectory(serverFolder);
                }

                // 复制服务端核心到服务器文件夹
                var coreFileName = Path.GetFileName(serverCorePath);
                var destinationPath = Path.Combine(serverFolder, coreFileName);
                if (File.Exists(serverCorePath))
                {
                    File.Copy(serverCorePath, destinationPath, true);
                }
            }
            catch (Exception ex)
            {
                if (!_isDialogOpen)
                {
                    _isDialogOpen = true;
                    var dialog = new ContentDialog
                    {
                        Title = "错误",
                        Content = $"创建服务器文件夹或复制服务端核心时发生错误：{ex.Message}",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                    _isDialogOpen = false;
                }
            }
        }

        private async Task CreateStartBatFile(string serverFilePath, string javaPath, string startArguments)
        {
            try
            {
                var startBatPath = Path.Combine(serverFilePath, "start.bat");
                using (StreamWriter writer = new StreamWriter(startBatPath))
                {
                    writer.WriteLine("@echo off");
                    writer.WriteLine("title Minecraft Server");
                    writer.WriteLine($"cd /d {serverFilePath}");
                    writer.WriteLine("echo Starting Minecraft server...");
                    writer.WriteLine($"{javaPath} {startArguments}");
                    writer.WriteLine("echo.");
                    writer.WriteLine("echo Server stopped. Press any key to continue...");
                    writer.WriteLine("pause >nul");
                }
            }
            catch (Exception ex)
            {
                if (!_isDialogOpen)
                {
                    _isDialogOpen = true;
                    var dialog = new ContentDialog
                    {
                        Title = "错误",
                        Content = $"创建 start.bat 文件时发生错误：{ex.Message}",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                    _isDialogOpen = false;
                }
            }
        }

        private void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            // 启动服务器逻辑
        }

        private void EditServerButton_Click(object sender, RoutedEventArgs e)
        {
            // 编辑服务器逻辑
        }

        private void DeleteServerButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var server = button.DataContext as MinecraftServer;
            Servers.Remove(server);
            UpdateEmptyMessageVisibility();
        }

        private void UpdateEmptyMessageVisibility()
        {
            EmptyMessage.Visibility = Servers.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private StackPanel CreateTextBox(string header, string placeholderText, string exampleText)
        {
            var stackPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 27, 0, 0) };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var textBox = new TextBox { Header = header, PlaceholderText = placeholderText, Name = header };
            Grid.SetColumn(textBox, 0);
            grid.Children.Add(textBox);

            stackPanel.Children.Add(grid);

            var exampleTextBlock = new TextBlock
            {
                Text = exampleText,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                Margin = new Thickness(0, 5, 0, 0)
            };
            stackPanel.Children.Add(exampleTextBlock);

            return stackPanel;
        }

        private StackPanel CreateTextBoxWithFilePicker(string header, string placeholderText, bool isFilePicker, string exampleText, bool isFolderPicker = false)
        {
            var stackPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 27, 0, 0) };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textBox = new TextBox { Header = header, PlaceholderText = placeholderText, Name = header };
            Grid.SetColumn(textBox, 0);
            grid.Children.Add(textBox);

            var button = new Button { Content = "浏览", Margin = new Thickness(12, 27, 0, 0), Width = 60, VerticalAlignment = VerticalAlignment.Center };
            button.Click += async (sender, e) =>
            {
                if (isFilePicker)
                {
                    if (isFolderPicker)
                    {
                        var picker = new FolderPicker();
                        picker.SuggestedStartLocation = PickerLocationId.Desktop;
                        picker.FileTypeFilter.Add("*");

                        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                        StorageFolder folder = await picker.PickSingleFolderAsync();
                        if (folder != null)
                        {
                            textBox.Text = folder.Path;
                        }
                    }
                    else
                    {
                        var picker = new FileOpenPicker();
                        picker.SuggestedStartLocation = PickerLocationId.Desktop;
                        picker.FileTypeFilter.Add("*");

                        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                        StorageFile file = await picker.PickSingleFileAsync();
                        if (file != null)
                        {
                            textBox.Text = file.Path;
                        }
                    }
                }
            };
            Grid.SetColumn(button, 1);
            grid.Children.Add(button);

            stackPanel.Children.Add(grid);

            var exampleTextBlock = new TextBlock
            {
                Text = exampleText,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                Margin = new Thickness(0, 5, 0, 0)
            };
            stackPanel.Children.Add(exampleTextBlock);

            return stackPanel;
        }
    }

    public class MinecraftServer
    {
        public string Name { get; set; }
        public string Info { get; set; }
        public string Address { get; set; }
        public string Port { get; set; }
    }
}

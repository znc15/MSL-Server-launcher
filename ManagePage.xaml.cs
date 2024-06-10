using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.UI.Xaml.Media;

namespace MinecraftServerLauncher
{
    public sealed partial class ManagePage : Page
    {
        private const string ServersFileName = "servers.json";
        private bool _isDialogOpen = false;

        public ObservableCollection<MinecraftServer> Servers { get; set; }

        public ManagePage()
        {
            this.InitializeComponent();
            Servers = new ObservableCollection<MinecraftServer>();
            ServerList.ItemsSource = Servers;
            LoadServersFromFile();
            UpdateEmptyMessageVisibility();
            Servers.CollectionChanged += (s, e) => UpdateEmptyMessageVisibility();
        }

        private async void CreateServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDialogOpen) return;
            _isDialogOpen = true;

            var dialog = CreateServerDialog("创建新服务器");

            var result = await dialog.ShowAsync();
            _isDialogOpen = false;

            if (result == ContentDialogResult.Primary)
            {
                var server = ExtractServerFromDialog(dialog.Content as StackPanel);
                await SetupServer(server);
                server.Port = await ReadServerPortAsync(Path.Combine(server.Address, server.Name, "server.properties"));
                Servers.Add(server);
                SaveServersToFile();
            }
        }

        private async void EditServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDialogOpen) return;
            _isDialogOpen = true;

            var button = sender as Button;
            var server = button.DataContext as MinecraftServer;

            var dialog = CreateServerDialog("编辑服务器", server);

            var result = await dialog.ShowAsync();
            _isDialogOpen = false;

            if (result == ContentDialogResult.Primary)
            {
                UpdateServerFromDialog(dialog.Content as StackPanel, server);
                var serverFolderPath = Path.Combine(server.Address, server.Name);
                await CreateStartBatFile(serverFolderPath, server.JavaPath, server.StartArguments);
                SaveServersToFile();
            }
        }

        private async void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var server = button.DataContext as MinecraftServer;
            var startBatPath = Path.Combine(server.Address, server.Name, "start.bat");

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = startBatPath,
                    WorkingDirectory = Path.Combine(server.Address, server.Name),
                    CreateNoWindow = false
                });
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("启动服务器时发生错误", ex.Message);
            }
        }

        private async void DeleteServerButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var server = button.DataContext as MinecraftServer;
            Servers.Remove(server);
            UpdateEmptyMessageVisibility();

            var serverFolderPath = Path.Combine(server.Address, server.Name);
            try
            {
                if (Directory.Exists(serverFolderPath))
                {
                    Directory.Delete(serverFolderPath, true);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("删除服务器文件夹时发生错误", ex.Message);
            }

            SaveServersToFile();
        }

        private void UpdateEmptyMessageVisibility()
        {
            EmptyMessage.Visibility = Servers.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void SaveServersToFile()
        {
            var serversData = JsonSerializer.Serialize(Servers);
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(ServersFileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, serversData);
        }

        private async void LoadServersFromFile()
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync(ServersFileName);
                var serversData = await FileIO.ReadTextAsync(file);
                var servers = JsonSerializer.Deserialize<ObservableCollection<MinecraftServer>>(serversData);

                foreach (var server in servers)
                {
                    server.Port = await ReadServerPortAsync(Path.Combine(server.Address, server.Name, "server.properties"));
                    Servers.Add(server);
                }
            }
            catch (FileNotFoundException)
            {
                // 文件未找到，忽略
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("加载服务器信息时发生错误", ex.Message);
            }
        }

        private ContentDialog CreateServerDialog(string title, MinecraftServer server = null)
        {
            var dialogContent = new StackPanel
            {
                Width = 500,
                Children =
        {
            CreateTextBox("服务器名称", server?.Name ?? string.Empty, "输入服务器名称", "例子: 我的服务器"),
            CreateTextBoxWithFilePicker("服务端核心位置", server?.Info ?? string.Empty, "选择服务端核心位置", "例子: C:\\Minecraft\\server.jar"),
            CreateTextBox("启动参数", server?.StartArguments ?? string.Empty, "输入启动参数", "例子: -Xmx1024M -Xms1024M -jar minecraft_server.jar nogui"),
            CreateTextBoxWithFilePicker("Java地址", server?.JavaPath ?? string.Empty, "选择Java地址", "例子: C:\\Program Files\\Java\\jdk-16\\bin\\java.exe"),
            CreateTextBoxWithFilePicker("服务器文件地址", server?.Address ?? string.Empty, "选择服务器文件地址", "例子: C:\\Minecraft\\server", isFolderPicker: true)
        }
            };

            return new ContentDialog
            {
                Title = title,
                PrimaryButtonText = "保存",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
                Width = 600,
                Content = dialogContent
            };
        }

        private MinecraftServer ExtractServerFromDialog(StackPanel dialogContent)
        {
            var serverNameBox = (TextBox)((Grid)((StackPanel)dialogContent.Children[0]).Children[0]).Children[0];
            var serverCorePathBox = (TextBox)((Grid)((StackPanel)dialogContent.Children[1]).Children[0]).Children[0];
            var startArgumentsBox = (TextBox)((Grid)((StackPanel)dialogContent.Children[2]).Children[0]).Children[0];
            var javaPathBox = (TextBox)((Grid)((StackPanel)dialogContent.Children[3]).Children[0]).Children[0];
            var serverFilePathBox = (TextBox)((Grid)((StackPanel)dialogContent.Children[4]).Children[0]).Children[0];

            return new MinecraftServer
            {
                Name = serverNameBox.Text,
                Info = serverCorePathBox.Text,
                StartArguments = startArgumentsBox.Text,
                Address = serverFilePathBox.Text,
                JavaPath = javaPathBox.Text
            };
        }

        private void UpdateServerFromDialog(StackPanel dialogContent, MinecraftServer server)
        {
            var serverNameBox = (TextBox)((Grid)((StackPanel)dialogContent.Children[0]).Children[0]).Children[0];
            var serverCorePathBox = (TextBox)((Grid)((StackPanel)dialogContent.Children[1]).Children[0]).Children[0];
            var startArgumentsBox = (TextBox)((Grid)((StackPanel)dialogContent.Children[2]).Children[0]).Children[0];
            var javaPathBox = (TextBox)((Grid)((StackPanel)dialogContent.Children[3]).Children[0]).Children[0];
            var serverFilePathBox = (TextBox)((Grid)((StackPanel)dialogContent.Children[4]).Children[0]).Children[0];

            server.Name = serverNameBox.Text;
            server.Info = serverCorePathBox.Text;
            server.StartArguments = startArgumentsBox.Text;
            server.Address = serverFilePathBox.Text;
            server.JavaPath = javaPathBox.Text;
        }

        private async Task SetupServer(MinecraftServer server)
        {
            var serverFolderPath = await CreateServerFolderAndCopyCore(server.Name, server.Info, server.Address);
            await CreateStartBatFile(serverFolderPath, server.JavaPath, server.StartArguments);
            await CreateEulaFile(serverFolderPath);
            server.Port = await ReadServerPortAsync(Path.Combine(server.Address, server.Name, "server.properties"));
        }

        private async Task<string> CreateServerFolderAndCopyCore(string serverName, string serverCorePath, string serverFilePath)
        {
            var serverFolder = Path.Combine(serverFilePath, serverName);
            try
            {
                if (!Directory.Exists(serverFolder))
                {
                    Directory.CreateDirectory(serverFolder);
                }

                var coreFileName = Path.GetFileName(serverCorePath);
                var destinationPath = Path.Combine(serverFolder, coreFileName);
                if (File.Exists(serverCorePath))
                {
                    File.Copy(serverCorePath, destinationPath, true);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("创建服务器文件夹或复制服务端核心时发生错误", ex.Message);
            }
            return serverFolder;
        }

        private async Task CreateStartBatFile(string serverFolderPath, string javaPath, string startArguments)
        {
            try
            {
                var startBatPath = Path.Combine(serverFolderPath, "start.bat");
                using (StreamWriter writer = new StreamWriter(startBatPath))
                {
                    writer.WriteLine("@echo off");
                    writer.WriteLine("title Minecraft Server");
                    writer.WriteLine("echo Starting Minecraft server...");
                    writer.WriteLine($"{javaPath} {startArguments}");
                    writer.WriteLine("echo.");
                    writer.WriteLine("echo Server stopped. Press any key to continue...");
                    writer.WriteLine("pause >nul");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("创建 start.bat 文件时发生错误", ex.Message);
            }
        }

        private async Task CreateEulaFile(string serverFolderPath)
        {
            try
            {
                var eulaPath = Path.Combine(serverFolderPath, "eula.txt");
                using (StreamWriter writer = new StreamWriter(eulaPath))
                {
                    writer.WriteLine("eula=true");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("创建 eula.txt 文件时发生错误", ex.Message);
            }
        }

        private async Task<string> ReadServerPortAsync(string propertiesFilePath)
        {
            try
            {
                if (File.Exists(propertiesFilePath))
                {
                    var lines = await File.ReadAllLinesAsync(propertiesFilePath);
                    var portLine = lines.FirstOrDefault(line => line.StartsWith("server-port="));
                    if (portLine != null)
                    {
                        return portLine.Split('=')[1].Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("读取 server.properties 文件时发生错误", ex.Message);
            }
            return "无法获取到端口号";
        }

        private async Task ShowErrorDialog(string title, string message)
        {
            if (!_isDialogOpen)
            {
                _isDialogOpen = true;
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = message,
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                _isDialogOpen = false;
            }
        }

        private StackPanel CreateTextBox(string header, string text, string placeholderText, string exampleText)
        {
            var stackPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 27, 0, 0) };

            var grid = new Grid { Width = 460 };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var textBox = new TextBox { Header = header, PlaceholderText = placeholderText, Name = header, Text = text };
            Grid.SetRow(textBox, 0);
            grid.Children.Add(textBox);

            var exampleTextBlock = new TextBlock
            {
                Text = exampleText,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                Margin = new Thickness(0, 5, 0, 0)
            };
            Grid.SetRow(exampleTextBlock, 1);
            grid.Children.Add(exampleTextBlock);

            stackPanel.Children.Add(grid);

            return stackPanel;
        }

        private StackPanel CreateTextBoxWithFilePicker(string header, string text, string placeholderText, string exampleText, bool isFolderPicker = false)
        {
            var stackPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 27, 0, 0) };

            var grid = new Grid { Width = 460 };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textBox = new TextBox { Header = header, PlaceholderText = placeholderText, Name = header, Text = text };
            Grid.SetColumn(textBox, 0);
            Grid.SetRow(textBox, 0);
            grid.Children.Add(textBox);

            var button = new Button { Content = "浏览", Margin = new Thickness(10, 27, 0, 0), Width = 60, VerticalAlignment = VerticalAlignment.Center };
            button.Click += async (sender, e) =>
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
            };
            Grid.SetColumn(button, 1);
            Grid.SetRow(button, 0);
            grid.Children.Add(button);

            var exampleTextBlock = new TextBlock
            {
                Text = exampleText,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                Margin = new Thickness(0, 5, 0, 0)
            };
            Grid.SetColumn(exampleTextBlock, 0);
            Grid.SetColumnSpan(exampleTextBlock, 2);
            Grid.SetRow(exampleTextBlock, 1);
            grid.Children.Add(exampleTextBlock);

            stackPanel.Children.Add(grid);

            return stackPanel;
        }
    }

    public class MinecraftServer
    {
        public string Name { get; set; }
        public string Info { get; set; }
        public string Address { get; set; }
        public string Port { get; set; }
        public string JavaPath { get; set; }
        public string StartArguments { get; set; }

        public string InfoWithPrefix => $"服务器核心：{Info}";
        public string AddressWithPrefix => $"服务器文件地址：{Address}";
        public string PortWithPrefix => $"服务器端口：{Port}";
    }

}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace MinecraftServerLauncher
{
    public sealed partial class FileEditPage : Page
    {
        private List<ServerInfo> servers = new List<ServerInfo>();
        private string selectedServerPath = string.Empty;
        private string selectedFile = string.Empty;

        public FileEditPage()
        {
            this.InitializeComponent();
            LoadServers();
        }

        private void LoadServers()
        {
            servers = LoadServerInfo();
            foreach (var server in servers)
            {
                ServerComboBox.Items.Add(server.Name);
            }
        }

        private List<ServerInfo> LoadServerInfo()
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string jsonPath = Path.Combine(localAppDataPath, @"Packages\bb4c497b-4ce1-43c0-9112-5b723b9f37d0_eh7281kjy6nfp\LocalState\servers.json");

            if (File.Exists(jsonPath))
            {
                var json = File.ReadAllText(jsonPath);
                return JsonConvert.DeserializeObject<List<ServerInfo>>(json);
            }
            return new List<ServerInfo>();
        }

        private void OnServerSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServerComboBox.SelectedItem != null)
            {
                var selectedServer = servers.Find(server => server.Name == ServerComboBox.SelectedItem.ToString());
                if (selectedServer != null)
                {
                    selectedServerPath = Path.Combine(selectedServer.Address, selectedServer.Name); // 拼接服务器路径和名称
                    LoadServerFiles(selectedServerPath);
                }
            }
        }

        private void LoadServerFiles(string serverPath)
        {
            FileList.Items.Clear();

            var files = new[] { "eula.txt", "server.properties", "spigot.yml", "purpur.yml" };
            foreach (var file in files)
            {
                var filePath = Path.Combine(serverPath, file);
                bool fileExists = File.Exists(filePath);

                // 输出文件路径和存在性
                if (App.IsDeveloperMode)
                {
                    var debugInfo = $"Checking file: {filePath}, Exists: {fileExists}";
                    System.Diagnostics.Debug.WriteLine(debugInfo);
                    FileList.Items.Add(new TextBlock { Text = debugInfo, FontStyle = Windows.UI.Text.FontStyle.Italic });
                }

                if (fileExists)
                {
                    FileList.Items.Add(file);
                }
                else
                {
                    // 显示文件不存在
                    FileList.Items.Add($"{file} (文件不存在)");
                }
            }
        }

        private async void OnEditFileClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                selectedFile = button.DataContext.ToString().Replace(" (文件不存在)", "");
                var filePath = Path.Combine(selectedServerPath, selectedFile);
                var content = await File.ReadAllTextAsync(filePath);
                EditFileContentTextBox.Text = content;

                // 根据文件类型提供描述
                FileDescription.Text = GetFileDescription(selectedFile);

                await EditFileDialog.ShowAsync();
            }
        }

        private string GetFileDescription(string fileName)
        {
            return fileName switch
            {
                "eula.txt" => "EULA 文件：用于接受 Minecraft 服务器的最终用户许可协议。",
                "server.properties" => "server.properties 文件：用于配置 Minecraft 服务器的各种设置，例如游戏模式、难度、白名单等。",
                "spigot.yml" => "spigot.yml 文件：用于配置 Spigot 服务器的专有设置。",
                "purpur.yml" => "purpur.yml 文件：用于配置 Purpur 服务器的专有设置。",
                _ => "未知文件。",
            };
        }

        private async void OnDialogSaveButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (!string.IsNullOrEmpty(selectedFile))
            {
                var filePath = Path.Combine(selectedServerPath, selectedFile);
                await File.WriteAllTextAsync(filePath, EditFileContentTextBox.Text);
                var dialog = new ContentDialog
                {
                    Title = "保存成功",
                    Content = "文件已成功保存。",
                    CloseButtonText = "确定"
                };
                dialog.XamlRoot = this.Content.XamlRoot;
                await dialog.ShowAsync();
            }
        }

        private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            // 这里可以添加其他保存操作
        }

        private Dictionary<string, string> GetServerPropertiesDescription()
        {
            return new Dictionary<string, string>
            {
                { "enable-jmx-monitoring", "是否启用 JMX 监视" },
                { "rcon.port", "设置 RCON 远程访问的端口号" },
                { "level-seed", "地图种子 默认留空" },
                { "gamemode", "游戏模式（survival, creative, adventure, spectator）" },
                { "enable-command-block", "启用命令方块" },
                { "enable-query", "是否允许使用 GameSpy4 协议的服务器查询" },
                { "generator-settings", "用于自定义世界产生器的参数，不在普通平地世界服务器中启用" },
                { "enforce-secure-profile", "如果开启，则没有 Mojang 验证的玩家将无法连接到服务器" },
                { "level-name", "世界（地图）名称 不要使用中文" }
            };
        }
    }

    public class ServerInfo
    {
        public string Name { get; set; }
        public string Info { get; set; }
        public string Address { get; set; }
        public string Port { get; set; }
        public string JavaPath { get; set; }
        public string StartArguments { get; set; }
        public string InfoWithPrefix { get; set; }
        public string AddressWithPrefix { get; set; }
        public string PortWithPrefix { get; set; }
    }
}

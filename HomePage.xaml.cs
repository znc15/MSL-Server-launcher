using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.ObjectModel;

namespace MinecraftServerLauncher
{
    public sealed partial class HomePage : Page
    {
        public ObservableCollection<Server> Servers { get; set; } = new ObservableCollection<Server>();

        public HomePage()
        {
            this.InitializeComponent();
            LoadServers();
        }

        private void LoadServers()
        {
            // 示例：加载服务器列表
            // 这里可以替换为实际加载服务器的逻辑
            if (Servers.Count == 0)
            {
                NoServerText.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                ServerListView.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                NoServerText.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                ServerListView.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                ServerListView.ItemsSource = Servers;
            }
        }
    }

    public class Server
    {
        public string ServerName { get; set; }
    }
}

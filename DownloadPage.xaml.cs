using Microsoft.UI.Xaml.Controls;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.System;
using System.Linq;

namespace MinecraftServerLauncher
{
    public sealed partial class DownloadPage : Page
    {
        public DownloadPage()
        {
            this.InitializeComponent();
            PurpurDownloadButton.Click += PurpurDownloadButton_Click;
            PaperDownloadButton.Click += PaperDownloadButton_Click;
            SpigotDownloadButton.Click += SpigotDownloadButton_Click;
            LoadVersions();
        }

        private async void LoadVersions()
        {
            await LoadPurpurVersions();
            await LoadPaperVersions();
            LoadSpigotVersions();
        }

        private async Task LoadPurpurVersions()
        {
            string apiUrl = "https://api.purpurmc.org/v2/purpur";
            using (HttpClientHandler handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using (HttpClient client = new HttpClient(handler))
                {
                    try
                    {
                        var response = await client.GetStringAsync(apiUrl);
                        var jsonDoc = JsonDocument.Parse(response);
                        var versions = jsonDoc.RootElement.GetProperty("versions").EnumerateArray().Select(v => v.GetString()).ToList();

                        foreach (var version in versions)
                        {
                            PurpurVersionComboBox.Items.Add(new ComboBoxItem { Content = version });
                        }

                        if (versions.Count > 0)
                        {
                            var latestVersion = versions.OrderBy(v => new Version(v.Split('-').First())).Last();
                            PurpurVersionComboBox.SelectedItem = PurpurVersionComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == latestVersion);
                        }
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorDialog("无法加载 Purpur 版本信息。");
                    }
                }
            }
        }

        private async Task LoadPaperVersions()
        {
            string apiUrl = "https://api.papermc.io/v2/projects/paper";
            using (HttpClientHandler handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using (HttpClient client = new HttpClient(handler))
                {
                    try
                    {
                        var response = await client.GetStringAsync(apiUrl);
                        var jsonDoc = JsonDocument.Parse(response);
                        var versions = jsonDoc.RootElement.GetProperty("versions").EnumerateArray().Select(v => v.GetString()).ToList();

                        var orderedVersions = versions.OrderBy(v => new Version(v.Split('-').First())).ToList();

                        foreach (var version in orderedVersions)
                        {
                            PaperVersionComboBox.Items.Add(new ComboBoxItem { Content = version });
                        }

                        if (orderedVersions.Count > 0)
                        {
                            var latestVersion = orderedVersions.Last();
                            PaperVersionComboBox.SelectedItem = PaperVersionComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == latestVersion);
                        }
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorDialog("无法加载 Paper 版本信息。");
                    }
                }
            }
        }

        private void LoadSpigotVersions()
        {
            var validVersions = new[] { "1.16.5", "1.17", "1.17.1", "1.18", "1.18.1", "1.18.2", "1.19", "1.19.1", "1.19.2", "1.19.3", "1.19.4", "1.20", "1.20.1", "1.20.2", "1.20.3", "1.20.4", "1.20.6" };

            foreach (var version in validVersions)
            {
                SpigotVersionComboBox.Items.Add(new ComboBoxItem { Content = version });
            }

            if (validVersions.Length > 0)
            {
                var latestVersion = validVersions.OrderBy(v => new Version(v.Split('-').First())).Last();
                SpigotVersionComboBox.SelectedItem = SpigotVersionComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == latestVersion);
            }
        }

        private async void PurpurDownloadButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var selectedVersion = (PurpurVersionComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                string downloadUrl = await GetPurpurDownloadUrl(selectedVersion);
                if (!string.IsNullOrEmpty(downloadUrl))
                {
                    await ShowDownloadDialog(downloadUrl);
                }
                else
                {
                    await ShowErrorDialog("无法获取下载链接。");
                }
            }
        }

        private async void PaperDownloadButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var selectedVersion = (PaperVersionComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                string downloadUrl = await GetPaperDownloadUrl(selectedVersion);
                if (!string.IsNullOrEmpty(downloadUrl))
                {
                    await ShowDownloadDialog(downloadUrl);
                }
                else
                {
                    await ShowErrorDialog("无法获取下载链接。");
                }
            }
        }

        private async void SpigotDownloadButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var selectedVersion = (SpigotVersionComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (!string.IsNullOrEmpty(selectedVersion))
            {
                string downloadUrl = GetSpigotDownloadUrl(selectedVersion);
                if (!string.IsNullOrEmpty(downloadUrl))
                {
                    await ShowDownloadDialog(downloadUrl);
                }
                else
                {
                    await ShowErrorDialog("无法获取下载链接。");
                }
            }
        }

        private async Task<string> GetPurpurDownloadUrl(string version)
        {
            try
            {
                string apiUrl = $"https://api.purpurmc.org/v2/purpur/{version}";
                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    using (HttpClient client = new HttpClient(handler))
                    {
                        var response = await client.GetStringAsync(apiUrl);
                        var jsonDoc = JsonDocument.Parse(response);
                        var latestBuild = jsonDoc.RootElement.GetProperty("builds").GetProperty("latest").GetString();
                        return $"{apiUrl}/{latestBuild}/download";
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private async Task<string> GetPaperDownloadUrl(string version)
        {
            try
            {
                string apiUrl = $"https://api.papermc.io/v2/projects/paper/versions/{version}";
                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    using (HttpClient client = new HttpClient(handler))
                    {
                        var response = await client.GetStringAsync(apiUrl);
                        var jsonDoc = JsonDocument.Parse(response);
                        var builds = jsonDoc.RootElement.GetProperty("builds").EnumerateArray().Select(b => b.GetInt32()).ToList();
                        var latestBuild = builds.Max();
                        return $"{apiUrl}/builds/{latestBuild}/downloads/paper-{version}-{latestBuild}.jar";
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private string GetSpigotDownloadUrl(string version)
        {
            var validVersions = new[] { "1.16.5", "1.17", "1.17.1", "1.18", "1.18.1", "1.18.2", "1.19", "1.19.1", "1.19.2", "1.19.3", "1.19.4", "1.20", "1.20.1", "1.20.2", "1.20.3", "1.20.4", "1.20.6" };
            if (validVersions.Contains(version))
            {
                return $"https://download.getbukkit.org/spigot/spigot-{version}.jar";
            }
            return null;
        }

        private async Task ShowDownloadDialog(string downloadUrl)
        {
            var dialog = new ContentDialog
            {
                Title = "下载确认",
                Content = $"将要下载以下网址的文件：\n{downloadUrl}",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                XamlRoot = this.XamlRoot // 设置 XamlRoot 属性
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var uri = new Uri(downloadUrl);
                var options = new LauncherOptions
                {
                    TreatAsUntrusted = false,
                    DisplayApplicationPicker = false
                };
                await Launcher.LaunchUriAsync(uri, options);
            }
        }

        private async Task ShowErrorDialog(string errorMessage)
        {
            var dialog = new ContentDialog
            {
                Title = "下载错误",
                Content = errorMessage,
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot // 设置 XamlRoot 属性
            };

            await dialog.ShowAsync();
        }
    }
}

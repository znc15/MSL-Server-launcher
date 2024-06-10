using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace MinecraftServerLauncher
{
    public partial class App : Application
    {
        public static MainWindow MainWindow { get; private set; }
        public static bool IsDeveloperMode { get; private set; }
        private const string SettingsFileName = "settings.json";

        public App()
        {
            this.InitializeComponent();
            LoadDeveloperMode();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
        }

        private async void LoadDeveloperMode()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile settingsFile;
            try
            {
                settingsFile = await localFolder.GetFileAsync(SettingsFileName);
                string content = await FileIO.ReadTextAsync(settingsFile);
                var settings = JsonConvert.DeserializeObject<AppSettings>(content);
                IsDeveloperMode = settings.IsDeveloperMode;
            }
            catch (FileNotFoundException)
            {
                IsDeveloperMode = false;
            }
        }

        public static async void SaveDeveloperMode(bool isEnabled)
        {
            IsDeveloperMode = isEnabled;
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile settingsFile = await localFolder.CreateFileAsync(SettingsFileName, CreationCollisionOption.ReplaceExisting);
            var settings = new AppSettings { IsDeveloperMode = isEnabled };
            string content = JsonConvert.SerializeObject(settings);
            await FileIO.WriteTextAsync(settingsFile, content);
        }
    }

    public class AppSettings
    {
        public bool IsDeveloperMode { get; set; }
    }
}

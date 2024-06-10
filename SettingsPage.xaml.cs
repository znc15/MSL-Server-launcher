using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MinecraftServerLauncher
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            LoadDeveloperMode();
        }

        private void LoadDeveloperMode()
        {
            DeveloperModeToggle.IsOn = App.IsDeveloperMode;
        }

        private void DeveloperModeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            var toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                App.SaveDeveloperMode(toggleSwitch.IsOn);
            }
        }
    }
}

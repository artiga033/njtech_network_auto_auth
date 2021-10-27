using Hardcodet.Wpf.TaskbarNotification;
using Shared;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private TaskbarIcon? taskbarIcon;
        private Config config = new Config();
        public App()
        {
            //this.taskbarIcon = (TaskbarIcon)new XamlReader().LoadAsync(Application.GetResourceStream(new Uri("/TaskbarIcon.xaml",UriKind.Relative)).Stream);
        }
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            this.taskbarIcon = (TaskbarIcon)this.FindResource("taskbarIcon");

            var configFile = File.Open(Config.FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using StreamReader reader = new StreamReader(configFile, Encoding.UTF8);
            var configStr = reader.ReadToEnd();
            bool configIllegal = false;

            try
            {
                config = JsonSerializer.Deserialize<Config>(configStr) ?? config;
            }
            catch
            {
                configIllegal = true;
            }
            if (Config.IsEmptyOrNull(config))
                configIllegal = true;

            if (configIllegal)
            {
                configFile.Close();
                ConfigWindow CWindow = new(ref config);
                CWindow.ShowDialog();
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(33);
        }

        private async void Manually_Connect(object sender, RoutedEventArgs e)
        {
            var authRes = await Libs.AuthAsync(config.Username, config.Password, config.Channel);
        }

        private void Change_Config(object sender, RoutedEventArgs e)
        {
            ConfigWindow CWindow = new(ref config);
            CWindow.ShowDialog();
        }

    }

    public class Config
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public Channel Channel { get; set; }
        [JsonIgnore]
        public static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NJTechAutoAuth.json");
        public static bool IsEmptyOrNull(Config? config)
        {
            var result =
                config == null ||
                string.IsNullOrWhiteSpace(config.Username) ||
                string.IsNullOrWhiteSpace(config.Password);

            return result;
        }
    }
}

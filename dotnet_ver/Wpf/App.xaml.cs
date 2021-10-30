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
using System.Net.NetworkInformation;

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
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            this.taskbarIcon = (TaskbarIcon)this.FindResource("taskbarIcon");
            var configIsLegal = await this.TryReadConfig();
            if (!configIsLegal)
            {
                ConfigWindow CWindow = new(ref config);
                var dialogResult = CWindow.ShowDialog();//此方法只在窗口被关闭后返回
                //这段逻辑的前提是 !configIsLegal
                //只有输入不合法时，用户必须修改配置，所以如果没有DialogResult==true就退出。
                //在另一些情况下，如修改一个本身合法的配置，这段逻辑不适用，或者，它必须有ConfigIlleagal的前提。
                if (!dialogResult.HasValue||!dialogResult.Value)
                {
                    this.ExitButton_Click(this, new RoutedEventArgs());
                }
            }

            NetworkChange.NetworkAvailabilityChanged += OnNetChange_Connect;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void Manually_Connect(object sender, RoutedEventArgs e)
        {
            var res = await AuthViaConfig();
            this.OnConnected(res);
        }
        private async void OnNetChange_Connect(object? sender,EventArgs e)
        {
            var res = await AuthViaConfig();
            this.OnConnected(res);
        }
        private void OnConnected(bool result)
        {
            this.taskbarIcon?.ShowBalloonTip("Connected", "Your devices is now connected to NjtechHome and ready for Internet.", BalloonIcon.Info);

        }
        private void Change_Config(object sender, RoutedEventArgs e)
        {
            ConfigWindow CWindow = new(ref config);
            CWindow.ShowDialog();
        }


        private async Task<bool> AuthViaConfig() => await Libs.AuthAsync(this.config.Username, this.config.Password, this.config.Channel);
        public async Task<bool> TryReadConfig()
        {
            var configFile = File.Open(Config.FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            using StreamReader reader = new StreamReader(configFile, Encoding.UTF8);
            var configStr = await reader.ReadToEndAsync();
            bool configIsLegal = true;

            try
            {
                config = JsonSerializer.Deserialize<Config>(configStr) ?? config;
            }
            catch
            {
                configIsLegal = false;
            }
            if (Config.IsEmptyOrNull(config))
                configIsLegal = false;

            return configIsLegal;
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

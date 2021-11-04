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
using System.Windows.Controls;
using System.Diagnostics;
using Microsoft.WindowsAPICodePack.Net;

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
            foreach (var i in this.taskbarIcon.ContextMenu.Items)
            {
                if (i is CheckBox checkBox && checkBox.Name == "setStartupCheckBox")
                {
                    checkBox.IsChecked = AutoStartup.Check();
                }
            }
            var configIsLegal = await this.TryReadConfig();
            if (!configIsLegal)
            {
                ConfigWindow CWindow = new(ref config);
                var dialogResult = CWindow.ShowDialog();//此方法只在窗口被关闭后返回
                //这段逻辑的前提是 !configIsLegal
                //只有输入不合法时，用户必须修改配置，所以如果没有DialogResult==true就退出。
                //在另一些情况下，如修改一个本身合法的配置，这段逻辑不适用，或者，它必须有ConfigIlleagal的前提。
                if (!dialogResult.HasValue || !dialogResult.Value)
                {
                    this.ExitButton_Click(this, new RoutedEventArgs());
                }
            }

            NetworkChange.NetworkAddressChanged += OnNetChange_Connect;
            //有时程序启动会晚于网络连接，所以在启动是要进行一次连接尝试
            if (IsNjtechHomeConnected() && !this.isConnecting)
            {
                bool res = await TryAuthViaConfigAsync();
                this.OnConnected(res);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void Manually_Connect(object sender, RoutedEventArgs e)
        {
            if (this.IsNjtechHomeConnected())
            {
                this.isConnecting = true;
                try
                {
                    var res = await AuthViaConfigAsync();
                    this.OnConnected(res);
                }
                catch (Exception ex)
                {
                    this.taskbarIcon?.ShowBalloonTip("Failed Connecting", "An erroe occurs when trying to connect." + "\n" + ex.Message, BalloonIcon.Error);
                }
            }
            else
            {
                this.taskbarIcon?.ShowBalloonTip("Not Connected", "You are not connected to Njtech-Home network.Please Check", BalloonIcon.Warning);
            }
        }
        private bool isConnecting = false;
        private async void OnNetChange_Connect(object? sender, EventArgs e)
        {
            if (isConnecting)
                return;

            this.isConnecting = true;
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            if (this.IsNjtechHomeConnected())
            {
                bool res = await TryAuthViaConfigAsync();
                this.OnConnected(res);
            }
        }
        private void OnConnected(bool result)
        {
            this.isConnecting = true;
            if (result)
            {
                this.taskbarIcon?.ShowBalloonTip("Connected", "Your device is now connected to NjtechHome and ready for Internet.", BalloonIcon.Info);
            }
            else
            {
                this.taskbarIcon?.ShowBalloonTip("Failed Connecting", "An erroe occurs when trying to connect.", BalloonIcon.Error);
            }
        }
        private void Change_Config(object sender, RoutedEventArgs e)
        {
            ConfigWindow CWindow = new(ref config);
            CWindow.ShowDialog();
        }

        private bool IsNjtechHomeConnected()
        {
            var isNjtechHomeConnected = false;
            var networks = NetworkListManager.GetNetworks(NetworkConnectivityLevels.Connected);
            foreach (var network in networks)
            {
                if (network.Name == "Njtech-Home")
                    isNjtechHomeConnected = true;
            }
            return isNjtechHomeConnected;
        }

        /// <summary>
        /// 由于一些原因，每次尝试连接都有可能失败，用此方法指定尝试次数及每次间隔
        /// </summary>
        /// <param name="times">尝试次数</param>
        /// <param name="interval">间隔时间，单位ms</param>
        /// <returns></returns>
        private async Task<bool> TryAuthViaConfigAsync(int times = 3, int interval = 1000)
        {
            int count = 0;
            bool success = false;

            while (count++ < times)
            {
                success = await AuthViaConfigAsync();
                if (success)
                    break;
                await Task.Delay(1000);
            }

            return success;
        }
        private async Task<bool> AuthViaConfigAsync() => await Libs.AuthAsync(this.config.Username, this.config.Password, this.config.Channel);
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

        private void Set_Startup(object sender, RoutedEventArgs e)
        {
            AutoStartup.Set(true);
        }

        private void Unset_Startup(object sender, RoutedEventArgs e)
        {
            AutoStartup.Set(false);
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

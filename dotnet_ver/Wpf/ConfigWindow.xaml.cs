using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Wpf
{
    /// <summary>
    /// ConfigWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigWindow : Window
    {
        public ConfigWindow(ref Config config)
        {
            InitializeComponent();
            this.Config = config;
            this.DataContext = this.Config;
            this.channelComboBox.ItemsSource = this.ChannelSelections;
        }

        private async void SaveConfig(object sender, RoutedEventArgs e)
        {
            this.Config.Password = this.PasswordTextBox.Password;
            var configFile = File.Open(Config.FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            configFile.SetLength(0);
            await configFile.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(Config));
            await configFile.FlushAsync();
            configFile.Close();
            this.Close();
        }

        private void ResetConfig(object sender, RoutedEventArgs e)
        {
            this.UsernameTextBox.Text = "";
            this.PasswordTextBox.Password = "";
        }
        public Config Config {  get; set; }
        public IEnumerable<Channel> ChannelSelections => Enum.GetValues<Channel>();
    }
}

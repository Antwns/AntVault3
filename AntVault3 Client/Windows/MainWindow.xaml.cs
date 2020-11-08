using AntVault3_Client.ClientWorkers;
using System;
using System.Windows;

namespace AntVault3_Client.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ClientMainWindow_Closed(object sender, EventArgs e)
        {
            Networking.Disconnect();
            Application.Current.Shutdown();
        }

        private void ClientMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.MaxHeight = 450;
            this.MaxWidth = 800;
            this.Content = WindowController.LoginPage;
        }

        private void ClientMainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(Networking.AntVaultClient.Connected == true)
            {
                try
                {
                    Networking.AntVaultClient.Send("/Disconnect -Content closed the app;");
                }
                catch (Exception exc)
                {
                    Console.WriteLine("Couldn't send disconnect message due to " + exc);
                }
            }
        }
    }
}

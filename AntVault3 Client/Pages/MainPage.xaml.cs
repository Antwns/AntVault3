﻿using AntVault3_Client.ClientWorkers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AntVault3_Client.Pages
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void GeneralChatInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                string MessageToSend = GeneralChatInputTextBox.Text;
                Task.Run(()=>MainClientWorker.SendMessage(MessageToSend));
                GeneralChatInputTextBox.Text = "";
            }
        }

        private void MainChatTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainChatTextBox.ScrollToEnd();
        }

        private void ChangeProfilePictureButton_Click(object sender, RoutedEventArgs e)
        {
            MainClientWorker.GetNewProfilePicture();
        }

        private void MyPageFrame_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() => ClientNetworking.AntVaultClient.SendMessage("/GetMyPage"));
        }
    }
}

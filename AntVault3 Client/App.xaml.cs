﻿using System;
using WatsonTcp;
using System.Threading.Tasks;
using System.Windows;
using AntVault3_Client.ClientWorkers;
using System.Threading;
using System.IO;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace AntVault3_Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static WatsonTcpClient AntVaultClient = new WatsonTcpClient(AuxiliaryClientWorker.ReadFromConfig("IP"), Convert.ToInt32(AuxiliaryClientWorker.ReadFromConfig("Port")));
        static bool HasSetupEvents = false;

        public static async Task ConnectAsync()
        {
            if (HasSetupEvents == false)
            {
                AntVaultClient.Settings.Logger = WriteToLog;
                AntVaultClient.Events.ExceptionEncountered += MainClientWorker.Events_ExceptionEncountered;
                AntVaultClient.Keepalive.EnableTcpKeepAlives = true;
                AntVaultClient.Keepalive.TcpKeepAliveInterval = 5;
                AntVaultClient.Keepalive.TcpKeepAliveRetryCount = 5;
                AntVaultClient.Keepalive.TcpKeepAliveTime = 5;
                AntVaultClient.Events.MessageReceived += MainClientWorker.Events_MessageReceivedAsync;
                HasSetupEvents = true;
                Console.WriteLine("Events setup complete");
            }
            try
            {
                AntVaultClient.Start();
            }
            catch (Exception exc)
            {
                Console.WriteLine("Could not connect to the server due to " + exc);
            }
            if(AntVaultClient.Connected == true)
            {
                await Task.Run(() => AntVaultClient.Send("/ServerStatus?"));
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    WindowController.LoginPage.StatusLabel.Content = "ERROR-Server offline, try to Vault later.";
                });
                await Task.Delay(10000);
                await Task.Run(() => ConnectAsync());
            }
        }

        internal static void WriteToLog(string LogEntry)
        {
            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "AntVaultClient.log", LogEntry + Environment.NewLine);
        }

        internal static void Disconnect()
        {
            AntVaultClient.Dispose();
        }


        internal static FlowDocument SortFriendsList()
        {
            FlowDocument DocumentToReturn = new FlowDocument();
            Paragraph CurrentParagraph = new Paragraph();
            #region Online status ellipse
            System.Windows.Shapes.Ellipse OnlineStatus = new System.Windows.Shapes.Ellipse();
            OnlineStatus.Height = 30;
            OnlineStatus.Width = 30;
            OnlineStatus.Stroke = Brushes.Black;
            OnlineStatus.StrokeThickness = 1.5;
            OnlineStatus.Fill = Brushes.Green;
            #endregion
            #region Offline status ellipse
            System.Windows.Shapes.Ellipse OfflineStatus = new System.Windows.Shapes.Ellipse();
            OfflineStatus.Height = 30;
            OfflineStatus.Width = 30;
            OfflineStatus.Stroke = Brushes.Black;
            OfflineStatus.StrokeThickness = 1.5;
            OfflineStatus.Fill = Brushes.Red;
            #endregion

            if (MainClientWorker.CurrentFriendsList.Count == 0)
            {
                Label TextToShow = new Label();
                TextToShow.FontSize = 16;
                TextToShow.Foreground = System.Windows.Media.Brushes.Black;
                TextToShow.Content = "Uh oh... It looks like you have no friends for now :(";
                CurrentParagraph.Inlines.Add(TextToShow);
                CurrentParagraph.Inlines.Add(Environment.NewLine);
                DocumentToReturn.Blocks.Add(CurrentParagraph);
                return DocumentToReturn;
            }
            else
            {
                foreach (string Friend in MainClientWorker.CurrentFriendsList)
                {

                    if (MainClientWorker.CurrentOnlineUsers.Contains(Friend))
                    {

                        CurrentParagraph.Inlines.Add(OnlineStatus);
                    }
                    else
                    {

                        CurrentParagraph.Inlines.Add(OfflineStatus);
                    }
                    #region Text to add after status ellipse
                    Label TextToShow = new Label();
                    TextToShow.FontSize = 16;
                    TextToShow.Foreground = Brushes.Black;
                    TextToShow.Content = Friend;
                    #endregion
                    CurrentParagraph.Inlines.Add(TextToShow);
                    CurrentParagraph.Inlines.Add(Environment.NewLine);
                    DocumentToReturn.Blocks.Add(CurrentParagraph);
                }
                return DocumentToReturn;
            }
        }

        internal static Paragraph AppendMessage(string MessageString)
        {
            string Sender = "Sender";
            string Message = "Message";
            try
            {
                Sender = AuxiliaryClientWorker.GetElement(MessageString, "-U ", " -Content");
                Message = AuxiliaryClientWorker.GetElement(MessageString, "-Content ", ";");
            }
            catch(Exception exc)
            {
                Console.WriteLine("Could not grab message information due to " + exc);
            }
            Paragraph ChatParagraph = new Paragraph();
            #region Profile picture before the text
            System.Windows.Shapes.Ellipse ImageToShowFrame = new System.Windows.Shapes.Ellipse();
            ImageToShowFrame.Height = 30;
            ImageToShowFrame.Width = 30;
            ImageToShowFrame.Stroke = Brushes.Black;
            ImageToShowFrame.StrokeThickness = 1.5;
            ImageBrush ImageToShowBrush = new ImageBrush(AuxiliaryClientWorker.GetBitmapImageFromBitmap(MainClientWorker.CurrentProfilePictures[MainClientWorker.CurrentOnlineUsers.IndexOf(Sender)]));
            ImageToShowFrame.Fill = ImageToShowBrush;
            ImageToShowFrame.MouseLeftButtonDown += Placeholder;
            #endregion
            ChatParagraph.Inlines.Add(ImageToShowFrame);

            #region Text to add after the profile picture
            Label TextToShow = new Label();
            TextToShow.FontSize = 16;
            TextToShow.Foreground = Brushes.Black;
            TextToShow.Content = "[" + Sender + "]: " + Message;
            #endregion
            ChatParagraph.Inlines.Add(TextToShow);
            ChatParagraph.Inlines.Add(Environment.NewLine);
            return ChatParagraph;
        }

        internal static Paragraph AppendNewUser(string NewUser)
        {
            #region Text to add when user joins
            Paragraph ChatParagraph = new Paragraph();
            Label TextToShow = new Label();
            TextToShow.FontSize = 20;
            TextToShow.Foreground = Brushes.Black;
            TextToShow.FontStyle = FontStyles.Oblique;
            TextToShow.FontStyle = FontStyles.Italic;
            TextToShow.Content = NewUser + " has joined the vault!";
            #endregion
            ChatParagraph.Inlines.Add(TextToShow);
            ChatParagraph.Inlines.Add(Environment.NewLine);
            return ChatParagraph;
        }

        internal static Paragraph RemoveUser(string UserToRemove)
        {
            #region Text to add when user joins
            Paragraph ChatParagraph = new Paragraph();
            Label TextToShow = new Label();
            TextToShow.FontSize = 20;
            TextToShow.Foreground = Brushes.Black;
            TextToShow.FontStyle = FontStyles.Oblique;
            TextToShow.FontStyle = FontStyles.Italic;
            TextToShow.Content = UserToRemove + " has left the vault!";
            #endregion
            ChatParagraph.Inlines.Add(TextToShow);
            ChatParagraph.Inlines.Add(Environment.NewLine);
            return ChatParagraph;
        }

        private static void Placeholder(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("This");
        }
    }
}

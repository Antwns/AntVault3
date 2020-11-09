using System;
using WatsonTcp;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Windows;

namespace AntVault3_Client.ClientWorkers
{
    class ClientNetworking
    {
        internal static WatsonTcpClient AntVaultClient = null;

        bool HasSetupEvents = false;
        bool UserProfilePictureMode;
        bool UserFriendsListMode;
        bool OnlineUsersMode;
        bool OnlineProfilePicturesMode;
        bool NewOnlineUserMode;
        bool NewThemeMode;
        bool HasSetNewUser;
        bool NewLoginScreenMode;

        internal void Connect()
        {
            AntVaultClient = new WatsonTcpClient(AuxiliaryClientWorker.ReadFromConfig("IP"), Convert.ToInt32(AuxiliaryClientWorker.ReadFromConfig("Port")));
            if (HasSetupEvents == false)
            {
                AntVaultClient.Settings.Logger = WriteToLog;
                AntVaultClient.Events.ExceptionEncountered += Events_ExceptionEncountered;
                AntVaultClient.Keepalive.EnableTcpKeepAlives = true;
                AntVaultClient.Keepalive.TcpKeepAliveInterval = 1;
                AntVaultClient.Keepalive.TcpKeepAliveTime = 1;
                AntVaultClient.Settings.Logger = WriteToLog;
                AntVaultClient.Events.ExceptionEncountered += Events_ExceptionEncountered;
                AntVaultClient.Events.MessageReceived += Events_MessageReceived;
                HasSetupEvents = true;
                Console.WriteLine("Events setup complete");
            }
            try
            {
                if (AntVaultClient.Connected == false)
                {
                    AntVaultClient.Start();
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("Could not connect to the server due to " + exc);
            }
            try
            {
                if (AntVaultClient.Connected == true)
                {
                    Task.Run(() => AntVaultClient.Send("/ServerStatus?"));
                }
                else
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        WindowController.LoginPage.StatusLabel.Content = "ERROR-Server offline, try to Vault later.";
                    });
                    Thread.Sleep(1000);
                    Task.Run(() => Connect());
                }
            }
            catch
            {
                Console.WriteLine("Client could not be used");
            }
        }

        internal void Events_MessageReceived(object sender, MessageReceivedFromServerEventArgs e)
        {
            string MessageString = AuxiliaryClientWorker.GetStringFromBytes(e.Data);
            #region debugging
            if (MessageString.StartsWith("�PNG") == false && MessageString.Contains("System.Collections.ObjectModel.Collection") == false && MessageString.Contains("WAVEfmt") == false && MessageString.Contains("GIF89a") == false)
            {
                Console.WriteLine("[Debug]: " + MessageString);
            }
            else if (MessageString.StartsWith("�PNG") == false && MessageString.Contains("System.Collections.ObjectModel.Collection") == true)
            {
                Console.WriteLine("[Collection]");
            }
            else if (MessageString.StartsWith("�PNG") == true && MessageString.Contains("System.Collections.ObjectModel.Collection") == false)
            {
                Console.WriteLine("[Image]");
            }
            else if (MessageString.Contains("WAVEfmt") == true && MessageString.Contains("System.Collections.ObjectModel.Collection") == false && MessageString.StartsWith("�PNG") == false)
            {
                Console.WriteLine("[Wav]");
            }
            else if (MessageString.Contains("GIF89a") == true && MessageString.Contains("WAVEfmt") == false && MessageString.Contains("System.Collections.ObjectModel.Collection") == false && MessageString.StartsWith("�PNG") == false)
            {
                Console.WriteLine("[GIF]");
            }
            else
            {
                Console.WriteLine("[Unknown data format]");
            }
            #endregion
            if (MessageString.StartsWith("/AcceptConnection"))
            {
                MessageBox.Show("Authentication successfull!" + Environment.NewLine + "Entering the vault...", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Task.Run(() => MainClientWorker.OpenMainPage());
            }
            if (MessageString.StartsWith("/DenyConnection"))
            {
                MessageBox.Show("Authetincation failed, please revise the login information you have provided", "Login error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            if (MessageString.StartsWith("/ServerStatus"))
            {
                string ServerStatus = AuxiliaryClientWorker.GetElement(MessageString, "/ServerStatus ", ";");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    WindowController.LoginPage.StatusLabel.Content = ServerStatus;
                    ClientNetworking.AntVaultClient.Send("/ServerTheme?");
                });

            }
            if (MessageString.StartsWith("/DefaultTheme"))
            {
                Console.WriteLine("Received default theme callback, will not try to update the track");
            }
            if (MessageString.StartsWith("/NewTheme") || NewThemeMode == true)
            {
                if (MessageString.StartsWith("/NewTheme"))
                {
                    NewThemeMode = true;
                }
                else
                {
                    NewThemeMode = false;
                    Task.Run(() => MainClientWorker.AssignNewTheme(e.Data));
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ClientNetworking.AntVaultClient.Send("/ServerLoginScreen?");
                    });
                }
            }
            if (MessageString.StartsWith("/DefaultLoginScreen"))
            {
                Console.WriteLine("Received default login screen callback, will not try to update");
            }
            if (MessageString.StartsWith("/NewLoginScreen") || NewLoginScreenMode == true)
            {
                if (MessageString.StartsWith("/NewLoginScreen") || NewLoginScreenMode == false)
                {
                    NewLoginScreenMode = true;
                }
                else
                {
                    NewLoginScreenMode = false;
                    Task.Run(() => MainClientWorker.AssignNewLoginScreen(e.Data));
                }
            }
            if (MessageString.StartsWith("/UserStringInfo"))
            {
                Task.Run(() => MainClientWorker.AssignUserInfo(MessageString));
            }
            if (MessageString.StartsWith("/UserProfilePictureMode") == true || UserProfilePictureMode == true)
            {
                if (MessageString.StartsWith("/UserProfilePictureMode") == true && UserProfilePictureMode == false)
                {
                    UserProfilePictureMode = true;
                }
                else
                {
                    UserProfilePictureMode = false;
                    Task.Run(() => MainClientWorker.AssignProfilePicture(e.Data));
                }
            }
            if (MessageString.StartsWith("/UserFriendsListMode") == true || UserFriendsListMode == true)
            {
                if (MessageString.StartsWith("/UserFriendsListMode") == true && UserFriendsListMode == false)
                {
                    UserFriendsListMode = true;
                }
                else
                {
                    UserFriendsListMode = false;
                    Task.Run(() => MainClientWorker.AssingFriendsList(e.Data));
                }
            }
            if (MessageString.StartsWith("/OnlineUsersListMode") == true || OnlineUsersMode == true)
            {
                if (MessageString.StartsWith("/OnlineUsersListMode") == true && OnlineUsersMode == false)
                {
                    OnlineUsersMode = true;
                }
                else
                {
                    OnlineUsersMode = false;
                    Task.Run(() => MainClientWorker.AssignOnlineUsers(e.Data));
                    Console.WriteLine("Sorting out friends list for " + MainClientWorker.CurrentUser + ", registering " + MainClientWorker.CurrentFriendsList.Count + " entries");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        WindowController.MainPage.FriendsListTextBox.Document = App.SortFriendsList();
                    });
                }
            }
            if (MessageString.StartsWith("/OnlineProfilePicturesMode") == true || OnlineProfilePicturesMode == true)
            {
                if (MessageString.StartsWith("/OnlineProfilePicturesMode") == true && OnlineProfilePicturesMode == false)
                {
                    OnlineProfilePicturesMode = true;
                }
                else
                {
                    OnlineProfilePicturesMode = false;
                    Task.Run(() => MainClientWorker.AssignOnlinePictures(e.Data));
                    Console.WriteLine("Assigned list for online users");
                }
            }
            if (MessageString.StartsWith("/NewUser") == true || NewOnlineUserMode == true)
            {
                try
                {
                    if (HasSetNewUser == false)
                    {
                        MainClientWorker.NewUser = AuxiliaryClientWorker.GetElement(MessageString, "-U ", " -S");
                        Console.WriteLine("New user is " + MainClientWorker.NewUser);
                        HasSetNewUser = true;
                    }
                }
                catch
                {
                    Console.WriteLine("Could not grab new user's username successfully");
                }
                if (MessageString.StartsWith("/NewUser") == true && NewOnlineUserMode == false)
                {
                    NewOnlineUserMode = true;
                    Task.Run(() => MainClientWorker.AssignNewOnlineUser(MessageString));
                }
                else
                {
                    NewOnlineUserMode = false;
                    Task.Run(() => MainClientWorker.AssignNewOnlineUserProfilePicture(e.Data, MainClientWorker.NewUser));
                    HasSetNewUser = false;
                }
            }
            if (MessageString.StartsWith("/UserDisconnect"))
            {
                Task.Run(() => HadndleDisconnect(MessageString));
            }
            if (MessageString.StartsWith("/Message") == true)
            {
                Task.Run(() => MainClientWorker.HandleMessage(MessageString));
            }
        }

        internal void HadndleDisconnect(string MessageString)
        {
            string UserToDisconnect = AuxiliaryClientWorker.GetElement(MessageString, "-U ", ";");
            MainClientWorker.CurrentProfilePictures.Remove(MainClientWorker.CurrentProfilePictures[MainClientWorker.CurrentOnlineUsers.IndexOf(UserToDisconnect)]);
            MainClientWorker.CurrentOnlineUsers.Remove(UserToDisconnect);
            Application.Current.Dispatcher.Invoke(() =>
            {
                WindowController.MainPage.MainChatTextBox.Document.Blocks.Add(App.RemoveUser(UserToDisconnect));
                WindowController.MainPage.FriendsListTextBox.Document = App.SortFriendsList();
            });
        }

        internal void Events_ExceptionEncountered(object sender, WatsonTcp.ExceptionEventArgs e)
        {
            WriteToLog("Json:");
            WriteToLog(e.Json);
            WriteToLog("Exception:");
            WriteToLog(e.Exception.ToString());
        }

        internal void WriteToLog(string LogEntry)
        {
            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "AntVaultClient.log", LogEntry + Environment.NewLine);
        }

        internal void Disconnect()
        {
            AntVaultClient.Dispose();
        }


    }
}

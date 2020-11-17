using System;
using System.Threading.Tasks;
using SimpleSockets.Client;
using System.Windows;
using System.Threading;

namespace AntVault3_Client.ClientWorkers
{
    class ClientNetworking
    {
        static bool UserProfilePictureMode;
        static bool UserFriendsListMode;
        static bool OnlineUsersMode;
        static bool OnlineProfilePicturesMode;
        static bool NewOnlineUserMode;
        static bool NewThemeMode;
        static bool NewLoginScreenMode;
        static bool NewProfilePictureMode;
        static bool CurrentPageUpdateMode;

        internal static string NewUser = "NewUser";
        static string UserToUpdateProfilePicture;

        internal static SimpleSocketClient AntVaultClient;

        static bool HasSetupEvents = false;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        internal static void Connect()
        {
            AntVaultClient = new SimpleSocketTcpClient();
            if (HasSetupEvents == false)
            {
                AntVaultClient.BytesReceived += BytesReceived;
                AntVaultClient.MessageReceived += MessageReceived;
                AntVaultClient.ObjectReceived += AntVaultClient_ObjectReceived;
                HasSetupEvents = true;
                Console.WriteLine("Events setup complete");
            }
            try
            {
                AntVaultClient.StartClient(App.AuxiliaryClientWorker.ReadFromConfig("IP", MainClientWorker.ConfigDir), Convert.ToInt32(App.AuxiliaryClientWorker.ReadFromConfig("Port", MainClientWorker.ConfigDir)));
                Task.Run(() => AntVaultClient.SendMessage("/ServerStatus?"));
                Task.Run(() => Console.WriteLine("Requested server's status"));
                Task.Run(() => Application.Current.Dispatcher.Invoke(() =>
                {
                    WindowController.LoginPage.ConnectButton.Content = "Loading...";
                })
                );
            }
            catch (Exception exc)
            {
                Task.Run(() => Console.WriteLine("Could not connect to the server due to " + exc));
                Task.Run(() => Application.Current.Dispatcher.Invoke(() =>
                {
                    WindowController.LoginPage.StatusLabel.Content = "ERROR-Server offline, try to Vault later.";
                })
                );
                Thread.Sleep(3000);
                Connect();
            }
        }

        private static void AntVaultClient_ObjectReceived(SimpleSocketClient client, object obj, Type objType)
        {

        }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed


        internal static async void BytesReceived(SimpleSocketClient Client, byte[] MessageByte)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            string MessageString = App.AuxiliaryClientWorker.GetStringFromBytes(MessageByte);//Translates stuff for debugging purposes
            #region debugging
            if (MessageString.StartsWith("�PNG") == false && MessageString.Contains("System.Collections.ObjectModel.Collection") == false && MessageString.Contains("WAVEfmt") == false && MessageString.Contains("GIF89a") == false && MessageString.Contains("2005/10/xaml/entry") == false)
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
            else if(MessageString.Contains("2005/10/xaml/entry") && MessageString.Contains("WAVEfmt") == false && MessageString.Contains("System.Collections.ObjectModel.Collection") == false && MessageString.StartsWith("�PNG") == false)
            {
                Console.WriteLine("[XAML]");
            }
            else
            {
                Console.WriteLine("[Unknown data format]");
            }
            #endregion
            if (NewThemeMode == true)
            {
                NewThemeMode = false;
                await Task.Run(() => MainClientWorker.AssignNewTheme(MessageByte));
                await Task.Run(() => Application.Current.Dispatcher.Invoke(() =>
                {
                    Client.SendMessage("/ServerLoginScreen?");
                })
                );
            }
            if (NewLoginScreenMode == true)
            {
                NewLoginScreenMode = false;
                Task.Run(() => MainClientWorker.AssignNewLoginScreen(MessageByte));
                App.Current.Dispatcher.Invoke(() =>
                {
                    WindowController.LoginPage.ConnectButton.Content = "Connect";
                    WindowController.LoginPage.ConnectButton.IsEnabled = true;
                });
            }
            if (UserProfilePictureMode == true)
            {
                UserProfilePictureMode = false;
                Task.Run(() => MainClientWorker.AssignProfilePicture(MessageByte));
            }
            if (UserFriendsListMode == true)
            {
                UserFriendsListMode = false;
                Task.Run(() => MainClientWorker.AssingFriendsList(MessageByte));
            }
            if (OnlineUsersMode == true)
            {
                OnlineUsersMode = false;
                Task.Run(() => MainClientWorker.AssignOnlineUsers(MessageByte));
                Console.WriteLine("Sorting out friends list for " + MainClientWorker.CurrentUser + ", registering " + MainClientWorker.CurrentFriendsList.Count + " entries");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    WindowController.MainPage.FriendsListTextBox.Document = App.SortFriendsList();
                });
            }
            if (OnlineProfilePicturesMode == true)
            {
                OnlineProfilePicturesMode = false;
                Task.Run(() => MainClientWorker.AssignOnlinePictures(MessageByte));
                Console.WriteLine("Assigned list for online users");
            }
            if (NewProfilePictureMode == true)
            {
                NewProfilePictureMode = false;
                Task.Run(() => MainClientWorker.UpdateProfilePicture(UserToUpdateProfilePicture, MessageByte));
            }
            if (NewOnlineUserMode == true)
            {
                NewOnlineUserMode = false;
                await Task.Run(() => MainClientWorker.AssignNewOnlineUserProfilePicture(MessageByte, NewUser));
            }
            if (CurrentPageUpdateMode == true)
            {
                Console.WriteLine("Received page updade for " + MainClientWorker.CurrentUser + "'s profile page");
                CurrentPageUpdateMode = false;
                MainClientWorker.AssignCurrentUserPage(MessageByte);
            }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        internal static void MessageReceived(SimpleSocketClient Client, string MessageString)
        {
            Console.WriteLine("[DEBUG] " + MessageString);
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
                string ServerStatus = App.AuxiliaryClientWorker.GetElement(MessageString, "/ServerStatus ", ";");
                Console.WriteLine("Server's status is " + ServerStatus);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    WindowController.LoginPage.StatusLabel.Content = ServerStatus;
                    Client.SendMessage("/ServerTheme?");
                });
            }
            if (MessageString.StartsWith("/DefaultTheme"))
            {
                Console.WriteLine("Received default theme callback, will not try to update the track");
                App.Current.Dispatcher.Invoke(() =>
                {
                    WindowController.LoginPage.ConnectButton.Content = "Connect";
                    WindowController.LoginPage.ConnectButton.IsEnabled = true;
                });
            }
            if (MessageString.StartsWith("/NewTheme"))
            {
                NewThemeMode = true;
            }
            if (MessageString.StartsWith("/DefaultLoginScreen"))
            {
                Console.WriteLine("Received default login screen callback, will not try to update");
                App.Current.Dispatcher.Invoke(() =>
                {
                    WindowController.LoginPage.ConnectButton.Content = "Connect";
                    WindowController.LoginPage.ConnectButton.IsEnabled = true;
                });
            }
            if (MessageString.StartsWith("/NewLoginScreen"))
            {
                NewLoginScreenMode = true;
            }
            if (MessageString.StartsWith("/UserStringInfo"))
            {
                Task.Run(() => MainClientWorker.AssignUserInfo(MessageString));
            }
            if (MessageString.StartsWith("/UserProfilePictureMode"))
            {
                UserProfilePictureMode = true;
            }
            if (MessageString.StartsWith("/UserFriendsListMode"))
            {
                UserFriendsListMode = true;
            }
            if (MessageString.StartsWith("/OnlineUsersListMode"))
            {
                OnlineUsersMode = true;
            }
            if (MessageString.StartsWith("/OnlineProfilePicturesMode"))
            {
                OnlineProfilePicturesMode = true;
            }
            if (MessageString.StartsWith("/NewProfilePicture"))
            {
                UserToUpdateProfilePicture = App.AuxiliaryClientWorker.GetElement(MessageString, "-U ", ";");
                Console.WriteLine("User that sent the profile picture update pulse is " + UserToUpdateProfilePicture);
                NewProfilePictureMode = true;
            }
            if (MessageString.StartsWith("/NewUser"))
            {
                NewUser = App.AuxiliaryClientWorker.GetElement(MessageString, "-U ", " -S");
                Console.WriteLine("New user is " + NewUser);
                Task.Run(() => MainClientWorker.AssignNewOnlineUser(MessageString));
                NewOnlineUserMode = true;
            }
            if (MessageString.StartsWith("/YourPage"))
            {
                CurrentPageUpdateMode = true;
            }
            if (MessageString.StartsWith("/UserDisconnect"))
            {
                Task.Run(() => MainClientWorker.HadndleDisconnect(MessageString));
            }
            if (MessageString.StartsWith("/Message") == true)
            {
                Task.Run(() => MainClientWorker.HandleMessage(MessageString));
            }
        }

        internal static void Disconnect()
        {
            AntVaultClient.Dispose();
        }


    }
}

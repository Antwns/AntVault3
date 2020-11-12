using System;
using System.Threading.Tasks;
using System.Threading;
using SimpleSockets.Client;
using System.Windows;

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

        internal static void Connect()
        {
             AntVaultClient = new SimpleSocketTcpClient();
            if (HasSetupEvents == false)
            {
                AntVaultClient.BytesReceived += BytesReceived;
                AntVaultClient.MessageReceived += MessageReceived;
                HasSetupEvents = true;
                Console.WriteLine("Events setup complete");
            }
            try
            {
                AntVaultClient.StartClient(AuxiliaryClientWorker.ReadFromConfig("IP"), Convert.ToInt32(AuxiliaryClientWorker.ReadFromConfig("Port")));
                Task.Run(() => AntVaultClient.SendMessage("/ServerStatus?"));
            }
            catch (Exception exc)
            {
                Console.WriteLine("Could not connect to the server due to " + exc);
                App.Current.Dispatcher.Invoke(() =>
                {
                    WindowController.LoginPage.StatusLabel.Content = "ERROR-Server offline, try to Vault later.";
                });
                Thread.Sleep(1000);
                Connect();
            }
        }

        internal static void BytesReceived(SimpleSocketClient Client, byte[] MessageByte)
        {
            string MessageString = AuxiliaryClientWorker.GetStringFromBytes(MessageByte);//Translates stuff for debugging purposes
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
            if (NewThemeMode == true)
            {
                NewThemeMode = false;
                Task.Run(() => MainClientWorker.AssignNewTheme(MessageByte));
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Client.SendMessage("/ServerLoginScreen?");
                });
            }
            if (NewLoginScreenMode == true)
            {
                NewLoginScreenMode = false;
                Task.Run(() => MainClientWorker.AssignNewLoginScreen(MessageByte));
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
                Task.Run(() => MainClientWorker.AssignNewOnlineUserProfilePicture(MessageByte, NewUser));
            }
            if (CurrentPageUpdateMode == true)
            {
                CurrentPageUpdateMode = false;
                Task.Run(() => MainClientWorker.AssignCurrentUserPage(MessageByte));
                Console.WriteLine("Updated " + MainClientWorker.CurrentUser + "'s profile page");
            }
        }

        internal static void MessageReceived(SimpleSocketClient Client, string MessageString)
        {
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
                    Client.SendMessage("/ServerTheme?");
                });
            }
            if (MessageString.StartsWith("/DefaultTheme"))
            {
                Console.WriteLine("Received default theme callback, will not try to update the track");
            }
            if (MessageString.StartsWith("/NewTheme"))
            {
                NewThemeMode = true;
            }
            if (MessageString.StartsWith("/DefaultLoginScreen"))
            {
                Console.WriteLine("Received default login screen callback, will not try to update");
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
                UserToUpdateProfilePicture = AuxiliaryClientWorker.GetElement(MessageString, "-U ", ";");
                Console.WriteLine("User that sent the profile picture update pulse is " + UserToUpdateProfilePicture);
                NewProfilePictureMode = true;
            }
            if (MessageString.StartsWith("/NewUser"))
            {
                NewUser = AuxiliaryClientWorker.GetElement(MessageString, "-U ", " -S");
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

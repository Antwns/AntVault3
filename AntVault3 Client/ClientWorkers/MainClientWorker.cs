using AntVault3_Client.Pages;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WpfAnimatedGif;

namespace AntVault3_Client.ClientWorkers
{
    public class MainClientWorker
    {

        static string CurrentStatus;
        internal static string CurrentUser;

        static Bitmap CurrentProfilePicture;

        internal static Collection<string> CurrentFriendsList = new Collection<string>();
        internal static Collection<string> CurrentOnlineUsers = new Collection<string>();
        internal static Collection<string> CurrentStatuses = new Collection<string>();
        internal static Collection<Bitmap> CurrentProfilePictures = new Collection<Bitmap>();

        internal static ClientNetworking Client = new ClientNetworking();

        internal static void AssignCurrentUserPage(byte[] Data)
        {
            System.Windows.Controls.Page CurrentUserPage = AuxiliaryClientWorker.GetPageFromBytes(Data);
            WindowController.ProfilePage.Content = CurrentUserPage.Content;
            App.Current.Dispatcher.Invoke(() =>
            {
                WindowController.MainPage.MyPageFrame.Content = WindowController.ProfilePage;
                //App.AppendCurrentUserPage(CurrentUserPage);
            });
        }

        internal static void UpdateProfilePicture(string UserToUpdate, byte[] Data)
        {
            CurrentProfilePictures[CurrentOnlineUsers.IndexOf(UserToUpdate)] = AuxiliaryClientWorker.GetBitmapFromBytes(Data);
            if (UserToUpdate == CurrentUser)
            {
                Task.Run(() => AssignProfilePicture(Data));
            }
        }

        internal static void AssignNewLoginScreen(byte[] Data)
        {
            if(File.Exists(AppDomain.CurrentDomain.BaseDirectory + "CustomLoginScreen.gif"))
            {
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "CustomLoginScreen.gif");
            }
            Console.WriteLine("Assigning new login screen now...");
            Bitmap NewLoginScreenbitmap = AuxiliaryClientWorker.GetBitmapFromBytes(Data);
            NewLoginScreenbitmap.Save(AppDomain.CurrentDomain.BaseDirectory + "CustomLoginScreen.gif", ImageFormat.Gif);
            Application.Current.Dispatcher.Invoke(() =>
            {
                ImageBehavior.SetAnimatedSource(WindowController.LoginPage.LoginBackground, new System.Windows.Media.Imaging.BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "CustomLoginScreen.gif")));
            });
        }

        internal static async void AssignNewTheme(byte[] Data)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                LoginPage.LoginMenuMediaPlayer.Stop();
            });
            PlayTransistion();
            await Task.Delay(2000);
            await Task.Run(() =>
            {
                MemoryStream MessageStream = new MemoryStream(Data);
                Console.WriteLine("Playing new theme now...");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (LoginPage.MusicChecked == false)
                    {
                        LoginPage.MusicChecked = true;
                        WindowController.LoginPage.MusicButton.Source = LoginPage.BellWithCheckmark;
                    }
                    LoginPage.LoginMenuPlayer(true, MessageStream.ToArray());
                });
            });
        }

        internal static void PlayTransistion()
        {
            SoundPlayer TransitionPlayer = new SoundPlayer(Properties.Resources.CasetteSwap);
            TransitionPlayer.Play();
        }

        internal static void AssignNewOnlineUserProfilePicture(byte[] Data, string NewUser)
        {
            if (NewUser != CurrentUser)
            {
                CurrentProfilePictures.Add(AuxiliaryClientWorker.GetBitmapFromBytes(Data));
                Console.WriteLine("Successfully assigned " + NewUser + "'s profile picture in " + CurrentUser + "'s profile picture list");
            }
            else
            {
                Console.WriteLine("New User was the same as the current user, no profile picture update needed");
            }
        }

        internal static void HadndleDisconnect(string MessageString)
        {
            string UserToDisconnect = AuxiliaryClientWorker.GetElement(MessageString, "-U ", ";");
            CurrentProfilePictures.Remove(MainClientWorker.CurrentProfilePictures[MainClientWorker.CurrentOnlineUsers.IndexOf(UserToDisconnect)]);
            CurrentOnlineUsers.Remove(UserToDisconnect);
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    WindowController.MainPage.MainChatTextBox.Document.Blocks.Add(App.RemoveUser(UserToDisconnect));
                    WindowController.MainPage.FriendsListTextBox.Document = App.SortFriendsList();
                });
            }
            catch(Exception exc)
            {
                Console.WriteLine("Failed to update UI due to " + exc);
            }

        }

        internal static void AssignNewOnlineUser(string MessageString)
        {
            if (ClientNetworking.NewUser != CurrentUser)
            {
                string NewUser = AuxiliaryClientWorker.GetElement(MessageString, "-U ", " -S");
                CurrentOnlineUsers.Add(NewUser);
                string Newstatus = AuxiliaryClientWorker.GetElement(MessageString, "-S ", ";");
                CurrentStatuses.Add(Newstatus);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    WindowController.MainPage.MainChatTextBox.Document.Blocks.Add(App.AppendNewUser(NewUser));
                    WindowController.MainPage.FriendsListTextBox.Document = App.SortFriendsList();
                });
                Console.WriteLine("Assigned " + NewUser + " in online list for " + CurrentUser);
            }
            else
            {
                Console.WriteLine("New user was the same as the current user, no online user list update needed");
            }

        }

        internal static void HandleMessage(string MessageString)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {

                    WindowController.MainPage.MainChatTextBox.Document.Blocks.Add(App.AppendMessage(MessageString));

                });
            }
            catch (Exception exc)
            {
                Console.WriteLine("Could not update chat due to " + exc);
            }
        }

        internal static void SendMessage(string MessageString)
        {
            Console.WriteLine("[Debug]: Sent message {" + MessageString + "}");
            ClientNetworking.AntVaultClient.SendMessage("/Message -Content " + MessageString + ";");
        }

        internal static void AssignOnlinePictures(byte[] Data)
        {
            CurrentProfilePictures = AuxiliaryClientWorker.GetBitmapCollectionFromBytes(Data);
        }

        internal static void AssignOnlineUsers(byte[] Data)
        {
            CurrentOnlineUsers = AuxiliaryClientWorker.GetStringCollectionFromBytes(Data);
            foreach (string User in CurrentOnlineUsers)
            {
                Console.WriteLine("Registered user " + User);
            }
        }

        internal static void AssingFriendsList(byte[] Data)
        {
            Console.WriteLine("Assigning friends list for " + CurrentUser + ", registering " + CurrentFriendsList.Count + " entries");
            CurrentFriendsList = AuxiliaryClientWorker.GetStringCollectionFromBytes(Data);
        }

        internal static void AssignProfilePicture(byte[] Data)
        {
            Console.WriteLine("Assigned profile picture for " + CurrentUser);//From this point on, consult the Github repository
            CurrentProfilePicture = AuxiliaryClientWorker.GetBitmapFromBytes(Data);
            Application.Current.Dispatcher.Invoke(() =>
            {
                WindowController.MainPage.ProfilePicture.Fill = new ImageBrush(AuxiliaryClientWorker.GetBitmapImageFromBitmap(CurrentProfilePicture));
            });
        }

        internal static void AssignUserInfo(string MessageString)
        {
            CurrentUser = AuxiliaryClientWorker.GetElement(MessageString, "-U ", " -S");
            CurrentStatus = AuxiliaryClientWorker.GetElement(MessageString, "-S ", ";");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.MainWindow.Title = "AntVault - " + CurrentUser;
                WindowController.MainPage.UsernameLabel.Content = CurrentUser;
                WindowController.MainPage.StatusLabel.Content = CurrentStatus;
                //Status UI update tbd;
            });
        }

        internal static async void OpenMainPage()
        {
            await Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() =>
            {
                LoginPage.LoginMenuMediaPlayer.Stop();
            }));
            PlayTransistion();
            await Task.Delay(2000);
            await Task.Run(() =>
            Application.Current.Dispatcher.Invoke(() =>
            {
                LoginPage.LoginMenuPlayer(false, null);
                Application.Current.MainWindow.Content = WindowController.MainPage;

                Application.Current.MainWindow.MaxWidth = 800;
                Application.Current.MainWindow.MaxHeight = 1024;

                Application.Current.MainWindow.Width = 800;
                Application.Current.MainWindow.Height = 1024;

            }));
        }

        internal static void GetNewProfilePicture()
        {
            OpenFileDialog NewProfilePictureDialog = new OpenFileDialog()
            {
                CheckFileExists = true,
                Filter = "png files (*.png)|*.png",
            };
            NewProfilePictureDialog.ShowDialog(App.Current.MainWindow);
            if (NewProfilePictureDialog.FileName != null || NewProfilePictureDialog.FileName != "" && AuxiliaryClientWorker.CheckIfImageIsPng(NewProfilePictureDialog.FileName) == true)
            {
                ClientNetworking.AntVaultClient.SendMessage("/NewProfilePicture");
                ClientNetworking.AntVaultClient.SendBytes(File.ReadAllBytes(NewProfilePictureDialog.FileName));
            }
            else
            {
                MessageBox.Show("You either did not select a file or the file selected was not of valid .png format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
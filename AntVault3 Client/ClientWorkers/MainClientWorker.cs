﻿using AntVault3_Client.Pages;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using WatsonTcp;
using WpfAnimatedGif;

namespace AntVault3_Client.ClientWorkers
{
    public class MainClientWorker
    {
        static string CurrentUser;
        static string CurrentStatus;
        static string NewUser = "NewUser";

        static Bitmap CurrentProfilePicture;

        static bool UserProfilePictureMode;
        static bool UserFriendsListMode;
        static bool OnlineUsersMode;
        static bool OnlineProfilePicturesMode;
        static bool NewOnlineUserMode;
        static bool NewThemeMode;
        static bool HasSetNewUser;
        static bool NewLoginScreenMode;

        internal static Collection<string> CurrentFriendsList = new Collection<string>();
        internal static Collection<string> CurrentOnlineUsers = new Collection<string>();
        internal static Collection<string> CurrentStatuses = new Collection<string>();
        internal static Collection<Bitmap> CurrentProfilePictures = new Collection<Bitmap>();

        internal static void Events_MessageReceivedAsync(object sender, MessageReceivedFromServerEventArgs e)
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
                Task.Run(() => OpenMainPage());
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
                    App.AntVaultClient.Send("/ServerTheme?");
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
                    Task.Run(() => AssignNewTheme(e.Data));
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        App.AntVaultClient.Send("/ServerLoginScreen?");
                    });
                }
            }
            if(MessageString.StartsWith("/DefaultLoginScreen"))
            {
                Console.WriteLine("Received default login screen callback, will not try to update");
            }
            if(MessageString.StartsWith("/NewLoginScreen") || NewLoginScreenMode == true)
            {
                if (MessageString.StartsWith("/NewLoginScreen") || NewLoginScreenMode == false)
                {
                    NewLoginScreenMode = true;
                }
                else
                {
                    NewLoginScreenMode = false;
                    Task.Run(() => AssignNewLoginScreen(e.Data));
                }
            }
            if (MessageString.StartsWith("/UserStringInfo"))
            {
                Task.Run(() => AssignUserInfo(MessageString));
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
                    Task.Run(() => AssignProfilePicture(e.Data));
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
                    Task.Run(() => AssingFriendsList(e.Data));
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
                    Task.Run(() => AssignOnlineUsers(e.Data));
                    Console.WriteLine("Sorting out friends list for " + CurrentUser + ", registering " + CurrentFriendsList.Count + " entries");
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
                    Task.Run(() => AssignOnlinePictures(e.Data));
                    Console.WriteLine("Assigned list for online users");
                }
            }
            if (MessageString.StartsWith("/NewUser") == true || NewOnlineUserMode == true)
            {
                try
                {
                    if(HasSetNewUser == false)
                    {
                        NewUser = AuxiliaryClientWorker.GetElement(MessageString, "-U ", " -S");
                        Console.WriteLine("New user is " + NewUser);
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
                    Task.Run(() => AssignNewOnlineUser(MessageString));
                }
                else
                {
                    NewOnlineUserMode = false;
                    Task.Run(() => AssignNewOnlineUserProfilePicture(e.Data, NewUser));
                    HasSetNewUser = false;
                }
            }
            if (MessageString.StartsWith("/UserDisconnect"))
            {
                Task.Run(() => HadndleDisconnect(MessageString));
            }
            if (MessageString.StartsWith("/Message") == true)
            {
                Task.Run(() => HandleMessage(MessageString));
            }
        }
        private static void HadndleDisconnect(string MessageString)
        {
            string UserToDisconnect = AuxiliaryClientWorker.GetElement(MessageString, "-U ", ";");
            CurrentProfilePictures.Remove(CurrentProfilePictures[CurrentOnlineUsers.IndexOf(UserToDisconnect)]);
            CurrentOnlineUsers.Remove(UserToDisconnect);
            Application.Current.Dispatcher.Invoke(() =>
            {
                WindowController.MainPage.MainChatTextBox.Document.Blocks.Add(App.RemoveUser(UserToDisconnect));
                WindowController.MainPage.FriendsListTextBox.Document = App.SortFriendsList();
            });
        }
        private static void AssignNewLoginScreen(byte[] Data)
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

        private static async void AssignNewTheme(byte[] Data)
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

        private static void PlayTransistion()
        {
            SoundPlayer TransitionPlayer = new SoundPlayer(Properties.Resources.CasetteSwap);
            TransitionPlayer.Play();
        }

        private static void AssignNewOnlineUserProfilePicture(byte[] Data, string NewUser)
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

        private static void AssignNewOnlineUser(string MessageString)
        {
            if (NewUser != CurrentUser)
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

        private static void HandleMessage(string MessageString)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    WindowController.MainPage.MainChatTextBox.Document.Blocks.Add(App.AppendMessage(MessageString));
                }
                catch (Exception exc)
                {
                    Console.WriteLine("Could not update chat due to " + exc);
                }
            });
        }

        internal static void SendMessage(string MessageString)
        {
            Console.WriteLine("[Debug]: Sent message {" + MessageString + "}");
            App.AntVaultClient.Send("/Message -Content " + MessageString + ";");
        }

        private static void AssignOnlinePictures(byte[] Data)
        {
            CurrentProfilePictures = AuxiliaryClientWorker.GetBitmapCollectionFromBytes(Data);
        }

        private static void AssignOnlineUsers(byte[] Data)
        {
            CurrentOnlineUsers = AuxiliaryClientWorker.GetStringCollectionFromBytes(Data);
            foreach (string User in CurrentOnlineUsers)
            {
                Console.WriteLine("Registered user " + User);
            }
        }

        private static void AssingFriendsList(byte[] Data)
        {
            Console.WriteLine("Assigning friends list for " + CurrentUser + ", registering " + CurrentFriendsList.Count + " entries");
            CurrentFriendsList = AuxiliaryClientWorker.GetStringCollectionFromBytes(Data);
        }

        private static void AssignProfilePicture(byte[] Data)
        {
            Console.WriteLine("Assigned profile picture for " + CurrentUser);//From this point on, consult the Github repository
            CurrentProfilePicture = AuxiliaryClientWorker.GetBitmapFromBytes(Data);
            Application.Current.Dispatcher.Invoke(() =>
            {
                WindowController.MainPage.ProfilePicture.Fill = new ImageBrush(AuxiliaryClientWorker.GetBitmapImageFromBitmap(CurrentProfilePicture));
            });
        }

        private static void AssignUserInfo(string MessageString)
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

        internal static void Events_ExceptionEncountered(object sender, WatsonTcp.ExceptionEventArgs e)
        {
            App.WriteToLog(e.Json);
        }
    }
}
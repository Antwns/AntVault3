using AntVault3_Client.Animations;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System;
using System.IO;
using System.Windows.Media;
using AntVault3_Client.ClientWorkers;
using System.Windows.Media.Imaging;

namespace AntVault3_Client.Pages
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        internal static bool MusicChecked = true;
        internal static bool UpdatedTheme = false;

        static BitmapImage Bell = new BitmapImage(new Uri("pack://application:,,,/Resources/Bell.png"));
        internal static BitmapImage BellWithCheckmark = new BitmapImage(new Uri("pack://application:,,,/Resources/BellWithCheckmark.png"));//Move bitmaps to app.xaml.cs and define method there so the dispatcher works properly
        public LoginPage()
        {
            InitializeComponent();
        }
        internal static MediaPlayer LoginMenuMediaPlayer = new MediaPlayer()
        {
            Volume = 1.0,
        };

        private void StatusLabel_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(()=> LoginAnimations.MoveLabel(StatusLabel));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoginMenuPlayer(true, null);
            Task.Run(() => ClientNetworking.Connect());
        }

        private void UsernameTextBox_MouseEnter(object sender, MouseEventArgs e)
        {
            if (UsernameTextBox.Text == "Username")
            {
                UsernameTextBox.Text = "";
            }
        }

        private void UsernameTextBox_MouseLeave(object sender, MouseEventArgs e)
        {
            if (UsernameTextBox.Text == "" && UsernameTextBox.IsSelectionActive == false)
            {
                UsernameTextBox.Text = "Username";
            }
        }

        private void PasswordTextBox_MouseEnter(object sender, MouseEventArgs e)
        {
            if (PasswordTextBox.Password == "Password")
            {
                PasswordTextBox.Password = "";
            }
        }

        private void PasswordTextBox_MouseLeave(object sender, MouseEventArgs e)
        {
            if (PasswordTextBox.Password == "" && PasswordTextBox.IsSelectionActive == false)
            {
                PasswordTextBox.Password = "Password";
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            ClientNetworking.AntVaultClient.SendMessage("/Login -U " + UsernameTextBox.Text + " -P " + PasswordTextBox.Password + ";");
        }

        internal static void LoginMenuPlayer(bool Play, byte[] BytesToPlay)
        {
            if (BytesToPlay == null)
            {
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Hyper.wav") == false)
                {
                    MemoryStream BGMFileStream = new MemoryStream(Convert.ToInt32(Properties.Resources.Hyper.Length));
                    Properties.Resources.Hyper.CopyTo(BGMFileStream);
                    try
                    {
                        File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + "Hyper.wav", BGMFileStream.ToArray());
                    }
                    catch
                    {
                        MessageBox.Show("Please move the app to a folder where reading and writing is allowed, then relaunch the application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown();
                    }
                }
                LoginMenuMediaPlayer.MediaEnded += LoginMenuMediaPlayer_MediaEnded;
                LoginMenuMediaPlayer.Open(new Uri(AppDomain.CurrentDomain.BaseDirectory + "Hyper.wav"));
                if (Play == true)
                {
                    LoginMenuMediaPlayer.Play();
                }
                else
                {
                    LoginMenuMediaPlayer.Stop();
                }
            }
            else
            {
                if (UpdatedTheme == false)
                {
                    if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "CustomTheme.wav"))
                    {
                        File.Delete(AppDomain.CurrentDomain.BaseDirectory + "CustomTheme.wav");//Delete old file in case it exists
                    }
                    MemoryStream BGMFileStream = new MemoryStream(BytesToPlay);
                    try
                    {
                        File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + "CustomTheme.wav", BGMFileStream.ToArray());
                    }
                    catch
                    {
                        MessageBox.Show("Please move the app to a folder where reading and writing is allowed, then relaunch the application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown();
                    }
                    LoginMenuMediaPlayer.MediaEnded += LoginMenuMediaPlayer_MediaEnded;
                    UpdatedTheme = true;
                }
                LoginMenuMediaPlayer.Open(new Uri(AppDomain.CurrentDomain.BaseDirectory + "CustomTheme.wav"));
                if (Play == true)
                {
                    LoginMenuMediaPlayer.Play();
                }
                else
                {
                    LoginMenuMediaPlayer.Stop();
                }
            }
        }

        internal static void LoginMenuMediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            LoginMenuMediaPlayer.Position = TimeSpan.Zero;
            LoginMenuMediaPlayer.Play();
        }

        private void MusicButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MusicChecked == false)
            {
                LoginMenuMediaPlayer.Play();
                MusicButton.Source = BellWithCheckmark;
                MusicChecked = true;
            }
            else
            {
                LoginMenuMediaPlayer.Pause();
                MusicButton.Source = Bell;
                MusicChecked = false;
            }
        }
    }
}

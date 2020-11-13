using AntVault3_Server.Resources;
using SimpleSockets.Messaging.Metadata;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace AntVault3_Server.ServerWorkers
{
    class MainServerWorker
    {
        internal static string DatabaseDir = AppDomain.CurrentDomain.BaseDirectory + "AntVaultServer.users";
        internal static string UserDirectories = AppDomain.CurrentDomain.BaseDirectory + "UserDirectories";

        internal static string ServerTheme = AppDomain.CurrentDomain.BaseDirectory + "ServerTheme.wav";
        internal static string ServerLoginScreen = AppDomain.CurrentDomain.BaseDirectory + "ServerLoginScreen.gif";

        internal static Collection<Session> Sessions = new Collection<Session>();
        static Collection<string> Usernames = new Collection<string>();
        static Collection<string> Passwords = new Collection<string>();
        static Collection<string> Statuses = new Collection<string>();
        static Collection<Bitmap> ProfilePictures = new Collection<Bitmap>();
        static Collection<Bitmap> OnlineProfilePictures = new Collection<Bitmap>();
        static Collection<string> OnlineUsers = new Collection<string>();
        internal static void SendPageForUser(IClientInfo Client)
        {
            string Username = Sessions.First(Sess => Sess.IpPort.Equals(Client.RemoteIPv4)).Username;
            if (File.Exists(UserDirectories + "\\" + Username + "\\" + Username + ".AVPage"))
            {
                AuxiliaryServerWorker.WriteOK("Validated " + Username + "'s page");
                ServerNetworking.AntVaultServer.SendBytes(AuxiliaryServerWorker.GetClientIDFromIpPort(Sessions.First(Sess => Sess.Username.Equals(Username)).IpPort), File.ReadAllBytes(UserDirectories + "\\" + Username + "\\" + Username + ".AVPage"));
                AuxiliaryServerWorker.WriteOK("Sent " + Username + " their page");
            }
            else
            {
                AuxiliaryServerWorker.WriteError("Could not find " + Username + "'s page, integrity check will now follow...");
                Thread PageCheckerThread = new Thread(CheckPages);
                PageCheckerThread.SetApartmentState(ApartmentState.STA);
                PageCheckerThread.Start();//mostly testing
            }
        }

        internal static void CheckServerLoginScreen()
        {
            if (File.Exists(ServerLoginScreen) == false)
            {
                AuxiliaryServerWorker.WriteError("Server login screen .gif file was not found. Generating new default one...");
                try
                {
                    MemoryStream ServerLoginScreenStream = new MemoryStream(AuxiliaryServerWorker.GetBytesFromBitmap(Properties.Resources.LoginMenuBg));
                    Properties.Resources.LoginMenuBg.Save(ServerLoginScreen, ImageFormat.Gif);
                    AuxiliaryServerWorker.WriteOK("Successfully generated default server login screen");
                }
                catch (Exception exc)
                {
                    AuxiliaryServerWorker.WriteError("Could not generate default server login screen due to " + exc);
                }
            }
            else
            {
                AuxiliaryServerWorker.WriteOK("Loaded server login screen successfully");
            }
        }

        internal static void CheckPages()
        {
            foreach (string User in Usernames)
            {
                if (File.Exists(UserDirectories + "\\" + User + "\\" + User + ".AVPage") == false)
                {
                    AuxiliaryServerWorker.WriteError("Couldn't find page for user " + User);
                    FlowDocument DefaultFlowDocument = new FlowDocument();
                    Paragraph DefaultParagraph = new Paragraph(new Run("Welcome to my page!" + Environment.NewLine + "This is a rich textbox! It can contain images and text! Means you can format it as you like!"));
                    DefaultFlowDocument.Blocks.Add(DefaultParagraph);
                    TextRange DefaultTextRange = new TextRange(DefaultFlowDocument.ContentStart, DefaultFlowDocument.ContentEnd);
                    using (MemoryStream DefaultTextMemoryStream = new MemoryStream())
                    {
                        DefaultTextRange.Save(DefaultTextMemoryStream, DataFormats.XamlPackage);
                        AVPage NewUserPage = new AVPage()
                        {
                            Banner = Properties.Resources.DefaultCover,
                            Content = DefaultTextMemoryStream.ToArray(),
                        };
                        File.WriteAllBytes(UserDirectories + "\\" + User + "\\" + User + ".AVPage", AuxiliaryServerWorker.GetBytesFromClass(NewUserPage));
                        AuxiliaryServerWorker.WriteOK("Successfully generated default page for " + User);
                    };
                }
            }
        }

        internal static void CheckServerTheme()
        {
            if(File.Exists(ServerTheme) == false)
            {
                AuxiliaryServerWorker.WriteError("Server theme .wav file was not found. Generating new default one...");
                try
                {
                    MemoryStream ServerThemeStream = new MemoryStream(Convert.ToInt32(Properties.Resources.ServerTheme.Length));
                    Properties.Resources.ServerTheme.CopyTo(ServerThemeStream);
                    File.WriteAllBytes(ServerTheme, ServerThemeStream.ToArray());
                    AuxiliaryServerWorker.WriteOK("Successfully generated default server theme");
                }
                catch (Exception exc)
                {
                    AuxiliaryServerWorker.WriteError("Could not generate default server theme due to " + exc);
                }
            }
            else
            {
                AuxiliaryServerWorker.WriteOK("Found server theme successfully");
            }
        }

        internal static void CheckDatabase()
        {
            bool ErrorsFound = false;
            string CurrentLine;
            int LineNumber = 0;
            AuxiliaryServerWorker.WriteInfo("Checking database...");
            if(File.Exists(DatabaseDir) == false)
            {
                AuxiliaryServerWorker.WriteError("Could not find database file. Creating a new default database...");
                try
                {
                    File.WriteAllText(DatabaseDir, Properties.Resources.DefaultDatabase);
                    AuxiliaryServerWorker.WriteOK("New default database successfully created.");
                }
                catch(Exception exc)
                {
                    AuxiliaryServerWorker.WriteError("Could not create new default database due to " + exc);
                }
            }
            StreamReader DatabaseStreamReader = new StreamReader(DatabaseDir);
            while ((CurrentLine = DatabaseStreamReader.ReadLine()) != null)
            {
                if(CurrentLine.StartsWith("/U ") && CurrentLine.EndsWith("."))
                {
                    Usernames.Add(AuxiliaryServerWorker.GetElement(CurrentLine, "/U ", " /P"));
                    Passwords.Add(AuxiliaryServerWorker.GetElement(CurrentLine, "/P ", " /S"));
                    Statuses.Add(AuxiliaryServerWorker.GetElement(CurrentLine, "/S ", "."));
                    try
                    {
                        ProfilePictures.Add(AuxiliaryServerWorker.GetBitmapFromBytes(File.ReadAllBytes(UserDirectories + "\\" + Usernames[LineNumber] + "\\ProfilePicture_" + Usernames[LineNumber] + ".png")));
                    }
                    catch
                    {
                        AuxiliaryServerWorker.WriteError("Profile picture not found for user " + Usernames[LineNumber] + " generating default profile picture instead...");
                        Directory.CreateDirectory(UserDirectories + "\\" + Usernames[LineNumber]);
                        try
                        {
                            File.WriteAllBytes(UserDirectories + "\\" + Usernames[LineNumber] + "\\ProfilePicture_" + Usernames[LineNumber] + ".png", AuxiliaryServerWorker.GetBytesFromBitmap(Properties.Resources.DefaultProfilePicture));
                            AuxiliaryServerWorker.WriteOK("Default profile picture created successfully for " + Usernames[LineNumber]);
                            ProfilePictures.Add(AuxiliaryServerWorker.GetBitmapFromBytes(File.ReadAllBytes(UserDirectories + "\\" + Usernames[LineNumber] + "\\ProfilePicture_" + Usernames[LineNumber] + ".png")));
                            AuxiliaryServerWorker.WriteOK("Successfully appended default profile picture for user " + Usernames[LineNumber]);
                        }
                        catch (Exception exc)
                        {
                            ErrorsFound = true;
                            AuxiliaryServerWorker.WriteError("Could not create default profile picture for " + Usernames[LineNumber] + " due to " + exc);
                        }
                    }
                }
                if(ErrorsFound == false)
                {
                    AuxiliaryServerWorker.WriteOK("Successfully loaded " + Usernames[LineNumber] + "'s user info");
                }
                LineNumber++;
            }
            AuxiliaryServerWorker.WriteOK("Successfully loaded " + LineNumber + " accounts");
            if(ErrorsFound == true)
            {
                AuxiliaryServerWorker.WriteInfo("One or more errors were met during the load process, please theck the integrity of the database or delete it to generate a new one with default values");
            }
        }

        internal static async Task SendNewProfilePicturePulseAsync(string UserToUpdate, byte[] Data)
        {
            foreach (Session Sess in Sessions)
            {
                await ServerNetworking.AntVaultServer.SendMessageAsync(AuxiliaryServerWorker.GetClientIDFromIpPort(Sess.IpPort), "/NewProfilePicture -U " + UserToUpdate + ";");
                await Task.Delay(10);
                await ServerNetworking.AntVaultServer.SendBytesAsync(AuxiliaryServerWorker.GetClientIDFromIpPort(Sess.IpPort), Data);
                AuxiliaryServerWorker.WriteOK("Sent profile picture update pulse to " + UserToUpdate);
            }
            AuxiliaryServerWorker.WriteOK("Finished sending profile picture update pulses to " + Sessions.Count + " active clients");
        }

        internal static void UpdateProfilePicture(IClientInfo Client, byte[] Data)
        {
            string UserToUpdate = Sessions.First(S => S.IpPort.Equals(Client.RemoteIPv4)).Username;
            string FileToUpdate = UserDirectories + "\\" + UserToUpdate + "\\" + "ProfilePicture_" + UserToUpdate + ".png";
            AuxiliaryServerWorker.WriteInfo("Received profile picture update pulse from " + UserToUpdate);
            Bitmap ProfilePictureToUpdate = AuxiliaryServerWorker.GetBitmapFromBytes(Data);
            ProfilePictures[Usernames.IndexOf(UserToUpdate)] = ProfilePictureToUpdate;
            if(File.Exists(FileToUpdate))
            {
                File.Delete(FileToUpdate);
            }
            ProfilePictureToUpdate.Save(FileToUpdate, ImageFormat.Png);
            AuxiliaryServerWorker.WriteOK("Updated local profile picture for " + UserToUpdate);
            Task.Run(() => SendNewProfilePicturePulseAsync(UserToUpdate, Data));
        }

        internal static void AboutUser(string UserToCheck)
        {
            AuxiliaryServerWorker.WriteInfo("User's information: " + Environment.NewLine + "IpPort: " + Sessions.First(Session => Session.Username.Equals(UserToCheck)).IpPort + Environment.NewLine + "Login time: " + Sessions.First(Session => Session.Username.Equals(UserToCheck)).LoginTime + Environment.NewLine + "ID: " + AuxiliaryServerWorker.GetClientIDFromIpPort(Sessions.First(Session => Session.Username.Equals(UserToCheck)).IpPort) + Environment.NewLine + "Status: " + Sessions.First(Session => Session.Username.Equals(UserToCheck)).Status);
        }

        internal static void UpdateLoginScreen(IClientInfo Client)
        {
            AuxiliaryServerWorker.WriteInfo(Client.RemoteIPv4 + " requested the current server login screen");
            byte[] ServerLoginScreenBytes = File.ReadAllBytes(ServerLoginScreen);
            MemoryStream DefaultServerLoginScreenStream = new MemoryStream();
            Properties.Resources.LoginMenuBg.Save(DefaultServerLoginScreenStream, ImageFormat.Gif);
            if(ServerLoginScreenBytes.Length == DefaultServerLoginScreenStream.ToArray().Length)
            {
                AuxiliaryServerWorker.WriteInfo("Default server login screen detected");
                ServerNetworking.AntVaultServer.SendMessage(Client.Id, "/DefaultLoginScreen");
                AuxiliaryServerWorker.WriteOK("No login screen set");
            }
            else
            {
                AuxiliaryServerWorker.WriteInfo("Custom login screen detected");
                if(Sessions.Any(Sess => Sess.IpPort.Equals(Client.RemoteIPv4)) == false)
                {
                    Task.Run(()=> ServerNetworking.AntVaultServer.SendMessage(Client.Id, "/NewLoginScreen"));
                    MemoryStream NewServerLoginScreenStream = new MemoryStream(File.ReadAllBytes(ServerLoginScreen));
                    ServerNetworking.AntVaultServer.SendBytes(Client.Id, NewServerLoginScreenStream.ToArray(), true);
                    AuxiliaryServerWorker.WriteOK("Custom login screen sent to " + Client.RemoteIPv4);
                }
            }
        }

        internal static async Task UpdateThemeAsync(IClientInfo Client)
        {
            AuxiliaryServerWorker.WriteInfo(Client.RemoteIPv4 + " requested the current server theme");
            byte[] ServerThemeBytes = File.ReadAllBytes(ServerTheme);
            MemoryStream DefaultServerThemeStream = new MemoryStream(Convert.ToInt32(Properties.Resources.ServerTheme.Length));
            Properties.Resources.ServerTheme.CopyTo(DefaultServerThemeStream);
            if(ServerThemeBytes.Length == DefaultServerThemeStream.ToArray().Length)
            {
                AuxiliaryServerWorker.WriteInfo("Default theme detected");
                //Send msg that the client should use the default theme
                ServerNetworking.AntVaultServer.SendMessage(Client.Id, "/DefaultTheme");
                AuxiliaryServerWorker.WriteOK("No theme sent");
            }
            else
            {
                AuxiliaryServerWorker.WriteInfo("Custom theme detected");
                //Send msg that the client shuold enter UpdateThemeMode and then send the new theme over a stream
                if (Sessions.Any(Sess => Sess.IpPort.Equals(Client.RemoteIPv4)) == false)
                {
                    await Task.Run(() => ServerNetworking.AntVaultServer.SendMessage(Client.Id, "/NewTheme"));
                    MemoryStream NewServerThemeStream = new MemoryStream(File.ReadAllBytes(ServerTheme));
                    await Task.Delay(50);
                    await Task.Run(() => ServerNetworking.AntVaultServer.SendBytesAsync(Client.Id, NewServerThemeStream.ToArray()));
                    AuxiliaryServerWorker.WriteOK("Custom theme sent to " + Client.RemoteIPv4);
                }
            }
        }

        internal static void HandleDisconnect(string MessageString, IClientInfo Client)
        {
            string Reason = AuxiliaryServerWorker.GetElement(MessageString, "-Content ", ";");
            string UserToDisconnect = Sessions.First(Sess => Sess.IpPort.Equals(Client.RemoteIPv4)).Username;
            AuxiliaryServerWorker.WriteInfo(UserToDisconnect + " disconnected due to " + Reason );
            OnlineProfilePictures.Remove(OnlineProfilePictures[OnlineUsers.IndexOf(UserToDisconnect)]);
            OnlineUsers.Remove(UserToDisconnect);
            Sessions.Remove(Sessions.First(S => S.Username.Equals(UserToDisconnect)));
            try
            {
                //ServerNetworking.AntVaultServer.DisconnectClient(IpPort);
            }
            catch
            {
                AuxiliaryServerWorker.WriteError(UserToDisconnect + " was already disconnected");
            }
            Task.Run(() =>
            {
                foreach (Session Sess in Sessions)
                {
                    ServerNetworking.AntVaultServer.SendMessage(AuxiliaryServerWorker.GetClientIDFromIpPort(Sess.IpPort), "/UserDisconnect -U " + UserToDisconnect + ";");
                }
            });
        }

        internal static void HandleMessage(string MessageStringC, IClientInfo Client)
        {
            bool ValidSender = false;
            string Sender = null;
            string Message = AuxiliaryServerWorker.GetElement(MessageStringC, "-Content ", ";");
            if (Sessions.Any(S => S.IpPort.Equals(Client.RemoteIPv4)))
            {
                ValidSender = true;
                Sender = Sessions.First(Session => Session.IpPort.Equals(Client.RemoteIPv4)).Username;
            }
            if (ValidSender == true)
            {
                AuxiliaryServerWorker.WriteInfo("[" + Sender + "]: " + Message);
                foreach (IClientInfo Cl in ServerNetworking.AntVaultServer.GetConnectedClients().Values)
                {
                    ServerNetworking.AntVaultServer.SendMessage(Cl.Id, "/Message -U " + Sender + " -Content " + Message + ";");
                }
            }
            else
            {
                AuxiliaryServerWorker.WriteError("Could not broadcast that message from " + Client.RemoteIPv4 + " due to improper formatting");
            }
        }

        internal static async Task DoAuthenticationAsync(string MessageString, IClientInfo Client)
        {
            string UsernameC = AuxiliaryServerWorker.GetElement(MessageString, "/Login -U ", " -P");
            string PasswordC = AuxiliaryServerWorker.GetElement(MessageString, "-P ", ";");
            if (Usernames.Contains(UsernameC) == true && Passwords[Usernames.IndexOf(UsernameC)] == PasswordC)
            {
                AuxiliaryServerWorker.WriteOK(Client.RemoteIPv4 + " successfully authenticated as " + UsernameC);
                Session Sess = new Session()
                {
                    IpPort = Client.RemoteIPv4,
                    Username = UsernameC,
                    LoginTime = DateTime.Now,
                    ProfilePicture = ProfilePictures[Usernames.IndexOf(UsernameC)],
                    Friends = AuxiliaryServerWorker.GetFriendsListForUser(UsernameC),
                    Status = Statuses[Usernames.IndexOf(UsernameC)]
                };
                Sessions.Add(Sess);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendMessage(Client.Id,"/AcceptConnection"));
                AuxiliaryServerWorker.WriteInfo("Created new session for " + UsernameC);
                OnlineUsers.Add(Sess.Username);
                OnlineProfilePictures.Add(Sess.ProfilePicture);
                AuxiliaryServerWorker.WriteInfo("Sending personal profile data to " + UsernameC + "...");
                await Task.Run(() => ServerNetworking.AntVaultServer.SendMessageAsync(Client.Id, "/UserStringInfo -U " + UsernameC + " -S " + Sess.Status + ";"));
                await Task.Delay(50);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendMessageAsync(Client.Id, "/UserProfilePictureMode"));
                await Task.Delay(50);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendBytesAsync(Client.Id, AuxiliaryServerWorker.GetBytesFromBitmap(Sess.ProfilePicture)));
                await Task.Delay(50);
                //^Profile picture
                AuxiliaryServerWorker.WriteInfo("Sent " + UsernameC + " their profile picture");
                //vCollections
                await Task.Delay(50);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendMessageAsync(Client.Id, "/UserFriendsListMode"));
                await Task.Delay(50);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendBytesAsync(Client.Id, AuxiliaryServerWorker.GetBytesFromStringCollection(Sess.Friends)));
                AuxiliaryServerWorker.WriteInfo("Sent " + UsernameC + " their friends list");
                await Task.Delay(100);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendMessageAsync(Client.Id, "/OnlineUsersListMode"));
                await Task.Delay(50);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendBytesAsync(Client.Id, AuxiliaryServerWorker.GetBytesFromStringCollection(OnlineUsers)));
                AuxiliaryServerWorker.WriteInfo("Sent " + UsernameC + " list of current online users");
                await Task.Delay(100);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendMessageAsync(Client.Id, "/OnlineProfilePicturesMode"));
                await Task.Delay(50);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendBytesAsync(Client.Id, AuxiliaryServerWorker.GetBytesFromBitmapCollection(OnlineProfilePictures)));
                AuxiliaryServerWorker.WriteInfo("Sent " + UsernameC + " list of current online user profile pictures");
                await Task.Delay(100);
                await Task.Run(() => NewUserUpdatePulseAsync(UsernameC, Sess.Status, Sess.ProfilePicture));
                AuxiliaryServerWorker.WriteInfo("Sent new user update pulse to alll clients");
                await Task.Run(() => ServerNetworking.AntVaultServer.SendMessageAsync(Client.Id, "/YourPage"));
                await Task.Delay(100);
                await Task.Run(() => SendPageForUser(Client));
            }
            else
            {
                AuxiliaryServerWorker.WriteError(Client.RemoteIPv4 + " tried to authenticate as " + UsernameC + " and failed");
                ServerNetworking.AntVaultServer.SendMessage(Client.Id, "/DenyConnection");
            }
        }

        private static async Task NewUserUpdatePulseAsync(string Username, string Status, Bitmap ProfilePicture)
        {
            foreach (Session Sess in Sessions)
            {
                await Task.Run(() => ServerNetworking.AntVaultServer.SendMessageAsync(AuxiliaryServerWorker.GetClientIDFromIpPort(Sess.IpPort), "/NewUser -U " + Username + " -S " + Status + ";"));
                await Task.Delay(50);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendBytesAsync(AuxiliaryServerWorker.GetClientIDFromIpPort(Sess.IpPort), AuxiliaryServerWorker.GetBytesFromBitmap(ProfilePicture)));
            }
        }

        internal static void UpdateStatus(string Text)
        {
            AuxiliaryServerWorker.WriteInfo("Entering status update mode");
            ServerNetworking.ServerStatus = Text;
            foreach(IClientInfo Client in ServerNetworking.AntVaultServer.GetConnectedClients().Values)
            {
                ServerNetworking.AntVaultServer.SendMessage(Client.Id, "/ServerStatus " + ServerNetworking.ServerStatus + ";");
                AuxiliaryServerWorker.WriteInfo("Sent status update pulse to " + Client.RemoteIPv4);
            }
            AuxiliaryServerWorker.WriteInfo("Updating config file...");
            string NewConfig = "/IP " + AuxiliaryServerWorker.ReadFromConfig("IP") + "\\" + Environment.NewLine + "/Port " + AuxiliaryServerWorker.ReadFromConfig("Port") + "\\" + Environment.NewLine + "/Status " + ServerNetworking.ServerStatus + "\\";
            try
            {
                File.WriteAllText(AuxiliaryServerWorker.ConfigDir, NewConfig);
                AuxiliaryServerWorker.WriteOK("Updated config file successfully");
            }
            catch(Exception exc)
            {
                AuxiliaryServerWorker.WriteError("Could not update config file due to " + exc);
            }
            AuxiliaryServerWorker.WriteInfo("Exiting status update mode");
        }
    }
}

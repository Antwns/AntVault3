using AntVault3_Server.Resources;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WatsonTcp;

namespace AntVault3_Server.ServerWorkers
{
    class MainServerWorker
    {
        internal static WatsonTcpServer AntVaultServer = new WatsonTcpServer(AuxiliaryServerWorker.ReadFromConfig("IP"), Convert.ToInt32(AuxiliaryServerWorker.ReadFromConfig("Port")));

        internal static string DatabaseDir = AppDomain.CurrentDomain.BaseDirectory + "AntVaultServer.users";
        internal static string UserDirectories = AppDomain.CurrentDomain.BaseDirectory + "UserDirectories";

        internal static string ServerTheme = AppDomain.CurrentDomain.BaseDirectory + "ServerTheme.wav";
        internal static string ServerLoginScreen = AppDomain.CurrentDomain.BaseDirectory + "ServerLoginScreen.gif";

        static Collection<Session> Sessions = new Collection<Session>();
        static Collection<string> Usernames = new Collection<string>();
        static Collection<string> Passwords = new Collection<string>();
        static Collection<string> Statuses = new Collection<string>();
        static Collection<Bitmap> ProfilePictures = new Collection<Bitmap>();
        static Collection<Bitmap> OnlineProfilePictures = new Collection<Bitmap>();
        static Collection<string> OnlineUsers = new Collection<string>();

        static string ServerStatus;
        static bool SetUpEvents = false;

        internal static void StartServer()
        {
            if(SetUpEvents == false)
            {
                AntVaultServer.Keepalive.EnableTcpKeepAlives = true;
                AntVaultServer.Keepalive.TcpKeepAliveInterval = 5;
                AntVaultServer.Keepalive.TcpKeepAliveRetryCount = 5;
                AntVaultServer.Keepalive.TcpKeepAliveTime = 5;
                AntVaultServer.Events.StreamReceived += Events_StreamReceived;
                AntVaultServer.Events.MessageReceived += Events_MessageReceived;
                SetUpEvents = true;
                AuxiliaryServerWorker.WriteOK("Event callbacks hooked successfully");
            }
            AuxiliaryServerWorker.WriteInfo("Reading server status from config...");
            ServerStatus = AuxiliaryServerWorker.ReadFromConfig("Status");
            CheckDatabase();
            AuxiliaryServerWorker.WriteOK("Server status is set to " + ServerStatus);
            AuxiliaryServerWorker.WriteInfo("Checking server theme...");
            CheckServerTheme();
            AuxiliaryServerWorker.WriteInfo("Checking server login screen...");
            CheckServerLoginScreen();
            try
            {
                AntVaultServer.Start();
                AuxiliaryServerWorker.WriteOK("Server started successfully on " + AuxiliaryServerWorker.ReadFromConfig("IP") + ":" + AuxiliaryServerWorker.ReadFromConfig("Port"));
            }
            catch (Exception exc)
            {
                AuxiliaryServerWorker.WriteError("Server could not be started due to " + exc);
            }
        }

        private static void CheckServerLoginScreen()
        {
            if (File.Exists(ServerLoginScreen) == false)
            {
                AuxiliaryServerWorker.WriteError("Server login screen .gif file was not found. Generating new default one...");
                try
                {
                    MemoryStream ServerLoginScreenStream = new MemoryStream(AuxiliaryServerWorker.GetBytesFromBitmap(Properties.Resources.ServerLoginScreen));
                    Properties.Resources.ServerLoginScreen.Save(ServerLoginScreen, ImageFormat.Gif);
                    AuxiliaryServerWorker.WriteOK("Successfully generated default server login screen");
                }
                catch (Exception exc)
                {
                    AuxiliaryServerWorker.WriteError("Could not generate default server login screen due to " + exc);
                }
            }
            else
            {
                AuxiliaryServerWorker.WriteOK("Found server login screen successfully");
            }
        }

        private static void CheckServerTheme()
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

        private static void CheckDatabase()
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

        private static void Events_StreamReceived(object sender, StreamReceivedFromClientEventArgs e)
        {
            AuxiliaryServerWorker.WriteDebug(AuxiliaryServerWorker.GetStringFromBytes(e.Data));
        }

        private static void Events_MessageReceived(object sender, MessageReceivedFromClientEventArgs e)
        {
            string MessageString = AuxiliaryServerWorker.GetStringFromBytes(e.Data);
            AuxiliaryServerWorker.WriteDebug(MessageString);
            if(MessageString.StartsWith("/ServerStatus?"))
            {
                Task.Run(() => UpdateStatus(ServerStatus));
            }
            if(MessageString.StartsWith("/ServerTheme?"))
            {
                Task.Run(() => UpdateTheme(e.IpPort));
            }
            if(MessageString.StartsWith("/Login"))
            {
                Task.Run(() => DoAuthenticationAsync(MessageString, e.IpPort));
            }
            if(MessageString.StartsWith("/Message"))
            {
                Task.Run(() => HandleMessage(MessageString, e.IpPort));
            }
            if(MessageString.StartsWith("/Disconnect"))
            {
                Task.Run(() => HandleDisconnect(MessageString, e.IpPort));
            }
            if(MessageString.StartsWith("/ServerLoginScreen?"))
            {
                Task.Run(() => UpdateLoginScreen(e.IpPort));
            }
        }

        private static void UpdateLoginScreen(string IpPort)
        {
            AuxiliaryServerWorker.WriteInfo(IpPort + " requested the current server login screen");
            byte[] ServerLoginScreenBytes = File.ReadAllBytes(ServerLoginScreen);
            MemoryStream DefaultServerLoginScreenStream = new MemoryStream();
            Properties.Resources.ServerLoginScreen.Save(DefaultServerLoginScreenStream, ImageFormat.Gif);
            if(ServerLoginScreenBytes.Length == DefaultServerLoginScreenStream.ToArray().Length)
            {
                AuxiliaryServerWorker.WriteInfo("Default server login screen detected");
                AntVaultServer.Send(IpPort, "/DefaultLoginScreen");
                AuxiliaryServerWorker.WriteOK("No login screen set");
            }
            else
            {
                AuxiliaryServerWorker.WriteInfo("Custom login screen detected");
                if(Sessions.Any(Sess => Sess.IpPort.Equals(IpPort)) == false)
                {
                    Task.Run(()=>AntVaultServer.Send(IpPort, "/NewLoginScreen"));
                    MemoryStream NewServerLoginScreenStream = new MemoryStream(File.ReadAllBytes(ServerLoginScreen));
                    AntVaultServer.Send(IpPort, NewServerLoginScreenStream.Length, NewServerLoginScreenStream);
                    AuxiliaryServerWorker.WriteOK("Custom login screen sent to " + IpPort);
                }
            }
        }

        private static void UpdateTheme(string IpPortC)
        {
            AuxiliaryServerWorker.WriteInfo(IpPortC + " requested the current server theme");
            byte[] ServerThemeBytes = File.ReadAllBytes(ServerTheme);
            MemoryStream DefaultServerThemeStream = new MemoryStream(Convert.ToInt32(Properties.Resources.ServerTheme.Length));
            Properties.Resources.ServerTheme.CopyTo(DefaultServerThemeStream);
            if(ServerThemeBytes.Length == DefaultServerThemeStream.ToArray().Length)
            {
                AuxiliaryServerWorker.WriteInfo("Default theme detected");
                //Send msg that the client should use the default theme
                AntVaultServer.Send(IpPortC, "/DefaultTheme");
                AuxiliaryServerWorker.WriteOK("No theme sent");
            }
            else
            {
                AuxiliaryServerWorker.WriteInfo("Custom theme detected");
                //Send msg that the client shuold enter UpdateThemeMode and then send the new theme over a stream
                if (Sessions.Any(Sess => Sess.IpPort.Equals(IpPortC)) == false)
                {
                    Task.Run(()=>AntVaultServer.Send(IpPortC, "/NewTheme"));
                    MemoryStream NewServerThemeStream = new MemoryStream(File.ReadAllBytes(ServerTheme));
                    AntVaultServer.Send(IpPortC, NewServerThemeStream.Length, NewServerThemeStream);
                    AuxiliaryServerWorker.WriteOK("Custom theme sent to " + IpPortC);
                }
            }
        }

        private static void HandleDisconnect(string MessageString, string IpPort)
        {
            string Reason = AuxiliaryServerWorker.GetElement(MessageString, "-Content ", ";");
            string UserToDisconnect = Sessions.First(Sess => Sess.IpPort.Equals(IpPort)).Username;
            AuxiliaryServerWorker.WriteInfo(UserToDisconnect + " disconnected due to " + Reason );
            OnlineProfilePictures.Remove(ProfilePictures[OnlineUsers.IndexOf(UserToDisconnect)]);
            OnlineUsers.Remove(UserToDisconnect);
            try
            {
                AntVaultServer.DisconnectClient(IpPort);
            }
            catch
            {
                AuxiliaryServerWorker.WriteError(UserToDisconnect + " was already disconnected");
            }
            Task.Run(() =>
            {
                foreach (Session Sess in Sessions)
                {
                    AntVaultServer.Send(Sess.IpPort, "/UserDisconnect -U " + UserToDisconnect + ";");
                }
            });
        }

        private static void HandleMessage(string MessageStringC, string IpPortC)
        {
            bool ValidSender = false;
            string Sender = null;
            string Message = AuxiliaryServerWorker.GetElement(MessageStringC, "-Content ", ";");
            foreach (Session Sess in Sessions)
            {
                if (Sess.IpPort == IpPortC)
                {
                    ValidSender = true;
                    Sender = Sess.Username;
                }
            }
            if (ValidSender == true)
            {
                AuxiliaryServerWorker.WriteInfo("[" + Sender + "]: " + Message);
                foreach (Session Sessn in Sessions)
                {
                    AntVaultServer.Send(Sessn.IpPort, "/Message -U " + Sender + " -Content " + Message + ";");
                }
            }
            else
            {
                AuxiliaryServerWorker.WriteError("Could not broadcast that message from " + IpPortC + " due to improper formatting");
            }
        }

        private static async Task DoAuthenticationAsync(string MessageString, string IpPortC)
        {
            string UsernameC = AuxiliaryServerWorker.GetElement(MessageString, "/Login -U ", " -P");
            string PasswordC = AuxiliaryServerWorker.GetElement(MessageString, "-P ", ";");
            if (Usernames.Contains(UsernameC) == true && Passwords[Usernames.IndexOf(UsernameC)] == PasswordC)
            {
                AuxiliaryServerWorker.WriteOK(IpPortC + " successfully authenticated as " + UsernameC);
                Session Sess = new Session()
                {
                    IpPort = IpPortC,
                    Username = UsernameC,
                    LoginTime = DateTime.Now,
                    ProfilePicture = ProfilePictures[Usernames.IndexOf(UsernameC)],
                    Friends = AuxiliaryServerWorker.GetFriendsListForUser(UsernameC),
                    Status = Statuses[Usernames.IndexOf(UsernameC)]
                };
                Sessions.Add(Sess);
                await Task.Run(() => AntVaultServer.Send(IpPortC,"/AcceptConnection"));
                AuxiliaryServerWorker.WriteInfo("Created new session for " + UsernameC);
                OnlineUsers.Add(Sess.Username);
                OnlineProfilePictures.Add(Sess.ProfilePicture);
                AuxiliaryServerWorker.WriteInfo("Sending personal profile data to " + UsernameC + "...");
                await Task.Run(() => AntVaultServer.Send(IpPortC, "/UserStringInfo -U " + UsernameC + " -S " + Sess.Status + ";"));
                await Task.Delay(50);
                await Task.Run(() => AntVaultServer.Send(IpPortC, "/UserProfilePictureMode"));
                await Task.Delay(50);
                await Task.Run(() => AntVaultServer.Send(IpPortC, AuxiliaryServerWorker.GetBytesFromBitmap(Sess.ProfilePicture)));
                await Task.Delay(50);
                //^Profile picture
                AuxiliaryServerWorker.WriteInfo("Sent " + UsernameC + " their profile picture");
                //vCollections
                await Task.Delay(50);
                await Task.Run(() => AntVaultServer.Send(IpPortC, "/UserFriendsListMode"));
                await Task.Delay(50);
                await Task.Run(() => AntVaultServer.Send(IpPortC, AuxiliaryServerWorker.GetBytesFromStringCollection(Sess.Friends)));
                await Task.Delay(50);
                AuxiliaryServerWorker.WriteInfo("Sent " + UsernameC + " their friends list");
                await Task.Delay(50);
                await Task.Run(() => AntVaultServer.Send(IpPortC, "/OnlineUsersListMode"));
                await Task.Delay(50);
                await Task.Run(() => AntVaultServer.Send(IpPortC, AuxiliaryServerWorker.GetBytesFromStringCollection(OnlineUsers)));
                AuxiliaryServerWorker.WriteInfo("Sent " + UsernameC + " list of current online users");
                await Task.Delay(50);
                await Task.Run(() => AntVaultServer.Send(IpPortC, "/OnlineProfilePicturesMode"));
                await Task.Delay(50);
                await Task.Run(() => AntVaultServer.Send(IpPortC, AuxiliaryServerWorker.GetBytesFromBitmapCollection(OnlineProfilePictures)));
                AuxiliaryServerWorker.WriteInfo("Sent " + UsernameC + " list of current online user profile pictures");
                await Task.Run(() => NewUserUpdatePulseAsync(UsernameC, Sess.Status, Sess.ProfilePicture));
                AuxiliaryServerWorker.WriteInfo("Sent new user update pulse to alll clients");
            }
            else
            {
                AuxiliaryServerWorker.WriteError(IpPortC + " tried to authenticate as " + UsernameC + " and failed");
                AntVaultServer.Send(IpPortC, "/DenyConnection");
            }
        }

        private static async Task NewUserUpdatePulseAsync(string Username, string Status, Bitmap ProfilePicture)
        {
            foreach (Session Sess in Sessions)
            {
                await Task.Run(() => AntVaultServer.Send(Sess.IpPort, "/NewUser -U " + Username + " -S " + Status + ";"));
                await Task.Run(() => AntVaultServer.Send(Sess.IpPort, AuxiliaryServerWorker.GetBytesFromBitmap(ProfilePicture)));
            }
        }

        internal static void StopServer()
        {
            try
            {
                AntVaultServer.Stop();
                AuxiliaryServerWorker.WriteOK("Server stopped successfully");
            }
            catch(Exception exc)
            {
                AuxiliaryServerWorker.WriteError("Server could not be stopped due to " + exc);
            }
        }

        internal static void UpdateStatus(string Text)
        {
            AuxiliaryServerWorker.WriteInfo("Entering status update mode");
            ServerStatus = Text;
            foreach(string Client in AntVaultServer.ListClients())
            {
                AntVaultServer.Send(Client, "/ServerStatus " + ServerStatus + ";");
                AuxiliaryServerWorker.WriteInfo("Sent status update pulse to " + Client);
            }
            AuxiliaryServerWorker.WriteInfo("Updating config file...");
            string NewConfig = "/IP " + AuxiliaryServerWorker.ReadFromConfig("IP") + "\\" + Environment.NewLine + "/Port " + AuxiliaryServerWorker.ReadFromConfig("Port") + "\\" + Environment.NewLine + "/Status " + ServerStatus + "\\";
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

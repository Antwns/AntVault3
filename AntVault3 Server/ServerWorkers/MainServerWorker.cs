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
using System.Windows.Documents;
using AntVault3_Common;
using System.Runtime.Serialization.Formatters.Binary;

namespace AntVault3_Server.ServerWorkers
{
    class MainServerWorker
    {
        internal static string ConfigDir = AppDomain.CurrentDomain.BaseDirectory + "AntVaultServer.config";
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
                try
                {
                    Program.AuxiliaryServerWorker.WriteOK("Validated " + Username + "'s page");
                    AVPage PageToSend = GetAVPageFromBytes(File.ReadAllBytes(UserDirectories + "\\" + Username + "\\" + Username + ".AVPage"));
                    Program.AuxiliaryServerWorker.WriteOK("Loaded " + Username + "'s page");
                    ServerNetworking.AntVaultServer.SendBytes(GetClientIDFromIpPort(Sessions.First(Sess => Sess.Username.Equals(Username)).IpPort), File.ReadAllBytes(UserDirectories + "\\" + Username + "\\" + Username + ".AVPage"));//Testing here
                    Program.AuxiliaryServerWorker.WriteOK("Sent " + Username + " their page");
                }
                catch (Exception exc)
                {
                    Program.AuxiliaryServerWorker.WriteError("Could not send " + Username + "'s page due to " + exc);
                }
            }
            else
            {
                Program.AuxiliaryServerWorker.WriteError("Could not find " + Username + "'s page, integrity check will now follow...");
                Thread PageCheckerThread = new Thread(CheckPages);
                PageCheckerThread.SetApartmentState(ApartmentState.STA);
                PageCheckerThread.Start();//mostly testing
            }
        }

        internal static void CheckServerLoginScreen()
        {
            if (File.Exists(ServerLoginScreen) == false)
            {
                Program.AuxiliaryServerWorker.WriteError("Server login screen .gif file was not found. Generating new default one...");
                try
                {
                    MemoryStream ServerLoginScreenStream = new MemoryStream(Program.AuxiliaryServerWorker.GetBytesFromBitmap(Properties.Resources.LoginMenuBg));
                    Properties.Resources.LoginMenuBg.Save(ServerLoginScreen, ImageFormat.Gif);
                    Program.AuxiliaryServerWorker.WriteOK("Successfully generated default server login screen");
                }
                catch (Exception exc)
                {
                    Program.AuxiliaryServerWorker.WriteError("Could not generate default server login screen due to " + exc);
                }
            }
            else
            {
                Program.AuxiliaryServerWorker.WriteOK("Loaded server login screen successfully");
            }
        }

        internal static void CheckPages()
        {
            foreach (string User in Usernames)
            {
                if (File.Exists(UserDirectories + "\\" + User + "\\" + User + ".AVPage") == false)
                {
                    Program.AuxiliaryServerWorker.WriteError("Couldn't find page for user " + User);
                    FlowDocument DefaultFlowDocument = new FlowDocument();
                    Paragraph DefaultParagraph = new Paragraph(new Run("Welcome to my page!" + Environment.NewLine + "This is a rich textbox! It can contain images and text! Means you can format it as you like!"));
                    System.Windows.Controls.Image SampleImage = new System.Windows.Controls.Image();
                    SampleImage.Source = Program.AuxiliaryServerWorker.GetBitmapImageFromBitmap(Properties.Resources.DefaultProfilePicture);
                    SampleImage.Height = 50;
                    SampleImage.Width = 50;
                    DefaultParagraph.Inlines.Add(SampleImage);
                    DefaultFlowDocument.Blocks.Add(DefaultParagraph);
                    TextRange DefaultTextRange = new TextRange(DefaultFlowDocument.ContentStart, DefaultFlowDocument.ContentEnd);
                    using (MemoryStream DefaultTextMemoryStream = new MemoryStream())
                    {
                        DefaultTextRange.Save(DefaultTextMemoryStream, DataFormats.Rtf);
                        AVPage NewUserPage = new AVPage()
                        {
                            Banner = Properties.Resources.DefaultCover,
                            Content = DefaultTextMemoryStream.ToArray()
                        };
                        File.WriteAllBytes(UserDirectories + "\\" + User + "\\" + User + ".AVPage", GetBytesFromAVPage(NewUserPage));
                    }
                    Program.AuxiliaryServerWorker.WriteOK("Successfully generated default page for " + User);
                }
                else
                {
                    Program.AuxiliaryServerWorker.WriteInfo("Page for " + User + " already exists and will be validated");
                }
            }
        }

        internal static void CheckServerTheme()
        {
            if(File.Exists(ServerTheme) == false)
            {
                Program.AuxiliaryServerWorker.WriteError("Server theme .wav file was not found. Generating new default one...");
                try
                {
                    MemoryStream ServerThemeStream = new MemoryStream(Convert.ToInt32(Properties.Resources.ServerTheme.Length));
                    Properties.Resources.ServerTheme.CopyTo(ServerThemeStream);
                    File.WriteAllBytes(ServerTheme, ServerThemeStream.ToArray());
                    Program.AuxiliaryServerWorker.WriteOK("Successfully generated default server theme");
                }
                catch (Exception exc)
                {
                    Program.AuxiliaryServerWorker.WriteError("Could not generate default server theme due to " + exc);
                }
            }
            else
            {
                Program.AuxiliaryServerWorker.WriteOK("Found server theme successfully");
            }
        }

        internal static void CheckDatabase()
        {
            bool ErrorsFound = false;
            string CurrentLine;
            int LineNumber = 0;
            Program.AuxiliaryServerWorker.WriteInfo("Checking database...");
            if(File.Exists(DatabaseDir) == false)
            {
                Program.AuxiliaryServerWorker.WriteError("Could not find database file. Creating a new default database...");
                try
                {
                    File.WriteAllText(DatabaseDir, Properties.Resources.DefaultDatabase);
                    Program.AuxiliaryServerWorker.WriteOK("New default database successfully created.");
                }
                catch(Exception exc)
                {
                    Program.AuxiliaryServerWorker.WriteError("Could not create new default database due to " + exc);
                }
            }
            StreamReader DatabaseStreamReader = new StreamReader(DatabaseDir);
            while ((CurrentLine = DatabaseStreamReader.ReadLine()) != null)
            {
                if(CurrentLine.StartsWith("/U ") && CurrentLine.EndsWith("."))
                {
                    Usernames.Add(Program.AuxiliaryServerWorker.GetElement(CurrentLine, "/U ", " /P"));
                    Passwords.Add(Program.AuxiliaryServerWorker.GetElement(CurrentLine, "/P ", " /S"));
                    Statuses.Add(Program.AuxiliaryServerWorker.GetElement(CurrentLine, "/S ", "."));
                    try
                    {
                        ProfilePictures.Add(Program.AuxiliaryServerWorker.GetBitmapFromBytes(File.ReadAllBytes(UserDirectories + "\\" + Usernames[LineNumber] + "\\ProfilePicture_" + Usernames[LineNumber] + ".png")));
                    }
                    catch
                    {
                        Program.AuxiliaryServerWorker.WriteError("Profile picture not found for user " + Usernames[LineNumber] + " generating default profile picture instead...");
                        Directory.CreateDirectory(UserDirectories + "\\" + Usernames[LineNumber]);
                        try
                        {
                            File.WriteAllBytes(UserDirectories + "\\" + Usernames[LineNumber] + "\\ProfilePicture_" + Usernames[LineNumber] + ".png", Program.AuxiliaryServerWorker.GetBytesFromBitmap(Properties.Resources.DefaultProfilePicture));
                            Program.AuxiliaryServerWorker.WriteOK("Default profile picture created successfully for " + Usernames[LineNumber]);
                            ProfilePictures.Add(Program.AuxiliaryServerWorker.GetBitmapFromBytes(File.ReadAllBytes(UserDirectories + "\\" + Usernames[LineNumber] + "\\ProfilePicture_" + Usernames[LineNumber] + ".png")));
                            Program.AuxiliaryServerWorker.WriteOK("Successfully appended default profile picture for user " + Usernames[LineNumber]);
                        }
                        catch (Exception exc)
                        {
                            ErrorsFound = true;
                            Program.AuxiliaryServerWorker.WriteError("Could not create default profile picture for " + Usernames[LineNumber] + " due to " + exc);
                        }
                    }
                }
                if(ErrorsFound == false)
                {
                    Program.AuxiliaryServerWorker.WriteOK("Successfully loaded " + Usernames[LineNumber] + "'s user info");
                }
                LineNumber++;
            }
            Program.AuxiliaryServerWorker.WriteOK("Successfully loaded " + LineNumber + " accounts");
            if(ErrorsFound == true)
            {
                Program.AuxiliaryServerWorker.WriteInfo("One or more errors were met during the load process, please theck the integrity of the database or delete it to generate a new one with default values");
            }
        }

        internal static async Task SendNewProfilePicturePulseAsync(string UserToUpdate, byte[] Data)
        {
            foreach (Session Sess in Sessions)
            {
                await ServerNetworking.AntVaultServer.SendMessageAsync(GetClientIDFromIpPort(Sess.IpPort), "/NewProfilePicture -U " + UserToUpdate + ";");
                await Task.Delay(10);
                await ServerNetworking.AntVaultServer.SendBytesAsync(GetClientIDFromIpPort(Sess.IpPort), Data);
                Program.AuxiliaryServerWorker.WriteOK("Sent profile picture update pulse to " + UserToUpdate);
            }
            Program.AuxiliaryServerWorker.WriteOK("Finished sending profile picture update pulses to " + Sessions.Count + " active clients");
        }

        internal static void UpdateProfilePicture(IClientInfo Client, byte[] Data)
        {
            string UserToUpdate = Sessions.First(S => S.IpPort.Equals(Client.RemoteIPv4)).Username;
            string FileToUpdate = UserDirectories + "\\" + UserToUpdate + "\\" + "ProfilePicture_" + UserToUpdate + ".png";
            Program.AuxiliaryServerWorker.WriteInfo("Received profile picture update pulse from " + UserToUpdate);
            Bitmap ProfilePictureToUpdate = Program.AuxiliaryServerWorker.GetBitmapFromBytes(Data);
            ProfilePictures[Usernames.IndexOf(UserToUpdate)] = ProfilePictureToUpdate;
            if(File.Exists(FileToUpdate))
            {
                File.Delete(FileToUpdate);
            }
            ProfilePictureToUpdate.Save(FileToUpdate, ImageFormat.Png);
            Program.AuxiliaryServerWorker.WriteOK("Updated local profile picture for " + UserToUpdate);
            Task.Run(() => SendNewProfilePicturePulseAsync(UserToUpdate, Data));
        }

        internal static void AboutUser(string UserToCheck)
        {
            Program.AuxiliaryServerWorker.WriteInfo("User's information: " + Environment.NewLine + "IpPort: " + Sessions.First(Session => Session.Username.Equals(UserToCheck)).IpPort + Environment.NewLine + "Login time: " + Sessions.First(Session => Session.Username.Equals(UserToCheck)).LoginTime + Environment.NewLine + "ID: " + GetClientIDFromIpPort(Sessions.First(Session => Session.Username.Equals(UserToCheck)).IpPort) + Environment.NewLine + "Status: " + Sessions.First(Session => Session.Username.Equals(UserToCheck)).Status);
        }

        internal static void UpdateLoginScreen(IClientInfo Client)
        {
            Program.AuxiliaryServerWorker.WriteInfo(Client.RemoteIPv4 + " requested the current server login screen");
            byte[] ServerLoginScreenBytes = File.ReadAllBytes(ServerLoginScreen);
            MemoryStream DefaultServerLoginScreenStream = new MemoryStream();
            Properties.Resources.LoginMenuBg.Save(DefaultServerLoginScreenStream, ImageFormat.Gif);
            if(ServerLoginScreenBytes.Length == DefaultServerLoginScreenStream.ToArray().Length)
            {
                Program.AuxiliaryServerWorker.WriteInfo("Default server login screen detected");
                ServerNetworking.AntVaultServer.SendMessage(Client.Id, "/DefaultLoginScreen");
                Program.AuxiliaryServerWorker.WriteOK("No login screen set");
            }
            else
            {
                Program.AuxiliaryServerWorker.WriteInfo("Custom login screen detected");
                if(Sessions.Any(Sess => Sess.IpPort.Equals(Client.RemoteIPv4)) == false)
                {
                    Task.Run(()=> ServerNetworking.AntVaultServer.SendMessage(Client.Id, "/NewLoginScreen"));
                    MemoryStream NewServerLoginScreenStream = new MemoryStream(File.ReadAllBytes(ServerLoginScreen));
                    ServerNetworking.AntVaultServer.SendBytes(Client.Id, NewServerLoginScreenStream.ToArray(), true);
                    Program.AuxiliaryServerWorker.WriteOK("Custom login screen sent to " + Client.RemoteIPv4);
                }
            }
        }

        internal static async Task UpdateThemeAsync(IClientInfo Client)
        {
            Program.AuxiliaryServerWorker.WriteInfo(Client.RemoteIPv4 + " requested the current server theme");
            byte[] ServerThemeBytes = File.ReadAllBytes(ServerTheme);
            MemoryStream DefaultServerThemeStream = new MemoryStream(Convert.ToInt32(Properties.Resources.ServerTheme.Length));
            Properties.Resources.ServerTheme.CopyTo(DefaultServerThemeStream);
            if(ServerThemeBytes.Length == DefaultServerThemeStream.ToArray().Length)
            {
                Program.AuxiliaryServerWorker.WriteInfo("Default theme detected");
                //Send msg that the client should use the default theme
                ServerNetworking.AntVaultServer.SendMessage(Client.Id, "/DefaultTheme");
                Program.AuxiliaryServerWorker.WriteOK("No theme sent");
            }
            else
            {
                Program.AuxiliaryServerWorker.WriteInfo("Custom theme detected");
                //Send msg that the client shuold enter UpdateThemeMode and then send the new theme over a stream
                if (Sessions.Any(Sess => Sess.IpPort.Equals(Client.RemoteIPv4)) == false)
                {
                    await Task.Run(() => ServerNetworking.AntVaultServer.SendMessage(Client.Id, "/NewTheme"));
                    MemoryStream NewServerThemeStream = new MemoryStream(File.ReadAllBytes(ServerTheme));
                    await Task.Delay(50);
                    await Task.Run(() => ServerNetworking.AntVaultServer.SendBytesAsync(Client.Id, NewServerThemeStream.ToArray()));
                    Program.AuxiliaryServerWorker.WriteOK("Custom theme sent to " + Client.RemoteIPv4);
                }
            }
        }

        internal static void HandleDisconnect(string MessageString, IClientInfo Client)
        {
            string Reason = Program.AuxiliaryServerWorker.GetElement(MessageString, "-Content ", ";");
            string UserToDisconnect = Sessions.First(Sess => Sess.IpPort.Equals(Client.RemoteIPv4)).Username;
            Program.AuxiliaryServerWorker.WriteInfo(UserToDisconnect + " disconnected due to " + Reason );
            OnlineProfilePictures.Remove(OnlineProfilePictures[OnlineUsers.IndexOf(UserToDisconnect)]);
            OnlineUsers.Remove(UserToDisconnect);
            Sessions.Remove(Sessions.First(S => S.Username.Equals(UserToDisconnect)));
            try
            {
                //ServerNetworking.AntVaultServer.DisconnectClient(IpPort);
            }
            catch
            {
                Program.AuxiliaryServerWorker.WriteError(UserToDisconnect + " was already disconnected");
            }
            Task.Run(() =>
            {
                foreach (Session Sess in Sessions)
                {
                    ServerNetworking.AntVaultServer.SendMessage(GetClientIDFromIpPort(Sess.IpPort), "/UserDisconnect -U " + UserToDisconnect + ";");
                }
            });
        }

        internal static void HandleMessage(string MessageStringC, IClientInfo Client)
        {
            bool ValidSender = false;
            string Sender = null;
            string Message = Program.AuxiliaryServerWorker.GetElement(MessageStringC, "-Content ", ";");
            if (Sessions.Any(S => S.IpPort.Equals(Client.RemoteIPv4)))
            {
                ValidSender = true;
                Sender = Sessions.First(Session => Session.IpPort.Equals(Client.RemoteIPv4)).Username;
            }
            if (ValidSender == true)
            {
                Program.AuxiliaryServerWorker.WriteInfo("[" + Sender + "]: " + Message);
                foreach (IClientInfo Cl in ServerNetworking.AntVaultServer.GetConnectedClients().Values)
                {
                    ServerNetworking.AntVaultServer.SendMessage(Cl.Id, "/Message -U " + Sender + " -Content " + Message + ";");
                }
            }
            else
            {
                Program.AuxiliaryServerWorker.WriteError("Could not broadcast that message from " + Client.RemoteIPv4 + " due to improper formatting");
            }
        }

        internal static async Task DoAuthenticationAsync(string MessageString, IClientInfo Client)
        {
            string UsernameC = Program.AuxiliaryServerWorker.GetElement(MessageString, "/Login -U ", " -P");
            string PasswordC = Program.AuxiliaryServerWorker.GetElement(MessageString, "-P ", ";");
            if (Usernames.Contains(UsernameC) == true && Passwords[Usernames.IndexOf(UsernameC)] == PasswordC)
            {
                Program.AuxiliaryServerWorker.WriteOK(Client.RemoteIPv4 + " successfully authenticated as " + UsernameC);
                Session Sess = new Session()
                {
                    IpPort = Client.RemoteIPv4,
                    Username = UsernameC,
                    LoginTime = DateTime.Now,
                    ProfilePicture = ProfilePictures[Usernames.IndexOf(UsernameC)],
                    Friends = GetFriendsListForUser(UsernameC),
                    Status = Statuses[Usernames.IndexOf(UsernameC)]
                };
                Sessions.Add(Sess);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendMessage(Client.Id,"/AcceptConnection"));
                Program.AuxiliaryServerWorker.WriteInfo("Created new session for " + UsernameC);
                OnlineUsers.Add(Sess.Username);
                OnlineProfilePictures.Add(Sess.ProfilePicture);
                Program.AuxiliaryServerWorker.WriteInfo("Sending personal profile data to " + UsernameC + "...");
                await Task.Run(() => ServerNetworking.AntVaultServer.SendMessageAsync(Client.Id, "/UserStringInfo -U " + UsernameC + " -S " + Sess.Status + ";"));
                await Task.Delay(50);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendMessageAsync(Client.Id, "/UserProfilePictureMode"));
                await Task.Delay(50);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendBytesAsync(Client.Id, Program.AuxiliaryServerWorker.GetBytesFromBitmap(Sess.ProfilePicture)));
                await Task.Delay(50);
                //^Profile picture
                Program.AuxiliaryServerWorker.WriteInfo("Sent " + UsernameC + " their profile picture");
                //vCollections
                await Task.Delay(50);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendMessageAsync(Client.Id, "/UserFriendsListMode"));
                await Task.Delay(50);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendBytesAsync(Client.Id, Program.AuxiliaryServerWorker.GetBytesFromStringCollection(Sess.Friends)));
                Program.AuxiliaryServerWorker.WriteInfo("Sent " + UsernameC + " their friends list");
                await Task.Delay(100);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendMessageAsync(Client.Id, "/OnlineUsersListMode"));
                await Task.Delay(50);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendBytesAsync(Client.Id, Program.AuxiliaryServerWorker.GetBytesFromStringCollection(OnlineUsers)));
                Program.AuxiliaryServerWorker.WriteInfo("Sent " + UsernameC + " list of current online users");
                await Task.Delay(100);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendMessageAsync(Client.Id, "/OnlineProfilePicturesMode"));
                await Task.Delay(50);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendBytesAsync(Client.Id, Program.AuxiliaryServerWorker.GetBytesFromBitmapCollection(OnlineProfilePictures)));
                Program.AuxiliaryServerWorker.WriteInfo("Sent " + UsernameC + " list of current online user profile pictures");
                await Task.Delay(100);
                await Task.Run(() => NewUserUpdatePulseAsync(UsernameC, Sess.Status, Sess.ProfilePicture));
                Program.AuxiliaryServerWorker.WriteInfo("Sent new user update pulse to alll clients");
                await Task.Run(() => ServerNetworking.AntVaultServer.SendMessageAsync(Client.Id, "/YourPage"));
                await Task.Delay(100);
                await Task.Run(() => SendPageForUser(Client));
            }
            else
            {
                Program.AuxiliaryServerWorker.WriteError(Client.RemoteIPv4 + " tried to authenticate as " + UsernameC + " and failed");
                ServerNetworking.AntVaultServer.SendMessage(Client.Id, "/DenyConnection");
            }
        }

        private static async Task NewUserUpdatePulseAsync(string Username, string Status, Bitmap ProfilePicture)
        {
            foreach (Session Sess in Sessions)
            {
                await Task.Run(() => ServerNetworking.AntVaultServer.SendMessageAsync(GetClientIDFromIpPort(Sess.IpPort), "/NewUser -U " + Username + " -S " + Status + ";"));
                await Task.Delay(50);
                await Task.Run(() => ServerNetworking.AntVaultServer.SendBytesAsync(GetClientIDFromIpPort(Sess.IpPort), Program.AuxiliaryServerWorker.GetBytesFromBitmap(ProfilePicture)));
            }
        }

        internal static void UpdateStatus(string Text)
        {
            Program.AuxiliaryServerWorker.WriteInfo("Entering status update mode");
            ServerNetworking.ServerStatus = Text;
            foreach(IClientInfo Client in ServerNetworking.AntVaultServer.GetConnectedClients().Values)
            {
                ServerNetworking.AntVaultServer.SendMessage(Client.Id, "/ServerStatus " + ServerNetworking.ServerStatus + ";");
                Program.AuxiliaryServerWorker.WriteInfo("Sent status update pulse to " + Client.RemoteIPv4);
            }
            Program.AuxiliaryServerWorker.WriteInfo("Updating config file...");
            string NewConfig = "/IP " + Program.AuxiliaryServerWorker.ReadFromConfig("IP", ConfigDir) + "\\" + Environment.NewLine + "/Port " + Program.AuxiliaryServerWorker.ReadFromConfig("Port", ConfigDir) + "\\" + Environment.NewLine + "/Status " + ServerNetworking.ServerStatus + "\\";
            try
            {
                File.WriteAllText(Program.AuxiliaryServerWorker.ConfigDir, NewConfig);
                Program.AuxiliaryServerWorker.WriteOK("Updated config file successfully");
            }
            catch(Exception exc)
            {
                Program.AuxiliaryServerWorker.WriteError("Could not update config file due to " + exc);
            }
            Program.AuxiliaryServerWorker.WriteInfo("Exiting status update mode");
        }

        internal static AVPage GetAVPageFromBytes(byte[] BytesToConvert)
        {
            BinaryFormatter ClassFormatter = new BinaryFormatter();
            using (MemoryStream StreamConverter = new MemoryStream(BytesToConvert))
            {
                AVPage PageToReturn = (AVPage)ClassFormatter.Deserialize(StreamConverter);
                return PageToReturn;
            }
        }

        internal static Collection<string> GetFriendsListForUser(string Username)
        {
            string CurrentUserFriendsListDirectory = MainServerWorker.UserDirectories + "\\" + Username + "\\FriendsList_" + Username + ".Friends";
            Program.AuxiliaryServerWorker.WriteInfo("Loading friends list for " + Username);
            if (File.Exists(CurrentUserFriendsListDirectory) == false)
            {
                Program.AuxiliaryServerWorker.WriteError("Could not locate friends list file for " + Username + ", creating new default friends list file now...");
                try
                {
                    Collection<string> CollectionToAdd = new Collection<string>();
                    CollectionToAdd.Add("User");
                    File.WriteAllBytes(CurrentUserFriendsListDirectory, Program.AuxiliaryServerWorker.GetBytesFromStringCollection(CollectionToAdd));
                    Program.AuxiliaryServerWorker.WriteInfo("Created new default friends list for " + Username);
                }
                catch (Exception exc)
                {
                    Program.AuxiliaryServerWorker.WriteError("Could not create default friends list file for " + Username + " due to " + exc);
                }
            }
            Collection<string> FriendsListToReturn = Program.AuxiliaryServerWorker.GetStringCollectionFromBytes(File.ReadAllBytes(CurrentUserFriendsListDirectory));
            return FriendsListToReturn;
        }

        internal static byte[] GetBytesFromAVPage(AVPage PageToConvert)
        {
            BinaryFormatter AVPageFormatter = new BinaryFormatter();
            using (MemoryStream CollectionStream = new MemoryStream())
            {
                AVPageFormatter.Serialize(CollectionStream, PageToConvert);
                byte[] PageArray = CollectionStream.ToArray();
                CollectionStream.Dispose();
                return PageArray;
            }
        }

        internal static int GetClientIDFromIpPort(string IpPort)
        {
            int ID = ServerNetworking.AntVaultServer.GetConnectedClients().First(Client => Client.Value.RemoteIPv4.Equals(IpPort)).Value.Id;
            return ID;
        }
    }
}

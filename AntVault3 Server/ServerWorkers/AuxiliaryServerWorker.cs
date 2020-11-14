using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Media;
using AntVault3_Common;

namespace AntVault3_Server.ServerWorkers
{
    //Merge with main server worker?
    class AuxiliaryServerWorker
    {
        internal static string ConfigDir = AppDomain.CurrentDomain.BaseDirectory + "AntVaultServer.config";

        #region Console entries
        internal static void WriteInfo(string Text)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[INFO] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Text);
        }

        internal static void WriteError(string Text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[ERROR] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Text);
        }

        internal static void WriteDebug(string Text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("[DEBUG] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Text);
        }

        internal static void WriteOK(string Text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[OK] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Text);
        }
        #endregion

        #region Config and database entries
        internal static string ReadFromConfig(string Property)
        {
            CheckConfig();
            string Config = File.ReadAllText(ConfigDir);
            if (Property == "IP")
            {
                string StringToReturn = GetElement(Config, "/IP ", "\\");
                return StringToReturn;
            }
            else if (Property == "Port")
            {
                string StringToReturn = GetElement(Config, "/Port ", "\\");
                return StringToReturn;
            }
            else if (Property == "Status")
            {
                string StringToReturn = GetElement(Config, "/Status ", "\\");
                return StringToReturn;
            }
            else
            {
                return null;
            }
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
        #endregion

        #region Data handling and integrity checks

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

        internal static void CheckConfig()
        {
            WriteInfo("Checking config file...");
            if(File.Exists(ConfigDir) == false)
            {
                File.WriteAllText(ConfigDir, Properties.Resources.DefaultConfig);
                WriteError("Config file was not found, default config file was created");
            }
            else
            {
                WriteOK("Config file found.");
            }
        }

        internal static string GetStringFromBytes(byte[] BytesToConvert)
        {
            string StringToReturn = Encoding.UTF8.GetString(BytesToConvert);
            return StringToReturn;
        }
        internal static string GetElement(string SourceString, string Start, string End)
        {
            if (SourceString.Contains(Start) && SourceString.Contains(End))
            {
                int StartPos, EndPos;
                StartPos = SourceString.IndexOf(Start, 0) + Start.Length;
                EndPos = SourceString.IndexOf(End, StartPos);
                return SourceString.Substring(StartPos, EndPos - StartPos);
            }
            return "";
        }
        internal static Bitmap GetBitmapFromBytes(byte[] Data)
        {
            MemoryStream ImageStreamConverter = new MemoryStream(Data);
            Image ImageToSave = Image.FromStream(ImageStreamConverter);
            Bitmap BitmapToReturn = new Bitmap(ImageToSave);
            return BitmapToReturn;
        }

        internal static ImageSource GetBitmapImageFromBitmap(Bitmap InputBitmap)
        {
            MemoryStream BitMapConverterStream = new MemoryStream();
            InputBitmap.Save(BitMapConverterStream, ImageFormat.Png);
            System.Windows.Media.Imaging.BitmapImage ConvertedBitmapImage = new System.Windows.Media.Imaging.BitmapImage();
            ConvertedBitmapImage.BeginInit();
            ConvertedBitmapImage.StreamSource = BitMapConverterStream;
            ConvertedBitmapImage.EndInit();
            return ConvertedBitmapImage;
        }

        internal static byte[] GetBytesFromBitmap(Bitmap Data)
        {
            {
                byte[] BytesToreturn = null;
                MemoryStream BitmapConverterStream = new MemoryStream();
                Data.Save(BitmapConverterStream, ImageFormat.Png);
                BytesToreturn = BitmapConverterStream.ToArray();
                return BytesToreturn;
            }
        }

        internal static Collection<string> GetFriendsListForUser(string Username)
        {
            string CurrentUserFriendsListDirectory = MainServerWorker.UserDirectories + "\\" + Username + "\\FriendsList_" + Username + ".Friends";
            WriteInfo("Loading friends list for " + Username);
            if(File.Exists(CurrentUserFriendsListDirectory) == false)
            {
                WriteError("Could not locate friends list file for " + Username + ", creating new default friends list file now...");
                try
                {
                    Collection<string> CollectionToAdd = new Collection<string>();
                    CollectionToAdd.Add("User");
                    File.WriteAllBytes(CurrentUserFriendsListDirectory, GetBytesFromStringCollection(CollectionToAdd));
                    WriteInfo("Created new default friends list for " + Username);
                }
                catch(Exception exc)
                {
                    WriteError("Could not create default friends list file for " + Username + " due to " + exc);
                }
            }
            Collection<string> FriendsListToReturn = GetStringCollectionFromBytes(File.ReadAllBytes(CurrentUserFriendsListDirectory));
            return FriendsListToReturn;
        }

        internal static Collection<string> GetStringCollectionFromBytes(byte[] BytesToConvert)
        {
            BinaryFormatter CollectionFormatter = new BinaryFormatter();
            using (MemoryStream StreamConverter = new MemoryStream(BytesToConvert))
            {
                Collection<string> CollectionToReturn = (Collection<string>)CollectionFormatter.Deserialize(StreamConverter);
                return CollectionToReturn;
            }
        }

        internal static byte[] GetBytesFromStringCollection(Collection<string> CollectionToConvert)
        {
            BinaryFormatter CollectionFormatter = new BinaryFormatter();
            using (MemoryStream CollectionStream = new MemoryStream())
            {
                CollectionFormatter.Serialize(CollectionStream, CollectionToConvert);
                byte[] CollectionArray = CollectionStream.ToArray();
                CollectionStream.Dispose();
                return CollectionArray;
            }
        }

        internal static byte[] GetBytesFromBitmapCollection(Collection<Bitmap> CollectionToConvert)
        {
            BinaryFormatter CollectionFormatter = new BinaryFormatter();
            using (MemoryStream CollectionStream = new MemoryStream())
            {
                CollectionFormatter.Serialize(CollectionStream, CollectionToConvert);
                byte[] CollectionArray = CollectionStream.ToArray();
                CollectionStream.Dispose();
                return CollectionArray;
            }
        }

        internal static int GetClientIDFromIpPort(string IpPort)
        {
            int ID = ServerNetworking.AntVaultServer.GetConnectedClients().First(Client => Client.Value.RemoteIPv4.Equals(IpPort)).Value.Id;
            return ID;
        }
        #endregion
    }
}

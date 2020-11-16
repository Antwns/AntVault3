using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows;
using AntVault3_Common;
using System.Windows.Documents;
using System.Windows.Media;

namespace AntVault3_Client.ClientWorkers
{
    public class AuxiliaryClientWorker
    {
        //Merge with main client worker?
        internal static string ConfigDir = AppDomain.CurrentDomain.BaseDirectory + "AntVaultClient.config";

        internal static string ReadFromConfig(string Property)
        {
            CheckConfig();
            string Config = File.ReadAllText(ConfigDir);
            if(Property == "IP")
            {
                string StringToReturn = GetElement(Config, "/IP ", "\\");
                return StringToReturn;
            }
            else if(Property == "Port")
            {
                string StringToReturn = GetElement(Config, "/Port", "\\");
                return StringToReturn;
            }
            else
            {
                return null;
            }
        }

        internal static void CheckConfig()
        {
            if (File.Exists(ConfigDir) == false)
            {
                File.WriteAllText(ConfigDir, Properties.Resources.DefaultConfig);
            }
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

        internal static string GetStringFromBytes(byte[] BytesToConvert)
        {
            string StringToReturn = Encoding.UTF8.GetString(BytesToConvert);
            return StringToReturn;
        }

        internal static Bitmap GetBitmapFromBytes(byte[] BytesToConvert)
        {
            using(MemoryStream ImageConverterStream = new MemoryStream(BytesToConvert))
            {
                Bitmap BitmapToReturn = (Bitmap)Bitmap.FromStream(ImageConverterStream);
                return BitmapToReturn;
            }
        }

        internal static byte[] GetBytesFromBitmap(Bitmap BitmapToConvert)
        {
            using(MemoryStream ImageConverterStream = new MemoryStream())
            {
                BitmapToConvert.Save(ImageConverterStream, ImageFormat.Png);
                byte[] BytesToReturn = ImageConverterStream.ToArray();
                return BytesToReturn;
            }
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

        internal static Collection<Bitmap> GetBitmapCollectionFromBytes(byte[] BytesToConvert)
        {
            BinaryFormatter CollectionFormatter = new BinaryFormatter();
            using (MemoryStream StreamConverter = new MemoryStream(BytesToConvert))
            {
                Collection<Bitmap> CollectionToReturn = (Collection<Bitmap>)CollectionFormatter.Deserialize(StreamConverter);
                return CollectionToReturn;
            }
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

        internal static byte[] GetBytesFromString(string DataString)
        {
            byte[] BytesToReturn = Encoding.ASCII.GetBytes(DataString);
            return BytesToReturn;
        }

        internal static bool CheckIfImageIsPng(string FileDir)
        {
            byte[] BytesToCheck = File.ReadAllBytes(FileDir);
            if(Encoding.ASCII.GetString(BytesToCheck).Contains("�PNG"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static AVPage GetAVPageFromBytes(byte[] BytesToConvert)
        {
            using (MemoryStream StreamConverter = new MemoryStream(BytesToConvert))
            {
                BinaryFormatter ClassFormatter = new BinaryFormatter();
                StreamConverter.Position = 0;
                object ReceivedObject = ClassFormatter.Deserialize(StreamConverter);
                AVPage PageToReturn = (AVPage)ReceivedObject;
                return PageToReturn;
            }
        }
    }
}

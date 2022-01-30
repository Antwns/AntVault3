using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using SimpleSockets;
using SimpleSockets.Messaging.Metadata;
using SimpleSockets.Server;

namespace AntVault3_Server.ServerWorkers
{
    class ServerNetworking
    {
        internal static SimpleSocketListener AntVaultServer = new SimpleSocketTcpListener();
        
        static bool SetUpEvents = false;
        static bool NewProfilePictureMode;

        internal static string ServerStatus = null;

        internal static async Task StartServer()
        {
            if (SetUpEvents == false)
            {
                AntVaultServer.MessageReceived += MessageReceived;
                AntVaultServer.BytesReceived += BytesReceived;
                AntVaultServer.ClientDisconnected += AntVaultServer_ClientDisconnected;
                AntVaultServer.ObjectReceived += AntVaultServer_ObjectReceived;
                SetUpEvents = true;
                Program.AuxiliaryServerWorker.WriteOK("Event callbacks hooked successfully");
            }
            Program.AuxiliaryServerWorker.WriteInfo("Reading server status from config...");
            ServerStatus = Program.AuxiliaryServerWorker.ReadFromConfig("Status", MainServerWorker.ConfigDir);
            MainServerWorker.CheckDatabase();
            Program.AuxiliaryServerWorker.WriteOK("Server status is set to " + ServerStatus);
            Program.AuxiliaryServerWorker.WriteInfo("Checking server theme...");
            MainServerWorker.CheckServerTheme();
            Program.AuxiliaryServerWorker.WriteInfo("Checking server login screen...");
            MainServerWorker.CheckServerLoginScreen();
            Thread PageCheckerThread = new Thread(MainServerWorker.CheckPages);
            PageCheckerThread.SetApartmentState(ApartmentState.STA);
            await Task.Run(() => PageCheckerThread.Start());
            try
            {
                AntVaultServer.StartListening(Program.AuxiliaryServerWorker.ReadFromConfig("IP", MainServerWorker.ConfigDir), Convert.ToInt32(Program.AuxiliaryServerWorker.ReadFromConfig("Port", MainServerWorker.ConfigDir)));
                Program.AuxiliaryServerWorker.WriteOK("Server started successfully on " + Program.AuxiliaryServerWorker.ReadFromConfig("IP", MainServerWorker.ConfigDir) + ":" + Program.AuxiliaryServerWorker.ReadFromConfig("Port", MainServerWorker.ConfigDir));
            }
            catch (Exception exc)
            {
                Program.AuxiliaryServerWorker.WriteError("Server could not be started due to " + exc);
            }
        }

        private static void AntVaultServer_ObjectReceived(IClientInfo client, object obj, Type type)
        {

        }

        private static void AntVaultServer_ClientDisconnected(IClientInfo Client, DisconnectReason Reason)
        {
            if (MainServerWorker.Sessions.Any(Session => Session.IpPort.Equals(Client.LocalIPv4)))
            {
                Program.AuxiliaryServerWorker.WriteInfo("Client " + MainServerWorker.Sessions.First(Session => Session.IpPort.Equals(Client.RemoteIPv4)) + " with IP " + Client.RemoteIPv4 + " and ID " + Client.Id + " disconnected due to " + Reason.ToString());
            }
        }

        private static void BytesReceived(IClientInfo Client, byte[] MessageBytes)
        {
            string MessageString = Program.AuxiliaryServerWorker.GetStringFromBytes(MessageBytes);
            if (MessageString.Contains("�PNG") == false)
            {
                Program.AuxiliaryServerWorker.WriteDebug(MessageString);
            }
            else
            {
                Program.AuxiliaryServerWorker.WriteDebug("[PNG]");
            }
            if(NewProfilePictureMode == true)
            {
                NewProfilePictureMode = false;
                Task.Run(() => MainServerWorker.UpdateProfilePicture(Client, MessageBytes));
            }
        }

        private static void MessageReceived(IClientInfo Client, string MessageString)
        {
            if (MessageString.StartsWith("/ServerStatus?"))
            {
                Task.Run(() => MainServerWorker.UpdateStatus(ServerStatus));
            }
            if (MessageString.StartsWith("/ServerTheme?"))
            {
                Task.Run(() => MainServerWorker.UpdateThemeAsync(Client));
            }
            if (MessageString.StartsWith("/Login"))
            {
                Task.Run(() => MainServerWorker.DoAuthenticationAsync(MessageString, Client));
            }
            if (MessageString.StartsWith("/Message"))
            {
                Task.Run(() => MainServerWorker.HandleMessage(MessageString, Client));
            }
            if (MessageString.StartsWith("/Disconnect"))
            {
                Task.Run(() => MainServerWorker.HandleDisconnect(MessageString, Client));
            }
            if (MessageString.StartsWith("/ServerLoginScreen?"))
            {
                Task.Run(() => MainServerWorker.UpdateLoginScreen(Client));
            }
            if (MessageString.StartsWith("/NewProfilePicture"))
            {
                NewProfilePictureMode = true;
            }
        }

            internal static void StopServer()
        {
            try
            {
                AntVaultServer.Dispose();
                Program.AuxiliaryServerWorker.WriteOK("Server stopped successfully");
            }
            catch (Exception exc)
            {
                Program.AuxiliaryServerWorker.WriteError("Server could not be stopped due to " + exc);
            }
        }
    }
}

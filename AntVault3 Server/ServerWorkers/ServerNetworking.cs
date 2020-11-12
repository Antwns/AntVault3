﻿using System;
using System.Threading.Tasks;
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
        static bool GetUserPageMode;

        internal static string ServerStatus = null;

        internal static void StartServer()
        {
            if (SetUpEvents == false)
            {
                AntVaultServer.MessageReceived += MessageReceived;
                AntVaultServer.BytesReceived += BytesReceived;
                SetUpEvents = true;
                AuxiliaryServerWorker.WriteOK("Event callbacks hooked successfully");
            }
            AuxiliaryServerWorker.WriteInfo("Reading server status from config...");
            ServerStatus = AuxiliaryServerWorker.ReadFromConfig("Status");
            MainServerWorker.CheckDatabase();
            AuxiliaryServerWorker.WriteOK("Server status is set to " + ServerStatus);
            AuxiliaryServerWorker.WriteInfo("Checking server theme...");
            MainServerWorker.CheckServerTheme();
            AuxiliaryServerWorker.WriteInfo("Checking server login screen...");
            MainServerWorker.CheckServerLoginScreen();
            MainServerWorker.CheckUserPages();
            try
            {
                AntVaultServer.StartListening(AuxiliaryServerWorker.ReadFromConfig("IP"), Convert.ToInt32(AuxiliaryServerWorker.ReadFromConfig("Port")));
                AuxiliaryServerWorker.WriteOK("Server started successfully on " + AuxiliaryServerWorker.ReadFromConfig("IP") + ":" + AuxiliaryServerWorker.ReadFromConfig("Port"));
            }
            catch (Exception exc)
            {
                AuxiliaryServerWorker.WriteError("Server could not be started due to " + exc);
            }
        }

        private static void BytesReceived(IClientInfo Client, byte[] MessageBytes)
        {
            string MessageString = AuxiliaryServerWorker.GetStringFromBytes(MessageBytes);
            if (MessageString.Contains("�PNG") == false)
            {
                AuxiliaryServerWorker.WriteDebug(MessageString);
            }
            else
            {
                AuxiliaryServerWorker.WriteDebug("[PNG]");
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
                Task.Run(() => MainServerWorker.UpdateTheme(Client));
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
            if (MessageString.StartsWith("/GetMyPage") == true || GetUserPageMode == true)
            {
                GetUserPageMode = true;
            }
            if (GetUserPageMode == true)
            {
                GetUserPageMode = false;
                Task.Run(() => MainServerWorker.SendUserPageAsync(Client));
            }
        }

        internal static void StopServer()
        {
            try
            {
                AntVaultServer.Dispose();
                AuxiliaryServerWorker.WriteOK("Server stopped successfully");
            }
            catch (Exception exc)
            {
                AuxiliaryServerWorker.WriteError("Server could not be stopped due to " + exc);
            }
        }
    }
}

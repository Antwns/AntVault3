using System;
using WatsonTcp;

namespace AntVault3_Server.ServerWorkers
{
    class ServerNetworking
    {
        internal static WatsonTcpServer AntVaultServer = null;

        static bool SetUpEvents = false;

        internal static string ServerStatus = null;

        internal static void StartServer()
        {
            AntVaultServer = new WatsonTcpServer(AuxiliaryServerWorker.ReadFromConfig("IP"), Convert.ToInt32(AuxiliaryServerWorker.ReadFromConfig("Port")));
            if (SetUpEvents == false)
            {
                AntVaultServer.Keepalive.EnableTcpKeepAlives = true;
                AntVaultServer.Keepalive.TcpKeepAliveInterval = 1;
                AntVaultServer.Keepalive.TcpKeepAliveTime = 1;
                AntVaultServer.Settings.AcceptInvalidCertificates = true;
                AntVaultServer.Settings.StreamBufferSize = 2048;
                AntVaultServer.Settings.Logger = AuxiliaryServerWorker.WriteDebug;
                AntVaultServer.Events.ExceptionEncountered += Events_ExceptionEncountered;
                AntVaultServer.Events.MessageReceived += MainServerWorker.Events_MessageReceived;
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

        internal static void StopServer()
        {
            try
            {
                AntVaultServer.Stop();
                AuxiliaryServerWorker.WriteOK("Server stopped successfully");
            }
            catch (Exception exc)
            {
                AuxiliaryServerWorker.WriteError("Server could not be stopped due to " + exc);
            }
        }

        private static void Events_ExceptionEncountered(object sender, ExceptionEventArgs e)
        {
            AuxiliaryServerWorker.WriteError(e.Exception.ToString());
        }
    }
}

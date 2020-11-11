using System;
using WatsonTcp;

namespace AntVault3_Server.ServerWorkers
{
    class ServerNetworking
    {
        internal WatsonTcpServer AntVaultServer = null;
        
        internal ServerNetworking()
        {
            AntVaultServer = new WatsonTcpServer(AuxiliaryServerWorker.ReadFromConfig("IP"), Convert.ToInt32(AuxiliaryServerWorker.ReadFromConfig("Port")));
        }
        
        bool SetUpEvents = false;

        internal string ServerStatus = null;

        internal void StartServer()
        {
            if (SetUpEvents == false)
            {
                AntVaultServer.Events.ExceptionEncountered += Events_ExceptionEncountered;
                AntVaultServer.Keepalive.EnableTcpKeepAlives = true;
                AntVaultServer.Keepalive.TcpKeepAliveInterval = 5;
                AntVaultServer.Keepalive.TcpKeepAliveTime = 5;
                AntVaultServer.Settings.AcceptInvalidCertificates = true;
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
        internal void StopServer()
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

        private void Events_ExceptionEncountered(object sender, ExceptionEventArgs e)
        {
            AuxiliaryServerWorker.WriteError(e.Exception.ToString());
        }
    }
}

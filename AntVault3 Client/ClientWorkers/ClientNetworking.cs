using System;
using WatsonTcp;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace AntVault3_Client.ClientWorkers
{
    class ClientNetworking
    {
        internal WatsonTcpClient AntVaultClient = new WatsonTcpClient(AuxiliaryClientWorker.ReadFromConfig("IP"), Convert.ToInt32(AuxiliaryClientWorker.ReadFromConfig("Port")));


        bool HasSetupEvents = false;

        internal void Connect()
        {
            if (HasSetupEvents == false)
            {
                AntVaultClient.Settings.Logger = WriteToLog;
                AntVaultClient.Events.ExceptionEncountered += Events_ExceptionEncountered;
                AntVaultClient.Keepalive.EnableTcpKeepAlives = true;
                AntVaultClient.Keepalive.TcpKeepAliveInterval = 1;
                AntVaultClient.Keepalive.TcpKeepAliveTime = 1;
                AntVaultClient.Settings.Logger = WriteToLog;
                AntVaultClient.Events.ExceptionEncountered += Events_ExceptionEncountered;
                HasSetupEvents = true;
                Console.WriteLine("Events setup complete");
            }
            try
            {
                if (AntVaultClient.Connected == false)
                {
                    AntVaultClient.Start();
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("Could not connect to the server due to " + exc);
            }
            try
            {
                if (AntVaultClient.Connected == true)
                {
                    try
                    {
                        Task.Run(() => AntVaultClient.Send("/ServerStatus?"));
                    }
                    catch(Exception exc)
                    {
                        Console.WriteLine("Could not send message to the server due to " + exc);
                    }
                }
                else
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        WindowController.LoginPage.StatusLabel.Content = "ERROR-Server offline, try to Vault later.";
                    });
                    Thread.Sleep(1000);
                    Task.Run(() => Connect());
                }
            }
            catch
            {
                Console.WriteLine("Client could not be used");
            }
        }

        internal void Events_ExceptionEncountered(object sender, ExceptionEventArgs e)
        {
            WriteToLog("Exception:");
            WriteToLog(e.Exception.ToString());
        }

        internal void WriteToLog(string LogEntry)
        {
            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "AntVaultClient.log", LogEntry + Environment.NewLine);
        }

        internal void Disconnect()
        {
            AntVaultClient.Dispose();
        }


    }
}

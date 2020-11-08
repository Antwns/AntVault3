﻿using System;
using WatsonTcp;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace AntVault3_Client.ClientWorkers
{
    class Networking
    {
        internal static WatsonTcpClient AntVaultClient = new WatsonTcpClient(AuxiliaryClientWorker.ReadFromConfig("IP"), Convert.ToInt32(AuxiliaryClientWorker.ReadFromConfig("Port")));
        static bool HasSetupEvents = false;

        internal static void Connect()
        {
            if (HasSetupEvents == false)
            {
                AntVaultClient.Settings.Logger = WriteToLog;
                AntVaultClient.Events.ExceptionEncountered += MainClientWorker.Events_ExceptionEncountered;
                AntVaultClient.Keepalive.EnableTcpKeepAlives = true;
                AntVaultClient.Keepalive.TcpKeepAliveInterval = 1;
                AntVaultClient.Keepalive.TcpKeepAliveTime = 1;
                AntVaultClient.Settings.Logger = WriteToLog;
                AntVaultClient.Events.ExceptionEncountered += Events_ExceptionEncountered;
                AntVaultClient.Events.MessageReceived += MainClientWorker.Events_MessageReceivedAsync;
                HasSetupEvents = true;
                Console.WriteLine("Events setup complete");
            }
            try
            {
                AntVaultClient.Start();
            }
            catch (Exception exc)
            {
                Console.WriteLine("Could not connect to the server due to " + exc);
            }
            try
            {
                if (AntVaultClient.Connected == true)
                {
                    Task.Run(() => AntVaultClient.Send("/ServerStatus?"));
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

        internal static void Events_ExceptionEncountered(object sender, WatsonTcp.ExceptionEventArgs e)
        {
            WriteToLog("Json:");
            WriteToLog(e.Json);
            WriteToLog("Exception:");
            WriteToLog(e.Exception.ToString());
        }

        internal static void WriteToLog(string LogEntry)
        {
            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "AntVaultClient.log", LogEntry + Environment.NewLine);
        }

        internal static void Disconnect()
        {
            AntVaultClient.Dispose();
        }


    }
}
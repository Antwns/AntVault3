using AntVault3_Server.ServerWorkers;
using System;
using System.Threading;
using AntLib;

namespace AntVault3_Server
{
    class Program
    {
        internal static MainTools AuxiliaryServerWorker = new MainTools()
        {
            ConfigDir = MainServerWorker.ConfigDir,
            AppName = "AntVault3_Server",
        };
        static void Main(string[] args)
        {
            AuxiliaryServerWorker.WriteInfo("Welcome to the AntVault 3 server.");
            AuxiliaryServerWorker.WriteInfo("Write /Start to get started!");
            string Command = Console.ReadLine();
            while(Command.ToLower() != "/exit")
            {
                if (Command.ToLower() == "/start")
                {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    ServerNetworking.StartServer();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    Command = Console.ReadLine();
                }
                else if (Command.ToLower() == "/stop")
                {
                    ServerNetworking.StopServer();
                    Command = Console.ReadLine();
                }
                else if (Command.ToLower().StartsWith("/updatestatus"))
                {
                    Command = Command + ";";
                    string NewStatus = AuxiliaryServerWorker.GetElement(Command, " ".ToUpperInvariant(), ";");
                    MainServerWorker.UpdateStatus(NewStatus);
                    Command = Console.ReadLine();
                }
                else if (Command.ToLower().StartsWith("/aboutuser"))
                {
                    Command = Command + ";";
                    string UserToCheck = AuxiliaryServerWorker.GetElement(Command, " ".ToUpperInvariant(), ";");
                    MainServerWorker.AboutUser(UserToCheck);
                    Command = Console.ReadLine();
                }
                else
                {
                    AuxiliaryServerWorker.WriteError("Command not recognised");
                    Command = Console.ReadLine();
                }
            }
            if(Command.ToLower() == "/exit")
            {
                AuxiliaryServerWorker.WriteInfo("Application will exit in");
                AuxiliaryServerWorker.WriteInfo("5");
                Thread.Sleep(1000);
                AuxiliaryServerWorker.WriteInfo("4");
                Thread.Sleep(1000);
                AuxiliaryServerWorker.WriteInfo("3");
                Thread.Sleep(1000);
                AuxiliaryServerWorker.WriteInfo("2");
                Thread.Sleep(1000);
                AuxiliaryServerWorker.WriteInfo("1");
                Thread.Sleep(1000);
                Environment.Exit(0);
            }
        }
    }
}
